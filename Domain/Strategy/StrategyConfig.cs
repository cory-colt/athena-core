using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingDataAnalytics.Domain.Strategy
{
    public class StrategyConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal PricePerTick { get; set; }

        public int Timeframe { get; set; }

        public TradingTimeWindow TradingWindowStartTime { get; set; }

        public TradingTimeWindow TradingWindowEndTime { get; set; }

        public decimal StartingAccountBalance { get; set; }

        public int MaxTradesPerSession { get; set; }

        public bool StopTradingAfterWinning { get; set; }

        public ExecutionSettings ExecutionSettings { get; set; }        
    }

    public struct TradingTimeWindow
    { 
        public int Hour { get; set; }

        public int Minute { get; set; }
    }

    public struct ExecutionSettings
    {
        public int Contracts { get; set; }

        public int InitialStopLoss { get; set; }

        public bool TrailStopToBreakeven { get; set; }

        public int TrailStopTrigger { get; set; }

        public List<ProfitTarget> ProfitTargets { get; set; }
    }

    public struct ProfitTarget
    {
        public int ProfitTargetPrice { get; set; }

        public int Contracts { get; set; }

        public int Stop { get; set; }
    }
}
