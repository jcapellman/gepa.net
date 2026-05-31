using Gepa.Net.Client.Models;

namespace Gepa.Net.Client;

/// <summary>
/// Client interface for interacting with GEPA optimization service
/// </summary>
public interface IGepaClient
{
    /// <summary>
    /// Trigger a new optimization job
    /// </summary>
    /// <param name="request">Optimization request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job ID</returns>
    Task<string> TriggerOptimizationAsync(OptimizationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of an optimization job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job status</returns>
    Task<JobStatus> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the result of a completed optimization job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimization result</returns>
    Task<OptimizationResult> GetJobResultAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a running optimization job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wait for an optimization job to complete, polling at intervals
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="pollingInterval">Time between status checks</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimization result</returns>
    Task<OptimizationResult> WaitForCompletionAsync(
        string jobId, 
        TimeSpan? pollingInterval = null, 
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
