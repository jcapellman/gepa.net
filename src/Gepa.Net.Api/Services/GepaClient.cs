using System.Net.Http.Json;
using System.Text.Json;
using Gepa.Net.Api.Models;
using Microsoft.Extensions.Options;

namespace Gepa.Net.Api.Services;

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
        _logger.LogInformation(
            "Triggering optimization for prompt {PromptId}",
            request.PromptId);

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
                callback_url = $"{_options.CallbackBaseUrl}/api/prompts/optimization-callback",
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
                throw new InvalidOperationException("GEPA service returned invalid response");
            }

            _logger.LogInformation(
                "Optimization job {JobId} started for prompt {PromptId}",
                result.JobId,
                request.PromptId);

            return result.JobId;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed to trigger optimization for prompt {PromptId}",
                request.PromptId);
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
                throw new InvalidOperationException("GEPA service returned invalid status");
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
                throw new InvalidOperationException("GEPA service returned invalid result");
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

    private record JobResponse(string JobId);
}

/// <summary>
/// Configuration options for GEPA client
/// </summary>
public class GepaClientOptions
{
    /// <summary>
    /// GEPA service URL
    /// </summary>
    public required string ServiceUrl { get; set; }

    /// <summary>
    /// Callback base URL for this API
    /// </summary>
    public required string CallbackBaseUrl { get; set; }

    /// <summary>
    /// Request timeout in minutes
    /// </summary>
    public int TimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Default task language model
    /// </summary>
    public string DefaultTaskModel { get; set; } = "openai/gpt-4.1-mini";

    /// <summary>
    /// Default reflection language model
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
