#!/bin/bash

# LocalStack initialization script
# Creates SQS queues and DynamoDB tables

echo "Initializing LocalStack resources..."

# Set AWS CLI to use LocalStack
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1

# Wait for LocalStack to be ready
echo "Waiting for LocalStack..."
sleep 5

# Create SQS Queues
echo "Creating SQS queues..."
awslocal sqs create-queue --queue-name gepa-optimization-requests
awslocal sqs create-queue --queue-name gepa-optimization-results

# Create DynamoDB Table
echo "Creating DynamoDB table..."
awslocal dynamodb create-table \
	--table-name gepa-jobs \
	--attribute-definitions \
		AttributeName=JobId,AttributeType=S \
		AttributeName=PromptId,AttributeType=S \
	--key-schema \
		AttributeName=JobId,KeyType=HASH \
	--global-secondary-indexes \
		"[{\"IndexName\": \"PromptIdIndex\",\"KeySchema\":[{\"AttributeName\":\"PromptId\",\"KeyType\":\"HASH\"}],\"Projection\":{\"ProjectionType\":\"ALL\"},\"ProvisionedThroughput\":{\"ReadCapacityUnits\":5,\"WriteCapacityUnits\":5}}]" \
	--provisioned-throughput \
		ReadCapacityUnits=5,WriteCapacityUnits=5

echo "LocalStack initialization complete!"
