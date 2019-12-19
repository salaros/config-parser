using System.IO;
using Xunit;

namespace Salaros.Configuration.Tests
{
    public class ConfigParserTests
    {
        [Fact]
        public void ExistingFileIsUpdatedCorrectly()
        {
            var configFilePathTmp = Path.GetTempFileName();
            File.WriteAllLines(configFilePathTmp, new[]
            {
                "[Settings]",
                "Recno = chocolate"
            });

            var configFile = new ConfigParser(configFilePathTmp);
            configFile.SetValue("Settings", "Recno", "123");
            configFile.Save(configFilePathTmp);

            Assert.Equal("[Settings]\r\nRecno = 123", configFile.ToString());
        }
    }
}
