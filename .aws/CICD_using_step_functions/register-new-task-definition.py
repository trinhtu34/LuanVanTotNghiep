import boto3
import os
import json

ecs_client = boto3.client('ecs')


def lambda_handler(event, context):
    print("Event from Step Functions:", json.dumps(event))

    # Lấy image URI từ input Step Function
    image_uri = event.get("imageUri")
    if not image_uri:
        raise ValueError("Missing imageUri in input")

    cluster_name = os.environ['ECS_CLUSTER']
    container_name = os.environ['CONTAINER_NAME']
    task_family = os.environ['TASK_FAMILY']

    # Lấy Task Definition cũ
    old_task_def = ecs_client.describe_task_definition(
        taskDefinition=task_family
    )['taskDefinition']

    # Clone container definitions và update image
    new_container_defs = old_task_def['containerDefinitions']
    for container in new_container_defs:
        if container['name'] == container_name:
            container['image'] = image_uri

    # Đăng ký Task Definition mới
    response = ecs_client.register_task_definition(
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

    new_task_def_arn = response['taskDefinition']['taskDefinitionArn']
    print("New Task Definition ARN:", new_task_def_arn)

    return {
        "status": "SUCCESS",
        "imageUri": image_uri,
        "newTaskDefinitionArn": new_task_def_arn
    }
