# Vấn đề cần chú ý khi tạo Resource

## ECS  

**_container name_** : thay đổi theo backend - frontend

**_Task family_** : thay đổi theo backend - frontend

Nhớ đặt **Task Role** là : ```task-role-luan-van```

**Lưu ý : Việc tạo container_name khi tạo task_family ảnh hưởng trực tiếp đến việc hoạt động của CodeBuild , vì ECS_Service_Name , Container_name đã được hardcode trong buildspec.yml của CodeBuild . File Buildspec.yml cũng có trong root folder của từng project .**

## CloudWatch

**_Alarm Name_** : thay đổi theo tên của target group , hiện tại phải đặt như sau : frontend-target-group-blue-Alarm , frontend-target-group-green-Alarm

**_Alarm Name_** : thay đổi theo tên của target group , hiện tại phải đặt như sau : backend-target-group-blue-Alarm , backend-target-group-green-Alarm

_Warning_ : Phải có ký tự ** - Alarm** ở cuối tên của Alarm , tên alarm phải là : TargetGroupName - Alarm --> vì đã hardcode trong step functions rồi

_Đây là mức đảm bảo an toàn_

- HTTPCode_Target_5XX_Count : < 1% (tùy SLA) trong 5 phút
- HTTPCode_Target_4XX_Count : tăng đột biến > baseline (dùng anomaly detection)
- TargetResponseTime : p95 < 500ms (hoặc theo SLO app)
- HealthyHostCount : ≥ 1 (service không bị down toàn bộ)

## API Gateway

Sẽ sử dụng 1 lambda funtions + API Gateway để làm API trả về Approve Information từ Slack + taskToken của _.waitfortasktoken_

Tại phần _Integration request_ chỉnh _Lambda proxy integration_ thành True : nếu không thì Slack call tới API sẽ không có **_JSON body_** , từ đó dẫn tới không có thông tin về **Action** của Dev , và token đã gửi đi của _.waitfortasktoken_

## Step Functions

**Lưu ý** : 
- Đảm bảo Step Functions Role có đủ Permission đối với các service sử dụng như SNS , Lambda , CloudWatch 
- "AlarmNames.$": "States.Array($.alarm.AlarmName)" lại state _describleAlarm_ phải truyền dữ liệu theo dạng array vào **Lỗi này xảy ra khi dùng JSONPath**
- Trong state Switch traffic nếu là Frontend thì không cần truyền vào Rule . Nếu Switch traffic backend thì Truyền vào Rule ARN , **Rule ARN** của ALB Listener của Backend được Hardcode trong buildspec.yml của CodeBuild 

## Lambda

- Trong quá trình làm khả năng rất cao là Step Functions bị False do Lambda thiếu Permission , vì vậy cho cẩn thận thì nếu lambda nào cần dùng service nào thì gắn FullAccess cho Lambda . Điều này chỉ nên làm ở môi trường Dev , Test , còn Production thì cần Permission gì thì gắn cho cái đó thôi , không gắn thừa . 
- Nhớ điền Environment Variable trong 1 số Lambda 





# Tài liệu tham khảo

## Slack 
### Tạo slack App

https://api.slack.com/legacy/custom-integrations#migrating_to_apps

https://api.slack.com/quickstart

https://api.slack.com/apps

### Cấu hình Manual Approval ở Slack

Webhook → chỉ gửi plain text --> notify vào slack , không callback được. --> Không dùng 

Bot Token + chat.postMessage → gửi Block Kit message có nút, khi bấm Slack sẽ gọi về Request URL (API Gateway) mà bạn đã bật ở Interactivity.

Lấy **SLACK_BOT_TOKEN** tại OAuth và Permission và **CHANNEL_ID** từ đường link khi vào App của Slack 


## Lưu ý trong khi viết step function theo dạng JSONPath

"AlarmNames.$": "States.Array($.alarm.AlarmName)" lại state _describleAlarm_ phải truyền dữ liệu theo dạng array vào 

