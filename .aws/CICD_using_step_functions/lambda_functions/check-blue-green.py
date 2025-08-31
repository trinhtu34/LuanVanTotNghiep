import boto3

elbv2 = boto3.client('elbv2')

def lambda_handler(event, context):
    listener_arn = event.get("listener_arn")
    rule_arn = event.get("rule_arn")  # 👈 truyền thẳng rule arn

    if not listener_arn:
        raise ValueError("Missing listener_arn in event")
    if not rule_arn:
        raise ValueError("Missing rule_arn in event")

    # lấy rule cụ thể
    rule = elbv2.describe_rules(ListenerArn=listener_arn, RuleArns=[rule_arn])["Rules"][0]

    actions = rule['Actions']
    tg_arns = actions[0]['ForwardConfig'].get('TargetGroups', [])

    if len(tg_arns) != 2:
        raise Exception("Expected exactly 2 target groups for blue/green strategy")

    if tg_arns[0]['Weight'] == 100:
        current_prod = tg_arns[0]['TargetGroupArn']
        next_deploy = tg_arns[1]['TargetGroupArn']
    else:
        current_prod = tg_arns[1]['TargetGroupArn']
        next_deploy = tg_arns[0]['TargetGroupArn']

    return {
        "current_prod": current_prod,
        "next_deploy": next_deploy
    }
