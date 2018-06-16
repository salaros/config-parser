using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Salaros.Config.Logging;

namespace Salaros.Config
{
    public class ConfigParser
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        protected List<IniLine> fileHeader = new List<IniLine>();
        protected Dictionary<string, IniSection> sections = new Dictionary<string, IniSection>();

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigParser"/> class.
        /// </summary>
        /// <param name="configFile">The configuration file.</param>
        /// <param name="settings">The settings.</param>
        /// <exception cref="ArgumentException">configFile</exception>
        public ConfigParser(string configFile, ConfigParserSettings settings)
        {
            if (string.IsNullOrWhiteSpace(configFile)) throw new ArgumentException(nameof(configFile));

            Settings = settings ?? new ConfigParserSettings();

            if (File.Exists(configFile))
            {
                var configFileInfo = new FileInfo(configFile);
                Settings.Encoding = Settings.Encoding ?? configFileInfo.GetEncoding();
                Settings.NewLine = configFileInfo.DetectNewLine(configFile);
            }

            configFile = File.ReadAllText(configFile, Settings.Encoding);
            Read(configFile);
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public ConfigParserSettings Settings { get; }

        #endregion Properties

        #region Helpers

        /// <summary>
        /// Reads the specified configuration content.
        /// </summary>
        /// <param name="configContent">Content of the configuration.</param>
        /// <exception cref="ArgumentException">configContent</exception>
        private void Read(string configContent)
        {
            if (string.IsNullOrWhiteSpace(configContent)) throw new ArgumentException(nameof(configContent));

            using (var stringReader = new StringReader(configContent))
            {
                string lineRaw;
                var lineNumber = 0;
                IniSection currentSection = null;
                IniLine currentLine = null;
                while (null != (lineRaw = stringReader.ReadLine()))
                {
                    lineNumber++;

                    switch (lineRaw)
                    {
                        case var _ when string.IsNullOrEmpty(lineRaw):
                            ReadEmptyLine(ref currentSection, ref currentLine, lineRaw, lineNumber);
                            continue;

                        case var _ when Settings.SectionMatcher.IsMatch(lineRaw):
                            ReadSection(ref currentSection, lineRaw, lineNumber);
                            break;

                        case var _ when Settings.CommentMatcher.IsMatch(lineRaw):
                            ReadComment(ref currentSection, ref currentLine, lineRaw, lineNumber);
                            break;

                        case var _ when Settings.KeyMatcher.IsMatch(lineRaw):
                            ReadKeyAndValue(ref currentSection, ref currentLine, lineRaw, lineNumber);
                            break;

                        case var _ when Settings.ValueMatcher.IsMatch(lineRaw):
                            AppendValueToKey(ref currentSection, ref currentLine, lineRaw, lineNumber);
                            break;

                        default:
                            throw new ConfigParserException($"Unknown element at line {lineNumber}", lineNumber);
                    }
                }
            }
        }

        /// <summary>
        /// Appends the value to key.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void AppendValueToKey(ref IniSection currentSection, ref IniLine currentLine, string lineRaw, int lineNumber)
        {
            if (MultuLineValues.NotAllowed == Settings.MultuLineValues || Settings.MultuLineValues.HasFlag(MultuLineValues.NotAllowed))
                throw new ConfigParserException(
                    "Multi-line values are explicitly disallowed by parser settings. Please consider changing them.", lineNumber);

            ReadKeyAndValue(ref currentSection, ref currentLine, lineRaw, lineNumber, true);
        }

        /// <summary>
        /// Reads the key and value.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="append">if set to <c>true</c> [append].</param>
        /// <exception cref="ConfigParserException">Arrays must start from a new line and not after the key!
        /// or</exception>
        /// <exception cref="NotImplementedException"></exception>
        private void ReadKeyAndValue(ref IniSection currentSection, ref IniLine currentLine, string lineRaw, int lineNumber, bool append = false)
        {
            if (null != currentLine && !append)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            if (append && null == currentLine)
                throw new ConfigParserException($"You are trying to append value to a null line!", lineNumber);

            var keyMatch = Settings.KeyMatcher.Match(lineRaw);
            var keyName = keyMatch.Groups["key"]?.Value;
            if (keyMatch.Success && keyMatch.Captures.Count > 0)
                lineRaw = lineRaw.Substring(keyMatch.Captures[0].Value.Length);

            var valueMatch = Settings.ValueMatcher.Match(lineRaw);
            var quote1 = valueMatch.Groups["quote1"]?.Value;
            var quote2 = valueMatch.Groups["quote2"]?.Value;
            var value = valueMatch.Groups["value"]?.Value;

            switch (Settings.MultuLineValues)
            {
                case var _ when Settings.MultuLineValues.HasFlag(MultuLineValues.Arrays):
                    if (!string.IsNullOrWhiteSpace(value?.Trim()))
                        throw new ConfigParserException("Arrays must start from a new line and not after the key!", lineNumber);
                    break;

                case var _ when Settings.MultuLineValues.HasFlag(MultuLineValues.NotAllowed) ||
                                Settings.MultuLineValues.HasFlag(MultuLineValues.Simple):
                    // Do nothing add with quotes if any
                    break;

                case var _ when Settings.MultuLineValues.HasFlag(MultuLineValues.OnlyDelimited):
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (Equals("\"", quote1) || Equals('"', value.First()))
                            value = value.Substring(1);
                        if (Equals("\"", quote2) || Equals('"', value.Last()))
                            value = value.Remove(value.Length - 1);
                    }
                    break;

                default:
                    throw new ConfigParserException($"Unknown key=value situation on {lineNumber} line", lineNumber);
            }

            if (append)
                currentLine.Content = $"{currentLine.Content}{Settings.NewLine}{value}";
            else
                currentLine = new IniKeyValue(keyName, value, lineNumber);
        }

        /// <summary>
        /// Reads the comment.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        private void ReadComment(ref IniSection currentSection, ref IniLine currentLine, string lineRaw, int lineNumber)
        {
            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            var commentMatch = Settings.CommentMatcher.Match(lineRaw);
            var delimiter = commentMatch.Groups["delimiter"]?.Value;
            var comment = commentMatch.Groups["comment"]?.Value;
            currentLine = new IniComment(delimiter, comment, lineNumber);

            if (null != currentSection)
            {
                currentSection.AddLine(currentLine);
                return;
            }

            fileHeader.Add(currentLine);
        }

        /// <summary>
        /// Reads the section.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        private void ReadSection(ref IniSection currentSection, string lineRaw, int lineNumber)
        {
            if (null != currentSection)
                sections.Add(currentSection.SectionName, currentSection);

            var sectionName = Settings.SectionMatcher.Match(lineRaw).Groups["name"]?.Value;
            currentSection = new IniSection(sectionName, lineNumber);
        }

        /// <summary>
        /// Reads the empty line.
        /// </summary>
        /// <param name="currentLine">The current line.</param>
        /// <param name="currentSection">The current section.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <exception cref="ConfigParserException"></exception>
        private void ReadEmptyLine(ref IniSection currentSection, ref IniLine currentLine, string lineRaw, int lineNumber)
        {
            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            currentLine = new IniLine(lineNumber, lineRaw);
            if (null == currentSection)
            {
                fileHeader.Add(currentLine);
                return;
            }
            currentSection.AddLine(currentLine);
        }

        /// <summary>
        /// Backups the current line.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <exception cref="ConfigParserException">This key value pair is orphan, all the keys must be preceded by a section.</exception>
        private void BackupCurrentLine(ref IniSection currentSection, ref IniLine currentLine, int lineNumber)
        {
            if (null == currentSection)
            {
                if (currentLine is IniKeyValue)
                    throw new ConfigParserException(
                        "This key value pair is orphan, all the keys must be preceded by a section.", lineNumber);

                fileHeader.Add(currentLine);
                currentLine = null;
                return;
            }
            currentSection.AddLine(currentLine);
            currentLine = null;
        }

        #endregion Helpers
    }
}
