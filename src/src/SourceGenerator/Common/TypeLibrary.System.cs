namespace Purview.EventSourcing.SourceGenerator.Common;

partial class TypeLibrary
{
	public static partial class System
	{
		public static readonly TypeIdentity AttributeUsageAttribute = TypeIdentity.Create<AttributeUsageAttribute>();

		public static readonly TypeIdentity AttributeTargets = TypeIdentity.Create<AttributeTargets>();

		public static readonly TypeIdentity Int32 = TypeIdentity.Create<int>();

		public static readonly TypeIdentity Boolean = TypeIdentity.Create<bool>();

		public static readonly TypeIdentity String = TypeIdentity.Create<string>();

		public static readonly TypeIdentity Guid = TypeIdentity.Create<Guid>();

		public static readonly TypeIdentity Uri = TypeIdentity.Create<Uri>();

		public static readonly TypeIdentity DateTime = TypeIdentity.Create<DateTime>();

		public static readonly TypeIdentity DateTimeOffset = TypeIdentity.Create<DateTimeOffset>();

		public static readonly TypeIdentity TimeSpan = TypeIdentity.Create<TimeSpan>();

		public static readonly TypeIdentity DateOnly = new("DateOnly", "System");

		public static readonly TypeIdentity TimeOnly = new("TimeOnly", "System");

		public static readonly TypeIdentity HashCode = new("HashCode", "System");
	}
}
