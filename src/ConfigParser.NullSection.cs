using System;
using System.Linq;

namespace Salaros.Configuration
{
    public partial class ConfigParser
    {
        public class NullConfigSection
        {
            private ConfigParser parent;

            public NullConfigSection(ConfigParser parent)
            {
                this.parent = parent;
            }

            public T GetValue<T>(string keyName, T defaultValue = default(T))
            {
                if (string.IsNullOrWhiteSpace(keyName))
                    throw new ArgumentException("Key name must be a non-empty string.", nameof(keyName));

                var iniKey = new ConfigKeyValue<T>(keyName, parent.Settings.KeyValueSeparator, defaultValue, -1);
                var key = parent.fileHeader.Section.Keys.FirstOrDefault(k => Equals(keyName, k.Name));
                if (key != null)
                    return (T)key.ValueRaw;

                parent.fileHeader.Section.AddLine(iniKey);
                return defaultValue;
            }

            public string GetValue(string keyName, string defaultValue = null) => GetValue<string>(keyName, defaultValue);
        }
    }
}
