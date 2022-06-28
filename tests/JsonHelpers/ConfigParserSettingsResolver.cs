using System;
using System.Globalization;
using System.Text;
using Koopman.CheckPoint.Json;
using Newtonsoft.Json.Serialization;

namespace Salaros.Configuration.Tests
{
    internal class ConfigParserSettingsResolver : DefaultContractResolver
    {
        /// <inheritdoc />
        /// <summary>
        /// Creates a <see cref="JsonPrimitiveContract" /> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// A <see cref="JsonPrimitiveContract" /> for the given type.
        /// </returns>
        protected override JsonPrimitiveContract CreatePrimitiveContract(Type objectType)
        {
            var contract = base.CreatePrimitiveContract(objectType);
            if (objectType == typeof(MultiLineValues))
            {
                contract.Converter = new EnumConverter();
            }
            return contract;
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a <see cref="JsonObjectContract" /> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// A <see cref="JsonObjectContract" /> for the given type.
        /// </returns>
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            if (objectType == null) throw new ArgumentNullException(nameof(objectType));

            var contract = base.CreateObjectContract(objectType);
            switch (objectType)
            {
                case var _ when objectType == typeof(Encoding) || objectType.IsSubclassOf(typeof(Encoding)):
                    contract.Converter = new EncodingConverter();
                    break;

                case var _ when objectType == typeof(CultureInfo):
                    contract.Converter = new CultureConverter();
                    break;
            }

            return contract;
        }
    }
}
