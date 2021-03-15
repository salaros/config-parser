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

        [Fact]
        public void ArrayIsReadCorrectly()
        {
            // Set up
            var settings = new ConfigParserSettings { MultiLineValues = MultiLineValues.Simple | MultiLineValues.QuoteDelimitedValues };
            var configFile = new ConfigParser(
                @"[Advanced]
                Select =
                     select * from
                     from table
                     where ID = '5'
                ",
                settings);

            // Act
            var arrayValues = configFile.GetArrayValue("Advanced", "Select");

            // Assert
            Assert.Equal(3, arrayValues?.Length ?? 0);
            Assert.Equal("select * from", arrayValues[0]);
            Assert.Equal("from table", arrayValues[1]);
            Assert.Equal("where ID = '5'", arrayValues[2]);
        }

        [Fact]
        public void JoinMultilineValueWorks()
        {
            // Set up
            var settings = new ConfigParserSettings { MultiLineValues = MultiLineValues.Simple };
            var configFile = new ConfigParser(
                @"[Advanced]
ExampleValue = Lorem ipsum dolor sit amet
consectetur adipiscing elit
sed do eiusmod tempor incididunt
                ",
                settings);

            // Act
            var multiLineJoint = configFile.JoinMultilineValue("Advanced", "ExampleValue", " ");

            // Assert
            Assert.Equal("Lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt", multiLineJoint);
        }
    }
}
