using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MiniCommerce.Catalog.API.HealthChecks;

public sealed class MongoDbHealthCheck(IMongoDatabase database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BsonDocument("ping", 1);

            await database.RunCommandAsync<BsonDocument>(
                command,
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("MongoDB connection is healthy.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "MongoDB connection is unhealthy.",
                exception);
        }
    }
}
