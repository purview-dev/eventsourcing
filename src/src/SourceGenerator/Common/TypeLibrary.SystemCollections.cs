namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class TypeLibrary
{
	partial class System
	{
		public static class Collections
		{
			public static class Generic
			{
				public static readonly TypeValueObject IEnumerable = new(typeof(IEnumerable<>));

				public static readonly TypeValueObject ICollection = new(typeof(ICollection<>));
			}
		}
	}
}
