﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace Salaros.Configuration.Tests
{
    public class IntegrationTests
    {
        private static readonly string[] RealWorldConfigFiles;
        private static readonly string[] StructureSampleFiles;
        private static readonly string[] ValuesSampleFiles;
        private static readonly string[] EncodingSampleFiles;

        /// <summary>
        /// Initializes the <see cref="IntegrationTests"/> class.
        /// </summary>
        static IntegrationTests()
        {
#if !NET40_OR_GREATER
            // Allow the usage of ANSI encoding other than the default one
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

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

            EncodingSampleFiles = Directory
                .GetFiles(Path.Combine(Environment.CurrentDirectory, "Resources", "Encoding"))
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

                var tempConfigFilePath = Path.GetTempFileName();
                configFile.Save(tempConfigFilePath);
                configFileFromDisk = File
                    .ReadAllText(tempConfigFilePath)
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
            var arraySampleFilePath =
                ValuesSampleFiles.FirstOrDefault(f => f.EndsWith("array.cnf", StringComparison.OrdinalIgnoreCase));
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
            var excludeArrayFromFile = configFile.GetValue("settings", "exclude", new string[] { });
            Assert.Equal(excludeArrayFromFile, excludeArray);
        }

        /// <summary>
        /// Checks if indented files are parsed correctly.
        /// </summary>
        [Fact]
        public void IndentedFilesAreParsedCorrectly()
        {
            var indentedFilePath =
                StructureSampleFiles.FirstOrDefault(f =>
                    f.EndsWith("indented.ini", StringComparison.OrdinalIgnoreCase));
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
                MultiLineValues = MultiLineValues.Simple,
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
                new[] {"Multi-line values", "disallowed"}
                    .All(s => multiLineException.Message?.Contains(s
#if !NET40_OR_GREATER
                    , StringComparison.InvariantCulture
#endif
                    ) ?? false)
            );
        }

        /// <summary>
        /// Checks if index access works.
        /// </summary>
        [Fact]
        public void IndexAccessWorks()
        {
            var indentedFilePath =
                StructureSampleFiles.FirstOrDefault(f =>
                    f.EndsWith("indented.ini", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(indentedFilePath);

            var configFile = new ConfigParser(indentedFilePath, new ConfigParserSettings
            {
                MultiLineValues = MultiLineValues.Simple
            });
            var valueReadUsingIndexing =
                configFile["Sections Can Be Indented"]["can_values_be_as_well"] ?? string.Empty;
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
                    Culture = new CultureInfo("en-US"),
                    BooleanConverter = new YesNoConverter("vero", "falso")
                }
            );

            const string valoriItaliani = "ValoriItaliani"; // [ValoriItaliani]
            Assert.True(configFileEnglish.GetValue(valoriItaliani, "positivo", false)); // positivo = vero
            Assert.False(configFileEnglish.GetValue(valoriItaliani, "sampleOff", true)); // sampleOff = falso

            const string simpleSection = "Simple"; // [Simple]
            Assert.False(configFileEnglish.GetValue(simpleSection, "empty", false)); // empty=
            Assert.True(configFileEnglish.GetValue(simpleSection, "numericTrue", false)); // numericTrue=1
            Assert.False(configFileEnglish.GetValue(simpleSection, "numericFalse", true)); // numericFalse=0
            Assert.True(configFileEnglish.GetValue(simpleSection, "textTrue", false)); // textTrue = true
            Assert.False(configFileEnglish.GetValue(simpleSection, "textFalse", true)); // textFalse = false

            // ReSharper disable once RedundantArgumentDefaultValue
            var yesNoConverter = new YesNoConverter(
                "yes", // ReSharper disable once RedundantArgumentDefaultValue
                "no"
            );
            const string yesNoSection = "YesNo"; // [YesNo]
            Assert.True(configFileEnglish.GetValue(yesNoSection, "sampleYes", false, yesNoConverter)); // sampleYes=Yes
            Assert.False(configFileEnglish.GetValue(yesNoSection, "sampleNo", true, yesNoConverter)); // sampleNo=no
            var onOffConverter = new YesNoConverter("on", "off");
            const string onOffSection = "OnOff"; // [OnOff]
            Assert.True(configFileEnglish.GetValue(onOffSection, "sampleOn", false, onOffConverter)); // sampleOn=on
            Assert.False(configFileEnglish.GetValue(onOffSection, "sampleOff", true, onOffConverter)); // sampleOff=Off
            var enDisConverter = new YesNoConverter("Enabled", "Disabled");
            const string enDisSection = "EnabledDisabled"; // [EnabledDisabled]
            Assert.True(configFileEnglish.GetValue(enDisSection, "sampleOn", false, enDisConverter)); // sampleOn=on
            Assert.False(configFileEnglish.GetValue(enDisSection, "sampleOff", true, enDisConverter)); // sampleOff=Off

            // =========================================================================================
            // +++++++++   Wow, boolean values parsed auto-magically: look mum no converters   +++++++++
            // =========================================================================================// [YesNo]
            Assert.True(configFileEnglish.GetValue(yesNoSection, "sampleYes", false)); // sampleYes=Yes
            Assert.False(configFileEnglish.GetValue(yesNoSection, "sampleNo", true)); // sampleNo=no

            //                                                                                          // [OnOff]
            Assert.True(configFileEnglish.GetValue(onOffSection, "sampleOn", false)); // sampleOn=on
            Assert.False(configFileEnglish.GetValue(onOffSection, "sampleOff", true)); // sampleOff=Off

            //                                                                                          // [EnabledDisabled]
            Assert.True(configFileEnglish.GetValue(enDisSection, "sampleOn", false)); // sampleOn=on
            Assert.False(configFileEnglish.GetValue(enDisSection, "sampleOff", true)); // sampleOff=Off
        }

        /// <summary>
        /// Checks if boolean values are written correctly.
        /// </summary>
        [Fact]
        public void BooleanValuesAreWrittenCorrectly()
        {
            var configBooleanSampleFilePath = Path.GetTempFileName();
            var configFileEnglish = new ConfigParser(configBooleanSampleFilePath);

            // Test "normal" and auto-magic set value
            const string yesNoSection = "YesNo", sampleYes = "sampleYesNo";
            configFileEnglish.SetValue(yesNoSection, sampleYes, true, new YesNoConverter()); // set to "yes"
            Assert.True(configFileEnglish.GetValue(yesNoSection, sampleYes, false)); // check if gets true ("yes")

            configFileEnglish.SetValue(yesNoSection, sampleYes, false); // set to false ("no")
            Assert.Equal("no",
                configFileEnglish.GetValue(yesNoSection, sampleYes, "yes")); // check if gets "no" (false)
            Assert.False(configFileEnglish.GetValue(yesNoSection, sampleYes, true)); // check if gets false ("no")


            // Test "normal" and auto-magic set value
            const string intSection = "Integer", sampleInt = "sample0and1";
            configFileEnglish.SetValue(intSection, sampleInt, false, new YesNoConverter("1", "0")); // set to "1"
            Assert.False(configFileEnglish.GetValue(intSection, sampleInt, true)); // check if gets true ("1")

            configFileEnglish.SetValue(intSection, sampleInt, true); // set to true ("1")
            Assert.Equal("1", configFileEnglish.GetValue(intSection, sampleInt, "0")); // check if gets "1" (true)
            Assert.True(configFileEnglish.GetValue(intSection, sampleInt, false)); // check if gets true ("1")
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

            const string worksSection = "Works"; // [Works]
            Assert.Equal(0D, configFileUsEnglish.GetValue(worksSection, "empty", 0D)); // empty=
            Assert.Equal(1.0, configFileUsEnglish.GetValue(worksSection, "integer", 0D)); // integer=1
            Assert.Equal(1E-06, configFileUsEnglish.GetValue(worksSection, "usual", 0D)); // usual=0.000001
            Assert.Equal(6E-01, configFileUsEnglish.GetValue(worksSection, "withD", 0D)); // withD=0.6D
            Assert.Equal(1700D,
                configFileUsEnglish.GetValue(worksSection, "engineeringNotation", 0D)); // engineeringNotation = 1.7E+3
            Assert.Equal(45E-01, configFileUsEnglish.GetValue(worksSection, "float", 0D)); // float = 4.5f
            Assert.Equal(1000D, configFileUsEnglish.GetValue(worksSection, "thousands", 0D)); // thousands=1,000
            Assert.Equal(2999D, configFileUsEnglish.GetValue(worksSection, "dollars", 0D, // dollars=$2,999
                NumberStyles.AllowCurrencySymbol));

            Assert.Throws<FormatException>(() =>
            {
                // [DoesntWork]
                Assert.Equal(0D, configFileUsEnglish.GetValue("DoesntWork", "random", 0D)); // random = sdgfery56d
            });

            var configFileItalian = new ConfigParser(booleanValues, new ConfigParserSettings
                {Culture = new CultureInfo("it-IT")}); // [ItalianLocalized]
            Assert.Equal(9.3D, configFileItalian.GetValue("ItalianLocalized", "withComa", 0D)); // withComa = 9,3
        }

        /// <summary>
        /// Checks if encoding setting works correctly.
        /// </summary>
        [Fact]
        public void EncodingSettingWorksCorrectly()
        {
            // All kinds of UTF* work
            Assert.All(
                EncodingSampleFiles.Where(f =>
                    Path.GetFileName(f).StartsWith("UTF", StringComparison.InvariantCultureIgnoreCase)),
                encodingSampleFile =>
                {
                    var settings = GetSettingsForFile(encodingSampleFile);
                    var encodingConfigFile = new ConfigParser(encodingSampleFile, settings);

                    Assert.Equal("국민경제의 발전을 위한 중요정책의 수립에 관하여 대통령의 자문에 응하기 위하여 국민경제자문회의를 둘 수 있다.",
                        encodingConfigFile["LoremIpsum"]["한국어"]);

                    Assert.Equal(
                        "είναι απλά ένα κείμενο χωρίς νόημα για τους επαγγελματίες της τυπογραφίας και στοιχειοθεσίας",
                        encodingConfigFile["LoremIpsum"]["Ελληνικά"]);

                    Assert.Equal("也称乱数假文或者哑元文本， 是印刷及排版领域所常用的虚拟文字。",
                        encodingConfigFile["LoremIpsum"]["中文"]);

                    Assert.Equal("旅ロ京青利セムレ弱改フヨス波府かばぼ意送でぼ調掲察たス日西重ケアナ住橋ユムミク順待ふかんぼ人奨貯鏡すびそ。",
                        encodingConfigFile["LoremIpsum"]["日本語"]);

                    Assert.Equal("छपाई और अक्षर योजना उद्योग का एक साधारण डमी पाठ है",
                        encodingConfigFile["LoremIpsum"]["हिंदी"]);

                    Assert.Equal("זוהי עובדה מבוססת שדעתו של הקורא תהיה מוסחת על ידי טקטס קריא כאשר הוא יביט בפריסתו",
                        encodingConfigFile["LoremIpsum"]["אנגלית"]);

                    Assert.Equal(
                        "Lorem ipsum – псевдо-латинский текст, который используется для веб дизайна, типографии",
                        encodingConfigFile["LoremIpsum"]["Русский"]);
                });

            // ANSI Cyrillic - works
            var ansiCyrillicFilePath = EncodingSampleFiles.FirstOrDefault(f =>
                f.EndsWith("ANSI Cyrillic.txt", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(ansiCyrillicFilePath);
            var ansiCyrillicFile = new ConfigParser(ansiCyrillicFilePath, new ConfigParserSettings
            {
                Encoding = Encoding.GetEncoding(1251)
            });
            Assert.Equal("Значение", ansiCyrillicFile["Тест"]["Ключ"]);

            // ANSI Cyrillic - encoding is misspecified
            Assert.Throws<DecoderFallbackException>(() =>
            {
                // ReSharper disable once UnusedVariable
                var willNeverWork = new ConfigParser(ansiCyrillicFilePath, new ConfigParserSettings
                {
                    Encoding = new UTF8Encoding(true, true)
                });
            });
            var invalidDataException = Assert.Throws<InvalidDataException>(() =>
            {
                // ReSharper disable once UnusedVariable
                var willNeverWork = new ConfigParser(ansiCyrillicFilePath);
            });
            Assert.True(invalidDataException.Message?.Contains("detect encoding"
#if !NET40_OR_GREATER
                , StringComparison.InvariantCulture
#endif
            ));

            // ANSI Latin1 - works
            var ansiLatin1FilePath = EncodingSampleFiles.FirstOrDefault(f =>
                f.EndsWith("ANSI Latin1.txt", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(ansiLatin1FilePath);
            var ansiLatin1File = new ConfigParser(ansiLatin1FilePath, new ConfigParserSettings
            {
                Encoding = Encoding.GetEncoding(1252)
            });
            Assert.Equal("información", ansiLatin1File["sección"]["configuración"]);
        }

        /// <summary>
        /// Checks if empty file initialization work.
        /// </summary>
        [Fact]
        public void EmptyFileInitializationWork()
        {
            var configBooleanSampleFilePath = Path.GetTempFileName();
            ConfigParser configFileEmpty = null;
            try
            {
                configFileEmpty = new ConfigParser(configBooleanSampleFilePath);
            }
            finally
            {
                Assert.NotNull(configFileEmpty);
            }
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
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    });
            }
            catch (Exception ex)
            {
                Debug.Assert(null == ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Checks if non-existing file works.
        /// </summary>
        [Fact]
        public void NonExistingFileWorks()
        {
            ConfigParser configFileFromMem = null;
            try
            {
                var notExistingFilePath = $"{Path.GetTempFileName()}.conf";
                configFileFromMem = new ConfigParser(notExistingFilePath, new ConfigParserSettings
                {
                    MultiLineValues = MultiLineValues.Simple | MultiLineValues.QuoteDelimitedValues,
                    Encoding = new UTF8Encoding(false, false),
                    NewLine = "\n"
                });
            }
            finally
            {
                Assert.NotNull(configFileFromMem);
            }
        }

        /// <summary>
        /// Checks if file built from scratch works.
        /// </summary>
        [Fact]
        public void FileBuiltFromScratchWorks()
        {
            var configFileFromMem = new ConfigParser(new ConfigParserSettings
            {
                Encoding = Encoding.Unicode,
                NewLine = "\n",
                Culture = CultureInfo.InvariantCulture
            });

            configFileFromMem.GetValue("section1", "string", "value");
            configFileFromMem.GetValue("section2", "bool", true);
            configFileFromMem.SetValue("section2", "bool", false);
            configFileFromMem.SetValue("section2", "bool", true);
            configFileFromMem.GetValue("section3", "doble", 00000.2);

            var fromScratchFile = StructureSampleFiles
                .FirstOrDefault(f => f.EndsWith("from-scratch.conf", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(fromScratchFile);

            var configContentFromDisk = File.ReadAllText(fromScratchFile);
            var configContentBuild = configFileFromMem.ToString();
            Assert.Equal(configContentFromDisk, configContentBuild);
        }

        /// <summary>
        /// Checks if configuration file parsed from string works.
        /// </summary>
        [Fact]
        public void ParsingConfigFileStringWorks()
        {
            var configFileFromString = new ConfigParser(@"
                [Strings]
                    canBeIndented = value
                andQuoted = ""quotes will be stripped""

                [Numbers]
                withD = 0.6D
                dollars = $2,999

                [boolean]
                numericTrue = 1
                textFalse = true
                yesWorks = yes
                upperCaseWorks = on
                worksAsWell = Enabled

                [Advanced]
                arrayWorkToo =
                    arrayElement1
                    arrayElement2
                valueLessKey",
                new ConfigParserSettings
                {
                    MultiLineValues = MultiLineValues.Simple | MultiLineValues.AllowValuelessKeys |
                                      MultiLineValues.QuoteDelimitedValues,
                    Culture = new CultureInfo("en-US"),

                }
            );

            // value
            Assert.Equal("value", configFileFromString.GetValue("Strings", "canBeIndented"));

            // quotes will be stripped
            Assert.Equal("quotes will be stripped", configFileFromString.GetValue("Strings", "andQuoted"));

            // 0.6
            Assert.Equal(0.6D, configFileFromString.GetValue("Numbers", "withD", 0D));

            // 2999
            Assert.Equal(2999D, configFileFromString.GetValue("Numbers", "dollars", 0D,
                NumberStyles.AllowCurrencySymbol));

            // $2,999
            Assert.Equal("$2,999", configFileFromString.GetValue("Numbers", "dollars"));

            // all "True"
            Assert.True(configFileFromString.GetValue("boolean", "numericTrue", false));
            Assert.True(configFileFromString.GetValue("boolean", "textFalse", false));
            Assert.True(configFileFromString.GetValue("boolean", "yesWorks", false));
            Assert.True(configFileFromString.GetValue("boolean", "upperCaseWorks", false));
            Assert.True(configFileFromString.GetValue("boolean", "worksAsWell", false));

            // ["arrayElement1","arrayElement2"]
            Assert.Equal(new[] {"arrayElement1", "arrayElement2"},
                configFileFromString.GetArrayValue("Advanced", "arrayWorkToo"));

            // null
            Assert.Null(configFileFromString.GetValue("Advanced", "valueLessKey"));
        }


        /// <summary>
        /// Reading non-existing empty values, then writing, then re-reading config file again.
        /// </summary>
        [Fact]
        public void ReadingWritingReadingConfigFile()
        {
            var configFilepath = $"{Path.GetTempFileName()}.wakatime.cfg";
            var configParser = new ConfigParser(
                configFilepath,
                new ConfigParserSettings
                {
                    MultiLineValues = MultiLineValues.Simple,
                    Encoding = new UTF8Encoding(false, false),
                    NewLine = "\r\n",
                }
            );

            configParser.GetValue("settings", "api_key", string.Empty);
            configParser.GetValue("settings", "debug", false);
            configParser.GetValue("settings", "proxy", string.Empty);

            configParser.SetValue("settings", "api_key", new Guid("00000000-0000-0000-0000-000000000000").ToString());
            configParser.SetValue("settings", "debug", true);
            configParser.SetValue("settings", "proxy", string.Empty);

            configParser.Save(configFilepath);

            var configFileContent = File.ReadAllText(configFilepath, Encoding.UTF8);
            var configExpectedBuilder = new StringBuilder();
            configExpectedBuilder.AppendLine("[settings]");
            configExpectedBuilder.AppendLine("api_key=00000000-0000-0000-0000-000000000000");
            configExpectedBuilder.AppendLine("debug=true");
            configExpectedBuilder.AppendLine("proxy=");
            var configExpectedContent = configExpectedBuilder.ToString().TrimEnd(new char[] { '\r', '\n' });

            Assert.Equal(configExpectedContent, configFileContent);
        }


        /// <summary>
        /// Tests if the usage of unicode symbols is allowed.
        /// </summary>
        [Fact]
        public void AreUnicodeSymbolsAllowed()
        {
            // Arrange
            var tempConfigFilePath = Path.GetTempFileName();
            var configParser = new ConfigParser(
               tempConfigFilePath,
               new ConfigParserSettings
               {
                   MultiLineValues = MultiLineValues.QuoteDelimitedValues,
                   KeyValueSeparator = " = ",
                   Encoding = new UTF8Encoding(false, false)
               }
            );

            configParser.SetValue("Temp", "name", "Windows");
            configParser.SetValue("Temp", "name.company", "Microsoft \u00A9");

            var configFromText = new StringBuilder();
            configFromText.AppendLine("[Temp]");
            configFromText.AppendLine("name = \"Windows\"");
            configFromText.AppendLine("name.company = \"Microsoft ©\"");

            // Act
            var configParserString = configParser.ToString().TrimEnd(new char[] { '\r', '\n' });
            var configFromTextString = configFromText.ToString().TrimEnd(new char[] { '\r', '\n' });

            // Assert
            Assert.Equal(configParserString, configFromTextString, ignoreCase: true);
        }

        /// <summary>
        /// Tests if the usage of unicode symbols is allowed.
        /// </summary>
        [Fact]
        public void IsSectionLessKeyLessFileParsed()
        {
            // Arrange
            var redisConfig = RealWorldConfigFiles.FirstOrDefault(f => f.EndsWith("redis.conf", StringComparison.OrdinalIgnoreCase));
            var configParser = new ConfigParser(
               redisConfig,
               new ConfigParserSettings
               {
                   MultiLineValues = MultiLineValues.AllowValuelessKeys | MultiLineValues.AllowEmptyTopSection,
                   KeyValueSeparator = " ",
               }
            );

            // Act
            var clientBufferLimitValue = configParser.NullSection.GetValue("client-output-buffer-limit");

            // Assert
            Assert.Equal("normal 0 0 0", clientBufferLimitValue, ignoreCase: true);
        }

        [Fact]
        public void IsCustomNumberFormatWorking_ThousandSeparator()
        {
            // Arrange
            var tempConfigFilePath = Path.GetTempFileName();
            var configParser = new ConfigParser(
               tempConfigFilePath,
               new ConfigParserSettings
               {
                   MultiLineValues = MultiLineValues.QuoteDelimitedValues,
                   KeyValueSeparator = " = ",
                   Encoding = new UTF8Encoding(false, false)
               }
            );

            configParser.SetValue("FormatNumber", "test.int", "1_234_567");
            configParser.SetValue("FormatNumber", "test.int2", "1_000");
            configParser.SetValue("FormatNumber", "double", "991_228_224_617.445");

            // Act
            var numberFormatInfo = new NumberFormatInfo {
                NumberGroupSeparator = "_",
                NumberDecimalSeparator = ".",
                CurrencyDecimalSeparator = "."
            };
            var testInt1 = configParser.GetValue("FormatNumber", "test.int", 0, NumberStyles.Integer, numberFormatInfo);
            var testInt2 = configParser.GetValue("FormatNumber", "test.int2", 0, NumberStyles.Integer, numberFormatInfo);
            var testDouble = configParser.GetValue("FormatNumber", "double", 0D, NumberStyles.Currency, numberFormatInfo);

            // Assert
            Assert.Equal(1234567, testInt1);
            Assert.Equal(1000, testInt2);
            Assert.Equal(991228224617.445, testDouble);
        }
    }
}
