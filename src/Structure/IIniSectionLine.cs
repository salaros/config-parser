using System;

namespace Salaros.Config.Ini
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

