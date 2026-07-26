using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.Services.Resources;

[ResourceDefinition<RedisResource>]
sealed partial class RedisKit
{
	protected override IResourceBuilder<RedisResource> BuildResource(IDistributedApplicationBuilder builder)
	{
		var redis = builder.AddRedis("redis");
		redis.WithRedisInsight(c => c.WithParentRelationship(redis));

		return redis;
	}
}
