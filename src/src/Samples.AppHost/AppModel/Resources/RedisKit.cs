using Purview.Aspire.ResourceKit;

namespace Purview.EventSourcing.Samples.AppHost.AppModel.Resources;

[ResourceDefinition<RedisResource>(Platform.Redis)]
sealed partial class RedisKit
{
	protected override IResourceBuilder<RedisResource> BuildResource(
		IDistributedApplicationBuilder builder
	)
	{
		var redis = builder.AddRedis(Name);
		if (HostKit.Options.IsLocal)
			redis.WithRedisInsight(c => c.WithParentRelationship(redis));

		return redis;
	}
}
