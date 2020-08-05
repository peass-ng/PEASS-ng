using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Represents a wildcard running on the
	/// <see cref="System.Text.RegularExpressions"/> engine.
	/// </summary>
	public class Wildcard : Regex
	{
		/// <summary>
		/// Initializes a wildcard with the given search pattern and options.
		/// </summary>
		/// <param name="pattern">The wildcard pattern to match.</param>
		/// <param name="options">A combination of one or more <see cref="System.Text.RegularExpressions.RegexOptions"/>.</param>
		public Wildcard([NotNull] string pattern, RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
			: base(WildcardToRegex(pattern), options)
		{
		}

		/// <summary>
		/// Converts a wildcard to a regular expression.
		/// </summary>
		/// <param name="pattern">The wildcard pattern to convert.</param>
		/// <returns>A regular expression equivalent of the given wildcard.</returns>
		public static string WildcardToRegex([NotNull] string pattern)
		{
			string s = Regex.Escape(pattern);
			s = Regex.Replace(s, @"(?<!\\)\\\*", @".*"); // Negative Lookbehind
			s = Regex.Replace(s, @"\\\\\\\*", @"\*");
			s = Regex.Replace(s, @"(?<!\\)\\\?", @".");  // Negative Lookbehind
			s = Regex.Replace(s, @"\\\\\\\?", @"\?");
			return string.Concat("^", Regex.Replace(s, @"\\\\\\\\", @"\\"), "$");
		}
	}
}
