using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Salaros.Config
{
    public class ConfigParserSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigParserSettings"/> class.
        /// </summary>
        /// <param name="multiLineValues">The multi line values.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="keyValueSeparator">The key value separator.</param>
        /// <param name="commentCharacters">The comment characters.</param>
        public ConfigParserSettings(
            MultiLineValues multiLineValues = MultiLineValues.NotAllowed,
            Encoding encoding = null,
            string keyValueSeparator = null,
            string[] commentCharacters = null
        )
        {
            MultiLineValues = multiLineValues;
            Encoding = encoding;
            KeyValueSeparator = keyValueSeparator ?? "=";
            CommentCharacters = commentCharacters ?? new [] { "#", ";" };

            SectionMatcher = new Regex(@"^(\s+)?\[(?<name>.*?)\].*$", RegexOptions.Compiled);
            KeyMatcher = new Regex($@"^(?<key>.*?)(?<separator>(\s+)?{Regex.Escape(KeyValueSeparator)}(\s+)?)", RegexOptions.Compiled);
            CommentMatcher = new Regex(
                $@"^(?<delimiter>(\s+)?({string.Join("|", CommentCharacters.Select(c => c.ToString()))})+(\s+)?)(?<comment>(\s+)?.*?)$",
                RegexOptions.Compiled);
            ValueMatcher = (multiLineValues.HasFlag(MultiLineValues.OnlyDelimited))
                ? new Regex(@"^(?<quote1>\"")?(?<value>[^\""]+)(?<quote2>\"")?(\s+)?$", RegexOptions.Compiled)
                : new Regex(@"^(?<value>.*?)?$", RegexOptions.Compiled);
        }

        /// <summary>
        /// Gets the multi-line value-related settings.
        /// </summary>
        /// <value>
        /// The multi-line value-related settings.
        /// </value>
        public MultiLineValues MultiLineValues { get; }

        /// <summary>
        /// Gets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        public Encoding Encoding { get; internal set; }

        /// <summary>
        /// Gets the new line string.
        /// </summary>
        /// <value>
        /// The new line string.
        /// </value>
        public string NewLine { get; internal set; } = Environment.NewLine;

        /// <summary>
        /// Gets the key value separator.
        /// </summary>
        /// <value>
        /// The key value separator.
        /// </value>
        public string KeyValueSeparator { get; }

        /// <summary>
        /// Gets the comment characters.
        /// </summary>
        /// <value>
        /// The comment characters.
        /// </value>
        public string[] CommentCharacters { get; }

        /// <summary>
        /// Gets the section matcher.
        /// </summary>
        /// <value>
        /// The section matcher.
        /// </value>
        internal Regex SectionMatcher { get; }

        /// <summary>
        /// Gets the comment matcher.
        /// </summary>
        /// <value>
        /// The comment matcher.
        /// </value>
        internal Regex CommentMatcher { get; }

        /// <summary>
        /// Gets the key matcher.
        /// </summary>
        /// <value>
        /// The key matcher.
        /// </value>
        internal Regex KeyMatcher { get; }

        /// <summary>
        /// Gets the value matcher.
        /// </summary>
        /// <value>
        /// The value matcher.
        /// </value>
        internal Regex ValueMatcher { get; }
    }

    /// <summary>
    /// Flags / settings for handling multi-line values
    /// </summary>
    [Flags]
    public enum MultiLineValues
    {
        Simple = 0,
        NotAllowed = 1,
        OnlyDelimited = 2,
        AllowValuelessKeys = 4,
        AllowEmptyTopSection = 8
    }
}
