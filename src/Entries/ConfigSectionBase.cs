using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Salaros.Configuration
{
    public abstract class ConfigSectionBase
    {
        protected List<ConfigLine> lines;

        protected ConfigSectionBase()
        {
            lines = new List<ConfigLine>();
        }

        #region Properties

        /// <summary>
        /// Gets all the lines of the given section.
        /// </summary>
        /// <value>The lines.</value>
        public abstract
#if NET40
        ReadOnlyCollection<IConfigLine> Lines
#else
        IReadOnlyCollection<IConfigLine> Lines
#endif
        { get; }

        public abstract string this[string value] { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Gets the line number of the given line.
        /// </summary>
        /// <returns>The line number.</returns>
        /// <param name="line">Line.</param>
        internal int GetLineNumber(ConfigLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            return (lines == null || !lines.Any())
                ? -1
                : lines.IndexOf(line);
        }

        /// <summary>
        /// Adds a configuration file line.
        /// </summary>
        /// <param name="configLine">The configuration file line to add.</param>
        internal virtual void AddLine(ConfigLine configLine)
        {
            lines.Add(configLine);
        }

        #endregion Methods
    }
}
