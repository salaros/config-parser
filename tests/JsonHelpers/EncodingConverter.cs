using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Salaros.Configuration.Tests
{
    public class EncodingConverter : JsonConverter
    {
        /// <inheritdoc />
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Encoding).IsAssignableFrom(objectType);
        }

        /// <inheritdoc />
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var json = JsonConvert.SerializeObject((EncodingPortable)(Encoding)value);
            writer.WriteRawValue(json);
        }

        /// <inheritdoc />
        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!(reader is JsonTextReader))
                return null;

            var encodingPortable = new EncodingPortable();
            while (reader.Read())
            {
                switch (reader.Value)
                {
                    case nameof(EncodingPortable.CodePage) when reader.Read():
                        encodingPortable.CodePage = int.Parse(reader.Value?.ToString());
                        break;

                    case nameof(EncodingPortable.WithPreamble) when reader.Read():
                        encodingPortable.WithPreamble = bool.Parse(reader.Value?.ToString());
                        break;

                    case null:
                        return (Encoding)encodingPortable;
                }
            }

            return null;
        }

        internal class EncodingPortable
        {
            public int CodePage { get; internal set; } = -1;

            public bool WithPreamble { get; internal set; } = true;

            // User-defined conversion from Digit to double
            public static implicit operator EncodingPortable(Encoding encoding)
            {
                if (encoding == null) throw new ArgumentNullException(nameof(encoding));

                return new EncodingPortable
                {
                    CodePage = encoding.CodePage,
                    WithPreamble = encoding.GetPreamble()?.Any() ?? false
                };
            }
            //  User-defined conversion from double to Digit
            public static implicit operator Encoding(EncodingPortable encodingPortable)
            {
                if (encodingPortable == null) throw new ArgumentNullException(nameof(encodingPortable));

                switch (encodingPortable.CodePage)
                {
                    case 65001:
                        return new UTF8Encoding(encodingPortable.WithPreamble);

                    default:
                        return Encoding.GetEncoding(encodingPortable.CodePage);
                }
            }
        }
    }
}
