using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        protected readonly ConfigSection fileHeader;
        protected readonly Dictionary<string, ConfigSection> sections;
        protected FileInfo fileInfo;

        private static readonly YesNoConverter[] YesNoBoolConverters;

        #region Constructor

        /// <summary>
        /// Initializes the <see cref="ConfigParser"/> class.
        /// </summary>
        static ConfigParser()
        {
            YesNoBoolConverters = new[]
            {
                new YesNoConverter(),
                new YesNoConverter("1", "0"),
                new YesNoConverter("on", "off"),
                new YesNoConverter("enabled", "disabled")
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigParser"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public ConfigParser(ConfigParserSettings settings = null)
        {
            Settings = settings ?? new ConfigParserSettings();

            fileHeader = new ConfigSection();
            sections = new Dictionary<string, ConfigSection>();
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.ConfigParser" /> class.
        /// </summary>
        /// <param name="configFile">The configuration file.</param>
        /// <param name="settings">The settings.</param>
        /// <exception cref="T:System.ArgumentException">configFile</exception>
        public ConfigParser(string configFile, ConfigParserSettings settings = null)
            : this(settings)
        {
            if (string.IsNullOrWhiteSpace(configFile)) throw new ArgumentException(nameof(configFile));

            if (File.Exists(configFile))
            {
                fileInfo = new FileInfo(configFile);
                Settings.Encoding = Settings.Encoding ?? fileInfo.GetEncoding(true);
                Settings.NewLine = fileInfo.DetectNewLine(configFile);
                configFile = File.ReadAllText(configFile, Settings.Encoding ?? Encoding.UTF8);
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
        public ReadOnlyCollection<ConfigSection> Sections =>
            new ReadOnlyCollection<ConfigSection>(sections.Values.ToArray());

        /// <summary>
        /// Gets configuration file's lines.
        /// </summary>
        /// <value>The lines.</value>
        public ReadOnlyCollection<IConfigLine> Lines
            => new ReadOnlyCollection<IConfigLine>(fileHeader.Lines.Concat(sections.Values.SelectMany(s => s.Lines))
                .ToArray());

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

#pragma warning disable IDE0034 // Simplify 'default' expression
            value = default(T);
#pragma warning restore IDE0034 // Simplify 'default' expression

            if (!sections.TryGetValue(sectionName, out var section))
                return false;

            var key = section.Keys.FirstOrDefault(k => Equals(keyName, k.Name));
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

            var iniKey = new ConfigKeyValue<T>(keyName, Settings.KeyValueSeparator, defaultValue, -1);

            if (!sections.TryGetValue(sectionName, out var section))
            {
                section = new ConfigSection(sectionName, Lines.Any() ? Lines.Max(l => l.LineNumber) : 0);
                if (Sections.Any())
                    Sections.Last().AddLine(new ConfigLine());
                sections.Add(sectionName, section);
                section.AddLine(iniKey);
                return defaultValue;
            }

            var key = section.Keys.FirstOrDefault(k => Equals(keyName, k.Name));
            return (key == null)
                ? defaultValue
                : (T) key.ValueRaw;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public virtual string GetValue(string sectionName, string keyName, string defaultValue = null)
        {
            return GetRawValue(sectionName, keyName, defaultValue);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
        /// <param name="booleanConverter">The boolean converter.</param>
        /// <returns></returns>
        public virtual bool GetValue(string sectionName, string keyName, bool defaultValue,
            BooleanConverter booleanConverter = null)
        {
            booleanConverter = booleanConverter ?? Settings.BooleanConverter;

            var booleanValue = GetRawValue<string>(sectionName, keyName, null);
            if (string.IsNullOrWhiteSpace(booleanValue))
            {
                SetValue(sectionName, keyName,
                    null == booleanConverter
                        // if some day Boolean.ToString(IFormatProvider) will work 
                        // https://msdn.microsoft.com/en-us/library/s802ct92(v=vs.110).aspx#Anchor_1
                        ? defaultValue.ToString(Settings.Culture).ToLowerInvariant()
                        : booleanConverter.ConvertToString(defaultValue));
                return defaultValue;
            }

#pragma warning disable IDE0046 // Convert to conditional expression
            foreach (var converter in YesNoBoolConverters)
            {
                if (converter.Yes.Equals(booleanValue.Trim(), StringComparison.InvariantCultureIgnoreCase) ||
                    converter.No.Equals(booleanValue.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    return converter.Yes.Equals(booleanValue, StringComparison.InvariantCultureIgnoreCase);
                }
            }
#pragma warning restore IDE0046 // Convert to conditional expression

            if(bool.TryParse(booleanValue, out var parseBoolean))
                return parseBoolean;

            // if some day Boolean.ToString(IFormatProvider) will work 
            // https://msdn.microsoft.com/en-us/library/s802ct92(v=vs.110).aspx#Anchor_1
            if (true.ToString(Settings.Culture).ToLowerInvariant().Equals(booleanValue, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (booleanConverter == null || !booleanConverter.CanConvertFrom(typeof(string))) 
                return defaultValue;

            var value = booleanConverter.ConvertFrom(booleanValue);
            return value is bool convertedBoolean
                ? convertedBoolean
                : defaultValue;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="numberStyles">The number styles.</param>
        /// <returns></returns>
        public virtual int GetValue(
            string sectionName,
            string keyName,
            int defaultValue,
            NumberStyles numberStyles = NumberStyles.Number
        )
        {
            if (!numberStyles.HasFlag(NumberStyles.Number))
                numberStyles |= NumberStyles.Number;

            var integerRaw = GetRawValue<string>(sectionName, keyName, null);
            if (!string.IsNullOrWhiteSpace(integerRaw))
                return int.TryParse(integerRaw, numberStyles, Settings.Culture, out var integerParsed)
                    ? integerParsed
                    : int.Parse(integerRaw, numberStyles, Settings.Culture); // yeah, throws format exception by design

            SetValue(sectionName, keyName, defaultValue);
            return defaultValue;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="numberStyles">The number styles.</param>
        /// <returns></returns>
        public virtual double GetValue(
            string sectionName,
            string keyName,
            double defaultValue,
            NumberStyles numberStyles = NumberStyles.Float | NumberStyles.AllowThousands
        )
        {
            if (!(numberStyles.HasFlag(NumberStyles.Float) || numberStyles.HasFlag(NumberStyles.AllowThousands)))
                numberStyles |= NumberStyles.AllowThousands | NumberStyles.Float;

            var doubleRaw = GetRawValue<string>(sectionName, keyName, null);
            if (string.IsNullOrWhiteSpace(doubleRaw))
            {
                SetValue(sectionName, keyName, defaultValue);
                return defaultValue;
            }

            if (doubleRaw.Contains("E") && !numberStyles.HasFlag(NumberStyles.AllowExponent))
                numberStyles = numberStyles | NumberStyles.AllowExponent;

            doubleRaw = doubleRaw.TrimEnd('d', 'D', 'f', 'F');
            return double.TryParse(doubleRaw, numberStyles, Settings.Culture,
                out var parsedDouble)
                ? parsedDouble
                : double.Parse(doubleRaw, numberStyles, Settings.Culture); // yeah, throws format exception by design
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public virtual byte[] GetValue(string sectionName, string keyName, byte[] defaultValue)
        {
            var stringValue = GetRawValue(sectionName, keyName, string.Empty);
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

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default array value.</param>
        /// <returns></returns>
        /// <exception cref="ConfigParserException"></exception>
        public virtual string[] GetValue(string sectionName, string keyName, string[] defaultValue)
        {
            var arrayRaw = GetRawValue(sectionName, keyName, string.Empty);
            if (string.IsNullOrWhiteSpace(arrayRaw))
                return null;

            var values = arrayRaw.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
            if (!values.Any())
                return null;

            if (!string.IsNullOrWhiteSpace(values.First()))
                throw new ConfigParserException(
                    $"Array values must start from the new line. The key [{sectionName}]{keyName} is malformed.");

            return values
                .SkipWhile(string.IsNullOrWhiteSpace)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim('\t', ' '))
                .ToArray();
        }

        /// <summary>
        /// Gets the array value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public virtual string[] GetArrayValue(string sectionName, string keyName, string[] defaultValue = null)
        {
            return GetValue(sectionName, keyName, defaultValue);
        }

        /// <summary>
        /// Checks if value the is an array.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        public virtual bool ValueIsArray(string sectionName, string keyName)
        {
            var arrayRaw = GetRawValue(sectionName, keyName, string.Empty);
            if (string.IsNullOrWhiteSpace(arrayRaw))
                return false;

            var values = arrayRaw.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
            return values.Any() && string.IsNullOrWhiteSpace(values.First());
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
                .FirstOrDefault(k => Equals(keyName, k.Name));

            if (iniKey != null)
            {
                iniKey.ValueRaw = value;
            }
            else
            {
                iniKey = new ConfigKeyValue<string>(keyName, Settings.KeyValueSeparator, value, -1);
                section.AddLine((ConfigLine) iniKey);
            }
            return true;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <param name="booleanConverter">The boolean converter.</param>
        /// <returns></returns>
        public virtual bool SetValue(string sectionName, string keyName, bool value,
            BooleanConverter booleanConverter = null)
        {
            var booleanValue = GetRawValue<string>(sectionName, keyName, null);
            // ReSharper disable once InvertIf
            if (!string.IsNullOrWhiteSpace(booleanValue))
            {
                // Check if current value matches one of boolean converters
                foreach (var converter in YesNoBoolConverters)
                {
                    if (Equals(converter.Yes, booleanValue.Trim()) || Equals(converter.No, booleanValue.Trim()))
                        return SetValue(sectionName, keyName, value ? converter.Yes : converter.No);
                }
            }

            booleanConverter = booleanConverter ?? Settings.BooleanConverter;
            return SetValue(sectionName, keyName, (null == booleanConverter)
                ? value.ToString(Settings.Culture ?? CultureInfo.InvariantCulture).ToLowerInvariant()
                : booleanConverter.ConvertToString(value)
            );
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <param name="customFormat">The custom format.</param>
        /// <returns></returns>
        public virtual bool SetValue(string sectionName, string keyName, int value, string customFormat = null)
        {
            return string.IsNullOrWhiteSpace(customFormat)
                ? SetValue(sectionName, keyName, value.ToString(Settings.Culture ?? CultureInfo.InvariantCulture))
                : SetValue(sectionName, keyName, value.ToString(customFormat));
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <param name="customFormat">The custom format.</param>
        /// <returns></returns>
        public virtual bool SetValue(string sectionName, string keyName, double value, string customFormat = null)
        {
            return string.IsNullOrWhiteSpace(customFormat)
                ? SetValue(sectionName, keyName, value.ToString(Settings.Culture ?? CultureInfo.InvariantCulture))
                : SetValue(sectionName, keyName, value.ToString(customFormat));
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool SetValue(string sectionName, string keyName, byte[] value)
        {
            return SetValue(sectionName, keyName, EncodeByteArray(value));
        }

        #endregion

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
                using (var fileWriter = new FileStream(fileInfo.FullName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(
                            fileWriter,
                            Settings.Encoding ?? new UTF8Encoding(false, false),
                            4096
#if !NET40
                            , true
#endif
                        )
                    )
                    {
                        writer.Write(ToString());
                    }
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
            var fileLines = new StringBuilder();
            foreach (var line in Lines.ToList())
                fileLines.AppendLine(line.ToString(Settings.MultiLineValues));
            return fileLines.ToString();
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
                            ReadKeyAndValue(ref currentSection, ref currentLine, lineRaw, lineNumber);
                            break;

                        // Multi-line + allow value-less option on
                        case var _ when Settings.ValueMatcher.IsMatch(lineRaw) &&
                                        Settings.KeyMatcher.IsMatch(currentLine?.ToString() ?? string.Empty) &&
                                        Settings.MultiLineValues.HasFlag(MultiLineValues.AllowValuelessKeys) &&
                                        Settings.MultiLineValues.HasFlag(MultiLineValues.Simple) &&
                                        lineRaw.TrimStart(' ', '\t').Length != lineRaw.Length:
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
                throw new ConfigParserException(
                    "Multi-line values are explicitly disallowed by parser settings. Please consider changing them.",
                    lineNumber);

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
            int lineNumber, bool append = false)
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
            if (keyMatch.Success && keyMatch.Captures.Count > 0)
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

            if (append)
                currentLine.Content = $"{currentLine.Content}{Settings.NewLine}{value}";
            else
                currentLine = new ConfigKeyValue<object>(keyName, separator, value, lineNumber);
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
