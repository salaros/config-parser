using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace Salaros.Config.Tests
{
    public class IniParserTests
    {
        private static readonly string[] RealWorldConfigFiles;
        private static readonly string[] StructureSampleFiles;

        /// <summary>
        /// Initializes the <see cref="IniParserTests"/> class.
        /// </summary>
        static IniParserTests()
        {
            RealWorldConfigFiles = Directory
                .GetFiles(Path.Combine(Environment.CurrentDirectory, "Resources", "RealWorld"))
                .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            StructureSampleFiles = Directory
                .GetFiles(Path.Combine(Environment.CurrentDirectory, "Resources", "Structure"))
                .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        /// <summary>
        /// Parses some real world configuration files.
        /// </summary>
        [Fact]
        public void ParseRealWorldFiles()
        {
            Assert.All(RealWorldConfigFiles, realConfigFile =>
            {
                ConfigParser config = null;
                try
                {
                    var settings = GetSettingsForFile(realConfigFile);
                    config = new ConfigParser(realConfigFile, settings);
                }
                finally
                {
                    Assert.NotNull(config);
                }
            });
        }

        /// <summary>
        /// Checks if real world files equal their ToString representation.
        /// </summary>
        [Fact]
        public void RealWorldFilesEqualToString()
        {
            Assert.All(RealWorldConfigFiles, realConfigFile =>
            {
                var settings = GetSettingsForFile(realConfigFile);
                var configFile = new ConfigParser(realConfigFile, settings);
                var configFileFromDisk = File
                                            .ReadAllText(realConfigFile)
                                            .Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\n")
                                            .TrimEnd('\n');
                var configFileText = configFile
                                            .ToString()
                                            .Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\n")
                                            .TrimEnd('\n');
                Assert.Equal(configFileText, configFileFromDisk);
            });
        }

        /// <summary>
        /// Checks if array values are parsed correctly.
        /// </summary>
        [Fact]
        public void ArrayValuesAreParsedCorrectly()
        {
            var arraySampleFilePath = StructureSampleFiles.FirstOrDefault(f => f.EndsWith("array-values.cnf", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(arraySampleFilePath);

            var configFile = new ConfigParser(arraySampleFilePath, new ConfigParserSettings(MultiLineValues.Simple));
            Assert.True(configFile.ValueIsArray("settings", "exclude"));

            var excludeArray = new[]
            {
                "^/var/",
                "^/tmp/",
                "^/private/",
                "COMMIT_EDITMSG$",
                "PULLREQ_EDITMSG$",
                "MERGE_MSG$",
            };
            var excludeArrayFromFile = configFile.GetValue("settings", "exclude", new string[] {});
            Assert.Equal(excludeArrayFromFile, excludeArray);
        }

        /// <summary>
        /// Checks if indented files are parsed correctly.
        /// </summary>
        [Fact]
        public void IndentedFilesAreParsedCorrectly()
        {
            var indentedFilePath = StructureSampleFiles.FirstOrDefault(f => f.EndsWith("indented.ini", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(indentedFilePath);

            var configFile = new ConfigParser(indentedFilePath, new ConfigParserSettings(MultiLineValues.Simple));
            Assert.All(configFile.Sections, section =>
            {
                Assert.Equal(section.SectionName, section.SectionName.Trim());
            });

            Assert.All(configFile.Sections.SelectMany(s => s.Keys), key =>
            {
                Assert.Equal(key.Name, key.Name.Trim());
            });

            var purposeKey = configFile.GetValue("Sections Can Be Indented", "purpose", string.Empty);
            Assert.Equal(
                "formatting for readability",
                purposeKey
            );

            var sectionName = configFile.Sections?.FirstOrDefault()?.SectionName;
            Assert.Equal(
                "Sections Can Be Indented",
                sectionName
            );

            var emptyLine = configFile.Lines.Last(l => string.IsNullOrWhiteSpace(l.ToString()));
            Assert.Equal(
                "\t\t",
                emptyLine.Content
            );

            var lastComment = configFile.Lines.Last(l => l is ConfigComment) as ConfigComment;
            Assert.Equal(
                "Did I mention we can indent comments, too?",
                lastComment?.Comment
            );
        }

        /// <summary>
        /// Checks if multi-line values are parsed correctly.
        /// </summary>
        [Fact]
        public void MultilineValuesAreParsedCorrectly()
        {
            var multiLineDelimitedFilePath = StructureSampleFiles.FirstOrDefault(f => 
                f.EndsWith("multi-line.ini", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(multiLineDelimitedFilePath);

            var configFile = new ConfigParser(multiLineDelimitedFilePath, new ConfigParserSettings(MultiLineValues.Simple));
            var multiLineDelimitedValue = configFile.GetValue("Multiline Values", "chorus", string.Empty);
            Assert.Equal(
                "I'm a lumberjack, and I'm okay\n" +
                "    I sleep all night and I work all day",
                multiLineDelimitedValue
            );
        }

        /// <summary>
        /// Checks if quote delimited (!) multiline values are parsed correctly.
        /// </summary>
        [Fact]
        public void DelimitedMultilineValuesAreParsedCorrectly()
        {
            var multiLineDelimitedFilePath = StructureSampleFiles.FirstOrDefault(f =>
                f.EndsWith("multi-line-delimited.ini", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(multiLineDelimitedFilePath);

            var configFile = new ConfigParser(multiLineDelimitedFilePath, new ConfigParserSettings(MultiLineValues.QuoteDelimitedValues));
            var multiLineDelimitedValue = configFile.GetValue("Multiline Values", "chorus", string.Empty);
            Assert.Equal(
                "I'm a lumberjack, and I'm okay\n" +
                "    I sleep all night and I work all day\n" +
                "\t",
                multiLineDelimitedValue
            );
        }

        /// <summary>
        /// Check if multi-line the values are not allowed with proper settings.
        /// </summary>
        [Fact]
        public void MultilineValuesAreNotAllowed()
        {
            var multiLineDelimitedFilePath = StructureSampleFiles.FirstOrDefault(f =>
                f.EndsWith("multi-line.ini", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(multiLineDelimitedFilePath);

            Exception multiLineException = Assert.Throws<ConfigParserException>(() =>
            {
                // ReSharper disable once ObjectCreationAsStatement
                // ReSharper disable once RedundantArgumentDefaultValue
                new ConfigParser(multiLineDelimitedFilePath, new ConfigParserSettings(MultiLineValues.NotAllowed));
            });

            Assert.True(
                new [] { "Multi-line values", "disallowed" }
                .All(s => multiLineException.Message?.Contains(s, StringComparison.InvariantCulture) ?? false)
            );
        }

        /// <summary>
        /// Checks if index access works.
        /// </summary>
        [Fact]
        public void IndexAccessWorks()
        {
            var indentedFilePath = StructureSampleFiles.FirstOrDefault(f => f.EndsWith("indented.ini", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(indentedFilePath);

            var configFile = new ConfigParser(indentedFilePath, new ConfigParserSettings(MultiLineValues.Simple));
            var valueReadUsingIndexing = configFile["Sections Can Be Indented"]["can_values_be_as_well"] ?? string.Empty;
            Assert.Equal(
                "True",
                valueReadUsingIndexing
            );
        }

        /// <summary>
            /// Gets the settings for file.
            /// </summary>
            /// <param name="pathToConfigFile">The path to configuration file.</param>
            /// <returns></returns>
        private static ConfigParserSettings GetSettingsForFile(string pathToConfigFile)
        {
            var realConfigSettingsPath = $"{pathToConfigFile}.json";
            if (!File.Exists(realConfigSettingsPath))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<ConfigParserSettings>(
                    File.ReadAllText(realConfigSettingsPath),
                    new JsonSerializerSettings
                    {
                        ContractResolver = new MultiLineValuesResolver()
                    });
            }
            catch
            {
                return null;
            }
        }
    }
}
