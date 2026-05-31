namespace Gepa.Net.Api.Models;

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
