terraform {
  required_version = ">= 1.5.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 5.0.0"
    }
  }
  backend "s3" {
    bucket         = "bucket-for-learn-terraform-save-state"
    key            = "ws1-infra-on-terraform"
    region         = "ap-southeast-1"
    dynamodb_table = "table-for-learn-terraform-save-state-file"
  }
}

provider "aws" {
  region = var.region
}

module "networking" {
  source              = "./modules/networking"
  region              = var.region
  availability_zone_1 = var.availability_zone_1
  availability_zone_2 = var.availability_zone_2
  cidr_block          = var.cidr_block
  public_subnet_ips   = var.public_subnet_ips
  private_subnet_ips  = var.private_subnet_ips
}
module "security" {
  source = "./modules/security"
  region = var.region
  vpc_id = module.networking.vpc_id
}