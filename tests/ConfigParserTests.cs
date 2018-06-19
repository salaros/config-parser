using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace Salaros.Config.Tests
{
    public class IniParserTests
    {
        private static readonly string[] RealWorldConfigFiles;
        private static readonly string[] StructureSampleFiles;
        private static readonly string[] ValuesSampleFiles;

        /// <summary>
        /// Initializes the <see cref="IniParserTests"/> class.
        /// </summary>
        static IniParserTests()
        {
            // Allow the usage of ANSI encoding other than the default one 
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            RealWorldConfigFiles = Directory
                .GetFiles(Path.Combine(Environment.CurrentDirectory, "Resources", "RealWorld"))
                .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            StructureSampleFiles = Directory
                .GetFiles(Path.Combine(Environment.CurrentDirectory, "Resources", "Structure"))
                .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            ValuesSampleFiles = Directory
                .GetFiles(Path.Combine(Environment.CurrentDirectory, "Resources", "Values"))
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
            var arraySampleFilePath = ValuesSampleFiles.FirstOrDefault(f => f.EndsWith("array.cnf", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(arraySampleFilePath);

            var configFile = new ConfigParser(arraySampleFilePath, new ConfigParserSettings
            {
                MultiLineValues = MultiLineValues.Simple
            });
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

            var configFile = new ConfigParser(indentedFilePath, new ConfigParserSettings
            {
                MultiLineValues = MultiLineValues.Simple
            });
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

            var configFile = new ConfigParser(multiLineDelimitedFilePath, new ConfigParserSettings
            {
                MultiLineValues = MultiLineValues.Simple
            });
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

            var configFile = new ConfigParser(multiLineDelimitedFilePath, new ConfigParserSettings
            {
                MultiLineValues = MultiLineValues.QuoteDelimitedValues
            });
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
                new ConfigParser(multiLineDelimitedFilePath, new ConfigParserSettings
                {
                    MultiLineValues = MultiLineValues.NotAllowed
                });
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

            var configFile = new ConfigParser(indentedFilePath, new ConfigParserSettings
            {
                MultiLineValues = MultiLineValues.Simple
            });
            var valueReadUsingIndexing = configFile["Sections Can Be Indented"]["can_values_be_as_well"] ?? string.Empty;
            Assert.Equal(
                "True",
                valueReadUsingIndexing
            );
        }

        /// <summary>
        /// Checks if <see cref="bool"/> values are parsed correctly.
        /// </summary>
        [Fact]
        public void BooleanValuesAreParsedCorrectly()
        {
            var booleanValues = ValuesSampleFiles.FirstOrDefault(f =>
                f.EndsWith("boolean.ini", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(booleanValues);

            var configFileEnglish = new ConfigParser(booleanValues, new ConfigParserSettings
                {
                    MultiLineValues = MultiLineValues.QuoteDelimitedValues,
                    Encoding = Encoding.UTF8,
                    // if some day Boolean.ToString(IFormatProvider) will work 
                    // https://msdn.microsoft.com/en-us/library/s802ct92(v=vs.110).aspx#Anchor_1
                    Culture = new CultureInfo("en-US")
                }
            );

            const string simpleSection = "Simple";                                                      // [Simple]
            Assert.False(configFileEnglish.GetValue(simpleSection, "empty", false));                    // empty=
            Assert.True(configFileEnglish.GetValue(simpleSection, "numericTrue", false));               // numericTrue=1
            Assert.False(configFileEnglish.GetValue(simpleSection, "numericFalse", true));              // numericFalse=0
            Assert.True(configFileEnglish.GetValue(simpleSection, "textTrue", false));                  // textTrue = true
            Assert.False(configFileEnglish.GetValue(simpleSection, "textFalse", true));                 // textFalse = false

            // ReSharper disable once RedundantArgumentDefaultValue
            var yesNoConverter = new YesNoConverter("yes", "no");
            const string yesNoSection = "YesNo";                                                        // [YesNo]
            Assert.True(configFileEnglish.GetValue(yesNoSection, "sampleYes", false, yesNoConverter));  // sampleYes=Yes
            Assert.False(configFileEnglish.GetValue(yesNoSection, "sampleNo", true, yesNoConverter));   // sampleNo=no

            var onOffConverter = new YesNoConverter("on", "off");
            const string onOffSection = "OnOff";                                                        // [OnOff]
            Assert.True(configFileEnglish.GetValue(onOffSection, "sampleOn", false, onOffConverter));   // sampleOn=on
            Assert.False(configFileEnglish.GetValue(onOffSection, "sampleOff", true, onOffConverter));  // sampleOff=Off

            var enDisConverter = new YesNoConverter("Enabled", "Disabled");
            const string enDisSection = "EnabledDisabled";                                              // [EnabledDisabled]
            Assert.True(configFileEnglish.GetValue(enDisSection, "sampleOn", false, enDisConverter));   // sampleOn=on
            Assert.False(configFileEnglish.GetValue(enDisSection, "sampleOff", true, enDisConverter));  // sampleOff=Off
        }

        /// <summary>
        /// Checks if double values are parsed correctly.
        /// </summary>
        [Fact]
        public void DoubleValuesAreParsedCorrectly()
        {
            var booleanValues = ValuesSampleFiles.FirstOrDefault(f =>
                f.EndsWith("double.conf", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(booleanValues);

            var configFileUsEnglish = new ConfigParser(booleanValues, new ConfigParserSettings
            {
                Culture = new CultureInfo("en-US")
            });

            const string worksSection = "Works";                                                        // [Works]
            Assert.Equal(0D, configFileUsEnglish.GetValue(worksSection, "empty", 0D));                  // empty=
            Assert.Equal(1.0, configFileUsEnglish.GetValue(worksSection, "integer", 0D));               // integer=1
            Assert.Equal(1E-06, configFileUsEnglish.GetValue(worksSection, "usual", 0D));               // usual=0.000001
            Assert.Equal(6E-01, configFileUsEnglish.GetValue(worksSection, "withD", 0D));               // withD=0.6D
            Assert.Equal(1700D, configFileUsEnglish.GetValue(worksSection, "engineeringNotation", 0D)); // engineeringNotation = 1.7E+3
            Assert.Equal(45E-01, configFileUsEnglish.GetValue(worksSection, "float", 0D));              // float = 4.5f
            Assert.Equal(1000D, configFileUsEnglish.GetValue(worksSection, "thousands", 0D));           // thousands=1,000
            Assert.Equal(2999D, configFileUsEnglish.GetValue(worksSection, "dollars", 0D,               // dollars=$2,999
                NumberStyles.AllowCurrencySymbol));

            Assert.Throws<FormatException>(() =>
            {                                                                                           // [DoesntWork]
                Assert.Equal(0D, configFileUsEnglish.GetValue("DoesntWork", "random", 0D));             // random = sdgfery56d
            });

            var configFileItalian = new ConfigParser(booleanValues, new ConfigParserSettings
                { Culture = new CultureInfo("it-IT") });                                                // [ItalianLocalized]
            Assert.Equal(9.3D, configFileItalian.GetValue("ItalianLocalized", "withComa", 0D));         // withComa = 9,3
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
                        ContractResolver = new ConfigParserSettingsResolver(),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
            }
            catch(Exception ex)
            {
                Debug.Assert(null == ex.Message);
                return null;
            }
        }
    }
}
