from fastapi import FastAPI, BackgroundTasks, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
import gepa
import httpx
import uuid
import logging
import os
from datetime import datetime, timezone
from enum import Enum

# Configure logging
logging.basicConfig(
	level=os.getenv("LOG_LEVEL", "INFO"),
	format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

app = FastAPI(
	title="GEPA Optimization Service",
	description="Wrapper service for GEPA prompt optimization",
	version="1.0.0"
)

# CORS middleware
app.add_middleware(
	CORSMiddleware,
	allow_origins=["*"],  # Configure appropriately for production
	allow_credentials=True,
	allow_methods=["*"],
	allow_headers=["*"],
)

# In-memory job store (use DynamoDB in production)
jobs: Dict[str, Dict[str, Any]] = {}

class OptimizationStatus(str, Enum):
	PENDING = "pending"
	RUNNING = "running"
	COMPLETED = "completed"
	FAILED = "failed"
	CANCELLED = "cancelled"

class TrainingExample(BaseModel):
	input: str
	expected_output: str
	metadata: Optional[Dict[str, Any]] = None

class OptimizationRequest(BaseModel):
	prompt_id: str
	seed_prompt: str
	trainset: List[TrainingExample]
	valset: List[TrainingExample]
	callback_url: str
	max_metric_calls: int = Field(default=150, ge=1, le=1000)
	task_lm: Optional[str] = Field(default="openai/gpt-4.1-mini")
	reflection_lm: Optional[str] = Field(default="openai/gpt-4o")
	user_id: Optional[str] = None

class JobResponse(BaseModel):
	job_id: str

class OptimizationMetrics(BaseModel):
	training_score: float
	validation_score: float
	evaluation_count: int
	iterations: int
	additional_metrics: Optional[Dict[str, float]] = None

class OptimizationResult(BaseModel):
	job_id: str
	prompt_id: str
	optimized_prompt: str
	metrics: OptimizationMetrics
	completed_at: datetime
	processing_time: float  # seconds

class JobStatus(BaseModel):
	job_id: str
	status: OptimizationStatus
	progress: float = Field(ge=0.0, le=100.0)
	result: Optional[OptimizationResult] = None
	error: Optional[str] = None
	created_at: datetime
	updated_at: datetime

def convert_examples(examples: List[TrainingExample]) -> List[Any]:
	"""Convert examples to GEPA format"""
	return [
		{
			"input": ex.input,
			"output": ex.expected_output,
			**(ex.metadata or {})
		}
		for ex in examples
	]

async def run_optimization(job_id: str, request: OptimizationRequest):
	"""Background task to run GEPA optimization"""
	logger.info(f"Starting optimization job {job_id} for prompt {request.prompt_id}")

	start_time = datetime.now(timezone.utc)
	jobs[job_id]["status"] = OptimizationStatus.RUNNING
	jobs[job_id]["updated_at"] = start_time

	try:
		# Convert training/validation sets
		trainset = convert_examples(request.trainset)
		valset = convert_examples(request.valset)

		logger.info(f"Job {job_id}: Running GEPA with {len(trainset)} training and {len(valset)} validation examples")

		# Run GEPA optimization
		result = gepa.optimize(
			seed_candidate={"system_prompt": request.seed_prompt},
			trainset=trainset,
			valset=valset,
			task_lm=request.task_lm,
			reflection_lm=request.reflection_lm,
			max_metric_calls=request.max_metric_calls,
		)

		end_time = datetime.now(timezone.utc)
		processing_time = (end_time - start_time).total_seconds()

		optimized_prompt = result.best_candidate.get('system_prompt', request.seed_prompt)

		logger.info(
			f"Job {job_id}: Optimization completed. "
			f"Score: {result.best_score:.4f}, Time: {processing_time:.2f}s"
		)

		# Prepare result
		optimization_result = OptimizationResult(
			job_id=job_id,
			prompt_id=request.prompt_id,
			optimized_prompt=optimized_prompt,
			metrics=OptimizationMetrics(
				training_score=getattr(result, 'training_score', result.best_score),
				validation_score=result.best_score,
				evaluation_count=getattr(result, 'num_evaluations', request.max_metric_calls),
				iterations=getattr(result, 'num_iterations', 0),
				additional_metrics=None
			),
			completed_at=end_time,
			processing_time=processing_time
		)

		jobs[job_id]["status"] = OptimizationStatus.COMPLETED
		jobs[job_id]["progress"] = 100.0
		jobs[job_id]["result"] = optimization_result
		jobs[job_id]["updated_at"] = end_time

		# Send callback to C# service
		try:
			async with httpx.AsyncClient(timeout=30.0) as client:
				logger.info(f"Job {job_id}: Sending callback to {request.callback_url}")
				response = await client.post(
					request.callback_url,
					json=optimization_result.model_dump(mode='json')
				)
				response.raise_for_status()
				logger.info(f"Job {job_id}: Callback sent successfully")
		except Exception as callback_error:
			logger.error(f"Job {job_id}: Failed to send callback: {callback_error}")
			# Don't fail the job if callback fails

	except Exception as e:
		logger.error(f"Job {job_id}: Optimization failed: {e}", exc_info=True)
		jobs[job_id]["status"] = OptimizationStatus.FAILED
		jobs[job_id]["error"] = str(e)
		jobs[job_id]["updated_at"] = datetime.now(timezone.utc)

@app.post("/optimize", response_model=JobResponse)
async def optimize_prompt(
	request: OptimizationRequest,
	background_tasks: BackgroundTasks
):
	"""
	Trigger a new optimization job

	Returns a job_id that can be used to check status and retrieve results.
	"""
	job_id = str(uuid.uuid4())

	logger.info(f"Received optimization request for prompt {request.prompt_id}, assigned job {job_id}")

	# Initialize job record
	now = datetime.now(timezone.utc)
	jobs[job_id] = {
		"job_id": job_id,
		"status": OptimizationStatus.PENDING,
		"progress": 0.0,
		"result": None,
		"error": None,
		"created_at": now,
		"updated_at": now,
		"request": request
	}

	# Start background optimization
	background_tasks.add_task(run_optimization, job_id, request)

	return JobResponse(job_id=job_id)

@app.get("/status/{job_id}", response_model=JobStatus)
async def get_job_status(job_id: str):
	"""
	Get the current status of an optimization job
	"""
	if job_id not in jobs:
		raise HTTPException(status_code=404, detail=f"Job {job_id} not found")

	job = jobs[job_id]

	return JobStatus(
		job_id=job_id,
		status=job["status"],
		progress=job["progress"],
		result=job.get("result"),
		error=job.get("error"),
		created_at=job["created_at"],
		updated_at=job["updated_at"]
	)

@app.get("/result/{job_id}", response_model=OptimizationResult)
async def get_job_result(job_id: str):
	"""
	Get the result of a completed optimization job
	"""
	if job_id not in jobs:
		raise HTTPException(status_code=404, detail=f"Job {job_id} not found")

	job = jobs[job_id]

	if job["status"] != OptimizationStatus.COMPLETED:
		raise HTTPException(
			status_code=400,
			detail=f"Job {job_id} is not completed (status: {job['status']})"
		)

	return job["result"]

@app.delete("/jobs/{job_id}")
async def cancel_job(job_id: str):
	"""
	Cancel a running optimization job
	"""
	if job_id not in jobs:
		raise HTTPException(status_code=404, detail=f"Job {job_id} not found")

	job = jobs[job_id]

	if job["status"] in [OptimizationStatus.COMPLETED, OptimizationStatus.FAILED]:
		raise HTTPException(
			status_code=400,
			detail=f"Cannot cancel job in {job['status']} state"
		)

	jobs[job_id]["status"] = OptimizationStatus.CANCELLED
	jobs[job_id]["updated_at"] = datetime.now(timezone.utc)

	logger.info(f"Job {job_id} cancelled")

	return {"message": f"Job {job_id} cancelled"}

@app.get("/health")
async def health_check():
	"""Health check endpoint"""
	return {
		"status": "healthy",
		"active_jobs": len([j for j in jobs.values() if j["status"] == OptimizationStatus.RUNNING]),
		"total_jobs": len(jobs)
	}

@app.get("/")
async def root():
	"""Root endpoint"""
	return {
		"service": "GEPA Optimization Service",
		"version": "1.0.0",
		"docs": "/docs"
	}

if __name__ == "__main__":
	import uvicorn
	port = int(os.getenv("PORT", 8000))
	uvicorn.run(app, host="0.0.0.0", port=port)
