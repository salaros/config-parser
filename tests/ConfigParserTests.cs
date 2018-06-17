using System;
using System.IO;
using System.Linq;
using Koopman.CheckPoint.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Salaros.Config.Tests
{
    public class IniParserTests
    {
        private static readonly string[] RealWorldConfigFiles;

        /// <summary>
        /// Initializes the <see cref="IniParserTests"/> class.
        /// </summary>
        static IniParserTests()
        {
            RealWorldConfigFiles = Directory
                .GetFiles(Path.Combine(Environment.CurrentDirectory, "Resources", "RealWorld"))
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
