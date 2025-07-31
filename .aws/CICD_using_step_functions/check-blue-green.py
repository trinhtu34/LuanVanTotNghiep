import boto3

elbv2 = boto3.client('elbv2')


def get_current_and_next_target_group(listener_arn):
    response = elbv2.describe_listeners(ListenerArns=[listener_arn])

    actions = response['Listeners'][0]['DefaultActions']

    tg_arns = actions[0]['ForwardConfig']['TargetGroups']

    if tg_arns[0]['Weight'] == 100:
        current_prod = tg_arns[0]['TargetGroupArn']
        next_deploy = tg_arns[1]['TargetGroupArn']
    else:
        current_prod = tg_arns[1]['TargetGroupArn']
        next_deploy = tg_arns[0]['TargetGroupArn']

    return current_prod, next_deploy
