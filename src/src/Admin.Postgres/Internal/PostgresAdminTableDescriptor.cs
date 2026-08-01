namespace Purview.EventSourcing.Admin.Postgres;

sealed record PostgresAdminTableDescriptor(string? AggregateTypeFilter, string SchemaName, string TableName);
