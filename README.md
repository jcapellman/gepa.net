# GEPA.NET

A .NET wrapper for the [GEPA (Genetic-Pareto) AI optimization framework](https://github.com/gepa-ai/gepa).

## Overview

GEPA.NET provides a production-ready REST API service for triggering GEPA prompt optimizations. It includes:

- **C# .NET 10 API** - REST endpoints for prompt management
- **Python GEPA Wrapper** - FastAPI service that wraps the GEPA library
- **AWS Infrastructure** - CloudFormation templates for ECS deployment
- **Docker Support** - Full containerization for local dev and production

## Architecture

```
User → C# API (Port 8080) → Python GEPA Service (Port 8000) → GEPA Library
         ↓
     DynamoDB (Job State)
```

### Why Wrapper Instead of Port?

We chose to wrap the Python GEPA library rather than port it to C# because:

1. **Active Development** - GEPA has 799+ commits and frequent releases. A port would require constant maintenance.
2. **Ecosystem** - Deep integration with Python ML ecosystem (DSPy, LangChain, vector stores).
3. **Time to Market** - Wrapper approach gets you production-ready in hours vs. months for a full port.
4. **Use Case** - GEPA optimizations are async workflows (minutes/hours), so wrapper overhead is negligible.

## Quick Start

### Prerequisites
```bash
# .NET 10 SDK
# Python 3.11+
# Docker Desktop
# OpenAI API Key
```

### Run Locally with Docker Compose

```bash
# Set your OpenAI API key
export OPENAI_API_KEY=sk-...

# Start both services
docker-compose up --build

# API available at: http://localhost:5000
# GEPA wrapper at: http://localhost:8000
```

### Test the API

```bash
# Upload and optimize a prompt
curl -X POST http://localhost:5000/api/prompts/upload \
  -H "Content-Type: application/json" \
  -d '{
    "promptId": "test-1",
    "seedPrompt": "You are a helpful assistant. Answer briefly.",
    "trainingSet": [
      {
        "input": "What is 2+2?",
        "expectedOutput": "4"
      }
    ],
    "validationSet": [
      {
        "input": "What is 5+5?",
        "expectedOutput": "10"
      }
    ],
    "maxMetricCalls": 150
  }'

# Response includes jobId for tracking
# {"promptId":"test-1","jobId":"550e8400...","message":"Optimization started",...}

# Check optimization status
curl http://localhost:5000/api/prompts/test-1/status?jobId=<job-id>
```

## API Endpoints

### `POST /api/prompts/upload`
Upload a prompt and trigger GEPA optimization.

**Response (202 Accepted):**
```json
{
  "promptId": "test-1",
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "message": "Optimization started",
  "statusUrl": "/api/prompts/test-1/status"
}
```

### `GET /api/prompts/{promptId}/status?jobId={jobId}`
Check optimization progress. Status values: `pending`, `running`, `completed`, `failed`, `cancelled`

### `DELETE /api/prompts/{promptId}/cancel?jobId={jobId}`
Cancel a running optimization.

## AWS Deployment

See [aws/README.md](./aws/README.md) for complete AWS deployment instructions using ECS Fargate.

**Quick Deploy:**
```bash
# Build and push to ECR
aws ecr get-login-password --region us-east-1 | docker login...

# Deploy with CloudFormation
aws cloudformation create-stack \
  --stack-name gepa-net-prod \
  --template-body file://aws/aws-cloudformation.yml \
  --parameters ParameterKey=OpenAIApiKey,ParameterValue=sk-... \
  --capabilities CAPABILITY_IAM
```

**Estimated Cost:** ~$110-185/month for moderate usage
