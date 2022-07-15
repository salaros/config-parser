// MIT License
//
// Copyright (c) 2018 Tim Koopman
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace

namespace Koopman.CheckPoint.Json
{
    /// <inheritdoc />
    /// <summary>
    /// Converts an <see cref="System.Enum" /> to and from its name string value.
    /// </summary>
    internal class EnumConverter : JsonConverter
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumConverter" /> class.
        /// </summary>
        public EnumConverter(
                StringCases outputCase,
                string outputSplitWords,
                OutputArrayOptions outputArray,
                bool nullAsZero)
        {
            OutputArray = outputArray;
            OutputCase = outputCase;
            OutputSplitWords = outputSplitWords;
            NullAsZero = nullAsZero;
        }

        public EnumConverter() : this(StringCases.NoChange, null, OutputArrayOptions.Auto, false)
        {
        }

        public EnumConverter(StringCases outputCase) : this(outputCase, null, OutputArrayOptions.Auto, false)
        {
        }

        public EnumConverter(StringCases outputCase, string outputSplitWords) : this(outputCase, outputSplitWords, OutputArrayOptions.Auto, false)
        {
        }

        #endregion Constructors

        #region Enums

        public enum OutputArrayOptions
        {
            Auto,
            Yes,
            No
        }

        // String case to write values in.
        public enum StringCases
        {
            NoChange,
            Uppercase,
            Lowercase
        }

        #endregion Enums

        #region Properties

        public bool NullAsZero { get; }
        public OutputArrayOptions OutputArray { get; }
        public StringCases OutputCase { get; }
        public string OutputSplitWords { get; }

        #endregion Properties

        #region Methods

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
            var t = (GetIsNullable(objectType))
                ? Nullable.GetUnderlyingType(objectType)
                : objectType;

            return t.GetTypeInfo().IsEnum;
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isNullable = GetIsNullable(objectType);
            var t = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;

            if (reader.TokenType == JsonToken.Null)
            {
                if (NullAsZero)
                    return 0;

                if (!isNullable)
                    throw new JsonSerializationException($"Cannot convert null value to {objectType}.");

                return null;
            }

            try
            {
                if (reader.TokenType == JsonToken.String)
                {
                    string enumText = reader.Value.ToString();

                    if (enumText == string.Empty)
                    {
                        if (NullAsZero)
                            return 0;

                        if (isNullable)
                            return null;
                    }

                    return DeserializeValue(enumText, t);
                }

                if (reader.TokenType == JsonToken.StartArray)
                {
                    int result = 0;
                    reader.Read();
                    while (reader.TokenType == JsonToken.String)
                    {
                        string enumText = reader.Value.ToString();
                        if (enumText != string.Empty)
                        {
                            result |= (int)DeserializeValue(enumText, t);
                        }
                        reader.Read();
                    }
                    return result;
                }
            }
            catch (Exception)
            {
                throw new JsonSerializationException($"Error converting value {reader.Value} to type '{objectType}'.");
            }

            throw new JsonSerializationException($"Error converting value {reader.Value} to type '{objectType}'.");
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            bool isNullable = GetIsNullable(value.GetType());
            var t = isNullable ? Nullable.GetUnderlyingType(value.GetType()) : value.GetType();

            bool asArray = (OutputArray == OutputArrayOptions.Auto) ?
                t.GetTypeInfo().GetCustomAttribute(typeof(FlagsAttribute)) != null :
                OutputArray == OutputArrayOptions.Yes;

            var e = (Enum)value;
            if (asArray)
            {
                writer.WriteStartArray();

                if (value != null)
                {
                    var values = new List<string>(); string[] flags = t.GetTypeInfo().GetEnumNames();
                    foreach (string flag in flags)
                    {
                        var flagEnum = (Enum)Enum.Parse(t, flag);
                        var flagInfo = t.GetTypeInfo().GetMember(flag);
                        var ignore = flagInfo[0].GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null;
                        if (!ignore && e.HasFlag(flagEnum))
                            writer.WriteValue(SerializeValue(flag, t));
                    }
                }

                writer.WriteEndArray();
            }
            else if (value == null)
                writer.WriteNull();
            else
            {
                var flagInfo = t.GetTypeInfo().GetMember(e.ToString());
                bool ignore = flagInfo[0].GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null;
                if (ignore)
                    writer.WriteNull();
                else
                    writer.WriteValue(SerializeValue(e.ToString(), t));
            }
        }

        private static bool GetIsNullable(Type objectType) => (objectType.GetTypeInfo().IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>));

        private object DeserializeValue(string value, Type type)
        {
            string enumText = value;
            // Check EnumMember Values first
            string[] flags = type.GetTypeInfo().GetEnumNames();
            foreach (string flag in flags)
            {
                var flagEnum = (Enum)Enum.Parse(type, flag);
                var flagInfo = type.GetTypeInfo().GetMember(flag);
                var att = (EnumMemberAttribute)flagInfo[0].GetCustomAttribute(typeof(EnumMemberAttribute));
                if (att != null && att.Value.Equals(enumText, StringComparison.CurrentCultureIgnoreCase))
                    return flagEnum;
            }

            // Remove SplitWordsWith from value so they can be matched to CamelCase Enum values
            if (OutputSplitWords != null)
                enumText = enumText.Replace(OutputSplitWords, "");

            return (Enum)Enum.Parse(enumType: type, value: enumText, ignoreCase: true);
        }

        private string SerializeValue(string value, Type type)
        {
            string v = value;
            var enumInfo = type.GetTypeInfo().GetMember(v);

            if (enumInfo != null && enumInfo.Length == 1)
            {
                var att = (EnumMemberAttribute)enumInfo[0].GetCustomAttribute(typeof(EnumMemberAttribute));
                if (att != null)
                    return att.Value;
            }

            if (OutputSplitWords != null)
                v = v.CamelCaseToRegular(OutputSplitWords);

            switch (OutputCase)
            {
                case StringCases.Lowercase:
                    v = v.ToLower();
                    break;

                case StringCases.Uppercase:
                    v = v.ToUpper();
                    break;
            }

            return v;
        }

        #endregion Methods
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string that is in CamelCase to a string array of separate words.
        /// </summary>
        /// <param name="source">The source string to convert.</param>
        /// <returns>String array to words</returns>
        /// <example>
        /// The following code:
        /// <code>
        /// "ThisIsInCamelCase".CamelCaseToArray();
        ///
        /// Result:
        /// ["This", "Is", "In", "Camel", "Case"]
        /// </code>
        /// </example>
        internal static string[] CamelCaseToArray(this string source)
        {
            return Regex.Matches(source, @"(^[a-z\d]+|[A-Z\d]+(?![a-z])|[A-Z][a-z\d]+)")
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();
        }

        /// <summary>
        /// Converts a string that is in CamelCase to a regular string.
        /// </summary>
        /// <param name="source">The source string to convert.</param>
        /// <param name="separator">The separator to use between words.</param>
        /// <example>
        /// The following code:
        /// <code>
        /// "ThisIsInCamelCase".CamelCaseToRegular();
        ///
        /// Result:
        /// "This Is In Camel Case"
        /// </code>
        /// </example>
        internal static string CamelCaseToRegular(this string source, string separator = " ")
        {
            string[] s = CamelCaseToArray(source);
            return string.Join(separator, s);
        }
    }
}