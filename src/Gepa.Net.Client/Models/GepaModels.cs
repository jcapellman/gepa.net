namespace Gepa.Net.Client.Models;

/// <summary>
/// Request to optimize a prompt using GEPA
/// </summary>
public record OptimizationRequest
{
    /// <summary>
    /// Unique identifier for the prompt
    /// </summary>
    public required string PromptId { get; init; }

    /// <summary>
    /// Original prompt text to optimize
    /// </summary>
    public required string SeedPrompt { get; init; }

    /// <summary>
    /// Training examples for optimization
    /// </summary>
    public required List<TrainingExample> TrainingSet { get; init; }

    /// <summary>
    /// Validation examples for optimization
    /// </summary>
    public required List<TrainingExample> ValidationSet { get; init; }

    /// <summary>
    /// Maximum number of metric calls (default: 150)
    /// </summary>
    public int MaxMetricCalls { get; init; } = 150;

    /// <summary>
    /// Task language model to use (e.g., "openai/gpt-4.1-mini")
    /// </summary>
    public string? TaskLanguageModel { get; init; }

    /// <summary>
    /// Reflection language model for optimization
    /// </summary>
    public string? ReflectionLanguageModel { get; init; }

    /// <summary>
    /// User ID who initiated the optimization
    /// </summary>
    public string? UserId { get; init; }
}

/// <summary>
/// Training/validation example
/// </summary>
public record TrainingExample
{
    /// <summary>
    /// Input text or question
    /// </summary>
    public required string Input { get; init; }

    /// <summary>
    /// Expected output or answer
    /// </summary>
    public required string ExpectedOutput { get; init; }

    /// <summary>
    /// Additional metadata for the example
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Result of a GEPA optimization
/// </summary>
public record OptimizationResult
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Prompt identifier
    /// </summary>
    public required string PromptId { get; init; }

    /// <summary>
    /// Optimized prompt text
    /// </summary>
    public required string OptimizedPrompt { get; init; }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public required OptimizationMetrics Metrics { get; init; }

    /// <summary>
    /// Timestamp when optimization completed
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Total processing time
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }
}

/// <summary>
/// Optimization performance metrics
/// </summary>
public record OptimizationMetrics
{
    /// <summary>
    /// Score on training set
    /// </summary>
    public double TrainingScore { get; init; }

    /// <summary>
    /// Score on validation set
    /// </summary>
    public double ValidationScore { get; init; }

    /// <summary>
    /// Number of evaluations performed
    /// </summary>
    public int EvaluationCount { get; init; }

    /// <summary>
    /// Number of iterations
    /// </summary>
    public int Iterations { get; init; }

    /// <summary>
    /// Additional metrics
    /// </summary>
    public Dictionary<string, double>? AdditionalMetrics { get; init; }
}

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
