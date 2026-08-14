namespace Purview.EventSourcing.SourceGenerator.ValueObject.Models;

readonly record struct GeneratedTypeModel(
	string Name,
	string? Namespace,
	string DeclarationPrefix,
	string FullyQualifiedName
);
