namespace Purview.EventSourcing.SourceGenerator.Common;

static partial class EventVerbMap
{
	// Identifiers with more PascalCase words than this fall back to a heap array
	// for the word-offset table. Real method names are far shorter.
	const int MaxStackWords = 32;

	/// <summary>
	/// If <see langword="true" />, a bare verb ("Create") keeps its bare past tense ("Created").
	/// If <see langword="false" />, the past tense is qualified with the aggregate name
	/// ("Create" on OrderAggregate -> "OrderCreated").
	/// </summary>
	/// <remarks>
	/// <b>WARNING:</b> event type names are persisted discriminators. Changing them is a
	/// breaking change for existing stores, and qualifying can collide two methods
	/// onto one name (Create() and CreateOrder() -> both "OrderCreated").
	/// </remarks>
	public static bool QualifyBareVerbWithAggregate { get; set; }

	// -------------------------------------------------------------------------
	// Public API
	// -------------------------------------------------------------------------

	/// <summary>Exact verb -> past-tense lookup. O(1).</summary>
	public static bool TryGetPastTense(string verb, out string pastTense)
	{
		if (PastTenseByVerb.TryGetValue(verb, out var value))
		{
			pastTense = value;
			return true;
		}

		pastTense = string.Empty;
		return false;
	}

	/// <summary>
	/// Converts a command-style method name into its past-tense event name.
	/// Verb-object:     "CreateOrder"    -> "OrderCreated"
	/// Multi-word verb: "SignInUser"     -> "UserSignedIn"
	/// Single verb:     "Ship"           -> "Shipped"
	/// Modifier prefix: "ForceSave"      -> "ForceSaved"
	/// Irregular whole: "Rollback"       -> "RolledBack"
	/// </summary>
	public static bool TryCreateGeneratedEventName(
		string methodName,
		string aggregateClassName,
		out string eventName
	)
	{
		if (!TryResolve(methodName, out var modifier, out var objectPart, out var pastTense))
		{
			eventName = string.Empty;
			return false;
		}

		if (objectPart.Length == 0)
			objectPart = ResolveBareSubject(aggregateClassName);

		eventName = string.Concat(modifier, objectPart, pastTense);
		return true;
	}

	public static bool IsVerbPhrase(string methodName) =>
		TryResolve(methodName, out _, out _, out _);

	public static bool IsPastTenseEventName(string eventName)
	{
		var core = TrimEventSuffix(eventName);
		return !string.IsNullOrWhiteSpace(core) && IsPastTenseCore(core);
	}

	public static bool TryGetPastTenseEventNameCore(string eventName, out string coreName)
	{
		coreName = TrimEventSuffix(eventName);
		return !string.IsNullOrWhiteSpace(coreName) && IsPastTenseCore(coreName);
	}

	public static bool TrySuggestVerbPhrase(string methodName, out string suggestedMethodName)
	{
		if (methodName.Length > 3 && methodName.StartsWith("New", StringComparison.Ordinal))
		{
			suggestedMethodName = "Register" + methodName.Substring(3);
			return true;
		}

		suggestedMethodName = string.Empty;
		return false;
	}

	// -------------------------------------------------------------------------
	// Core resolver
	// -------------------------------------------------------------------------

	/// <summary>
	/// Resolves the (optional) modifier, object, and past-tense verb from a
	/// command identifier. The verb is the LONGEST leading whole-word span that
	/// is a known verb, so multi-word verbs ("SignIn") beat their prefixes
	/// ("Sign"), and sub-word matches ("Set" inside "Settle") are impossible
	/// because spans always land on PascalCase boundaries.
	/// </summary>
	static bool TryResolve(
		string identifier,
		out string modifier,
		out string objectPart,
		out string pastTense
	)
	{
		modifier = string.Empty;
		objectPart = string.Empty;
		pastTense = string.Empty;

		if (string.IsNullOrEmpty(identifier))
			return false;

		var wordCount = CountWords(identifier);

		// starts[i] = start index of word i; starts[wordCount] = identifier.Length.
		var starts =
			wordCount <= MaxStackWords ? stackalloc int[MaxStackWords + 1] : new int[wordCount + 1];
		FillWordStarts(identifier, starts);

		// A modifier can only occupy the FIRST word; the verb search starts after it.
		var verbWord = 0;
		if (wordCount >= 2)
		{
			var firstWord = identifier.Substring(0, starts[1]);
			if (ModifierPrefixes.Contains(firstWord))
			{
				modifier = firstWord;
				verbWord = 1;
			}
		}

		var verbStart = starts[verbWord];

		// Greedy: try the longest leading whole-word span first, shrinking by a
		// word each iteration. Length bounds prune spans that can't be verbs.
		for (var k = wordCount; k > verbWord; k--)
		{
			var verbLen = starts[k] - verbStart;
			if (verbLen > MaxVerbLength)
				continue;
			if (verbLen < MinVerbLength)
				break; // spans only get shorter from here — nothing left to match

			var candidate = identifier.Substring(verbStart, verbLen);
			if (!PastTenseByVerb.TryGetValue(candidate, out var past))
				continue;

			pastTense = past;
			var verbEnd = starts[k];
			objectPart = verbEnd < identifier.Length ? identifier.Substring(verbEnd) : string.Empty;
			return true;
		}

		// No verb resolved — don't leak a matched modifier to the caller.
		modifier = string.Empty;
		return false;
	}

	// -------------------------------------------------------------------------
	// Validation
	// -------------------------------------------------------------------------

	/// <summary>Operates on an already-suffix-trimmed core name (no re-trim).</summary>
	static bool IsPastTenseCore(string core)
	{
		var lastStart = LastWordStart(core);
		var lastWord = lastStart == 0 ? core : core.Substring(lastStart);
		return KnownPastTenseForms.Contains(lastWord);
	}

	// -------------------------------------------------------------------------
	// PascalCase scanning (regex-free, allocation-free)
	//
	// Boundaries match the original pattern exactly (ASCII):
	//   (?<=[a-z0-9])(?=[A-Z]) | (?<=[A-Z])(?=[A-Z][a-z])
	// i.e. lower/digit -> Upper, and Upper -> Upper-followed-by-lower ("XMLParse").
	// -------------------------------------------------------------------------

	static int CountWords(string s)
	{
		var count = 1;
		for (var i = 1; i < s.Length; i++)
		{
			if (IsWordBoundary(s, i))
				count++;
		}

		return count;
	}

	static void FillWordStarts(string s, Span<int> starts)
	{
		starts[0] = 0;
		var w = 1;
		for (var i = 1; i < s.Length; i++)
		{
			if (IsWordBoundary(s, i))
				starts[w++] = i;
		}

		starts[w] = s.Length; // sentinel at index == wordCount
	}

	static int LastWordStart(string s)
	{
		var last = 0;
		for (var i = 1; i < s.Length; i++)
		{
			if (IsWordBoundary(s, i))
				last = i;
		}

		return last;
	}

	static bool IsWordBoundary(string s, int i)
	{
		var prev = s[i - 1];
		var cur = s[i];

		// lower/digit -> Upper   ("forceSave" splits before 'S')
		if ((IsAsciiLower(prev) || IsAsciiDigit(prev)) && IsAsciiUpper(cur))
			return true;

		// Upper -> Upper followed by lower   ("XMLParse" -> "XML" | "Parse")
		if (IsAsciiUpper(prev) && IsAsciiUpper(cur) && i + 1 < s.Length && IsAsciiLower(s[i + 1]))
			return true;

		// No boundary here
		return false;
	}

	static bool IsAsciiUpper(char c) => c is >= 'A' and <= 'Z';

	static bool IsAsciiLower(char c) => c is >= 'a' and <= 'z';

	static bool IsAsciiDigit(char c) => c is >= '0' and <= '9';

	// -------------------------------------------------------------------------
	// Trimming helpers
	// -------------------------------------------------------------------------
	static string ResolveBareSubject(string aggregateClassName) =>
		QualifyBareVerbWithAggregate ? TrimAggregateSuffix(aggregateClassName) : string.Empty;

	static string TrimEventSuffix(string name) =>
		name.EndsWith("Event", StringComparison.Ordinal)
			? name.Substring(0, name.Length - "Event".Length)
			: name;

	static string TrimAggregateSuffix(string aggregateClassName) =>
		aggregateClassName.EndsWith("Aggregate", StringComparison.Ordinal)
			? aggregateClassName.Substring(0, aggregateClassName.Length - "Aggregate".Length)
			: aggregateClassName;
}
