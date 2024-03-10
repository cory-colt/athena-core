﻿namespace TradingDataAnalytics.Domain.Strategy
{
    public struct StrategyExecutionSettings
    {
        public int Contracts { get; set; }

        public int InitialStopLoss { get; set; }

        public bool TrailStopToBreakeven { get; set; }

        public int TrailStopTrigger { get; set; }

        public List<StrategyProfitTarget> ProfitTargets { get; set; }
    }
}