namespace Salaros.Config
{
    public class ConfigLine : IConfigLine
    {
        protected int lineNumber;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigLine"/> class.
        /// </summary>
        /// <param name="lineNumber">Line number.</param>
        /// <param name="lineContent">Raw line content.</param>
        public ConfigLine(int lineNumber = -1, string lineContent = "")
        {
            Content = lineContent;
            this.lineNumber = lineNumber;
        }

        #endregion

        #region Properties

        #region IConfigLine implementation

        /// <inheritdoc />
        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        public virtual int LineNumber
        {
            get
            {
                if (lineNumber < 0 && Section != null)
                    lineNumber = Section.GetLineNumber(this);

                return lineNumber;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the raw content of the line.
        /// </summary>
        /// <value>The raw content of the line.</value>
        public string Content { get; internal set; }

        #endregion

        /// <inheritdoc />
        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public ConfigSection Section
        {
            get;  internal set;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="ConfigLine"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="ConfigLine"/>.</returns>
        public override string ToString()
        {
            return Content;
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns a <see cref="T:System.String" /> that represents this instance.
        /// </summary>
        /// <param name="multiLineSettings">The multi line settings.</param>
        /// <returns>
        /// A <see cref="T:System.String" /> that represents this instance.
        /// </returns>
        public virtual string ToString(MultiLineValues multiLineSettings)
        {
            return ToString();
        }

        #endregion
    }
}
