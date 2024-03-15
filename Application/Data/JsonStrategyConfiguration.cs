using Athena.Domain.Interfaces;
using Athena.Domain.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Athena.Application.Data
{
    public class JsonStrategyConfiguration : IStrategyConfiguration
    {
        /// <summary>
        /// Reads the strategies.json file and deserializes it into a collection of <see cref="StrategyConfig"/> objects
        /// </summary>
        /// <param name="configFile">Fully qualified name of the strategies.json file</param>
        /// <returns>Deserialized collection of <see cref="StrategyConfig"/> objects</returns>
        public List<StrategyConfig> LoadStrategies()
        {
            var strategiesConfigFile = Path.Combine(@"I:\Windows Projects\athena-core", "strategies.json");

            using (StreamReader r = new StreamReader(strategiesConfigFile))
            {
                string json = r.ReadToEnd();
                var configs = JsonSerializer.Deserialize<List<StrategyConfig>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return configs;
            }
        }
    }
}
