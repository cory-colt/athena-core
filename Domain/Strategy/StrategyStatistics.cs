using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Domain.Strategy
{
    public class StrategyStatistics
    {
        /// <summary>
        /// Overall win-rate of the strategy
        /// </summary>
        public decimal WinRate { get; set; }

        /// <summary>
        /// Total number of losing trades
        /// </summary>
        public int LosingTrades { get; set; }

        /// <summary>
        /// Total number of profitable/winning trades
        /// </summary>
        public int WinningTrades { get; set; }

        /// <summary>
        /// Total amount of profit made from all the executed trades
        /// </summary>
        public decimal TotalProfit { get; set; }

        /// <summary>
        /// Total amount of losses made from all the executed trades
        /// </summary>
        public decimal TotalLosses { get; set; }
    }
}
