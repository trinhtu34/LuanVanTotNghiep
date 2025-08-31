import json
import boto3

sfn = boto3.client("stepfunctions")

def lambda_handler(event, context):
    body = event.get("body")
    payload = json.loads(body["payload"])
    action = payload["actions"][0]["value"]
    data = json.loads(action)

    if data["action"] not in ["approve", "reject"]:
        return {
            "statusCode": 400,
            "body": "Invalid action"
        }

    task_token = data["taskToken"]
    decision = data["action"]

    if decision == "approve":
        sfn.send_task_success(
            taskToken=task_token,
            output=json.dumps({"approved": True})
        )
    else:
        sfn.send_task_success(
            taskToken=task_token,
            output=json.dumps({"approved": False})
        )

    return {
        "statusCode": 200,
        "body": "OK"
    }
