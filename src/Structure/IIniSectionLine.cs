namespace Salaros.Config
{
    public interface IIniSectionLine : IIniLine
    {
        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>The section.</value>
        IniSection Section { get; }
    }
}

