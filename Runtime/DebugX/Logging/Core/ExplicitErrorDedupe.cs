using System;

namespace AetherNexus.FoundationPlatform.DebugX
{
	/// <summary>
	/// Suppresses a re-log of an exception that a caller has already explicitly logged (marked via
	/// <c>exception.Data[ExplicitlyLoggedKey] = true</c>) before it propagates further up the stack.
	/// Scoped strictly to the exact same exception instance/chain — not a message-text match, which
	/// would risk dropping unrelated future errors that merely share message text.
	/// </summary>
	internal static class ExplicitErrorDedupe
	{
		private const string ExplicitlyLoggedKey = "FoundationPlatform.DebugX.ExplicitlyLogged";

		internal static bool ShouldSkipErrorLog(Exception exception)
		{
			for (var current = exception; current != null; current = current.InnerException)
			{
				if (current.Data.Contains(ExplicitlyLoggedKey) && current.Data[ExplicitlyLoggedKey] is true)
				{
					return true;
				}
			}

			return false;
		}
	}
}
