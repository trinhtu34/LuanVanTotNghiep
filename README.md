## Bối cảnh 
 
Trong bối cảnh các hệ thống phần mềm cho hệ thống website đặt bàn và đặt đồ ăn online cho nhiều nhà hàng , lượng khách hàng truy cập trang web vào giờ cao điểm khoảng từ 80 - 150 khách truy cập đồng thời , lượng khách có thể tăng giảm theo thời gian . Hiện nay cần một khả năng triển khai nhanh chóng , ổn định , ít rủi ro . Thì chiến lược Blue / Green Deployment trở thành một trong những mô hình quan trọng nhằm đảm bảo : 

- **High availability** : Nhằm đảm bảo hệ thống luôn sẵn sàng phục vụ traffic 
- **Minimal downtime** : Nhằm giảm thiểu gián đoạn tới người dùng cuối khi code mới release
- **Operational safety** : Giúp hạn chế lỗi khi phát hành phiên bản mới 

Bằng cách kết hợp với việc sử dụng các AWS Service thì giải pháp này không chỉ giúp tối ưu chi phí vận hành , giảm công sức làm thủ công quá trình release mà còn tăng độ an toàn cho quá trình release code mới lên Production 

## Mục tiêu dự án

Dự án này tập trung vào việc xây dựng giải pháp Blue / Green Deployment Automation và Canary Switch traffic . Có monitoring dựa trên CloudWatch Alarm và Manual Approval dựa trên cơ sở hạ tầng AWS, với các mục tiêu chính như sau : 

- **Automation deployment** : Tích hợp CICD Pipline để tự động Build và Deploy từ Github vào ECS
- **Health validation** : Thực hiện health check khi deploy code mới , điều này nhằm đảm bảo chương trình sau khi triển khai hoạt động ổn định 
- **Traffic management** : Sử dụng phương pháp Canary switch traffic để chuyển đổi traffic từ môi trường cũ sang môi trường mới một cách từ từ , không chuyển tất cả traffic trong một lần , tránh việc chương trình hoạt động không tốt làm ảnh hưởng tới tất cả người dùng 
- **Rollback mechanisms** : Tự động rollback trong trường hợp Health check thất bại 
- **Monitoring integration** : Kết hợp với CloudWatch để giám sát trong quá trình switch traffic . Nếu CloudWatch Alarm vào trạng thái Alert thì tự động rollback ngay lập tức 

## Dịch vụ được sử dụng 

Hệ thống sử dụng cơ sở hạ tầng AWS với cách dịch vụ như : 

- Compute : ECS , EC2
- Database : RDS
- Networking : VPC
- CI/CD : CodeBuild , CodePipline
- Container Management : ECR 
- Orchestration and Automation : Step Functions , Lambda 
- Monitoring and Notification : CloudWatch , SNS 
- Storage : S3
- Secrets Management: Secrets Manager 


## Kiến trúc hệ thống
**Network Design**
![NetworkDesign](/images/WS1_NetworkDesign.png)

**Security Design**
![SecurityDesign](/images/WS1-SecurityDesign.png)

**Service and Resource**
![ServiceAndResource](/images/WS1_Service_And_Resource.png)

**Frontend CICD Design**
![FrontendCICD](/images/WS1_CICD_Design.png)

**Backend CICD Design**
![BackendCICD](/images/WS1_CICD_Backend.png)

**CI/CD Grap**
![BackendCICD](/images/stepfunctions_graph.png)

## Quy trình hoạt động của Pipline

1. Phát hiện thay đổi trong mã nguồn từ Github Repository 
    - Khi có thay đổi ( Commit / Merge ) vào nhánh **master** của Github Repository thì quy trình CI/CD sẽ được kích hoạt thông qua AWS CodePipline .
2. Build và Push images vào ECR
    - CodeBuild trong CodePipline sẽ thực hiện checkout code từ Github .
    - CodeBuild sẽ sử dụng Docker cho việc build , tài khoản và mật khẩu của docker được lưu trong **Secrets Manager** , và thực hiện Build theo hướng dẫn trong file Buildspec.yml mà CodeBuild được cấp .
    - Sau khi Build thành công thì images sẽ được Push vào ECR Repository . Ngoài ra còn một file là imageDetail.json cũng được tạo trong quá trình build , trong file này sẽ lưu các thông tin để làm input cho Step Functions như imageUri , BASE_SERVICE_NAME , rule_arn , file này sẽ lưu trong S3 Bucket .
3. Khởi động Step Functions 
    - Sau khi push images thành công , CodePipline sẽ invoke _Step Functions_ .
    - Input của Step Functions sẽ là 1 file imageDetail.json được lưu trong S3 bucket , trong đó bao gồm các thông số được truyền từ Buildspec.yml như : imageUri , BASE_SERVICE_NAME , rule_arn . Những thông số này sẽ là input cho các **Lambda functions** trong quá trình deployment .
4. Triển khai ECS Task Definition mới và deploy vào ECS Service 
    - Lambda trong Step Functions sẽ tạo _Task definition_ mới từ images vừa được push vào ECR .
    - Tiếp theo , một Lambda khác sẽ thực hiện việc Scale out một ECS Service Blue hoặc Green tùy tình trạng hiện tại bằng _Task definition_ mới vừa tạo ở trên .
5. Xác định Target Group cần Triển khai 
    - Một Lambda sẽ thực hiện công việc xác định Target Group tương ứng với Rule đang triển khai , do Frontend và Backend được triển khai trên cùng một Application Load Balancer , và được phân biệt bằng Rule . 
6. Target Group Health check
    - Lambda sẽ thực hiện kiểm tra trạng thái _Health Check_ của target group của ECS Service vừa được Scale out .
    - Nếu _unhealthy_ thì sẽ _Rollback_ toàn bộ quá trình deploy trước đó về trạng thái ban đầu .
    - Nếu _healthy_ thì tiếp tục quá trình Canary Switch traffic .
7. Canary Switch traffic 
    - Quá trình Switch traffic diễn ra theo nhiều bước , traffic sẽ được chuyển dần dần từ 25% , 50% , 75% và cuối cùng là 100% như sau : 
        - Chuyển 25% traffic đầu tiên : Step Functions sẽ gọi một Lambda thực hiện việc switch 25% traffic sang ECS Service có Task Definitions mới vừa được Scale out . Tiếp theo Step Functions gọi CloudWatch describleAlarm để kiểm tra Alarm của Target Group , nếu cách Alarm này có StateValue = OK thì sẽ tiếp tục quá trình switch traffic cho tới 100% .
        - Nếu các Alarm có StateValue = Alert thì thực hiện Rollback toàn bộ về trạng thái ban đầu .
8. Manual Approval 
    - Sau khi switch 100% traffic sang Service mới , Step Functions sẽ đi đến bước Mannual Approval .
    - Step Functions sẽ gọi Lambda có sử dụng .waitForTaskToken để gửi thông báo qua Slack kèm 2 nút lựa chọn : Approve hoặc Reject .
    - Sau khi người dùng bên Slack chọn 1 trong 2 lựa chọn là Approve hoặc Reject thì Slack sẽ call tới 1 API gửi về TaskToken và lựa chọn của người dùng bên Slack .
    - Nếu thông tin gửi về là Approve thì hoàn tất việc switch traffic và Scale in ECS Service cũ . Nếu Reject thì Rollback toàn bộ hệ thống bao gồm traffic , ECS Service .

Quy trình đảm bảo việc triển khai an toàn , có khả năng tự động Rollback chính xác , và cho phép Manual Approval 

## Phân tích chi phí của toàn bộ project

#### Tài nguyên sử dụng trong bài lab này

**Vì lý do để demo cho bài lab nên các instance type tôi sẽ dùng ở mức nhỏ nhất để tối ưu chi phí .**

**Còn đối với khi triển khai trên production thì nên dùng Instance type cho RDS là t4g.large ,t4g.xlarge , t4g.2xlarge . Storage type nên chọn GP3 và Allocated Storage là 30GB , Maximum storage threshold là 200GB**

EC2 : 
- Instance type : t3.micro

RDS :
- Instance type : db.t3.micro
- Storage type : gp2
- Allocated storage : 30GB
- Provisioned IOPS : 3000
- Storage throughput : 125
- Maximum storage threshold : 100

ECS : 
- Frontend fargate : 0.5vCPU , 1GB Memory , min - 0 , desired - 2 , max - 4
- Backend fargate : 1vCPU , 2GB Memory , min - 0 , desired - 2 , max - 6

## Chi phí khi duy trì lâu dài 


## Lợi ích đem lại 

- Giảm downtime khi phát hành phiên bản mới .
- Tăng độ an toàn nhờ health check và rollback tự động .
- Tối ưu chi phí và công sức bỏ ra khi vận hành nhờ quá trình tự động hóa .
- Khả năng mở rộng linh hoạt cho Pipline khi sử dụng Lambda và Step Functions .
