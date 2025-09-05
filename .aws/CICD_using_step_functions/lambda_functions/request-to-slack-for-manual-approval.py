import os
import json
import urllib.request

SLACK_BOT_TOKEN = os.environ["SLACK_BOT_TOKEN"]
CHANNEL_ID = os.environ["SLACK_CHANNEL_ID"]

def lambda_handler(event, context):
    task_token = event["TaskToken"]

    message = {
        "channel": CHANNEL_ID,
        "text": "Có một request switch traffic. Approve hay Reject?",
        "blocks": [
            {
                "type": "section",
                "text": {
                    "type": "mrkdwn",
                    "text": "Có một request switch traffic. Approve hay Reject?"
                }
            },
            {
                "type": "actions",
                "elements": [
                    {
                        "type": "button",
                        "text": {"type": "plain_text", "text": "Approve ✅"},
                        "style": "primary",
                        "value": json.dumps({"action": "approve", "taskToken": task_token})
                    },
                    {
                        "type": "button",
                        "text": {"type": "plain_text", "text": "Reject ❌"},
                        "style": "danger",
                        "value": json.dumps({"action": "reject", "taskToken": task_token})
                    }
                ]
            }
        ]
    }

    req = urllib.request.Request(
        "https://slack.com/api/chat.postMessage",
        data=json.dumps(message).encode("utf-8"),
        headers={
            "Content-Type": "application/json; charset=utf-8",
            "Authorization": f"Bearer {SLACK_BOT_TOKEN}"
        }
    )

    with urllib.request.urlopen(req) as resp:
        resp_body = resp.read()
        print(resp_body)

    return {"status": "Message sent to Slack"}
