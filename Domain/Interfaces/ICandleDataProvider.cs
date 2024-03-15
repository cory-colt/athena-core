using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Models;

namespace Athena.Domain.Interfaces
{
    public interface ICandleDataProvider
    {
        /// <summary>
        /// Load 1 minute timeframe candle stick data
        /// </summary>
        /// <returns>Collection of <see cref="CandleStick"/> objects in chronological order and in 1 minute timeframe</returns>
        IEnumerable<CandleStick> LoadCandleStickData(DateTime? startDate = null, DateTime? endDate = null);
    }
}
