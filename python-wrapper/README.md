# Python GEPA Wrapper Service

FastAPI service that wraps the GEPA optimization library for consumption by the .NET API.

## Setup

### Prerequisites
- Python 3.11+
- pip or uv

### Installation

```bash
pip install -r requirements.txt
```

Or with uv:
```bash
uv pip install -r requirements.txt
```

### Environment Variables

Create a `.env` file:

```bash
# OpenAI API Key (required)
OPENAI_API_KEY=sk-...

# Optional: Other LLM provider keys
ANTHROPIC_API_KEY=...
COHERE_API_KEY=...

# Service Configuration
PORT=8000
LOG_LEVEL=INFO

# AWS Configuration (for production)
AWS_REGION=us-east-1
DYNAMODB_TABLE_NAME=gepa-jobs
SQS_QUEUE_URL=...
```

## Running Locally

```bash
# Development mode with auto-reload
uvicorn main:app --reload --host 0.0.0.0 --port 8000

# Production mode
gunicorn main:app --workers 4 --worker-class uvicorn.workers.UvicornWorker --bind 0.0.0.0:8000
```

## API Endpoints

### POST /optimize
Trigger a new optimization job

**Request:**
```json
{
  "prompt_id": "prompt-123",
  "seed_prompt": "You are a helpful assistant...",
  "trainset": [
	{
	  "input": "What is 2+2?",
	  "expected_output": "4"
	}
  ],
  "valset": [...],
  "max_metric_calls": 150,
  "callback_url": "https://your-api.com/api/prompts/optimization-callback"
}
```

**Response:**
```json
{
  "job_id": "550e8400-e29b-41d4-a716-446655440000"
}
```

### GET /status/{job_id}
Get job status

**Response:**
```json
{
  "job_id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "running",
  "progress": 45.5,
  "created_at": "2025-01-15T10:30:00Z",
  "updated_at": "2025-01-15T10:35:00Z"
}
```

### GET /result/{job_id}
Get optimization result (only available when status is "completed")

### DELETE /jobs/{job_id}
Cancel a running job

## Docker

```bash
# Build
docker build -t gepa-wrapper .

# Run
docker run -p 8000:8000 -e OPENAI_API_KEY=sk-... gepa-wrapper
```

## Testing

```bash
pytest tests/
```
