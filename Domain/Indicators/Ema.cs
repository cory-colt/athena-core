using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Models;
using Athena.Domain.Interfaces;

namespace Athena.Domain.Indicators
{
    /// <summary>
    /// Exponential Moving Average (EMA) Indicator
    /// </summary>
    public class Ema : IIndicator<EmaValue>
    {
        /// <summary>
        /// Calculates an Exponential Moving Average (EMA) from a collection of <see cref="CandleStick"/> objects given a specific lookback period
        /// </summary>
        /// <param name="candles">Collection of <see cref="CandleStick"/> objects</param>
        /// <param name="lookbackPeriod">Lookback period to use for calculating the EMA. (ex. 20 = 20 period EMA)</param>
        /// <returns>Collection of <see cref="EmaValue"/> objects that associates the time of day (candle) with its corresponding EMA value</returns>
        public IEnumerable<EmaValue> Calculate(IEnumerable<CandleStick> candles, int lookbackPeriod)
        {
            List<EmaValue> emaValues = new List<EmaValue>();

            var multiplier = 2 / (decimal)(lookbackPeriod + 1);
            var candlesToEvaluate = candles.ToArray();
            decimal result = 0;

            for (var i = 0; i < candlesToEvaluate.Length; i++)
            {
                result = i == 0
                    ? candlesToEvaluate[i].Close
                    : multiplier * candlesToEvaluate[i].Close + (1 - multiplier) * result;

                // add the ema value to the collection
                emaValues.Add(new EmaValue
                {
                    TimeOfDay = candlesToEvaluate[i].TimeOfDay,
                    Value = Math.Round(result, 2), 
                });
            }

            return emaValues;
        }
    }
}
