using System;

namespace Salaros.Configuration
{
    public class ConfigParserException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigParserException" /> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="lineNumber">Line number.</param>
        /// <param name="innterException">Inner exception.</param>
        public ConfigParserException(string message, int lineNumber = -1, Exception innterException = null)
            : base(message, innterException)
        {
            LineNumber = lineNumber;
            Message = (lineNumber < 0)
                ? message
                : $"{message}. On the line no. #{lineNumber}.";
        }

        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        public int LineNumber { get; }

        /// <inheritdoc />
        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message { get; }
    }
}

