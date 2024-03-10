namespace TradingDataAnalytics.Domain.Strategy
{
    public struct StrategyProfitTarget
    {
        public int ProfitTarget { get; set; }

        public int Contracts { get; set; }

        public int Stop { get; set; }
    }
}
