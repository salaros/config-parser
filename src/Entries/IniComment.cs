namespace Salaros.Config.Ini
{
    public class IniComment : IniLine
    {
        protected char delimiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="IniComment"/> class.
        /// </summary>
        /// <param name="delimiter">Delimiter.</param>
        /// <param name = "comment"></param>
        /// <param name="lineNumber">Line number.</param>
        public IniComment(char delimiter = ';', string comment = "", int lineNumber = -1)
            : base(lineNumber)
        {
            Delimiter = delimiter;
            Comment = comment;
        }

        /// <summary>
        /// Gets the delimiter.
        /// </summary>
        /// <value>The delimiter.</value>
        public char Delimiter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the delimiter.
        /// </summary>
        /// <value>The delimiter.</value>
        public string Comment
        {
            get;
            internal set;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="IniComment"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="IniComment"/>.</returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", Delimiter, Comment);
        }
    }
}

