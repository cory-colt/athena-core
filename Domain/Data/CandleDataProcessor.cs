using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Models;
using Athena.Domain.TradeManagement;

namespace Athena.Domain.Data
{
    public class CandleDataProcessor
    {
        /// <summary>
        /// Converts raw CSV candlestick data into collection of <see cref="CandleStick"/> objects
        /// </summary>
        /// <param name="items">Candlestick data lines as they come from the raw data file</param>
        /// <returns>Collection of <see cref="CandleStick"/> objects</returns>
        /// <exception cref="FormatException"></exception>
        public static List<CandleStick> ConvertRawDataToCandleSticks(IEnumerable<string> items)
        {
            List<CandleStick> candles = new List<CandleStick>();

            foreach (string line in items)
            {
                // check if he line is blank, if so break out of this iteration
                if (string.IsNullOrEmpty(line)) 
                    continue;

                try
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

                    // do not add data for Sundays
                    if (candle.TimeOfDay.DayOfWeek.ToString().ToLower() != "sunday")
                    {
                        // add the candle to the collection
                        candles.Add(candle);
                    }
                }
                catch (System.FormatException ex)
                {
                    Console.WriteLine($"An error occurred while processing the candlestick data: {ex.Message}");
                    throw;
                }
                catch (System.Exception err)
                {
                    Console.WriteLine(err.Message);
                }
                
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

        /// <summary>
        /// Takes a collection of <see cref="CandleStick"/> objects and groups them by calendar day. One trading day (24 period) is considered a "session." 
        /// </summary>
        /// <param name="candles">Collection of <see cref="CandleStick"/> objects to group together by day/trading session</param>
        /// <returns>Dictionary of <see cref="TradingSession"/>'s where a Trading Session is comprised of candles for each respective day. The key is the trading session day as m/d/yyyy format</returns>
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
