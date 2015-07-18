using System;
using System.Collections.Generic;

namespace Salaros.Config.Ini
{
    public class IniKeyValue : IniLine, IIniKeyValuePair<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Salaros.Config.Ini.IniKeyValue"/> class.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="lineNumber">Line number.</param>
        public IniKeyValue(string key, object value, int lineNumber = -1)
            : base(lineNumber)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            
            Key = key;
            Value = value;
        }

        #region IIniKeyValuePair implementation

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value
        {
            get;
            private set;
        }

        public string SectionName
        {
            get
            {
                return (Section == null) ? string.Empty : Section.SectionName;
            }
        }

        #endregion

        /// <summary>
        /// Gets the string representation of key value.
        /// </summary>
        /// <value>The string value.</value>
        public object StringValue
        {
            get 
            { 
                return (Value == null)
                    ? string.Empty
                        : Value.ToString();
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Salaros.Config.Ini.IniKeyValue"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Salaros.Config.Ini.IniKeyValue"/>.</returns>
        public override string ToString()
        {
            return string.Format("{0}={1}", Key, StringValue);
        }
    }
}

