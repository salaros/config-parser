using System;

namespace Salaros.Config
{
    public class IniKeyValue : IniLine
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.IniKeyValue" /> class.
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

        /// <inheritdoc />
        /// <summary>
        /// Gets the raw content of the line.
        /// </summary>
        /// <value>
        /// The raw content of the line.
        /// </value>
        public override string Content
        {
            get => Value?.ToString();
            internal set => Value = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns a <see cref="T:string" /> that represents the current <see cref="T:Salaros.Config.IniKeyValue" />.
        /// </summary>
        /// <returns>A <see cref="T:string" /> that represents the current <see cref="T:Salaros.Config.IniKeyValue" />.</returns>
        public override string ToString()
        {
            return $"{Key}={Content}";
        }
    }
}
