using System;

namespace Salaros.Config
{
    public class ConfigParserException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.ConfigParserException" /> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="lineNumber">Line number.</param>
        /// <param name="innterException">Inner exception.</param>
        public ConfigParserException(string message, int lineNumber = -1, Exception innterException = null)
            :base(message, innterException)
        {
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        public int LineNumber
        {
            get;
        }
    }
}

