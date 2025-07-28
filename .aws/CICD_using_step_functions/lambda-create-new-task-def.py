import json
import boto3
import os

ecs_client = boto3.client('ecs')


def lambda_handler(event, context):
    print("Event:", event)
    image_uri = event['imageUri']
    cluster_name = os.environ['ECS_CLUSTER']
    container_name = os.environ['CONTAINER_NAME']
    task_family = os.environ['TASK_FAMILY']

    # Lấy task definition cũ
    old_task_def = ecs_client.describe_task_definition(
        taskDefinition=task_family
    )['taskDefinition']

    # Tạo container definition mới với image mới
    new_container_defs = old_task_def['containerDefinitions']
    for c in new_container_defs:
        if c['name'] == container_name:
            c['image'] = image_uri

    # Đăng ký task definition mới
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

    new_revision_arn = response['taskDefinition']['taskDefinitionArn']
    print("New Task Definition ARN:", new_revision_arn)

    return {
        "imageUri": image_uri,
        "newTaskDefinitionArn": new_revision_arn
    }
