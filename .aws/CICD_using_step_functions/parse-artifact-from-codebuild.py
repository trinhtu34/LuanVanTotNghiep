import boto3
import zipfile
import tempfile
import json
import os


def lambda_handler(event, context):
    print("Event from Step Functions:", event)

    output = {
        "ECS_CLUSTER": os.environ.get('ECS_CLUSTER'),
        "CONTAINER_NAME": os.environ.get('CONTAINER_NAME'),
        "TASK_FAMILY": os.environ.get('TASK_FAMILY'),
        "LISTENER_ARN": os.environ.get('LISTENER_ARN')
    }

    print("Parsed output:", output)
    return output
