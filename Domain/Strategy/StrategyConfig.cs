using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Domain.Strategy
{
    public class StrategyConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal PricePerTick { get; set; }

        public int Timeframe { get; set; }

        public StrategyTradingTimeWindow TradingWindowStartTime { get; set; }

        public StrategyTradingTimeWindow TradingWindowEndTime { get; set; }

        public decimal StartingAccountBalance { get; set; }

        public int MaxTradesPerSession { get; set; }

        public bool StopTradingAfterWinning { get; set; }

        public StrategyExecutionSettings ExecutionSettings { get; set; }        
    }
}
