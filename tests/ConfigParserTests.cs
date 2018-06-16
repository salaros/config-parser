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
                    var realConfigSettingsPath = $"{realConfigFile}.json";
                    var jsonSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new MultiLineValuesResolver()
                    };

                    var realConfigSettings = File.Exists(realConfigSettingsPath)
                        ? JsonConvert.DeserializeObject<ConfigParserSettings>(File.ReadAllText(realConfigSettingsPath), jsonSettings)
                        : null;

                    config = new ConfigParser(realConfigFile, realConfigSettings);
                }
                finally
                {
                    Assert.NotNull(config);
                }
            });
        }
    }
}
