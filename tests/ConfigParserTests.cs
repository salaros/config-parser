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

        [Fact]
        public void ParseRealWorldFiles()
        {
            Assert.All(RealWorldConfigFiles, realConfigFile =>
            {
                ConfigParser config = null;
                try
                {
                    var realConfigSettingsPath = Path.Combine(
                        Directory.GetParent(realConfigFile).FullName,
                        $"{Path.GetFileNameWithoutExtension(realConfigFile)}.json"
                    );
                    
                    var jsonSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new MultuLineValuesResolver()
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

    internal class MultuLineValuesResolver : DefaultContractResolver
    {
        protected override JsonPrimitiveContract CreatePrimitiveContract(Type objectType)
        {
            var contract = base.CreatePrimitiveContract(objectType);
            if (objectType == typeof(MultuLineValues))
            {
                contract.Converter = new EnumConverter();
            }
            return contract;
        }
    }
}
