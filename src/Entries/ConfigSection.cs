using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Salaros.Config
{
    public class ConfigSection : IConfigLine
	{
        protected int lineNumber;
        protected string sectionName;
        protected List<ConfigLine> lines;

        #region Constructors

	    /// <summary>
	    /// Initializes a new instance of the <see cref="ConfigSection" /> class.
	    /// </summary>
	    /// <param name="sectionName">Section name.</param>
	    /// <param name="lineNumber">Line number.</param>
	    public ConfigSection(string sectionName, int lineNumber = -1)
	    {
	        lines = new List<ConfigLine>();
	        this.sectionName = sectionName;
	        this.lineNumber = lineNumber;
	    }

        #endregion

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

        protected internal void AddLine(ConfigLine iniLine)
        {
            lines.Add(iniLine);
            iniLine.Section = this;
        }

	    /// <summary>
	    /// Returns a <see cref="string"/> that represents the current <see cref="ConfigSection"/>.
	    /// </summary>
	    /// <returns>A <see cref="string"/> that represents the current <see cref="ConfigSection"/>.</returns>
	    public override string ToString()
	    {
	        return $"[{sectionName}]";
	    }

        #endregion

        #region Properties

        #region IConfigLine implementation

        /// <inheritdoc />
        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        public virtual int LineNumber => lineNumber;

        #endregion

        /// <summary>
        /// Gets the name of the section.
        /// </summary>
        /// <value>The name of the section.</value>
        public string SectionName => sectionName;

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ReadOnlyCollection<IConfigKeyValue> Keys
        {
            get
            {
                return new ReadOnlyCollection<IConfigKeyValue>(lines.OfType<IConfigKeyValue>().OrderBy(k => k.LineNumber).ToList());
            }
        }

        /// <summary>
        /// Gets all the lines of the given section.
        /// </summary>
        /// <value>The lines.</value>
        public ReadOnlyCollection<IConfigLine> Lines
        {
            get
            {
                var result = new List<IConfigLine> { this };
                result.AddRange(lines.Cast<IConfigLine>().OrderBy(k => k.LineNumber));
                return new ReadOnlyCollection<IConfigLine>(result);
            }
        }

        #endregion
	}
}
