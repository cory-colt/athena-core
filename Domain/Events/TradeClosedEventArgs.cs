using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain.Enums;

namespace TradingDataAnalytics.Domain.Events
{
    public class TradeClosedEventArgs
    {
        public Trade ClosedTrade { get; set; }
        
        public decimal OldAccountBalance { get; set; }

        public decimal NewAccountBalance { get; set; }
    }
}
