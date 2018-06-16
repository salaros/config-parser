namespace Salaros.Config
{
	public interface IConfigLine
    {
	    /// <summary>
	    /// Gets the line number.
	    /// </summary>
	    /// <value>The line number.</value>
	    int LineNumber { get; }

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

