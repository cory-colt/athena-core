using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Models;
using Athena.Domain.Indicators;

namespace Athena.Domain.Interfaces
{
    public interface IIndicator<T>
    {
        IEnumerable<T> Calculate(IEnumerable<CandleStick> candles, int lookbackPeriod);
    }
}
