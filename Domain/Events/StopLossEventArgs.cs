using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain.Enums;

namespace TradingDataAnalytics.Domain.Events
{
    public class StopLossEventArgs
    {
        public Order? StopLossOrder { get; set; }

        public Guid TradeId { get; set; }

        public TradeOutcome Outcome { get; set; }

        public decimal LossAmount { get; set; }
    }
}
