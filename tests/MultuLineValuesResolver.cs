using System;
using Koopman.CheckPoint.Json;
using Newtonsoft.Json.Serialization;

namespace Salaros.Config.Tests
{
    internal class MultuLineValuesResolver : DefaultContractResolver
    {
        /// <summary>
        /// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonPrimitiveContract" /> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// A <see cref="T:Newtonsoft.Json.Serialization.JsonPrimitiveContract" /> for the given type.
        /// </returns>
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
