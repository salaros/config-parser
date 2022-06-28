using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Salaros.Configuration.Logging;

namespace Salaros.Configuration
{
    public partial class ConfigParser : ConfigSectionBase
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        protected readonly ConfigSection fileHeader;
        protected readonly Dictionary<string, ConfigSection> sections;
        protected FileInfo fileInfo;

        public static readonly string[] InvalidConfigFileChars;

        #region Constructor

        /// <summary>
        /// Initializes the <see cref="ConfigParser"/> class.
        /// </summary>
        static ConfigParser()
        {
            InvalidConfigFileChars = new[] {"\r\n", "\n", "\r"}
                .Concat(Path.GetInvalidPathChars().Select(c => c.ToString()))
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigParser"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public ConfigParser(ConfigParserSettings settings = null)
        {
            Settings = settings ?? new ConfigParserSettings();
            NullSection = new NullConfigSection(this);

            fileHeader = new ConfigSection();
            sections = new Dictionary<string, ConfigSection>();
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigParser" /> class.
        /// </summary>
        /// <param name="configFile">The configuration file (may be path or file content).</param>
        /// <param name="settings">The settings.</param>
        /// <exception cref="ArgumentException">configFilePath</exception>
        public ConfigParser(string configFile, ConfigParserSettings settings = null)
            : this(settings)
        {
            if (string.IsNullOrWhiteSpace(configFile))
            {
                throw new ArgumentException($"{nameof(configFile)} must contain be a non-empty string.", nameof(configFile));
            }

            try
            {
                var file = configFile;
                if (!InvalidConfigFileChars.Any(c => file.Contains(c)))
                    fileInfo = new FileInfo(configFile);
            }
            finally
            {
                if (null != fileInfo)
                {
                    if (fileInfo.Exists)
                    {
                        Settings.Encoding = Settings.Encoding ?? fileInfo.GetEncoding(true);
                        Settings.NewLine = fileInfo.DetectNewLine(configFile);
                        configFile = File.ReadAllText(configFile, Settings.Encoding ?? Encoding.UTF8);
                    }
                    else
                        configFile = string.Empty;
                }
            }

            if (!string.IsNullOrWhiteSpace(configFile))
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

        /// <summary>
        /// Gets the sections.
        /// </summary>
        /// <value>
        /// The sections.
        /// </value>
        public override
#if NET40
        ReadOnlyCollection<ConfigSection> Sections => new ReadOnlyCollection<ConfigSection>(
#else
        IReadOnlyCollection<ConfigSection> Sections => new Collection<ConfigSection>(
#endif
            sections.Values.ToList());

        /// <summary>
        /// Gets configuration file's lines.
        /// </summary>
        /// <value>The lines.</value>
        public override
#if NET40
        ReadOnlyCollection<IConfigLine> Lines => new ReadOnlyCollection<IConfigLine>(
#else
        IReadOnlyCollection<IConfigLine> Lines => new Collection<IConfigLine>(
#endif
            fileHeader.Lines.Concat(sections.Values.SelectMany(s => s.Lines)).ToList());

        public NullConfigSection NullSection { get; }

        #endregion Properties

        #region Methods

        #region Indexing

        /// <summary>
        /// Gets the <see cref="ConfigSection"/> with the specified section name.
        /// </summary>
        /// <value>
        /// The <see cref="ConfigSection"/>.
        /// </value>
        /// <param name="sectionName">Name of the section.</param>
        /// <returns></returns>
        public ConfigSection this[string sectionName]
        {
            get
            {
                return (null == sectionName)
                    ? null
                    : sections
                          ?.FirstOrDefault(s => sectionName.Equals(s.Key, StringComparison.InvariantCultureIgnoreCase))
                          .Value ?? new ConfigSection(sectionName);
            }
        }

        #endregion Indexing

        /// <summary>
        /// Save configuration file's content.
        /// </summary>
        /// <param name="configFilePath">The configuration file path.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">configFilePath</exception>
        /// <exception cref="ConfigParserException">
        /// -1
        /// </exception>
        public bool Save(string configFilePath = null)
        {
            configFilePath ??= fileInfo?.FullName;

            if (string.IsNullOrWhiteSpace(configFilePath) || Path.GetInvalidPathChars().Any(configFilePath.Contains) ||
                Path.GetInvalidFileNameChars().Any(Path.GetFileName(configFilePath).Contains))
            {
                throw new ArgumentException($"{nameof(configFilePath)} must contain a valid path to a file",
                    nameof(configFilePath));
            }

            if (!string.IsNullOrWhiteSpace(configFilePath))
                fileInfo = new FileInfo(configFilePath);

            if (null == fileInfo)
                throw new InvalidOperationException(
                    "Configuration file cannot be saved, it doesn't have a file path assigned. Please specify one.");

            try
            {
                if (!(fileInfo.Directory?.Exists ?? false))
                    Directory.CreateDirectory(fileInfo.Directory?.FullName);
            }
            catch (Exception ex)
            {
                Logger?.Fatal(ex.Message);
                throw new ConfigParserException(
                    $"Failed to create parent directory for {nameof(ConfigParser)} file to the following file: '{fileInfo.FullName}'", -1, ex);
            }

            try
            {
                using (var fileWriter = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write))
                {
                    using var writer = new StreamWriter(
                        fileWriter,
                        Settings.Encoding ?? new UTF8Encoding(false, false)
                    );
                    var fileContent = ToString();
                    writer.Write(fileContent);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger?.Fatal(ex.Message);
                throw new ConfigParserException(
                    $"Failed to write {nameof(ConfigParser)} content to the following file: '{fileInfo.FullName}'", -1, ex);
            }
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Join(
                Settings.NewLine ?? Environment.NewLine,
                Lines.Select(l => l.ToString(Settings.MultiLineValues))
            );
        }

        #endregion Methods

        #region Helpers

        /// <summary>
        /// Reads the specified configuration content.
        /// </summary>
        /// <param name="configContent">Content of the configuration.</param>
        /// <exception cref="ArgumentException">configContent</exception>
        private void Read(string configContent)
        {
            if (string.IsNullOrWhiteSpace(configContent)) throw new ArgumentException(nameof(configContent));

            using var stringReader = new StringReader(configContent);
            string lineRaw;
            var lineNumber = 0;
            ConfigSection currentSection = null;
            ConfigLine currentLine = null;
            while (null != (lineRaw = stringReader.ReadLine()))
            {
                lineNumber++;

                switch (lineRaw)
                {
                    case var _ when string.IsNullOrWhiteSpace(lineRaw):
                        ReadEmptyLine(ref currentSection, ref currentLine, lineRaw, lineNumber);
                        continue;

                    case var _ when ConfigParserSettings.SectionMatcher.IsMatch(lineRaw):
                        ReadSection(ref currentSection, ref currentLine, lineRaw, lineNumber);
                        break;

                    case var _ when Settings.CommentMatcher.IsMatch(lineRaw):
                        ReadComment(ref currentSection, ref currentLine, lineRaw, lineNumber);
                        break;

                    case var _ when Settings.KeyMatcher.IsMatch(lineRaw):
                        if (!Settings.MultiLineValues.HasFlag(MultiLineValues.NotAllowed) && IsKeyLikeArrayValue(currentLine?.Content, lineRaw))
                            ReadKeyAndValue(ref currentSection, ref currentLine, lineRaw, lineNumber, append: true, forceIncludeKey: true);
                        else
                            ReadKeyAndValue(ref currentSection, ref currentLine, lineRaw, lineNumber);
                        break;

                    // Multi-line + allow value-less option on
                    case var _ when Settings.ValueMatcher.IsMatch(lineRaw) && currentLine != null &&
                                    Settings.KeyMatcher.IsMatch(currentLine.ToString()) &&
                                    Settings.MultiLineValues.HasFlag(MultiLineValues.AllowValuelessKeys) &&
                                    Settings.MultiLineValues.HasFlag(MultiLineValues.Simple) &&
                                    ConfigLine.IndentationMatcher.IsMatch(lineRaw) &&
                                    !Equals(currentLine.Indentation, ConfigLine.IndentationMatcher.Match(lineRaw).Value):
                        AppendValueToKey(ref currentSection, ref currentLine, lineRaw, lineNumber);
                        break;

                    case var _ when Settings.ValueMatcher.IsMatch(lineRaw):
                        if (Settings.MultiLineValues.HasFlag(MultiLineValues.AllowValuelessKeys))
                            ReadValuelessKey(ref currentSection, ref currentLine, lineRaw, lineNumber);
                        else
                            AppendValueToKey(ref currentSection, ref currentLine, lineRaw, lineNumber);
                        break;

                    default:
                        throw new ConfigParserException("Unknown element found!", lineNumber);
                }
            }

            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            if (null != currentSection)
                sections.Add(currentSection.SectionName, currentSection);
        }

        private bool IsKeyLikeArrayValue(string currentLine, string lineRaw)
        {
            if (string.IsNullOrWhiteSpace(currentLine) || string.IsNullOrWhiteSpace(lineRaw))
                return false;

            var lastLine = currentLine?.Split(new string[] { Settings.NewLine }, StringSplitOptions.None)?.LastOrDefault();
            var prevLineMatch = Settings.ArrayStartMatcher.Match(lastLine);
            var curLineMatch = Settings.ArrayStartMatcher.Match(lineRaw);

            if (!prevLineMatch.Success || !curLineMatch.Success)
                return false;

            return prevLineMatch.Groups[0].Value.Length == curLineMatch.Groups[0].Value.Length;
        }

        /// <summary>
        /// Reads the valueless key.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        private void ReadValuelessKey(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw,
            int lineNumber)
        {
            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            currentLine = new ConfigKeyValue<object>(lineRaw, Settings.KeyValueSeparator, null, lineNumber);
        }

        /// <summary>
        /// Appends the value to key.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void AppendValueToKey(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw,
            int lineNumber)
        {
            if (MultiLineValues.NotAllowed == Settings.MultiLineValues ||
                Settings.MultiLineValues.HasFlag(MultiLineValues.NotAllowed))
            {
                throw new ConfigParserException(
                    "Multi-line values are explicitly disallowed by parser settings. Please consider changing them.",
                    lineNumber);
            }

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
        private void ReadKeyAndValue(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw,
            int lineNumber, bool append = false, bool forceIncludeKey = false)
        {
            if (null != currentLine && !append)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            if (append && null == currentLine)
                throw new ConfigParserException("You are trying to append value to a null line!", lineNumber);

            var keyMatch = Settings.KeyMatcher.Match(lineRaw);
            var keyName = keyMatch.Groups["key"]?.Value;
            var separator = (string.IsNullOrWhiteSpace(keyMatch.Groups["separator"]?.Value))
                ? Settings.KeyValueSeparator
                : keyMatch.Groups["separator"]?.Value;

            if (!forceIncludeKey && keyMatch.Success && keyMatch.Captures.Count > 0)
                lineRaw = lineRaw.Substring(keyMatch.Captures[0].Value.Length);

            var valueMatch = Settings.ValueMatcher.Match(lineRaw);
            var value = valueMatch.Groups["value"]?.Value;

            switch (Settings.MultiLineValues)
            {
                case var _ when Settings.MultiLineValues.HasFlag(MultiLineValues.QuoteDelimitedValues):
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (Equals('"', value.First()))
                            value = value.Substring(1);
                        if (Equals('"', value.Last()))
                            value = value.Remove(value.Length - 1);
                    }
                    break;

                case var _ when Settings.MultiLineValues.HasFlag(MultiLineValues.NotAllowed) ||
                                Settings.MultiLineValues.HasFlag(MultiLineValues.Simple):
                    // Do nothing add with quotes if any
                    break;

                default:
                    throw new ConfigParserException("Unknown key=value situation detected!", lineNumber);
            }

            try
            {
                if (append)
                    currentLine.Content = $"{currentLine.Content}{Settings.NewLine}{value}";
                else
                    currentLine = new ConfigKeyValue<object>(keyName, separator, value, lineNumber);
            }
            catch (Exception ex)
            {
                throw new ConfigParserException($"Failed to parse the following line: '{lineRaw}'", lineNumber, ex);
            }
        }

        /// <summary>
        /// Reads the comment.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        private void ReadComment(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw,
            int lineNumber)
        {
            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            var commentMatch = Settings.CommentMatcher.Match(lineRaw);
            var delimiter = commentMatch.Groups["delimiter"]?.Value;
            var comment = commentMatch.Groups["comment"]?.Value;
            currentLine = new ConfigComment(delimiter, comment, lineNumber);
        }

        /// <summary>
        /// Reads the section.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        private void ReadSection(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw,
            int lineNumber)
        {
            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            if (null != currentSection)
                sections.Add(currentSection.SectionName, currentSection);

            var sectionMatch = ConfigParserSettings.SectionMatcher.Match(lineRaw);
            var sectionName = sectionMatch.Groups["name"]?.Value;
            var indentation = sectionMatch.Groups["indentation"]?.Value;
            var comment = sectionMatch.Groups["comment"]?.Value;
            currentSection = new ConfigSection(sectionName, lineNumber, indentation, comment);
        }

        /// <summary>
        /// Reads the empty line.
        /// </summary>
        /// <param name="currentLine">The current line.</param>
        /// <param name="currentSection">The current section.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <exception cref="ConfigParserException"></exception>
        private void ReadEmptyLine(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw,
            int lineNumber)
        {
            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            currentLine = new ConfigLine(lineNumber, lineRaw);
        }

        /// <summary>
        /// Backups the current line.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <exception cref="ConfigParserException">This key value pair is orphan, all the keys must be preceded by a section.</exception>
        private void BackupCurrentLine(ref ConfigSection currentSection, ref ConfigLine currentLine, int lineNumber)
        {
            if (null == currentSection)
            {
                if (currentLine is IConfigKeyValue &&
                    !Settings.MultiLineValues.HasFlag(MultiLineValues.AllowEmptyTopSection))
                    throw new ConfigParserException(
                        "This key value pair is orphan, all the keys must be preceded by a section.", lineNumber);

                fileHeader.AddLine(currentLine);
                currentLine = null;
                return;
            }
            currentSection.AddLine(currentLine);
            currentLine = null;
        }

        /// <summary>
        /// Decode byte array
        /// </summary>
        /// <param name="value">String value to decode</param>
        /// <returns></returns>
        private static byte[] DecodeByteArray(string value)
        {
            if (value == null)
                return null;

            var l = value.Length;
            if (l < 2)
                return new byte[] { };

            l /= 2;
            var result = new byte[l];
            for (var i = 0; i < l; i++)
                result[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            return result;
        }

        /// <summary>
        /// Encode byte array
        /// </summary>
        /// <param name="value">Byte array value to encode.</param>
        /// <returns></returns>
        private static string EncodeByteArray(byte[] value)
        {
            if (value == null)
                return null;

            var sb = new StringBuilder();
            foreach (var b in value)
            {
                var hex = Convert.ToString(b, 16);
                var l = hex.Length;
                if (l > 2)
                {
                    sb.Append(hex.Substring(l - 2, 2));
                }
                else
                {
                    if (l < 2)
                        sb.Append("0");
                    sb.Append(hex);
                }
            }
            return sb.ToString();
        }

        #endregion Helpers
    }
}
