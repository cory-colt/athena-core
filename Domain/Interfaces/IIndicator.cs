using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain;
using TradingDataAnalytics.Domain.Indicators;

namespace TradingDataAnalytics.Domain.Interfaces
{
    public interface IIndicator<T>
    {
        IEnumerable<T> Calculate(IEnumerable<CandleStick> candles, int lookbackPeriod);
    }
}
