using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Salaros.Config.Ini
{
    public class IniSection : IIniLine
	{
        protected int lineNumber;
        protected string sectionName;
        protected List<IniLine> lines;

        /// <summary>
        /// Initializes a new instance of the <see cref="IniSection"/> class.
        /// </summary>
        /// <param name="sectionName">Section name.</param>
        /// <param name="lineNumber">Line number.</param>
        public IniSection(string sectionName, int lineNumber = -1)
        {
            lines = new List<IniLine>();
            this.sectionName = sectionName;
            this.lineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the line number of the given line.
        /// </summary>
        /// <returns>The line number.</returns>
        /// <param name="line">Line.</param>
        internal int GetLineNumber(IniLine line)
        {
            if (line == null)
                throw new ArgumentNullException("line");
            
            return (lines == null || !lines.Any())
                ? -1 
                : lines.IndexOf(line);
        }

        internal protected void AddLine(IniLine iniLine)
        {
            lines.Add(iniLine);
            iniLine.Section = this;
        }

        #region IIniLine implementation

        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        public virtual int LineNumber
        {
            get
            {
                return lineNumber;
            }
        }
            
        #endregion

        /// <summary>
        /// Gets the name of the section.
        /// </summary>
        /// <value>The name of the section.</value>
        public string SectionName
        {
            get
            {
                return sectionName;
            }
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ReadOnlyCollection<IniKeyValue> Keys
        {
            get
            {
                return new ReadOnlyCollection<IniKeyValue>(lines.OfType<IniKeyValue>().OrderBy(k => k.LineNumber).ToList());
            }
        }

        /// <summary>
        /// Gets all the lines of the given section.
        /// </summary>
        /// <value>The lines.</value>
        public ReadOnlyCollection<IIniLine> Lines
        {
            get
            {
                var result = new List<IIniLine> { this };
                result.AddRange(lines.Cast<IIniLine>().OrderBy(k => k.LineNumber));
                return new ReadOnlyCollection<IIniLine>(result);
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="IniSection"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="IniSection"/>.</returns>
        public override string ToString()
        {
            return string.Format("[{0}]", sectionName);
        }
	}
}

