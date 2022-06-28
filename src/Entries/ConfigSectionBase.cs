using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Salaros.Configuration.Logging;

namespace Salaros.Configuration
{
    public abstract class ConfigSectionBase
    {
        private ILog logger;
        protected List<ConfigLine> lines;

        protected ConfigSectionBase()
        {
            logger = LogProvider.GetCurrentClassLogger();
            lines = new List<ConfigLine>();
        }

        private static readonly YesNoConverter[] YesNoBoolConverters = new[]
        {
            new YesNoConverter(),
            new YesNoConverter("1", "0"),
            new YesNoConverter("on", "off"),
            new YesNoConverter("enabled", "disabled"),
        };

        #region Properties

        public abstract
#if NET40
        ReadOnlyCollection<ConfigSection> Sections
#else
        IReadOnlyCollection<ConfigSection> Sections
#endif
        { get; }

        /// <summary>
        /// Gets all the lines of the given section.
        /// </summary>
        /// <value>The lines.</value>
        public abstract
#if NET40
        ReadOnlyCollection<IConfigLine> Lines
#else
        IReadOnlyCollection<IConfigLine> Lines
#endif
        { get; }

        #endregion Properties

        #region Methods

        #region GetValue

        /// <summary>
        /// Tries to get the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// sectionName
        /// or
        /// keyName
        /// </exception>
        private bool TryGetValue<T>(string keyName, out T value)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentException("Key name must be a non-empty string.", nameof(keyName));

#pragma warning disable IDE0034 // Simplify 'default' expression
            value = default(T);
#pragma warning restore IDE0034 // Simplify 'default' expression

            var key = Keys?.FirstOrDefault(k => Equals(keyName, k.Name));
            if (key == null)
                return false;

            value = (T)key.ValueRaw;
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
        private T GetRawValue<T>(string sectionName, string keyName, T defaultValue)
        {
            if (sectionName is null)
                throw new ArgumentNullException(nameof(sectionName));

            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentException("Key name must be a non-empty string.", nameof(keyName));

            var iniKey = new ConfigKeyValue<T>(keyName, Settings.KeyValueSeparator, defaultValue, -1);
            if (!sections.TryGetValue(sectionName, out var section))
            {
                section = new ConfigSection(sectionName, Lines.Any() ? Lines.Max(l => l.LineNumber) : 0);
                if (Sections.Any())
                    Sections.Last().AddLine(new ConfigLine());
                sections.Add(sectionName, section);
            }

            var key = (section ?? fileHeader?.Section).Keys.FirstOrDefault(k => Equals(keyName, k.Name));
            if (key != null)
                return (T)key.ValueRaw;

            if (section is null && Settings.MultiLineValues.HasFlag(MultiLineValues.AllowEmptyTopSection))
                section = fileHeader.Section;

            section?.AddLine(iniKey);
            return defaultValue;
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

        /// <summary>Joins a multiline value using a separator.</summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="separator">The separator (defaults to whitespace).</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public string JoinMultilineValue(string sectionName, string keyName, string separator = " ")
        {
            var multiLineVal = GetValue(sectionName, keyName);
            return string.Join(separator, multiLineVal?.Split(new[] { Settings.NewLine }, StringSplitOptions.None));
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

            if (bool.TryParse(booleanValue, out var parseBoolean))
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
        /// Gets integer value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="numberStyles">The number styles.</param>
        /// <param name="numberFormatInfo">The number format information.</param>
        /// <returns></returns>
        public virtual int GetValue(
            string sectionName,
            string keyName,
            int defaultValue,
            NumberStyles numberStyles = NumberStyles.Number,
            NumberFormatInfo numberFormatInfo = null
        )
        {
            if (!numberStyles.HasFlag(NumberStyles.Number))
                numberStyles |= NumberStyles.Number;

            var integerRaw = GetRawValue<string>(sectionName, keyName, null);
            if (!string.IsNullOrWhiteSpace(integerRaw))
                return int.TryParse(integerRaw, numberStyles, (IFormatProvider)numberFormatInfo ?? Settings.Culture, out var integerParsed)
                    ? integerParsed
                    : int.Parse(integerRaw, numberStyles, (IFormatProvider)numberFormatInfo ?? Settings.Culture); // yeah, throws format exception by design

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
        /// <param name="numberFormatInfo">The number format information.</param>
        /// <returns></returns>
        public virtual double GetValue(
            string sectionName,
            string keyName,
            double defaultValue,
            NumberStyles numberStyles = NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.Number,
            NumberFormatInfo numberFormatInfo = null
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
                numberStyles |= NumberStyles.AllowExponent;

            doubleRaw = doubleRaw.TrimEnd('d', 'D', 'f', 'F');
            return double.TryParse(doubleRaw, numberStyles, (IFormatProvider)numberFormatInfo ?? Settings.Culture,
                out var parsedDouble)
                ? parsedDouble
                : double.Parse(doubleRaw, numberStyles, (IFormatProvider)numberFormatInfo ?? Settings.Culture); // yeah, throws format exception by design
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

            var values = arrayRaw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
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

            var values = arrayRaw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
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

            var section = Sections.FirstOrDefault(s => s.SectionName )
            if (!Sections.TryGetValue(sectionName, out var section))
            {
                var lineNumber = (null != Lines && Lines.Any()) ? Lines.Max(l => l.LineNumber) : -1;
                section = new ConfigSection(sectionName, lineNumber);
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
                section.AddLine((ConfigLine)iniKey);
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

        #endregion SetValue

        /// <summary>
        /// Gets the line number of the given line.
        /// </summary>
        /// <returns>The line number.</returns>
        /// <param name="line">Line.</param>
        internal int GetLineNumber(ConfigLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            return (lines == null || !lines.Any())
                ? -1
                : lines.IndexOf(line);
        }

        /// <summary>
        /// Adds a configuration file line.
        /// </summary>
        /// <param name="configLine">The configuration file line to add.</param>
        internal virtual void AddLine(ConfigLine configLine)
        {
            lines.Add(configLine);
        }

        #endregion Methods
    }
}
