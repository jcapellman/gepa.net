using System.Net.Http.Json;
using System.Text.Json;
using Gepa.Net.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gepa.Net.Client;

/// <summary>
/// HTTP client for GEPA optimization service
/// </summary>
public class GepaClient : IGepaClient
{
    private readonly HttpClient _httpClient;
    private readonly GepaClientOptions _options;
    private readonly ILogger<GepaClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GepaClient(
        HttpClient httpClient,
        IOptions<GepaClientOptions> options,
        ILogger<GepaClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        _httpClient.BaseAddress = new Uri(_options.ServiceUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(_options.TimeoutMinutes);
    }

    public async Task<string> TriggerOptimizationAsync(
        OptimizationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Triggering optimization for prompt {PromptId}", request.PromptId);

        try
        {
            var payload = new
            {
                prompt_id = request.PromptId,
                seed_prompt = request.SeedPrompt,
                trainset = request.TrainingSet.Select(t => new
                {
                    input = t.Input,
                    expected_output = t.ExpectedOutput,
                    metadata = t.Metadata
                }),
                valset = request.ValidationSet.Select(v => new
                {
                    input = v.Input,
                    expected_output = v.ExpectedOutput,
                    metadata = v.Metadata
                }),
                max_metric_calls = request.MaxMetricCalls,
                task_lm = request.TaskLanguageModel ?? _options.DefaultTaskModel,
                reflection_lm = request.ReflectionLanguageModel ?? _options.DefaultReflectionModel,
                callback_url = string.IsNullOrEmpty(_options.CallbackBaseUrl) 
                    ? null 
                    : $"{_options.CallbackBaseUrl}/api/prompts/optimization-callback",
                user_id = request.UserId
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/optimize",
                payload,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JobResponse>(
                _jsonOptions,
                cancellationToken);

            if (result?.JobId == null)
            {
                throw new GepaClientException("GEPA service returned invalid response");
            }

            _logger.LogInformation(
                "Optimization job {JobId} started for prompt {PromptId}",
                result.JobId,
                request.PromptId);

            return result.JobId;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to trigger optimization for prompt {PromptId}", request.PromptId);
            throw new GepaClientException("Failed to communicate with GEPA service", ex);
        }
    }

    public async Task<JobStatus> GetJobStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching status for job {JobId}", jobId);

        try
        {
            var response = await _httpClient.GetAsync(
                $"/status/{jobId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var status = await response.Content.ReadFromJsonAsync<JobStatus>(
                _jsonOptions,
                cancellationToken);

            if (status == null)
            {
                throw new GepaClientException("GEPA service returned invalid status");
            }

            return status;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch status for job {JobId}", jobId);
            throw new GepaClientException("Failed to fetch job status", ex);
        }
    }

    public async Task<OptimizationResult> GetJobResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching result for job {JobId}", jobId);

        try
        {
            var response = await _httpClient.GetAsync(
                $"/result/{jobId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OptimizationResult>(
                _jsonOptions,
                cancellationToken);

            if (result == null)
            {
                throw new GepaClientException("GEPA service returned invalid result");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch result for job {JobId}", jobId);
            throw new GepaClientException("Failed to fetch job result", ex);
        }
    }

    public async Task CancelJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling job {JobId}", jobId);

        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/jobs/{jobId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Job {JobId} cancelled successfully", jobId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
            throw new GepaClientException("Failed to cancel job", ex);
        }
    }

    public async Task<OptimizationResult> WaitForCompletionAsync(
        string jobId,
        TimeSpan? pollingInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollingInterval ?? TimeSpan.FromSeconds(5);
        var maxWait = timeout ?? TimeSpan.FromHours(1);
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Waiting for job {JobId} to complete (polling every {Interval}s, timeout {Timeout}m)",
            jobId,
            interval.TotalSeconds,
            maxWait.TotalMinutes);

        while (true)
        {
            if (DateTime.UtcNow - startTime > maxWait)
            {
                throw new GepaClientException($"Job {jobId} did not complete within {maxWait.TotalMinutes} minutes");
            }

            var status = await GetJobStatusAsync(jobId, cancellationToken);

            switch (status.Status)
            {
                case OptimizationStatus.Completed:
                    _logger.LogInformation("Job {JobId} completed successfully", jobId);
                    return status.Result 
                        ?? await GetJobResultAsync(jobId, cancellationToken);

                case OptimizationStatus.Failed:
                    throw new GepaClientException($"Job {jobId} failed: {status.Error}");

                case OptimizationStatus.Cancelled:
                    throw new GepaClientException($"Job {jobId} was cancelled");

                case OptimizationStatus.Pending:
                case OptimizationStatus.Running:
                    _logger.LogDebug(
                        "Job {JobId} still {Status} ({Progress:F1}% complete)",
                        jobId,
                        status.Status,
                        status.Progress);
                    await Task.Delay(interval, cancellationToken);
                    break;
            }
        }
    }

    private record JobResponse(string JobId);
}

/// <summary>
/// Configuration options for GEPA client
/// </summary>
public class GepaClientOptions
{
    /// <summary>
    /// GEPA service URL (required)
    /// </summary>
    public required string ServiceUrl { get; set; }

    /// <summary>
    /// Callback base URL for this API (optional - only needed if using webhooks)
    /// </summary>
    public string? CallbackBaseUrl { get; set; }

    /// <summary>
    /// Request timeout in minutes (default: 5)
    /// </summary>
    public int TimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Default task language model (default: "openai/gpt-4.1-mini")
    /// </summary>
    public string DefaultTaskModel { get; set; } = "openai/gpt-4.1-mini";

    /// <summary>
    /// Default reflection language model (default: "openai/gpt-4o")
    /// </summary>
    public string DefaultReflectionModel { get; set; } = "openai/gpt-4o";
}

/// <summary>
/// Exception thrown by GEPA client
/// </summary>
public class GepaClientException : Exception
{
    public GepaClientException(string message) : base(message) { }
    public GepaClientException(string message, Exception innerException) : base(message, innerException) { }
}
