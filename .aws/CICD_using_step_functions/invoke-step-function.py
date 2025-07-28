import boto3
import zipfile
import tempfile
import json
import os

s3 = boto3.client('s3')
sfn = boto3.client('stepfunctions')


def lambda_handler(event, context):
    print("Event from CodePipeline:", json.dumps(event))

    # Lấy thông tin artifact từ event
    job_data = event['CodePipeline.job']['data']
    artifact = job_data['inputArtifacts'][0]
    bucket = artifact['location']['s3Location']['bucketName']
    key = artifact['location']['s3Location']['objectKey']

    # Download artifact zip về /tmp
    with tempfile.NamedTemporaryFile() as tmp_file:
        s3.download_file(bucket, key, tmp_file.name)

        # Giải nén
        with zipfile.ZipFile(tmp_file.name, 'r') as zip_ref:
            zip_ref.extractall('/tmp/artifact')

    # Đọc imageDetail.json
    with open('/tmp/artifact/imageDetail.json', 'r') as f:
        image_data = json.load(f)

    image_uri = image_data['imageUri']
    print("Image URI:", image_uri)

    # Invoke Step Function
    step_function_arn = os.environ['STEP_FUNCTION_ARN']
    response = sfn.start_execution(
        stateMachineArn=step_function_arn,
        input=json.dumps({"imageUri": image_uri})
    )

    print("Step Function started:", response)
    return response
