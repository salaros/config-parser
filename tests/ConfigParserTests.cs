using System;
using System.IO;
using Xunit;

namespace Salaros.Config.Tests
{
    public class IniParserTests
    {
        private static readonly string[] RealWorldConfigFiles;

        static IniParserTests()
        {
            RealWorldConfigFiles = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Resources", "RealWorld"));
        }

        [Fact]
        public void ParseValidFiles()
        {
            Assert.All(RealWorldConfigFiles, realConfigFile =>
            {
                ConfigParser config = null;
                try
                {
                    config = new ConfigParser(realConfigFile);
                }
                finally
                {
                    Assert.NotNull(config);
                }
            });
        }
    }
}
