using Gepa.Net.Api.Models;
using Gepa.Net.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gepa.Net.Api.Controllers;

/// <summary>
/// API endpoints for prompt management and optimization
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PromptsController : ControllerBase
{
    private readonly IGepaClient _gepaClient;
    private readonly ILogger<PromptsController> _logger;

    public PromptsController(
        IGepaClient gepaClient,
        ILogger<PromptsController> logger)
    {
        _gepaClient = gepaClient;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new prompt and trigger optimization
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPrompt(
        [FromBody] OptimizationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received prompt upload request for prompt {PromptId}",
            request.PromptId);

        try
        {
            // TODO: Save original prompt to database
            // var prompt = await _promptRepository.CreateAsync(new Prompt { ... });

            // Trigger GEPA optimization (non-blocking)
            var jobId = await _gepaClient.TriggerOptimizationAsync(request, cancellationToken);

            // TODO: Update database with job ID
            // await _promptRepository.UpdateJobIdAsync(request.PromptId, jobId);

            return Accepted(new UploadResponse
            {
                PromptId = request.PromptId,
                JobId = jobId,
                Message = "Optimization started. Use /status endpoint to check progress.",
                StatusUrl = $"/api/prompts/{request.PromptId}/status"
            });
        }
        catch (GepaClientException ex)
        {
            _logger.LogError(ex, "Failed to trigger optimization for prompt {PromptId}", request.PromptId);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Failed to start optimization",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get optimization status for a prompt
    /// </summary>
    [HttpGet("{promptId}/status")]
    [ProducesResponseType(typeof(JobStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOptimizationStatus(
        string promptId,
        CancellationToken cancellationToken)
    {
        // TODO: Fetch prompt and jobId from database
        // var prompt = await _promptRepository.GetByIdAsync(promptId);
        // if (prompt == null) return NotFound();
        // var jobId = prompt.OptimizationJobId;

        // For now, expecting jobId in query parameter
        var jobId = Request.Query["jobId"].ToString();
        if (string.IsNullOrEmpty(jobId))
        {
            return BadRequest(new { error = "jobId query parameter required" });
        }

        try
        {
            var status = await _gepaClient.GetJobStatusAsync(jobId, cancellationToken);
            return Ok(status);
        }
        catch (GepaClientException ex)
        {
            _logger.LogError(ex, "Failed to fetch status for job {JobId}", jobId);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Failed to fetch status",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Webhook endpoint called by GEPA service when optimization completes
    /// </summary>
    [HttpPost("optimization-callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OptimizationCallback(
        [FromBody] OptimizationResult result)
    {
        _logger.LogInformation(
            "Received optimization callback for prompt {PromptId}, job {JobId}",
            result.PromptId,
            result.JobId);

        // TODO: Update database with optimized prompt
        // var prompt = await _promptRepository.GetByIdAsync(result.PromptId);
        // if (prompt == null) return NotFound();
        //
        // prompt.OptimizedContent = result.OptimizedPrompt;
        // prompt.Metrics = result.Metrics;
        // prompt.Status = PromptStatus.Optimized;
        // await _promptRepository.UpdateAsync(prompt);

        // TODO: Optional - Send notification via SignalR or other means
        // await _notificationService.NotifyOptimizationComplete(result.PromptId, result.Metrics);

        _logger.LogInformation(
            "Optimization completed for prompt {PromptId} with validation score {Score}",
            result.PromptId,
            result.Metrics.ValidationScore);

        return Ok(new { message = "Callback processed successfully" });
    }

    /// <summary>
    /// Cancel an optimization job
    /// </summary>
    [HttpDelete("{promptId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOptimization(
        string promptId,
        CancellationToken cancellationToken)
    {
        // TODO: Fetch jobId from database
        var jobId = Request.Query["jobId"].ToString();
        if (string.IsNullOrEmpty(jobId))
        {
            return BadRequest(new { error = "jobId query parameter required" });
        }

        try
        {
            await _gepaClient.CancelJobAsync(jobId, cancellationToken);

            _logger.LogInformation(
                "Cancelled optimization job {JobId} for prompt {PromptId}",
                jobId,
                promptId);

            return Ok(new { message = "Optimization cancelled" });
        }
        catch (GepaClientException ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Failed to cancel optimization",
                details = ex.Message
            });
        }
    }
}

public record UploadResponse
{
    public required string PromptId { get; init; }
    public required string JobId { get; init; }
    public required string Message { get; init; }
    public required string StatusUrl { get; init; }
}
