import boto3
import json

elbv2 = boto3.client("elbv2")

def lambda_handler(event, context):
    print("Event:", json.dumps(event))

    listener_arn = event.get("listener_arn")
    rule_arn = event.get("rule_arn")
    current_prod_tg = event.get("cu rrent_prod")
    next_deploy_tg = event.get("next_deploy")

    WeightProd = int(event.get("WeightProd"))
    WeightNext = int(event.get("WeightNext"))

    if not current_prod_tg or not next_deploy_tg:
        raise ValueError("Missing required parameters: current_prod or next_deploy")

    if not listener_arn and not rule_arn:
        raise ValueError("Missing listener_arn or rule_arn in event")

    if rule_arn:
        response = elbv2.modify_rule(
            RuleArn=rule_arn,
            Actions=[
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
        print(f"Traffic switched for rule {rule_arn} → {next_deploy_tg}")
    else:
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
        print(f"Traffic switched for listener {listener_arn} → {next_deploy_tg}")

    return {
        "status": "success",
        "rule_arn": rule_arn,
        "listener_arn": listener_arn,
        "newProdTG": next_deploy_tg,
        "oldProdTG": current_prod_tg
    }
