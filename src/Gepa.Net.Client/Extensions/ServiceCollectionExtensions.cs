using Gepa.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Gepa.Net.Client.Extensions;

/// <summary>
/// Extension methods for configuring GEPA client services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds GEPA client services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGepaClient(
        this IServiceCollection services,
        Action<GepaClientOptions> configure)
    {
        services.Configure(configure);
        services.AddHttpClient<IGepaClient, GepaClient>();

        return services;
    }

    /// <summary>
    /// Adds GEPA client services to the service collection with options from configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceUrl">GEPA service URL</param>
    /// <param name="callbackBaseUrl">Optional callback base URL</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGepaClient(
        this IServiceCollection services,
        string serviceUrl,
        string? callbackBaseUrl = null)
    {
        return services.AddGepaClient(options =>
        {
            options.ServiceUrl = serviceUrl;
            options.CallbackBaseUrl = callbackBaseUrl;
        });
    }
}
