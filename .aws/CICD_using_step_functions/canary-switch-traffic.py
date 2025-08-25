import boto3
import json

elbv2 = boto3.client('elbv2')


def lambda_handler(event, context):
    print("Event:", json.dumps(event))

    # Lấy listener ARN từ event thay vì biến môi trường
    listener_arn = event.get("listener_arn")

    current_prod_tg = event.get("current_prod")
    next_deploy_tg = event.get("next_deploy")

    WeightProd = int(event.get("WeightProd"))
    WeightNext = int(event.get("WeightNext"))

    if not listener_arn or not current_prod_tg or not next_deploy_tg:
        raise ValueError(
            "Missing required parameters: listener_arn, current_prod, or next_deploy")

    # Canary switch traffic
    response = elbv2.modify_listener(
        ListenerArn=listener_arn,
        DefaultActions=[
            {
                "Type": "forward",
                "ForwardConfig": {
                    "TargetGroups": [
                        {
                            "TargetGroupArn": next_deploy_tg,
                            "Weight": WeightNext
                        },
                        {
                            "TargetGroupArn": current_prod_tg,
                            "Weight": WeightProd
                        }
                    ]
                }
            }
        ]
    )

    print("Traffic switched to new target group:", next_deploy_tg)

    return {
        "status": "success",
        "listener_arn": listener_arn,
        "newProdTG": next_deploy_tg,
        "oldProdTG": current_prod_tg
    }
