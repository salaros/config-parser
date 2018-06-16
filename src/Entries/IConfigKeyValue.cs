namespace Salaros.Config
{
    public interface IConfigKeyValue : IConfigLine
    {
        string Key { get; }

        /// <summary>
        /// Gets the value raw.
        /// </summary>
        /// <value>
        /// The value raw.
        /// </value>
        object ValueRaw { get; set; }

        /// <summary>
        /// Gets the raw content of the line.
        /// </summary>
        /// <value>
        /// The raw content of the line.
        /// </value>
        string Content { get; }

        /// <summary>
        /// Gets the name of the section.
        /// </summary>
        /// <value>
        /// The name of the section.
        /// </value>
        string SectionName { get; }
    }
}
