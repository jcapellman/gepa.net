namespace Gepa.Net.Api.Models;

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
