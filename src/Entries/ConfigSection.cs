using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Salaros.Configuration
{
    public class ConfigSection : ConfigSectionBase, IConfigLine
    {
        protected int lineNumber;
        protected string sectionName, comment;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSection" /> class.
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
            this.sectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            this.lineNumber = lineNumber;
            this.comment = comment;

            Indentation = indentation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSection"/> class.
        /// </summary>
        internal ConfigSection()
            : base()
        {
            sectionName = string.Empty;
            lineNumber = 0;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="ConfigSection"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="ConfigSection"/>.</returns>
        public override string ToString() => Content;

        /// <summary>
        /// Adds a configuration file line.
        /// </summary>
        /// <param name="configLine">The configuration file line to add.</param>
        internal override void AddLine(ConfigLine configLine)
        {
            base.AddLine(configLine);
            configLine.Section = this;
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <param name="multiLineSettings">The multi line settings.</param>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public string ToString(MultiLineValues multiLineSettings) => ToString();

        #endregion

        #region Properties

        #region IConfigLine implementation

        /// <inheritdoc />
        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public ConfigSection Section => this;


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
        public
#if NET40
        ReadOnlyCollection<IConfigKeyValue> Keys
#else
        IReadOnlyCollection<IConfigKeyValue> Keys
#endif
            => new ReadOnlyCollection<IConfigKeyValue>(lines.OfType<IConfigKeyValue>().OrderBy(k => k.LineNumber).ToList());

        /// <summary>
        /// Gets all the lines of the given section.
        /// </summary>
        /// <value>The lines.</value>
        public override
#if NET40
            ReadOnlyCollection<IConfigLine> Lines
#else
            IReadOnlyCollection<IConfigLine> Lines
#endif
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

        /// <summary>
        /// Gets the raw content of the line.
        /// </summary>
        /// <value>
        /// The raw content of the line.
        /// </value>
        /// ReSharper disable once InheritdocConsiderUsage
        public string Content => string.IsNullOrWhiteSpace(sectionName)
            ? string.Empty
            : $"{Indentation}[{sectionName}]{comment}";

        /// <inheritdoc />
        /// <summary>
        /// Gets the indentation.
        /// </summary>
        /// <value>
        /// The indentation.
        /// </value>
        public string Indentation { get; }

        public override
#if NET40
        ReadOnlyCollection<ConfigSection> Sections => new ReadOnlyCollection<ConfigSection>(new List<ConfigSection> { this });
#else
        IReadOnlyCollection<ConfigSection> Sections => new List<ConfigSection> { this };
# endif

        #endregion

        #region Indexing

        /// <summary>
        /// Gets the <see cref="string"/> with the specified key name.
        /// </summary>
        /// <value>
        /// The <see cref="string"/>.
        /// </value>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>Key entry with the specified key name.</returns>
        public string this[string keyName]
        {
            get
            {
                return (keyName is null)
                    ? null
                    : Keys
                        ?.FirstOrDefault(s => keyName.Equals(s.Name, StringComparison.InvariantCultureIgnoreCase))
                        ?.ValueRaw as string;
            }
        }

#endregion
    }
}
