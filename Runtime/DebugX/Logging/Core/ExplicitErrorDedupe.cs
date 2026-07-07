using System;
using System.Collections.Generic;

namespace FoundationPlatform.DebugX
{
	internal static class ExplicitErrorDedupe
	{
		private const string ExplicitlyLoggedKey = "DebugXLogging.ExplicitlyLogged";

		[ThreadStatic]
		private static HashSet<string> s_failureMessages;

		internal static bool ShouldSkipErrorLog(Exception exception)
		{
			for (var current = exception; current != null; current = current.InnerException)
			{
				if (current.Data.Contains(ExplicitlyLoggedKey) && current.Data[ExplicitlyLoggedKey] is true)
				{
					return true;
				}

				if (s_failureMessages != null && s_failureMessages.Contains(current.Message))
				{
					return true;
				}
			}

			return false;
		}

		internal static void RegisterExplicitFailure(LogProperty[] properties)
		{
			var message = ExtractFailureMessage(properties);
			if (string.IsNullOrEmpty(message))
			{
				return;
			}

			s_failureMessages ??= new HashSet<string>();
			s_failureMessages.Add(message);
		}

		private static string ExtractFailureMessage(LogProperty[] properties)
		{
			if (properties == null || properties.Length == 0)
			{
				return null;
			}

			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].Key == "Message" && properties[i].Value is string message)
				{
					return message;
				}
			}

			if (properties.Length == 1 && properties[0].Value is string singleMessage)
			{
				return singleMessage;
			}

			return null;
		}
	}
}
