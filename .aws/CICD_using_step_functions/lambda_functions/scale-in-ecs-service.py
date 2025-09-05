import boto3
import os
import json

ecs = boto3.client('ecs')


def lambda_handler(event, context):
    print("Event:", json.dumps(event))

    cluster_name = event.get("ECS_CLUSTER")
    old_service_name = event.get("oldServiceName")

    if not old_service_name:
        raise ValueError("Missing oldServiceName in event")

    # Scale service cũ xuống 0 task
    response = ecs.update_service(
        cluster=cluster_name,
        service=old_service_name,
        desiredCount=0
    )

    print(f"Scaled down old ECS service: {old_service_name}")

    return {
        "status": "scale in success",
        "service": old_service_name,
        "desiredCount": 0
    }
