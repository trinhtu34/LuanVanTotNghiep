# Note

Hiện tại chỉ deploy HTTP nên dùng PORT 80 ở cả SG và container port
Nhớ đặt CloudWatch Alarm là : blue-tg-Alarm , green-tg-Alarm . Vì AlarmName đã hardCode trong code của Step Functions
