using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Salaros.Config
{
    public class ConfigParserSettings
    {
        public ConfigParserSettings(
            MultuLineValues multiLineValues = MultuLineValues.NotAllowed,
            Encoding encoding = null,
            char keyValueSeparator = '=',
            string[] commentCharacters = null
        )
        {
            MultuLineValues = multiLineValues;
            Encoding = encoding;
            KeyValueSeparator = keyValueSeparator;
            CommentCharacters = commentCharacters ?? new [] { "#", ";" };

            SectionMatcher = new Regex(@"^(\s+)?\[(?<name>.*?)\].*$", RegexOptions.Compiled);
            KeyMatcher = new Regex(@"^(?<key>.*?)(\s+)?=(\s+)?", RegexOptions.Compiled);
            CommentMatcher = new Regex(
                $@"^(\s+)?(?<delimiter>({string.Join("|", CommentCharacters.Select(c => c.ToString()))})+)(\s+)?(?<comment>.*?)$",
                RegexOptions.Compiled);
            ValueMatcher = (multiLineValues.HasFlag(MultuLineValues.OnlyDelimited))
                ? new Regex(@"^(?<quote1>\"")?(?<value>[^\""]+)(?<quote2>\"")?(\s+)?$", RegexOptions.Compiled)
                : new Regex(@"^(?<value>.*?)?$", RegexOptions.Compiled);
        }

        public MultuLineValues MultuLineValues { get; }

        public Encoding Encoding { get; internal set; }

        public string NewLine { get; internal set; } = Environment.NewLine;

        public char KeyValueSeparator { get; }

        public string[] CommentCharacters { get; }

        internal Regex SectionMatcher { get; }

        internal Regex CommentMatcher { get; }

        internal Regex KeyMatcher { get; }

        internal Regex ValueMatcher { get; }
    }

    [Flags]
    public enum MultuLineValues
    {
        Simple = 0,
        NotAllowed = 1,
        OnlyDelimited = 2,
        Arrays = 4,
    }
}
