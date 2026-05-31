namespace Gepa.Net.Api.Models;

/// <summary>
/// Status of an optimization job
/// </summary>
public record JobStatus
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Current status
    /// </summary>
    public required OptimizationStatus Status { get; init; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double Progress { get; init; }

    /// <summary>
    /// Result if completed
    /// </summary>
    public OptimizationResult? Result { get; init; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Timestamp when job was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Timestamp of last update
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Optimization job status enum
/// </summary>
public enum OptimizationStatus
{
    /// <summary>
    /// Job is queued but not started
    /// </summary>
    Pending,

    /// <summary>
    /// Job is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed with error
    /// </summary>
    Failed,

    /// <summary>
    /// Job was cancelled
    /// </summary>
    Cancelled
}
