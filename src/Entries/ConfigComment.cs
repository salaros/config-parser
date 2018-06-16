namespace Salaros.Config
{
    public class ConfigComment : ConfigLine
    {
        protected string delimiter;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IniComment" /> class.
        /// </summary>
        /// <param name="delimiter">Delimiter.</param>
        /// <param name="comment"></param>
        /// <param name="lineNumber">Line number.</param>
        public ConfigComment(string delimiter = ";", string comment = "", int lineNumber = -1)
            : base(lineNumber)
        {
            Delimiter = delimiter;
            Comment = comment;
        }

        /// <summary>
        /// Gets the delimiter.
        /// </summary>
        /// <value>The delimiter.</value>
        public string Delimiter
        {
            get;
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

        /// <inheritdoc />
        /// <summary>
        /// Returns a <see cref="T:System.String" /> that represents the current <see cref="T:Salaros.Config.IniComment" />.
        /// </summary>
        /// <returns>A <see cref="T:System.String" /> that represents the current <see cref="T:Salaros.Config.IniComment" />.</returns>
        public override string ToString()
        {
            return (string.IsNullOrWhiteSpace(Comment))
                ? Delimiter
                : $"{Delimiter}{Comment}";
        }
    }
}

