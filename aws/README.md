# AWS Deployment Configuration

## Architecture

```
Internet → ALB → ECS (C# API) → HTTP → ECS (Python GEPA)
					 ↓
				 DynamoDB (Job State)
					 ↓
			  CloudWatch Logs
```

## Services

### 1. C# .NET API (ECS Fargate)
- **Service**: gepa-net-api
- **Container**: Runs on Fargate
- **Port**: 8080
- **Resources**: 0.5 vCPU, 1GB RAM
- **Auto-scaling**: 1-10 tasks based on CPU/Memory

### 2. Python GEPA Wrapper (ECS Fargate)
- **Service**: gepa-wrapper
- **Container**: Runs on Fargate  
- **Port**: 8000
- **Resources**: 2 vCPU, 4GB RAM (GEPA is compute-intensive)
- **Auto-scaling**: 1-5 tasks

### 3. DynamoDB Table
- **Name**: gepa-jobs
- **Partition Key**: job_id (String)
- **On-Demand billing**
- **TTL**: 7 days for completed jobs

### 4. Application Load Balancer
- **Target Group 1**: C# API (port 8080)
- **Target Group 2**: Python Wrapper (port 8000) - Internal only
- **Health checks**: /health endpoint

## Setup Instructions

### Prerequisites
```bash
# Install AWS CLI
# Install Terraform or CDK
# Configure AWS credentials
aws configure
```

### 1. Build and Push Docker Images

```bash
# Login to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com

# Create ECR repositories
aws ecr create-repository --repository-name gepa-net-api
aws ecr create-repository --repository-name gepa-wrapper

# Build and push .NET API
docker build -t gepa-net-api:latest .
docker tag gepa-net-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/gepa-net-api:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/gepa-net-api:latest

# Build and push Python wrapper
docker build -t gepa-wrapper:latest ./python-wrapper
docker tag gepa-wrapper:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/gepa-wrapper:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/gepa-wrapper:latest
```

### 2. Deploy Infrastructure

Option A: Using CloudFormation template (see `aws-cloudformation.yml`)
```bash
aws cloudformation create-stack \
  --stack-name gepa-net-stack \
  --template-body file://aws/aws-cloudformation.yml \
  --parameters ParameterKey=OpenAIApiKey,ParameterValue=sk-... \
  --capabilities CAPABILITY_IAM
```

Option B: Using Terraform (see `aws/terraform/`)
```bash
cd aws/terraform
terraform init
terraform plan
terraform apply
```

### 3. Configure Environment Variables

Update ECS task definitions with:
- `OPENAI_API_KEY` (from Secrets Manager)
- `Gepa__ServiceUrl` - Internal Python service URL
- `Gepa__CallbackBaseUrl` - External C# API URL

## Cost Estimates (Monthly)

Assuming moderate usage (100 optimizations/day):

| Service | Cost |
|---------|------|
| ECS Fargate (C# API) | ~$15-30 |
| ECS Fargate (Python) | ~$60-120 |
| Application Load Balancer | ~$20 |
| DynamoDB (on-demand) | ~$5 |
| CloudWatch Logs | ~$5 |
| Data Transfer | ~$10 |
| **Total** | **~$115-190/month** |

## Monitoring

### CloudWatch Dashboards
- Request rate and latency
- Optimization success/failure rate
- GEPA processing time
- Container CPU/Memory usage

### Alarms
- High error rate (>5%)
- Long optimization times (>30 min)
- Container health check failures

## Security

### IAM Roles
- **API Task Role**: DynamoDB read/write, CloudWatch logs
- **Worker Task Role**: DynamoDB read/write, Secrets Manager read, CloudWatch logs

### Secrets
- Store OpenAI API key in AWS Secrets Manager
- Reference in ECS task definitions

### Network
- Private subnets for ECS tasks
- Security groups restricting internal communication
- ALB in public subnets with HTTPS

## Scaling Strategy

### C# API
- Target CPU: 70%
- Min: 1, Max: 10 tasks
- Scale-up: +1 task per 30s
- Scale-down: -1 task per 5 min

### Python GEPA Worker
- Target CPU: 80%
- Min: 1, Max: 5 tasks
- Scale-up: +1 task per 60s (slower due to cold start)
- Scale-down: -1 task per 10 min

## Deployment Pipeline

Recommended: GitHub Actions or AWS CodePipeline

```yaml
# .github/workflows/deploy.yml
name: Deploy to AWS
on:
  push:
	branches: [main]

jobs:
  deploy:
	runs-on: ubuntu-latest
	steps:
	  - uses: actions/checkout@v3
	  - name: Configure AWS credentials
		uses: aws-actions/configure-aws-credentials@v2
		with:
		  aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
		  aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
		  aws-region: us-east-1

	  - name: Login to ECR
		uses: aws-actions/amazon-ecr-login@v1

	  - name: Build and push images
		run: |
		  docker build -t $ECR_REGISTRY/gepa-net-api:$GITHUB_SHA .
		  docker push $ECR_REGISTRY/gepa-net-api:$GITHUB_SHA

	  - name: Deploy to ECS
		run: |
		  aws ecs update-service --cluster gepa-cluster \
			--service gepa-net-api \
			--force-new-deployment
```
