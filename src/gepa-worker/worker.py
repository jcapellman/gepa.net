"""
GEPA Worker - Consumes optimization requests from SQS and processes them
"""
import json
import logging
import os
import time
from typing import Dict, Any
from concurrent.futures import ThreadPoolExecutor

import boto3
from botocore.config import Config
from gepa_optimizer import GepaOptimizer

# Configure logging
logging.basicConfig(
	level=os.getenv("LOG_LEVEL", "INFO"),
	format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class GepaWorker:
	"""Worker that processes GEPA optimization jobs from SQS"""

	def __init__(self):
		# AWS Configuration
		self.aws_config = Config(
			region_name=os.getenv("AWS_REGION", "us-east-1"),
			retries={'max_attempts': 3, 'mode': 'adaptive'}
		)

		# For LocalStack
		endpoint_url = os.getenv("AWS_ENDPOINT_URL")

		# Initialize AWS clients
		self.sqs = boto3.client(
			'sqs',
			config=self.aws_config,
			endpoint_url=endpoint_url
		)

		self.dynamodb = boto3.resource(
			'dynamodb',
			config=self.aws_config,
			endpoint_url=endpoint_url
		)

		# Queue URLs
		self.request_queue_url = os.getenv("SQS_REQUEST_QUEUE_URL")
		self.response_queue_url = os.getenv("SQS_RESPONSE_QUEUE_URL")

		if not self.request_queue_url or not self.response_queue_url:
			raise ValueError("SQS queue URLs must be configured")

		# DynamoDB table
		self.table_name = os.getenv("DYNAMODB_TABLE_NAME", "gepa-jobs")
		self.table = self.dynamodb.Table(self.table_name)

		# GEPA Optimizer
		self.optimizer = GepaOptimizer()

		# Worker configuration
		self.concurrency = int(os.getenv("WORKER_CONCURRENCY", "2"))

		logger.info(f"Initialized GEPA Worker with concurrency={self.concurrency}")
		logger.info(f"Request queue: {self.request_queue_url}")
		logger.info(f"Response queue: {self.response_queue_url}")
		logger.info(f"DynamoDB table: {self.table_name}")

	def update_job_status(
		self, 
		job_id: str, 
		status: str, 
		progress: float = None,
		result: Dict[str, Any] = None,
		error: str = None
	):
		"""Update job status in DynamoDB"""
		try:
			update_expr = "SET #status = :status, UpdatedAt = :updated"
			expr_values = {
				":status": status,
				":updated": int(time.time())
			}
			expr_names = {"#status": "Status"}

			if progress is not None:
				update_expr += ", Progress = :progress"
				expr_values[":progress"] = progress

			if result is not None:
				update_expr += ", Result = :result"
				expr_values[":result"] = result

			if error is not None:
				update_expr += ", #error = :error"
				expr_values[":error"] = error
				expr_names["#error"] = "Error"

			self.table.update_item(
				Key={'JobId': job_id},
				UpdateExpression=update_expr,
				ExpressionAttributeValues=expr_values,
				ExpressionAttributeNames=expr_names
			)

			logger.info(f"Updated job {job_id} status to {status}")
		except Exception as e:
			logger.error(f"Failed to update job status: {e}")

	def process_message(self, message: Dict[str, Any]):
		"""Process a single optimization request"""
		try:
			# Parse message body
			body = json.loads(message['Body'])

			job_id = body['job_id']
			prompt_id = body['prompt_id']
			seed_prompt = body['seed_prompt']
			trainset = body.get('trainset', [])
			valset = body.get('valset', [])
			max_metric_calls = body.get('max_metric_calls', 150)

			logger.info(f"Processing job {job_id} for prompt {prompt_id}")

			# Create job record in DynamoDB
			self.table.put_item(Item={
				'JobId': job_id,
				'PromptId': prompt_id,
				'Status': 'running',
				'Progress': 0.0,
				'CreatedAt': int(time.time()),
				'UpdatedAt': int(time.time()),
				'SeedPrompt': seed_prompt
			})

			# Run GEPA optimization
			self.update_job_status(job_id, 'running', progress=0.1)

			result = self.optimizer.optimize(
				seed_prompt=seed_prompt,
				trainset=trainset,
				valset=valset,
				max_metric_calls=max_metric_calls,
				progress_callback=lambda p: self.update_job_status(
					job_id, 'running', progress=p
				)
			)

			# Update with results
			optimization_result = {
				'optimized_prompt': result['optimized_prompt'],
				'metrics': result['metrics'],
				'iterations': result.get('iterations', 0)
			}

			self.update_job_status(
				job_id, 
				'completed', 
				progress=1.0, 
				result=optimization_result
			)

			# Send result to response queue
			self.sqs.send_message(
				QueueUrl=self.response_queue_url,
				MessageBody=json.dumps({
					'job_id': job_id,
					'prompt_id': prompt_id,
					'status': 'completed',
					'result': optimization_result
				})
			)

			logger.info(f"Successfully completed job {job_id}")

			# Delete message from request queue
			self.sqs.delete_message(
				QueueUrl=self.request_queue_url,
				ReceiptHandle=message['ReceiptHandle']
			)

		except Exception as e:
			logger.error(f"Error processing message: {e}", exc_info=True)

			# Update job as failed
			try:
				body = json.loads(message['Body'])
				job_id = body['job_id']

				self.update_job_status(
					job_id, 
					'failed', 
					error=str(e)
				)

				# Send failure notification
				self.sqs.send_message(
					QueueUrl=self.response_queue_url,
					MessageBody=json.dumps({
						'job_id': job_id,
						'prompt_id': body['prompt_id'],
						'status': 'failed',
						'error': str(e)
					})
				)
			except Exception as update_error:
				logger.error(f"Failed to update error state: {update_error}")

	def poll_queue(self):
		"""Poll SQS queue for messages"""
		logger.info("Starting to poll for messages...")

		with ThreadPoolExecutor(max_workers=self.concurrency) as executor:
			while True:
				try:
					# Long polling (20 seconds)
					response = self.sqs.receive_message(
						QueueUrl=self.request_queue_url,
						MaxNumberOfMessages=self.concurrency,
						WaitTimeSeconds=20,
						VisibilityTimeout=3600  # 1 hour for long optimizations
					)

					messages = response.get('Messages', [])

					if messages:
						logger.info(f"Received {len(messages)} message(s)")

						# Process messages concurrently
						futures = [
							executor.submit(self.process_message, msg)
							for msg in messages
						]

						# Wait for all to complete
						for future in futures:
							try:
								future.result()
							except Exception as e:
								logger.error(f"Worker thread failed: {e}")

				except Exception as e:
					logger.error(f"Error polling queue: {e}")
					time.sleep(5)  # Back off on error


def main():
	"""Main entry point"""
	logger.info("Starting GEPA Worker...")

	worker = GepaWorker()

	try:
		worker.poll_queue()
	except KeyboardInterrupt:
		logger.info("Shutting down worker...")
	except Exception as e:
		logger.error(f"Fatal error: {e}", exc_info=True)
		raise


if __name__ == "__main__":
	main()
