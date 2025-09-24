provider "aws" {
  region = var.region
}

resource "aws_vpc" "my_vpc" {
  cidr_block           = var.cidr_block
  enable_dns_hostnames = true
  tags = {
    Name = "the-first-workshop-project-vpc"
  }
}

resource "aws_internet_gateway" "my_igw" {
  vpc_id = aws_vpc.my_vpc.id
}


# Create public subnets
resource "aws_subnet" "public_subnet_1" {
  vpc_id            = aws_vpc.my_vpc.id
  cidr_block        = var.public_subnet_ips[0]
  availability_zone = var.availability_zone_1
  tags = {
    Name = "public-subnet-1"
  }
}

resource "aws_subnet" "public_subnet_2" {
  vpc_id            = aws_vpc.my_vpc.id
  cidr_block        = var.public_subnet_ips[1]
  availability_zone = var.availability_zone_2
  tags = {
    Name = "public-subnet-2"
  }
}
# Create private subnets
resource "aws_subnet" "private_subnet_1" {
  vpc_id            = aws_vpc.my_vpc.id
  cidr_block        = var.private_subnet_ips[0]
  availability_zone = var.availability_zone_1
  tags = {
    Name = "private-subnet-1"
  }
}

resource "aws_subnet" "private_subnet_2" {
  vpc_id            = aws_vpc.my_vpc.id
  cidr_block        = var.private_subnet_ips[1]
  availability_zone = var.availability_zone_2
  tags = {
    Name = "private-subnet-2"
  }
}

# Create VPC endpoint for s3
resource "aws_vpc_endpoint" "s3_endpoint" {
  vpc_id            = aws_vpc.my_vpc.id
  service_name      = "com.amazonaws.${var.region}.s3"
  vpc_endpoint_type = "Gateway"
  route_table_ids   = [aws_route_table.public_rt.id]
  tags = {
    Name = "s3-vpc-endpoint"
  }
}
# Create VPC endpoint for ECR
resource "aws_vpc_endpoint" "ecr_endpoint" {
  vpc_id              = aws_vpc.my_vpc.id
  service_name        = "com.amazonaws.${var.region}.ecr.dkr"
  vpc_endpoint_type   = "Interface"
  security_group_ids  = [aws_security_group.ecr_sg.id]
  private_dns_enabled = true
  tags = {
    Name = "ecr-vpc-endpoint"
  }
}
resource "aws_vpc_endpoint" "ecr_api_endpoint" {
  vpc_id              = aws_vpc.my_vpc.id
  service_name        = "com.amazonaws.${var.region}.ecr.api"
  vpc_endpoint_type   = "Interface"
  security_group_ids  = [aws_security_group.ecr_sg.id]
  private_dns_enabled = true
  tags = {
    Name = "ecr-api-vpc-endpoint"
  }
}

#Create route table for public subnets
resource "aws_route_table" "public_rt" {
  vpc_id = aws_vpc.my_vpc.id
  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.my_igw.id
  }
  tags = {
    Name = "public-route-table"
  }
}

# Create route table for private subnets
resource "aws_route_table" "private_rt" {
  vpc_id = aws_vpc.my_vpc.id
  tags = {
    Name = "private-route-table"
  }
}

# Associate route tables with subnets
resource "aws_route_table_association" "public_rt_assoc_1" {
  subnet_id      = aws_subnet.public_subnet_1.id
  route_table_id = aws_route_table.public_rt.id
}

resource "aws_route_table_association" "public_rt_assoc_2" {
  subnet_id      = aws_subnet.public_subnet_2.id
  route_table_id = aws_route_table.public_rt.id
}

# Associate route tables with private subnets
resource "aws_route_table_association" "private_rt_assoc_1" {
  subnet_id      = aws_subnet.private_subnet_1.id
  route_table_id = aws_route_table.private_rt.id
}
resource "aws_route_table_association" "private_rt_assoc_2" {
  subnet_id      = aws_subnet.private_subnet_2.id
  route_table_id = aws_route_table.private_rt.id
}

