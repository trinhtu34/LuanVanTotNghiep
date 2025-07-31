import boto3
import os
import json

ecs = boto3.client('ecs')
elbv2 = boto3.client('elbv2')


def get_current_and_next_target_group(listener_arn):
    response = elbv2.describe_listeners(ListenerArns=[listener_arn])
    actions = response['Listeners'][0]['DefaultActions']
    tg_arns = actions[0]['ForwardConfig']['TargetGroups']
    if tg_arns[0]['Weight'] == 100:
        return tg_arns[0]['TargetGroupArn'], tg_arns[1]['TargetGroupArn']
    else:
        return tg_arns[1]['TargetGroupArn'], tg_arns[0]['TargetGroupArn']


def lambda_handler(event, context):
    print("Event:", json.dumps(event))

    cluster_name = os.environ['ECS_CLUSTER']
    new_service_name = event.get("newServiceName")
    listener_arn = os.environ['ALB_LISTENER_ARN']

    # Lấy TG Prod và TG Staging
    current_prod_tg, staging_tg = get_current_and_next_target_group(
        listener_arn)

    # Switch traffic về TG Prod
    elbv2.modify_listener(
        ListenerArn=listener_arn,
        DefaultActions=[{
            "Type": "forward",
            "ForwardConfig": {
                "TargetGroups": [{"TargetGroupArn": current_prod_tg, "Weight": 100}]
            }
        }]
    )
    print(f"✅ Switched traffic back to Prod TG: {current_prod_tg}")

    # Scale service mới xuống 0
    ecs.update_service(
        cluster=cluster_name,
        service=new_service_name,
        desiredCount=0
    )
    print(f"✅ Scaled down failed service: {new_service_name}")

    return {
        "status": "rollback_success",
        "prodTargetGroup": current_prod_tg,
        "scaledDownService": new_service_name
    }
