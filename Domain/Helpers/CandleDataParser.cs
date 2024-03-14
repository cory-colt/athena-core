using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Models;
using Athena.Domain.TradeManagement;

namespace Athena.Domain.Helpers
{
    public class CandleDataParser
    {
        /// <summary>
        /// Converts raw candlestick data into collection of <see cref="CandleStick"/> objects
        /// </summary>
        /// <param name="items">Candlestick data lines as they come from the raw data file</param>
        /// <returns>Collection of CandleStick objects</returns>
        public static List<CandleStick> ConvertRawDataToCandleSticks(IEnumerable<string> items)
        {
            List<CandleStick> candles = new List<CandleStick>();

            foreach (string line in items)
            {
                // split the line item to get the individual candle data pieces
                string[] split = line.Split(',');

                // create a new candle
                var candle = new CandleStick();
                candle.TimeOfDay = DateTime.Parse(split[0]);
                candle.Open = Convert.ToDecimal(split[1]);
                candle.High = Convert.ToDecimal(split[2]);
                candle.Low = Convert.ToDecimal(split[3]);
                candle.Close = Convert.ToDecimal(split[4]);
                candle.Volume = Convert.ToDecimal(split[5]);

                // add the candle to the collection
                candles.Add(candle);
            }

            return candles;
        }

        /// <summary>
        /// Converts 1-min candles to some other timeframe
        /// </summary>
        /// <param name="candles">Collection of 1-min <see cref="CandleStick"/> objects to be converted to a different timeframe</param>
        /// <param name="timeframeMinutes">Timeframe, in minutes, to convert the 1-min candles to</param>
        /// <returns>Collection of <see cref="CandleStick"/> objects that represent the new timeframe</returns>
        public static List<CandleStick> ConvertCandleTimeframe(List<CandleStick> candles, int timeframeMinutes = 1)
        {
            List<CandleStick> newCandles = new List<CandleStick>();

            int skipCounter = 0;

            do
            {
                var workingCandles = candles.Skip(skipCounter).Take(timeframeMinutes).ToList();

                newCandles.Add(new CandleStick
                {
                    Volume = workingCandles.Sum(m => m.Volume),
                    Open = workingCandles.First().Open,
                    High = workingCandles.Max(m => m.High),
                    Low = workingCandles.Min(m => m.Low),
                    Close = workingCandles.Last().Close,
                    TimeOfDay = workingCandles.First().TimeOfDay
                });

                skipCounter += timeframeMinutes;
            }
            while (skipCounter < candles.Count);

            return newCandles;
        }

        public static Dictionary<string, TradingSession> SplitCandlesIntoSessions(List<CandleStick> candles)
        {
            var _sessions = new Dictionary<string, TradingSession>();

            foreach (var candle in candles)
            {
                var key = candle.TimeOfDay.ToShortDateString();

                if (_sessions.ContainsKey(key))
                {
                    _sessions[key].Candles.Add(candle);
                }
                else
                {
                    _sessions.Add(key, new TradingSession { Candles = [candle] });
                }
            }

            return _sessions;
        }
    }
}
