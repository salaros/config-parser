using System;

namespace Salaros.Config
{
    public class ConfigKeyValue<T> : ConfigLine, IConfigKeyValue
    {
        protected string keyName;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.ConfigKeyValue`T" /> class.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="separator">The separator.</param>
        /// <param name="value">Value.</param>
        /// <param name="lineNumber">Line number.</param>
        /// <exception cref="ArgumentNullException">key</exception>
        /// <inheritdoc />
        public ConfigKeyValue(string keyName, string separator, T value, int lineNumber)
            : base(lineNumber)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentNullException(nameof(keyName));

            if (string.IsNullOrWhiteSpace(separator))
                throw new ArgumentNullException(nameof(separator));

            this.keyName = keyName;
            Separator = separator;
            Value = value;
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        /// <summary>
        /// Gets the name of the key.
        /// </summary>
        /// <value>
        /// The name of the key.
        /// </value>
        public string Name => keyName.Trim();

        /// <summary>
        /// Gets or sets the separator.
        /// </summary>
        /// <value>
        /// The separator.
        /// </value>
        public string Separator { get; }

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
            get; set;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the name of the section.
        /// </summary>
        /// <value>
        /// The name of the section.
        /// </value>
        public string SectionName => Section?.SectionName;

        /// <summary>
        /// Gets the raw content of the line.
        /// </summary>
        /// <value>
        /// The raw content of the line.
        /// </value>
        /// ReSharper disable once InheritdocConsiderUsage
        public new string Content
        {
            get => ValueRaw?.ToString();
            internal set => ValueRaw = value;
        }

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
                case MultiLineValues.AllowValuelessKeys when string.IsNullOrWhiteSpace(Content):
                    return keyName;

                default:
                    return $"{keyName}{Separator}{Content}";
            }
        }

        #endregion
    }
}
