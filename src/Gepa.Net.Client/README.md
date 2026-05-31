# Gepa.Net.Client

A .NET client library for integrating with [GEPA (Genetic-Pareto)](https://github.com/gepa-ai/gepa) AI prompt optimization framework.

## Installation

```bash
dotnet add package Gepa.Net.Client
```

## Quick Start

### 1. Configure Services

```csharp
using Gepa.Net.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Option A: Simple configuration
builder.Services.AddGepaClient(
	serviceUrl: "http://gepa-service:8000",
	callbackBaseUrl: "https://your-api.com"
);

// Option B: Advanced configuration
builder.Services.AddGepaClient(options =>
{
	options.ServiceUrl = "http://gepa-service:8000";
	options.CallbackBaseUrl = "https://your-api.com";
	options.TimeoutMinutes = 10;
	options.DefaultTaskModel = "openai/gpt-4.1-mini";
	options.DefaultReflectionModel = "openai/gpt-4o";
});

// Option C: From configuration
builder.Services.AddGepaClient(options =>
{
	builder.Configuration.GetSection("Gepa").Bind(options);
});
```

### 2. Use in Your Code

```csharp
public class PromptService
{
	private readonly IGepaClient _gepaClient;

	public PromptService(IGepaClient gepaClient)
	{
		_gepaClient = gepaClient;
	}

	public async Task<string> OptimizePromptAsync(string prompt)
	{
		// Trigger optimization
		var request = new OptimizationRequest
		{
			PromptId = Guid.NewGuid().ToString(),
			SeedPrompt = prompt,
			TrainingSet = new List<TrainingExample>
			{
				new() { Input = "What is 2+2?", ExpectedOutput = "4" }
			},
			ValidationSet = new List<TrainingExample>
			{
				new() { Input = "What is 5+5?", ExpectedOutput = "10" }
			},
			MaxMetricCalls = 150
		};

		var jobId = await _gepaClient.TriggerOptimizationAsync(request);

		// Option 1: Poll for result
		var result = await _gepaClient.WaitForCompletionAsync(
			jobId, 
			pollingInterval: TimeSpan.FromSeconds(5),
			timeout: TimeSpan.FromMinutes(30)
		);

		return result.OptimizedPrompt;

		// Option 2: Check status manually
		var status = await _gepaClient.GetJobStatusAsync(jobId);
		if (status.Status == OptimizationStatus.Completed)
		{
			var finalResult = await _gepaClient.GetJobResultAsync(jobId);
			return finalResult.OptimizedPrompt;
		}
	}
}
```

## Features

✅ **Simple Integration** - Add GEPA to any .NET application with one line  
✅ **Async/Await** - Non-blocking optimization operations  
✅ **Polling Helper** - `WaitForCompletionAsync` with configurable intervals  
✅ **Strongly Typed** - Full IntelliSense support  
✅ **Logging** - Built-in ILogger integration  
✅ **Retry Logic** - Automatic retry with HttpClient  
✅ **Cancellation** - CancellationToken support throughout  

## Configuration

### appsettings.json

```json
{
  "Gepa": {
	"ServiceUrl": "http://gepa-service:8000",
	"CallbackBaseUrl": "https://your-api.com",
	"TimeoutMinutes": 10,
	"DefaultTaskModel": "openai/gpt-4.1-mini",
	"DefaultReflectionModel": "openai/gpt-4o"
  }
}
```

## Examples

### Basic Optimization

```csharp
var request = new OptimizationRequest
{
	PromptId = "my-prompt-1",
	SeedPrompt = "You are a helpful assistant.",
	TrainingSet = examples,
	ValidationSet = validationExamples,
	MaxMetricCalls = 100
};

var jobId = await gepaClient.TriggerOptimizationAsync(request);
Console.WriteLine($"Job started: {jobId}");
```

### Wait for Completion

```csharp
try
{
	var result = await gepaClient.WaitForCompletionAsync(
		jobId,
		pollingInterval: TimeSpan.FromSeconds(10),
		timeout: TimeSpan.FromMinutes(30)
	);

	Console.WriteLine($"Optimized: {result.OptimizedPrompt}");
	Console.WriteLine($"Score: {result.Metrics.ValidationScore}");
}
catch (GepaClientException ex)
{
	Console.WriteLine($"Optimization failed: {ex.Message}");
}
```

### Manual Status Checking

```csharp
var status = await gepaClient.GetJobStatusAsync(jobId);

switch (status.Status)
{
	case OptimizationStatus.Pending:
		Console.WriteLine("Job queued...");
		break;
	case OptimizationStatus.Running:
		Console.WriteLine($"Running... {status.Progress:F1}%");
		break;
	case OptimizationStatus.Completed:
		var result = status.Result 
			?? await gepaClient.GetJobResultAsync(jobId);
		Console.WriteLine($"Done! {result.OptimizedPrompt}");
		break;
	case OptimizationStatus.Failed:
		Console.WriteLine($"Failed: {status.Error}");
		break;
}
```

### Cancel a Job

```csharp
await gepaClient.CancelJobAsync(jobId);
Console.WriteLine("Job cancelled");
```

## Requirements

- .NET 10.0 or later
- GEPA Python service running and accessible

## Resources

- **GEPA Framework**: https://github.com/gepa-ai/gepa
- **Documentation**: https://gepa-ai.github.io/gepa/
- **Paper**: https://arxiv.org/abs/2507.19457

## License

MIT
