namespace Purview.EventSourcing.Admin.SqlServer;

sealed record SqlServerAdminTableDescriptor(string? AggregateTypeFilter, string SchemaName, string TableName);
