using Athena.Domain.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Domain.Interfaces
{
    public interface IStrategyConfiguration
    {
        List<StrategyConfig> LoadStrategies();
    }
}
