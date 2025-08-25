import boto3
import json
import os

ecs = boto3.client('ecs')


def lambda_handler(event, context):
    print("Event received:", json.dumps(event))

    # Lấy task definition ARN mới
    new_task_def_arn = event.get("newTaskDefinitionArn")
    if not new_task_def_arn:
        raise ValueError("Missing newTaskDefinitionArn in event input")

    # Lấy cluster và service base name (frontend hoặc backend)
    cluster_name = event.get("ECS_CLUSTER")
    base_service = event.get("BASE_SERVICE")

    if not cluster_name or not base_service:
        raise ValueError("Missing ECS_CLUSTER or BASE_SERVICE")

    # Xác định tên service Blue/Green
    blue_service = f"{base_service}-blue"
    green_service = f"{base_service}-green"

    # Lấy trạng thái service Blue/Green
    blue_desc = ecs.describe_services(cluster=cluster_name, services=[
                                      blue_service])["services"][0]
    green_desc = ecs.describe_services(cluster=cluster_name, services=[
                                       green_service])["services"][0]

    # Chọn target service (deploy vào service còn lại)
    if blue_desc["desiredCount"] > 0:
        active_service = blue_service
        target_service = green_service
        active_desc = blue_desc
        target_desc = green_desc
    else:
        active_service = green_service
        target_service = blue_service
        active_desc = green_desc
        target_desc = blue_desc

    print(f"Active service: {active_service}, Deploying to: {target_service}")

    # Update target service với Task Definition mới
    response = ecs.update_service(
        cluster=cluster_name,
        service=target_service,
        taskDefinition=new_task_def_arn,
        desiredCount=active_desc["desiredCount"],
        forceNewDeployment=True
    )

    print("ECS Service update response:", json.dumps(response, default=str))

    # Lấy Target Group ARN của service vừa scale out (nếu có)
    target_group_arn = None
    if "loadBalancers" in target_desc and len(target_desc["loadBalancers"]) > 0:
        target_group_arn = target_desc["loadBalancers"][0].get(
            "targetGroupArn")

    return {
        "status": "SUCCESS",
        "activeService": active_service,
        "updatedService": target_service,
        "taskDefinitionArn": new_task_def_arn,
        "targetGroupArn": target_group_arn
    }
