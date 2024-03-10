using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain.Enums;

namespace TradingDataAnalytics.Domain.Events
{
    public class TradeCreatedEventArgs : EventArgs
    {
        public Trade TradeCreated { get; set; }
    }
}
