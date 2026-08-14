using System.Text.RegularExpressions;

namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class EventVerbMap
{
	static readonly Regex PascalCaseSplitter = new(
		// Boundary before an uppercase letter that starts a new word:
		//  - lower/digit -> Upper   (forceSave -> force|Save)
		//  - Upper -> Upper+lower   (XMLParse  -> XML|Parse)
		@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
		RegexOptions.Compiled
	);

	static List<string> SplitPascalCase(string identifier) =>
		[.. PascalCaseSplitter.Split(identifier)];

	public static bool TryGetPastTense(string verb, out string pastTense)
	{
		foreach (var kvp in VerbPairsByPrefixLength)
		{
			if (!string.Equals(kvp.Key, verb, StringComparison.Ordinal))
				continue;

			pastTense = kvp.Value;
			return true;
		}

		pastTense = string.Empty;
		return false;
	}

	public static bool TryCreateGeneratedEventName(
		string methodName,
		string aggregateClassName,
		out string eventName
	)
	{
		var match = ToPastTense(methodName);
		if (match is not null)
		{
			eventName = match;
			return true;
		}

		if (TryCreatePropertySpecificEventName(methodName, out eventName))
			return true;

		if (TryCreateVerbMappedEventName(methodName, aggregateClassName, out eventName))
			return true;

		eventName = string.Empty;
		return false;
	}

	public static bool IsVerbPhrase(string methodName) =>
		TryCreatePropertySpecificEventName(methodName, out _)
		|| TryGetVerbPrefix(methodName, out _, out _);

	public static bool IsPastTenseEventName(string eventName)
	{
		var coreName = TrimEventSuffix(eventName);
		return !string.IsNullOrWhiteSpace(coreName) && TryGetPastTenseSuffix(coreName, out _);
	}

	public static bool TryGetPastTenseEventNameCore(string eventName, out string coreName)
	{
		coreName = TrimEventSuffix(eventName);
		return !string.IsNullOrWhiteSpace(coreName) && IsPastTenseEventName(coreName);
	}

	/// <summary>
	/// Converts a command-style identifier into its past-tense event form.
	/// Default (verb-object): "CreateOrder" -> "OrderCreated", "ShipOrder" -> "OrderShipped".
	/// Single verb:           "Ship" -> "Shipped".
	/// Modifier prefix:       "ForceSave" -> "ForceSaved" (inflect last word, preserve order).
	/// </summary>
	/// <returns>The past-tense event identifier, or null if no verb could be resolved.</returns>
	static string? ToPastTense(string identifier)
	{
		if (string.IsNullOrEmpty(identifier))
			return null;

		// 1. Fixed whole-identifier forms (e.g. "Rollback" -> "RolledBack").
		if (PastTenseByVerb.TryGetValue(identifier, out var wholePast))
			return wholePast;

		var words = SplitPascalCase(identifier);
		if (words.Count == 0)
			return null;

		// 2. Single word: it's the verb. "Ship" -> "Shipped".
		if (words.Count == 1)
		{
			return PastTenseByVerb.TryGetValue(words[0], out var single) ? single : null;
		}

		var lastIndex = words.Count - 1;

		// 3. Modifier-prefix compound: inflect the LAST word, preserve word order.
		//    "ForceSave" -> "Force" + "Saved" = "ForceSaved".
		if (ModifierPrefixes.Contains(words[0]))
		{
			if (!PastTenseByVerb.TryGetValue(words[lastIndex], out var headPast))
				return null;

			words[lastIndex] = headPast;
			return string.Concat(words);
		}

		// 4. Default verb-object: FIRST word is the verb, remainder is the object.
		//    Reorder to object + past-verb. "CreateOrder" -> "Order" + "Created" = "OrderCreated".
		if (!PastTenseByVerb.TryGetValue(words[0], out var verbPast))
			return null;

		var sb = new System.Text.StringBuilder();
		for (var i = 1; i < words.Count; i++)
			sb.Append(words[i]); // object: "Order" (or multi-word "LineItem")
		sb.Append(verbPast); // past verb: "Created"

		return sb.ToString();
	}

	public static bool TrySuggestVerbPhrase(string methodName, out string suggestedMethodName)
	{
		if (methodName.StartsWith("New", StringComparison.Ordinal) && methodName.Length > 3)
		{
			var subject = methodName.Substring(3);
			suggestedMethodName = $"Register{subject}";
			return true;
		}

		suggestedMethodName = string.Empty;
		return false;
	}

	static bool TryCreatePropertySpecificEventName(string methodName, out string eventName)
	{
		foreach (var (prefix, suffix) in PropertySpecificPatterns)
		{
			if (!methodName.StartsWith(prefix, StringComparison.Ordinal))
				continue;

			var subject = methodName.Substring(prefix.Length);
			if (subject.Length == 0)
				continue;

			eventName = subject + suffix;
			return true;
		}

		eventName = string.Empty;
		return false;
	}

	static bool TryCreateVerbMappedEventName(
		string methodName,
		string aggregateClassName,
		out string eventName
	)
	{
		if (!TryGetVerbPrefix(methodName, out var verb, out var pastTense))
		{
			eventName = string.Empty;
			return false;
		}

		var subject = methodName.Substring(verb.Length);
		if (subject.Length == 0)
			subject = TrimAggregateSuffix(aggregateClassName);

		if (string.IsNullOrWhiteSpace(subject))
		{
			eventName = string.Empty;
			return false;
		}

		eventName = subject + pastTense;
		return true;
	}

	static bool TryGetVerbPrefix(string methodName, out string verb, out string pastTense)
	{
		foreach (var kvp in VerbPairsByPrefixLength)
		{
			if (!methodName.StartsWith(kvp.Key, StringComparison.Ordinal))
				continue;

			verb = kvp.Key;
			pastTense = kvp.Value;
			return true;
		}

		verb = string.Empty;
		pastTense = string.Empty;
		return false;
	}

	static bool TryGetPastTenseSuffix(string eventName, out string suffix)
	{
		foreach (var pastTense in PastTenseSuffixes)
		{
			if (!eventName.EndsWith(pastTense, StringComparison.Ordinal))
				continue;

			suffix = pastTense;
			return true;
		}

		suffix = string.Empty;
		return false;
	}

	static string TrimEventSuffix(string name) =>
		name.EndsWith("Event", StringComparison.Ordinal)
			? name.Substring(0, name.Length - "Event".Length)
			: name;

	static string TrimAggregateSuffix(string aggregateClassName) =>
		aggregateClassName.EndsWith("Aggregate", StringComparison.Ordinal)
			? aggregateClassName.Substring(0, aggregateClassName.Length - "Aggregate".Length)
			: aggregateClassName;
}
