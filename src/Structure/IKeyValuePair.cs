namespace Salaros.Config
{
    public interface IIniKeyValuePair<out TValue>
	{
        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        string Key { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        TValue Value { get; }

        /// <summary>
        /// Gets the name of the section.
        /// </summary>
        /// <value>The name of the section.</value>
        string SectionName { get; }
	}
}

