using System;
using Koopman.CheckPoint.Json;
using Newtonsoft.Json.Serialization;

namespace Salaros.Config.Tests
{
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
