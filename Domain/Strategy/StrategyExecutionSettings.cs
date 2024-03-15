namespace Athena.Domain.Strategy
{
    public struct StrategyExecutionSettings
    {
        /// <summary>
        /// Determines the starting time of when new order entries are allowed to be executed - this is only applicable when there are currently no pending trades
        /// </summary>
        public StrategyTradingTimeWindow AllowedOrderEntryWindowStartTime { get; set; }

        /// <summary>
        /// Determines the ending time of when new order entries are allowed to be executed - no new orders (except for closing existing orders) will be allowed after this time
        /// </summary>
        public StrategyTradingTimeWindow AllowedOrderEntryWindowEndTime { get; set; }
        
        /// <summary>
        /// Number of contracts to trade
        /// </summary>
        public int Contracts { get; set; }

        /// <summary>
        /// Initial stop loss for the overall position
        /// </summary>
        public int InitialStopLoss { get; set; }

        /// <summary>
        /// Determines if the stop loss can be trailed to breakeven
        /// </summary>
        public bool TrailStopToBreakeven { get; set; }

        /// <summary>
        /// Trigger for the stop loss
        /// </summary>
        public int TrailStopTrigger { get; set; }

        /// <summary>
        /// Determines if the stop loss can be cut in half
        /// </summary>
        public bool TrailStopToHalfStop { get; set; }

        /// <summary>
        /// Collection of profit targets to try and hit for any given trade. See <see cref="StrategyProfitTarget"/> for more info
        /// </summary>
        public List<StrategyProfitTarget> ProfitTargets { get; set; }
    }
}
