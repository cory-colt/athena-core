using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingDataAnalytics.Domain.Enums
{
    public enum TradeOutcome
    {
        Loss,
        Win,
        Breakeven,
        Pending, 
        StoppedOut
    }
}
