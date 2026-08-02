namespace Purview.EventSourcing.Admin.SqlServer.Internal;

sealed record SqlServerAdminTableDescriptor(string? AggregateTypeFilter, string SchemaName, string TableName);
