using System;

namespace Salaros.Config.Ini
{
    public class IniKeyValue : IniLine, IIniKeyValuePair<object>
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.Ini.IniKeyValue" /> class.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="lineNumber">Line number.</param>
        public IniKeyValue(string key, object value, int lineNumber = -1)
            : base(lineNumber)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            
            Key = key;
            Value = value;
        }

        #region IIniKeyValuePair implementation

        /// <inheritdoc />
        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key
        {
            get;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value
        {
            get;
            internal set;
        }

        public string SectionName => (Section == null) ? string.Empty : Section.SectionName;

        #endregion

        /// <summary>
        /// Gets the string representation of key value.
        /// </summary>
        /// <value>The string value.</value>
        public object StringValue => (Value == null)
            ? string.Empty
            : Value.ToString();

        /// <inheritdoc />
        /// <summary>
        /// Returns a <see cref="T:string" /> that represents the current <see cref="T:Salaros.Config.Ini.IniKeyValue" />.
        /// </summary>
        /// <returns>A <see cref="T:string" /> that represents the current <see cref="T:Salaros.Config.Ini.IniKeyValue" />.</returns>
        public override string ToString()
        {
            return $"{Key}={StringValue}";
        }
    }
}
