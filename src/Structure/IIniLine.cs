using System;
using System.Collections.Generic;

namespace Salaros.Config.Ini
{
	public interface IIniLine
	{
        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        int LineNumber { get; }
	}

}

