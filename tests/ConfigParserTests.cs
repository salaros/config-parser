using System;
using System.IO;
using System.Threading;
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

        [Fact]
        public void FileCanBeSavedToTheSamePath()
        {
            var configFilePathTmp = Path.GetTempFileName();
            File.WriteAllLines(configFilePathTmp, new[]
            {
                "[Baz]",
                "Foo = bar"
            });
            var dateModifiedOld = File.GetLastWriteTime(configFilePathTmp);

            var configFile = new ConfigParser(configFilePathTmp);
            configFile.Save();

            Assert.True(File.GetLastWriteTime(configFilePathTmp).Ticks >= dateModifiedOld.Ticks);
        }

        [Fact]
        public void FileCanBeSavedToNewPath()
        {
            var configFilePathTmp = Path.GetTempFileName();
            File.WriteAllLines(configFilePathTmp, new[]
            {
                "[Baz]",
                "Foo = bar"
            });

            var configFile = new ConfigParser(configFilePathTmp);
            var configFilePathTmpNew = Path.GetTempFileName();
            configFile.Save(configFilePathTmpNew);

            Assert.True(File.Exists(configFilePathTmpNew));
        }
    }
}
