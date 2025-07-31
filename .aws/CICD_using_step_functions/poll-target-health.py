import boto3
import os
import time

elbv2 = boto3.client('elbv2')


def lambda_handler(event, context):
    target_group_arn = os.environ['TARGET_GROUP_ARN']
    max_retries = int(os.environ.get('MAX_RETRIES', 10))
    delay = int(os.environ.get('DELAY', 15))

    for attempt in range(max_retries):
        res = elbv2.describe_target_health(TargetGroupArn=target_group_arn)
        healthy_count = sum(1 for t in res['TargetHealthDescriptions']
                            if t['TargetHealth']['State'] == 'healthy')
        total = len(res['TargetHealthDescriptions'])

        print(f"[{attempt+1}] Healthy {healthy_count}/{total}")

        if total > 0 and healthy_count == total:
            return {"isHealthy": True}

        time.sleep(delay)

    return {"isHealthy": False}
