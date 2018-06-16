using System;

namespace Salaros.Config
{
    public class IniKeyValue<T> : IniLine, IIniKeyValue
    {
        #region Constructors

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.IniKeyValue" /> class.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="lineNumber">Line number.</param>
        public IniKeyValue(string key, T value, int lineNumber = -1)
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
        /// Returns a <see cref="T:string" /> that represents the current <see cref="T:Salaros.Config.IniKeyValue" />.
        /// </summary>
        /// <returns>A <see cref="T:string" /> that represents the current <see cref="T:Salaros.Config.IniKeyValue" />.</returns>
        public override string ToString()
        {
            return $"{Key}={Content}";
        }

        #endregion
    }
}
