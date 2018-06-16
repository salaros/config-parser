using System;

namespace Salaros.Config
{
    public class ConfigKeyValue<T> : ConfigLine, IConfigKeyValue
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.ConfigKeyValue`T" /> class.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="lineNumber">Line number.</param>
        /// <exception cref="ArgumentNullException">key</exception>
        /// <inheritdoc />
        public ConfigKeyValue(string key, T value, int lineNumber = -1)
            : base(lineNumber)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            
            Key = key;
            Value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key
        {
            get;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public T Value
        {
            get => (T)ValueRaw;
            internal set => ValueRaw = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the value raw.
        /// </summary>
        /// <value>
        /// The value raw.
        /// </value>
        public object ValueRaw
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the raw content of the line.
        /// </summary>
        /// <value>
        /// The raw content of the line.
        /// </value>
        /// ReSharper disable once InheritdocConsiderUsage
        public override string Content
        {
            get => ValueRaw?.ToString();
            internal set => ValueRaw = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the name of the section.
        /// </summary>
        /// <value>
        /// The name of the section.
        /// </value>
        public string SectionName => (Section == null) ? string.Empty : Section.SectionName;

        #endregion

        #region Methods

        /// <inheritdoc />
        /// <summary>
        /// Returns a <see cref="T:string" /> that represents the current <see cref="T:Salaros.Config.ConfigKeyValue" />.
        /// </summary>
        /// <returns>A <see cref="T:string" /> that represents the current <see cref="T:Salaros.Config.ConfigKeyValue" />.</returns>
        public override string ToString()
        {
            return ToString(MultiLineValues.NotAllowed);
        }

        /// ReSharper disable once InheritdocInvalidUsage
        /// <inheritdoc cref="ConfigLine" />
        /// <summary>
        /// Returns a <see cref="T:System.String" /> that represents this instance.
        /// </summary>
        /// <param name="multiLineSettings">The multi line settings.</param>
        /// <returns>
        /// A <see cref="T:System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(MultiLineValues multiLineSettings)
        {
            switch (multiLineSettings)
            {
                case MultiLineValues.AllowValuelessKeys:
                    return Key;

                default:
                    return $"{Key}={Content}";
            }
        }

        #endregion
    }
}
