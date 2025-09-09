import boto3
import os
import json

ecs_client = boto3.client('ecs')


def lambda_handler(event, context):
    print("=== Event from Step Functions ===")
    print(json.dumps(event))

    image_uri = event.get("imageUri")
    
    container_name = event.get("CONTAINER_NAME")
    task_family = event.get("TASK_FAMILY")
    if not image_uri:
        raise ValueError("Missing imageUri in input")
    if not container_name:
        raise ValueError("Missing CONTAINER_NAME in input")
    if not task_family:
        raise ValueError("Missing TASK_FAMILY in input")

    # Lấy Task Definition cũ
    old_task_response = ecs_client.describe_task_definition(
        taskDefinition=task_family)
    old_task_def = old_task_response['taskDefinition']
    old_task_def_arn = old_task_def['taskDefinitionArn']

    print(f"[INFO] Old Task Definition ARN: {old_task_def_arn}")

    # Clone container definitions và update image
    new_container_defs = old_task_def['containerDefinitions']
    for container in new_container_defs:
        if container['name'] == container_name:
            container['image'] = image_uri

    # Đăng ký Task Definition mới
    register_response = ecs_client.register_task_definition(
        family=old_task_def['family'],
        taskRoleArn=old_task_def.get('taskRoleArn'),
        executionRoleArn=old_task_def.get('executionRoleArn'),
        networkMode=old_task_def['networkMode'],
        containerDefinitions=new_container_defs,
        volumes=old_task_def.get('volumes', []),
        placementConstraints=old_task_def.get('placementConstraints', []),
        requiresCompatibilities=old_task_def['requiresCompatibilities'],
        cpu=old_task_def.get('cpu'),
        memory=old_task_def.get('memory')
    )

    new_task_def_arn = register_response['taskDefinition']['taskDefinitionArn']
    print(f"[INFO] New Task Definition ARN: {new_task_def_arn}")

    # Trả về cho Step Functions để các step tiếp theo dùng (update service / rollback)
    return {
        "status": "SUCCESS",
        "oldTaskDefinitionArn": old_task_def_arn,
        "newTaskDefinitionArn": new_task_def_arn
    }
