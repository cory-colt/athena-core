using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain.Enums;

namespace TradingDataAnalytics.Domain
{
    public class Trade
    {
        /// <summary>
        /// Internal unique identifier for the trade
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Date and time the trade was initiated
        /// </summary>
        public DateTime DateInitiated { get; set; }

        /// <summary>
        /// The resulting outcome of the trade <see cref="TradeOutcome"/>
        /// </summary>
        public TradeOutcome Outcome { get; set; } = TradeOutcome.Pending;

        /// <summary>
        /// Total P/L of the trade
        /// </summary>
        public decimal Profit { get; set; }

        /// <summary>
        /// Number of contracts being executed in this order
        /// </summary>
        public int Contracts { get; set; }

        /// <summary>
        /// Initial entry price for the trade
        /// </summary>
        public decimal InitialEntryPrice { get; set; }

        /// <summary>
        /// Gets or sets the direction of the trade (i.e. long or short)
        /// </summary>
        public TradeDirection TradeDirection { get; set; }

        /// <summary>
        /// Limit price for the stoploss order
        /// </summary>
        public Order? StopLoss { get; set; }

        /// <summary>
        /// Profit targets to execute
        /// </summary>
        public List<Order> ProfitTargets { get; set; } = new List<Order>();
    }
}
