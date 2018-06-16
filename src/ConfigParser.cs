using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Salaros.Config.Logging;

namespace Salaros.Config
{
    public class ConfigParser
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        protected readonly List<ConfigLine> fileHeader;
        protected readonly Dictionary<string, ConfigSection> sections;
        protected FileInfo fileInfo;

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

            fileHeader = new List<ConfigLine>();
            sections = new Dictionary<string, ConfigSection>();

            Settings = settings ?? new ConfigParserSettings();

            if (File.Exists(configFile))
            {
                fileInfo = new FileInfo(configFile);
                Settings.Encoding = Settings.Encoding ?? fileInfo.GetEncoding();
                Settings.NewLine = fileInfo.DetectNewLine(configFile);
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

#if NET40
        public ReadOnlyDictionary<string, ConfigSection> Sections => new ReadOnlyDictionary<string, ConfigSection>(sections);
#else
        public IReadOnlyDictionary<string, ConfigSection> Sections => sections;
#endif

        /// <summary>
        /// Gets configuration file's lines.
        /// </summary>
        /// <value>The lines.</value>
        public ReadOnlyCollection<IConfigLine> Lines
        {
            get
            {
                return new ReadOnlyCollection<IConfigLine>(fileHeader.Concat(sections.Values.SelectMany(s => s.Lines)).ToArray());
            }
        }

        #endregion Properties

        #region Methods

        #region GetValue

        /// <summary>
        /// Tries to get the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// sectionName
        /// or
        /// keyName
        /// </exception>
        internal virtual bool TryGetValue<T>(string sectionName, string keyName, out T value)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentNullException(nameof(sectionName));
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentNullException(nameof(keyName));

            value = default(T);

            if (!sections.TryGetValue(sectionName, out var section))
                return false;

            var key = section.Keys.FirstOrDefault(k => k.Key.Equals(keyName));
            if (key == null)
                return false;

            value = (T) key.ValueRaw;
            return true;
        }

        /// <summary>
        /// Gets the raw value of the key.
        /// </summary>
        /// <returns>The value raw.</returns>
        /// <param name="sectionName">Section name.</param>
        /// <param name="keyName">Key name.</param>
        /// <param name="defaultValue">Default value returned if the key with the given name does not exist.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        internal virtual T GetRawValue<T>(string sectionName, string keyName, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentNullException(nameof(sectionName));
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentNullException(nameof(keyName));

            var iniKey = new ConfigKeyValue<T>(keyName, defaultValue);

            if (!sections.TryGetValue(sectionName, out var section))
            {
                section = new ConfigSection(sectionName, Lines.Max(l => l.LineNumber));
                sections.Add(sectionName, section);
                section.AddLine(iniKey);
                return defaultValue;
            }

            var key = section.Keys.FirstOrDefault(k => k.Key.Equals(keyName));
            return (key == null)
                ? defaultValue
                : (T)key.ValueRaw;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public virtual string GetValue(string sectionName, string key, string defaultValue)
        {
            return GetRawValue(sectionName, key, defaultValue);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
        /// <returns></returns>
        public virtual bool GetValue(string sectionName, string key, bool defaultValue)
        {
            return GetRawValue(sectionName, key, (defaultValue ? "1" : "0")) == "1";
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public virtual int GetValue(string sectionName, string key, int defaultValue)
        {
            return GetRawValue(sectionName, key, defaultValue);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public virtual double GetValue(string sectionName, string key, double defaultValue)
        {
            return GetRawValue(sectionName, key, defaultValue);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public virtual byte[] GetValue(string sectionName, string key, byte[] defaultValue)
        {
            var stringValue = GetRawValue(sectionName, key, string.Empty);
            try
            {
                return (string.IsNullOrWhiteSpace(stringValue))
                    ? defaultValue
                    : DecodeByteArray(stringValue);
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message);
                return defaultValue;
            }
        }

        #endregion

        #region SetValue

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool SetValue(string sectionName, string keyName, string value)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentNullException(nameof(sectionName));
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentNullException(nameof(keyName));

            if (!sections.TryGetValue(sectionName, out var section))
            {
                section = new ConfigSection(sectionName, Lines.Max(l => l.LineNumber));
                sections.Add(sectionName, section);
            }

            if (section == null)
            {
                Logger?.Warn($"Failed to create {sectionName} and store {keyName}={value} key");
                return false;
            }

            var iniKey = section.Keys
                .FirstOrDefault(k => k.Key.Equals(keyName));

            if (iniKey != null)
            {
                iniKey.ValueRaw = value;
            }
            else
            {
                iniKey = new ConfigKeyValue<string>(keyName, value);
                section.AddLine((ConfigLine)iniKey);
            }
            return true;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns></returns>
        public virtual bool SetValue(string sectionName, string key, bool value)
        {
            return SetValue(sectionName, key, (value) ? "1" : "0");
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool SetValue(string sectionName, string key, int value)
        {
            return SetValue(sectionName, key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool SetValue(string sectionName, string key, double value)
        {
            return SetValue(sectionName, key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool SetValue(string sectionName, string key, byte[] value)
        {
            return SetValue(sectionName, key, EncodeByteArray(value));
        }

        #endregion

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
            if (string.IsNullOrWhiteSpace(configFilePath) || Path.GetInvalidPathChars().Any(configFilePath.Contains) ||
                Path.GetInvalidFileNameChars().Any(Path.GetFileName(configFilePath).Contains))
            {
                throw new ArgumentException($"{nameof(configFilePath)} must contain a valid path to a file", nameof(configFilePath));
            }

            if (!string.IsNullOrWhiteSpace(configFilePath))
                fileInfo = new FileInfo(configFilePath);

            if (null == fileInfo)
                throw new InvalidOperationException("Configuration file cannot be saved, it doesn't have a file path assigned. Please specify one.");

            var fileLines = new StringBuilder();
            foreach (var line in Lines.ToList())
                fileLines.AppendLine(line.ToString(Settings.MultuLineValues));

            try
            {
                using (var fileWriter = new FileStream(fileInfo.FullName, FileMode.Truncate, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(
                            fileWriter,
                            Encoding.UTF8,
                            4096
#if !NET40
                        , true
#endif
                        )
                    )
                    {
                        writer.Write(fileLines);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger?.Fatal(ex.Message);
                throw new ConfigParserException(
                    $"Failed to write IniParser content to the following file: '{fileInfo.FullName}'", -1, ex);
            }
        }

        #endregion

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
                ConfigSection currentSection = null;
                ConfigLine currentLine = null;
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

                if (null != currentLine)
                    BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

                if (null != currentSection)
                    sections.Add(currentSection.SectionName, currentSection);
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
        private void AppendValueToKey(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw, int lineNumber)
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
        private void ReadKeyAndValue(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw, int lineNumber, bool append = false)
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
                currentLine = new ConfigKeyValue<object>(keyName, value, lineNumber);
        }

        /// <summary>
        /// Reads the comment.
        /// </summary>
        /// <param name="currentSection">The current section.</param>
        /// <param name="currentLine">The current line.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        private void ReadComment(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw, int lineNumber)
        {
            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            var commentMatch = Settings.CommentMatcher.Match(lineRaw);
            var delimiter = commentMatch.Groups["delimiter"]?.Value;
            var comment = commentMatch.Groups["comment"]?.Value;
            currentLine = new ConfigComment(delimiter, comment, lineNumber);

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
        private void ReadSection(ref ConfigSection currentSection, string lineRaw, int lineNumber)
        {
            if (null != currentSection)
                sections.Add(currentSection.SectionName, currentSection);

            var sectionName = Settings.SectionMatcher.Match(lineRaw).Groups["name"]?.Value;
            currentSection = new ConfigSection(sectionName, lineNumber);
        }

        /// <summary>
        /// Reads the empty line.
        /// </summary>
        /// <param name="currentLine">The current line.</param>
        /// <param name="currentSection">The current section.</param>
        /// <param name="lineRaw">The line raw.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <exception cref="ConfigParserException"></exception>
        private void ReadEmptyLine(ref ConfigSection currentSection, ref ConfigLine currentLine, string lineRaw, int lineNumber)
        {
            if (null != currentLine)
                BackupCurrentLine(ref currentSection, ref currentLine, lineNumber);

            currentLine = new ConfigLine(lineNumber, lineRaw);
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
        private void BackupCurrentLine(ref ConfigSection currentSection, ref ConfigLine currentLine, int lineNumber)
        {
            if (null == currentSection)
            {
                if (currentLine is IConfigKeyValue)
                    throw new ConfigParserException(
                        "This key value pair is orphan, all the keys must be preceded by a section.", lineNumber);

                fileHeader.Add(currentLine);
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
