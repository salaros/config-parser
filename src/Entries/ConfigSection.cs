using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Salaros.Config
{
    public class ConfigSection : IConfigLine
	{
        protected int lineNumber;
	    protected string sectionName, indentation, comment;
        protected List<ConfigLine> lines;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.ConfigSection" /> class.
        /// </summary>
        /// <param name="sectionName">Section name.</param>
        /// <param name="lineNumber">Line number.</param>
        /// <param name="indentation">The indentation.</param>
        /// <param name="comment">The comment.</param>
        /// <exception cref="ArgumentNullException">sectionName</exception>
        /// <inheritdoc />
        public ConfigSection(string sectionName, int lineNumber = -1, string indentation = "", string comment = "")
            : this()
	    {
	        lines = new List<ConfigLine>();
	        this.sectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
	        this.lineNumber = lineNumber;
	        this.indentation = indentation;
	        this.comment = comment;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSection"/> class.
        /// </summary>
        internal ConfigSection()
	    {
	        lines = new List<ConfigLine>();
	        sectionName = string.Empty;
	        lineNumber = 0;
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
	        return string.IsNullOrWhiteSpace(sectionName)
                ? string.Empty
                : $"{indentation}[{sectionName}]{comment}";
	    }

	    /// <inheritdoc />
	    /// <summary>
	    /// Returns a <see cref="T:System.String" /> that represents this instance.
	    /// </summary>
	    /// <param name="multiLineSettings">The multi line settings.</param>
	    /// <returns>
	    /// A <see cref="T:System.String" /> that represents this instance.
	    /// </returns>
        public string ToString(MultiLineValues multiLineSettings)
	    {
	        return ToString();
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
                var allLines = (string.IsNullOrWhiteSpace(sectionName))
                    ? new List<IConfigLine>()
                    : new List<IConfigLine> { this };
                allLines.AddRange(lines.Cast<IConfigLine>().OrderBy(k => k.LineNumber));
                return new ReadOnlyCollection<IConfigLine>(allLines);
            }
        }

        #endregion
	}
}
