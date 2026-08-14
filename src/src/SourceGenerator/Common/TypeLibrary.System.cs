namespace Purview.EventSourcing.SourceGenerator.Common;

partial class TypeLibrary
{
	public static class System
	{
		public static readonly TypeValueObject String = TypeValueObject.Create<string>();

		public static readonly TypeValueObject Guid = TypeValueObject.Create<Guid>();

		public static readonly TypeValueObject Uri = TypeValueObject.Create<Uri>();

		public static readonly TypeValueObject DateTime = TypeValueObject.Create<DateTime>();

		public static readonly TypeValueObject DateTimeOffset =
			TypeValueObject.Create<DateTimeOffset>();

		public static readonly TypeValueObject TimeSpan = TypeValueObject.Create<TimeSpan>();

		public static readonly TypeValueObject DateOnly = new("DateOnly", "System");

		public static readonly TypeValueObject TimeOnly = new("TimeOnly", "System");
	}
}
