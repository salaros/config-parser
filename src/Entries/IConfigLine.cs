namespace Salaros.Configuration
{
	public interface IConfigLine
    {
	    /// <summary>
	    /// Gets the line number.
	    /// </summary>
	    /// <value>The line number.</value>
	    int LineNumber { get; }

        /// <summary>
        /// Gets the raw content of the line.
        /// </summary>
        /// <value>The raw content of the line.</value>
        string Content { get; }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        ConfigSection Section { get; }
        
        /// <summary>
        /// Gets the indentation.
        /// </summary>
        /// <value>
        /// The indentation.
        /// </value>
        string Indentation { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <param name="multiLineSettings">The multi line settings.</param>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        string ToString(MultiLineValues multiLineSettings);
    }
}

