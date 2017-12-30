using System;

namespace Salaros.Config.Ini
{
    public class IniParserException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.Ini.IniParserException" /> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="lineNumber">Line number.</param>
        /// <param name="innterException">Inner exception.</param>
        public IniParserException(string message, int lineNumber = -1, Exception innterException = null)
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

