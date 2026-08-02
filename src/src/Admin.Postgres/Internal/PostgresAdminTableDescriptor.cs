namespace Purview.EventSourcing.Admin.Postgres.Internal;

sealed record PostgresAdminTableDescriptor(string? AggregateTypeFilter, string SchemaName, string TableName);
