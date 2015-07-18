namespace Salaros.Config.Ini
{
    public class IniLine : IIniLine
    {
        protected int lineNumber;
        protected string lineContent;
        protected IniSection section;

        #region Contructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Salaros.Config.Ini.IniLine"/> class.
        /// </summary>
        /// <param name="lineNumber">Line number.</param>
        /// <param name="lineContent">Raw line content.</param>
        public IniLine(int lineNumber = -1, string lineContent = "")
        {
            this.lineContent = lineContent;
            this.lineNumber = lineNumber;
        }

        #endregion

        #region Properties

        #region IIniLine implementation

        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        public virtual int LineNumber
        {
            get
            {
                if (lineNumber == -1 && Section != null)
                    lineNumber = Section.GetLineNumber(this);

                return lineNumber;
            }
        }

        /// <summary>
        /// Gets the raw content of the line.
        /// </summary>
        /// <value>The raw content of the line.</value>
        public virtual string Content
        {
            get
            { 
                return lineContent;
            }
        }

        #endregion

        #region IIniSectionLine implementation

        public IniSection Section
        {
            get
            {
                return section;   
            }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Salaros.Config.Ini.IniLine"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Salaros.Config.Ini.IniLine"/>.</returns>
        public override string ToString()
        {
            return Content;
        }

        #endregion
    }
}

