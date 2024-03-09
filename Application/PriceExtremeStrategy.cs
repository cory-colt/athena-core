using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain;
using TradingDataAnalytics.Domain.Enums;
using TradingDataAnalytics.Domain.Indicators;
using TradingDataAnalytics.Domain.Strategy;

namespace TradingDataAnalytics.Application
{
    public class PriceExtremeStrategy : Strategy
    {
        #region public properties
        public Dictionary<int, List<EmaValue>> EmaIndicators { get; set; }
        #endregion

        #region constructors
        /// <summary>
        /// Instantiates a new PriceExtremeStrategy with minimal information
        /// </summary>
        /// <param name="timeframe">Timeframe the strategy will be tested against in minutes. (i.e. 3 = 3 minute timeframe; 5 = 5 minute timeframe, etc)</param>
        /// <param name="tradingWindowStartTime">Starting time for the trade execution window. This is a window of time when trades are allowed to be executed during any given session</param>
        /// <param name="tradingWindowEndTime">Ending time for the trade execution window. This is a window of time when trades are allowed to be executed during any given session</param>
        /// <param name="contracts">Number of contracts being executed with each trade</param>
        /// <param name="accountBalance">Starting account balance</param>
        /// <param name="pricePerTick">How much an executed trade is worth per-tick. MNQ = .50 cents per tick</param>
        public PriceExtremeStrategy(
            int timeframe,
            TimeOnly tradingWindowStartTime,
            TimeOnly tradingWindowEndTime,
            int contracts = 5,
            decimal accountBalance = 1000,
            decimal pricePerTick = .5m) 
            : base(
                timeframe, 
                tradingWindowStartTime, 
                tradingWindowEndTime, 
                contracts, 
                accountBalance, 
                pricePerTick)
        {
            this.EmaIndicators = new Dictionary<int, List<EmaValue>>();
        }
        #endregion

        /// <summary>
        /// Checks a candle to see if a long entry condition has been met
        /// </summary>
        /// <param name="candle"><see cref="CandleStick"/> to check against the long entry condition criteria</param>
        /// <returns></returns>
        public override bool LongEntryCondition(CandleStick candle)
        {
            if (Status == StrategyStatus.OutOfTheMarket)
            {
                // get the 20 EMA value for this same time of day
                var ema = this.EmaIndicators[20].Where(m => m.TimeOfDay == candle.TimeOfDay).FirstOrDefault();

                if (ema == null)
                    return false;

                var priceChange = CalculateEmaPriceChange(candle, ema.Value);

                // TODO: this price change amount should not be hard-coded
                if (priceChange < 0 && priceChange <= -.24m)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks a candle to see if a short entry condition has been met
        /// </summary>
        /// <param name="candle"><see cref="CandleStick"/> to check against the short entry condition criteria</param>
        /// <returns></returns>
        public override bool ShortEntryCondition(CandleStick candle)
        {
            // entry conditions for only new orders
            if (Status == StrategyStatus.OutOfTheMarket)
            {
                // get the 20 EMA value for this same time of day
                var ema = this.EmaIndicators[20].Where(m => m.TimeOfDay == candle.TimeOfDay).FirstOrDefault();
                
                if (ema == null)
                    return false;

                var priceChange = CalculateEmaPriceChange(candle, ema.Value);

                // TODO: this price change amount should not be hard-coded
                if (priceChange > 0 && priceChange >= .24m)
                {
                    return true;
                }
            }

            return false;
        }

        private decimal CalculateEmaPriceChange(CandleStick candle, decimal emaValue)
        {
            var priceChange = candle.Close > emaValue
                ? Math.Round(((candle.High - emaValue) / emaValue) * 100, 2)
                : Math.Round(((candle.Low - emaValue) / emaValue) * 100, 2);

            return priceChange;
        }
    }
}
