import boto3
import zipfile
import tempfile
import json
import os

s3 = boto3.client('s3')
sfn = boto3.client('stepfunctions')
codepipeline = boto3.client('codepipeline')

def lambda_handler(event, context):
    print("Event from CodePipeline:", json.dumps(event))

    job_id = event['CodePipeline.job']['id']

    try:
        # Lấy artifact
        job_data = event['CodePipeline.job']['data']
        artifact = job_data['inputArtifacts'][0]
        bucket = artifact['location']['s3Location']['bucketName']
        key = artifact['location']['s3Location']['objectKey']

        with tempfile.NamedTemporaryFile() as tmp_file:
            s3.download_file(bucket, key, tmp_file.name)
            with zipfile.ZipFile(tmp_file.name, 'r') as zip_ref:
                zip_ref.extractall('/tmp/artifact')

        # Đọc imageDetail.json
        with open('/tmp/artifact/imageDetail.json', 'r') as f:
            image_data = json.load(f)

        image_uri = image_data['imageUri']
        base_service = image_data['BASE_SERVICE']

        # Invoke Step Function
        step_function_arn = os.environ['STEP_FUNCTION_ARN']
        cluster_name = os.environ.get('ECS_CLUSTER')
        container_name = os.environ.get('CONTAINER_NAME')
        task_family = os.environ.get('TASK_FAMILY')
        listener_arn = os.environ.get('LISTENER_ARN')

        sfn.start_execution(
            stateMachineArn=step_function_arn,
            input=json.dumps({
                "imageUri": image_uri,
                "BASE_SERVICE": base_service,
                "ECS_CLUSTER": cluster_name,
                "CONTAINER_NAME": container_name,
                "TASK_FAMILY": task_family,
                "LISTENER_ARN": listener_arn
            })
        )

        # Báo thành công cho CodePipeline
        codepipeline.put_job_success_result(jobId=job_id)

    except Exception as e:
        print("Error:", e)
        codepipeline.put_job_failure_result(
            jobId=job_id,
            failureDetails={
                'message': str(e),
                'type': 'JobFailed'
            }
        )
