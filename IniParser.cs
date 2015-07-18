using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Salaros.Config.Ini
{
    public class IniParser
    {
        protected List<string> textContent;
        protected List<IniLine> header = new List<IniLine>();
        protected Dictionary<string, IniSection> sections = new Dictionary<string, IniSection>();
        protected FileInfo iniFile;

        #region Line matchers

        const string SECTION_REGEX = @"^\[(?<name>.*)\]$";
        const string COMMENT_REGEX = @"((?<delimiter>(;|:|#))\s*?(?<comment>.*?))?";
        const string KEY_REGEX = "^(?<key>.*)\\s*?=\\s*?";
        const string VALUE_REGEX = @"(?<quote1>\"")?(?<value>[^\""]*.?)?(?<quote2>\"")?\s*?";

        static readonly Regex _sectionMatcher;
        static readonly Regex _commentMatcher;
        static readonly Regex _keyValueMatcher;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the <see cref="Salaros.Config.Ini.IniParser"/> class.
        /// </summary>
        static IniParser()
        {
            _sectionMatcher = new Regex(SECTION_REGEX, RegexOptions.Compiled);
            _commentMatcher = new Regex(string.Format("^{0}$",COMMENT_REGEX), RegexOptions.Compiled);
            _keyValueMatcher = new Regex(KEY_REGEX + VALUE_REGEX + COMMENT_REGEX + '$', RegexOptions.Compiled);
        }
            
        /// <summary>
        /// Initializes a new instance of the <see cref="Salaros.Config.Ini.IniParser"/> class.
        /// </summary>
        /// <param name="iniContent">Ini content.</param>
        /// <param name="newLine">New line.</param>
        /// <param name = "autoParse"></param>
        public IniParser(StringBuilder iniContent, string newLine = null, bool autoParse = true)
        {
            if (iniContent == null || string.IsNullOrWhiteSpace(iniContent.ToString()))
                throw new ArgumentNullException("iniContent");
            
            var newLineSeps = (string.IsNullOrWhiteSpace(newLine))
                ? new[]{ Environment.NewLine }
                : new[]{ newLine };

            iniFile = null;
            textContent = iniContent.ToString().Split(newLineSeps, StringSplitOptions.None).ToList();
            if (autoParse)
                Read();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Salaros.Config.Ini.IniParser"/> class.
        /// </summary>
        /// <param name="iniFilePath">Ini file path.</param>
        /// <param name = "autoParse"></param>
        public IniParser(string iniFilePath, bool autoParse = true)
        {
            if (iniFilePath == null)
                throw new ArgumentNullException("iniFilePath");

            this.iniFile = null;
            try
            {
                iniFile = new FileInfo(iniFilePath);
            }
            catch (Exception ex)
            {
                var message = string.Format("Failed to initialize IniParser for the following file: '{0}'", iniFilePath);
                throw new IniParserException(message, -1, ex);
            }

            textContent = GetFileContent(iniFile);

            if (autoParse)
                Read();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Salaros.Config.Ini.IniParser"/> class.
        /// </summary>
        /// <param name="iniFile">Ini file.</param>
        /// <param name = "autoParse"></param>
        public IniParser(FileInfo iniFile, bool autoParse = true)
        {
            this.iniFile = iniFile;
            textContent = GetFileContent(iniFile);
            if (autoParse)
                Read();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the sections.
        /// </summary>
        /// <value>The sections.</value>
        public ReadOnlyDictionary<string, IniSection> Sections
        {
            get 
            { 
                var result = sections.Values.OrderBy(s => s.LineNumber).ToDictionary(s => s.SectionName, s => s);
                return new ReadOnlyDictionary<string, IniSection>(result);
            }
        }

        #endregion

        #region Methods

        #region Read & write

        public void Read()
        {
            if (textContent == null)
                throw new InvalidOperationException();

            IniSection lastSection = null;
            for(int lineNumber = 0; lineNumber < textContent.Count; lineNumber++)
            {
                var line = textContent[lineNumber];

                // Check for empty line
                if (string.IsNullOrWhiteSpace(line))
                {
                    var emptyLine = new IniLine(lineNumber, line);
                    if (lastSection == null)
                        header.Add(emptyLine);
                    else
                        lastSection.AddLine(emptyLine);                    
                    continue;
                }

                // Check if the line is a comment
                var commentMatch = _commentMatcher.Match(line);
                if (commentMatch.Success)
                {
                    var delimiter = commentMatch.Groups["delimiter"].Value[0];
                    var comment = commentMatch.Groups["comment"].Value;
                    var commentLine = new IniComment(delimiter, comment, lineNumber);
                    lastSection.AddLine(commentLine);
                    continue;
                }

                // Check if the line is a section
                var sectionMatcher = _sectionMatcher.Match(line);
                if (sectionMatcher.Success)
                {
                    var sectionName = sectionMatcher.Groups["name"].Value;
                    var sectionLine = new IniSection(sectionName, lineNumber);
                    if (sections.ContainsKey(sectionLine.SectionName))
                        throw new IniParserException(string.Format("Duplicate section name '{0}'", sectionLine.SectionName), lineNumber);

                    sections.Add(sectionLine.SectionName, sectionLine);
                    lastSection = sectionLine;
                    continue;
                }

                // Check if the line is a key-value pair
                var keyMatcher = _keyValueMatcher.Match(line);
                if (keyMatcher.Success)
                {
                    if (lastSection == null)
                        throw new IniParserException("This key value pair is orphan, all the keys must be preceded by a section.", lineNumber);

                    var key = keyMatcher.Groups["key"].Value;
                    var value = keyMatcher.Groups["value"].Value;
                    var keyLine = new IniKeyValue(key, value, lineNumber);
                    lastSection.AddLine(keyLine);
                    continue;
                }

                throw new IniParserException("Unknown line type. Only empty lines, sections, comments and key-value pairs are accepted.", lineNumber);
            }
        }

        public bool Write()
        {
            var sb = new StringBuilder();
            foreach (var line in Lines.ToList())
            {
                sb.AppendLine(line.ToString());
            }

            return SetFileContent(iniFile, sb);
        }

        #endregion

        #region GetValue

        #region Plain

        public ReadOnlyCollection<IIniLine> Lines
        {
            get 
            {
                var lines = sections.Values.SelectMany(s => s.Lines);
                return new ReadOnlyCollection<IIniLine>(header.Concat(lines).ToList());
            }
        }

        internal virtual T GetValueRaw<T>(string sectionName, string keyName, T defaultValue) 
        {
            if (string.IsNullOrWhiteSpace(sectionName)) throw new ArgumentNullException("sectionName");
            if (string.IsNullOrWhiteSpace(keyName))throw new ArgumentNullException("keyName");

            var iniKey = new IniKeyValue(keyName, defaultValue);

            IniSection section;
            if (!sections.TryGetValue(sectionName, out section))
            {
                section = new IniSection(sectionName, GetNewLineNumber());
                sections.Add(sectionName, section);
                section.AddLine(iniKey);
                return defaultValue;
            }

            var key = section.Keys.FirstOrDefault(k => k.Key.Equals(keyName));
            return (key == null)
                ? defaultValue
                    : (T)key.Value;
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
            return GetValueRaw(sectionName, key, defaultValue); ;
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
            return GetValue(sectionName, key, (defaultValue ? "1" : "0"))  == "1";
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
            return GetValueRaw(sectionName, key, defaultValue);
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
            return GetValueRaw(sectionName, key, defaultValue);
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
            var stringValue = GetValueRaw(sectionName, key, string.Empty);
            try
            {
                return (string.IsNullOrWhiteSpace(stringValue))
                    ? defaultValue
                    : DecodeByteArray(stringValue);
            }
            catch
            {
                // TODO log the error
                return defaultValue;
            }
        }

        #endregion Plain

        #region Section key

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException"><key> section must have a not null reference value</exception>
        public string GetValue(KeyData<string> key)
        {
            if (key == null) 
                throw new ArgumentNullException();
            
            if (string.IsNullOrWhiteSpace(key.SectionName)) 
                throw new ArgumentException("<key> section must have a not null reference value");

            return GetValue(key.SectionName, key.Key, key.Value);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public bool GetValue(KeyData<bool> key)
        {
            return GetValue(key.SectionName, key.Key, key.Value);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public int GetValue(KeyData<int> key)
        {
            return GetValue(key.SectionName, key.Key, key.Value);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public double GetValue(KeyData<double> key)
        {
            return GetValue(key.SectionName, key.Key, key.Value);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public byte[] GetValue(KeyData<byte[]> key)
        {
            return GetValue(key.SectionName, key.Key, key.Value);
        }

        #endregion Section key

        #endregion

        #region SetValue

        #region Plain

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public bool SetValue(string sectionName, string keyName, string value)
        {
            if (string.IsNullOrWhiteSpace(sectionName)) throw new ArgumentNullException("sectionName");
            if (string.IsNullOrWhiteSpace(keyName))throw new ArgumentNullException("keyName");

            IniSection section;
            if (!sections.TryGetValue(sectionName, out section))
            {
                section = new IniSection(sectionName, GetNewLineNumber());
                sections.Add(sectionName, section);
            }

            if (section == null)
            {
                // TODO log the error
                return false;
            }             

            var iniKey = new IniKeyValue(keyName, value);
            section.AddLine(iniKey);
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

        #region Section key

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns></returns>
        public virtual bool SetValue(KeyData<bool> key, bool value)
        {
            return SetValue(key.SectionName, key.Key, value);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool SetValue(KeyData<int> key, int value)
        {
            return SetValue(key.SectionName, key.Key, value);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool SetValue(KeyData<string> key, string value)
        {
            return SetValue(key.SectionName, key.Key, value);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool SetValue(KeyData<double> key, double value)
        {
            return SetValue(key.SectionName, key.Key, value);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool SetValue(KeyData<byte[]> key, byte[] value)
        {
            return SetValue(key.SectionName, key.Key, value);
        }


        #endregion

        #endregion

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the content of the Ini file.
        /// </summary>
        /// <param name="iniFile">Ini file to read.</param>
        private static List<string> GetFileContent(FileInfo iniFile)
        {
            if (iniFile == null)
                throw new ArgumentNullException("iniFile");

            try 
            {
                return iniFile.Exists ? File.ReadLines(iniFile.FullName).ToList() : new List<string>(0);
            }
            catch (Exception ex)
            {
                var message = string.Format("Failed to initialize IniParser for the following file: '{0}'", iniFile.FullName);
                throw new IniParserException(message, -1, ex);
            }
        }

        /// <summary>
        /// Sets the content of the Ini file.
        /// </summary>
        /// <returns><c>true</c>, if file content was set, <c>false</c> otherwise.</returns>
        /// <param name="iniFile">Ini file to write to.</param>
        /// <param name="sb">Sb.</param>
        private static bool SetFileContent(FileInfo iniFile, StringBuilder sb)
        {
            if (iniFile == null)
                throw new ArgumentNullException("iniFile");
            
            if (sb == null)
                throw new ArgumentNullException("sb");

            try 
            {
                File.WriteAllText(iniFile.FullName, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                var message = string.Format("Failed to write IniParser content to the following file: '{0}'", iniFile.FullName);
                throw new IniParserException(message, -1, ex);
            }
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

        /// <summary>
        /// Gets the new line number.
        /// </summary>
        /// <returns>The new line number.</returns>
        private int GetNewLineNumber()
        {
            var lastLine = Lines.LastOrDefault();
            return (lastLine == null) 
                ? 0 
                : lastLine.LineNumber + 1;
        }

        #endregion
    }
}
