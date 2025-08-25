import boto3

elbv2 = boto3.client('elbv2')


def lambda_handler(event, context):
    listener_arn = event.get("listener_arn")
    if not listener_arn:
        raise ValueError("Missing listener_arn in event")

    response = elbv2.describe_listeners(ListenerArns=[listener_arn])

    actions = response['Listeners'][0]['DefaultActions']

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
