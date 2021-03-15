using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Salaros.Configuration
{
    public class ConfigParserSettings
    {
        protected Regex keyMatcher, commentMatcher, valueMatcher, arrayStartMatcher;

        /// <summary>
        /// Initializes the <see cref="ConfigParserSettings"/> class.
        /// </summary>
        static ConfigParserSettings()
        {
            SectionMatcher = new Regex(@"^(?<indentation>(\s+)?)\[(?<name>.*?)\](?<comment>.*)$", RegexOptions.Compiled);
        }

        /// <summary>
        /// Gets the culture used for reading boolean, decimal values etc.
        /// </summary>
        /// <value>
        /// The culture used for reading boolean, decimal values etc.
        /// </value>
        public CultureInfo Culture { get; set; } = Thread.CurrentThread.CurrentCulture;

        /// <summary>
        /// Gets the multi-line value-related settings.
        /// </summary>
        /// <value>
        /// The multi-line value-related settings.
        /// </value>
        public MultiLineValues MultiLineValues { get; set; } = MultiLineValues.NotAllowed;

        /// <summary>
        /// Gets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        public Encoding Encoding { get; set; } = null;

        /// <summary>
        /// Gets or sets the boolean converter.
        /// </summary>
        /// <value>
        /// The boolean converter.
        /// </value>
        public BooleanConverter BooleanConverter { get; set; } = null;

        /// <summary>
        /// Gets the new line string.
        /// </summary>
        /// <value>
        /// The new line string.
        /// </value>
        public string NewLine { get; set; } = Environment.NewLine;

        /// <summary>
        /// Gets the key value separator.
        /// </summary>
        /// <value>
        /// The key value separator.
        /// </value>
        public string KeyValueSeparator { get; set; } = "=";

        /// <summary>
        /// Gets the comment characters.
        /// </summary>
        /// <value>
        /// The comment characters.
        /// </value>
        public string[] CommentCharacters { get; set; } = { "#", ";" };

        /// <summary>
        /// Gets the section matcher.
        /// </summary>
        /// <value>
        /// The section matcher.
        /// </value>
        internal static Regex SectionMatcher { get; set; }

        /// <summary>Gets the array value line matcher (matches the whitespaces at the beggining of each array value).</summary>
        /// <value>The array start matcher.</value>
        internal Regex ArrayStartMatcher => arrayStartMatcher ?? (arrayStartMatcher =
                    new Regex(@"^(\s{1,})", RegexOptions.Compiled));

        /// <summary>
        /// Gets the comment matcher.
        /// </summary>
        /// <value>
        /// The comment matcher.
        /// </value>
        internal Regex CommentMatcher => commentMatcher ?? (commentMatcher =
                    new Regex(
                        $@"^(?<delimiter>(\s+)?({string.Join("|", CommentCharacters.Select(c => c.ToString()))})+(\s+)?)(?<comment>(\s+)?.*?)$",
                        RegexOptions.Compiled));

        /// <summary>
        /// Gets the key matcher.
        /// </summary>
        /// <value>
        /// The key matcher.
        /// </value>
        internal Regex KeyMatcher => keyMatcher ?? (keyMatcher =
                                         new Regex($@"^(?<key>.*?)(?<separator>(\s+)?{Regex.Escape(KeyValueSeparator)}(\s+)?)",
                                             RegexOptions.Compiled));

        /// <summary>
        /// Gets the value matcher.
        /// </summary>
        /// <value>
        /// The value matcher.
        /// </value>
        internal Regex ValueMatcher => valueMatcher ?? (valueMatcher = MultiLineValues.HasFlag(MultiLineValues.QuoteDelimitedValues)
                ? new Regex(@"^(?<quote1>\"")?(?<value>[^\""]+)(?<quote2>\"")?(\s+)?$", RegexOptions.Compiled)
                : new Regex(@"^(?<value>.*?)?$", RegexOptions.Compiled));
    }

    /// <summary>
    /// Flags / settings for handling multi-line values
    /// </summary>
    [Flags]
    public enum MultiLineValues
    {
        Simple = 0,
        NotAllowed = 1,
        QuoteDelimitedValues = 2,
        AllowValuelessKeys = 4,
        AllowEmptyTopSection = 8
    }
}
