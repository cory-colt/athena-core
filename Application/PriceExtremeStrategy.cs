﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain;
using TradingDataAnalytics.Domain.Enums;
using TradingDataAnalytics.Domain.Indicators;
using TradingDataAnalytics.Domain.Interfaces;
using TradingDataAnalytics.Domain.Strategy;

namespace TradingDataAnalytics.Application
{
    public class PriceExtremeStrategy : Strategy
    {
        #region public properties
        public Dictionary<int, List<EmaValue>> EmaIndicators { get; set; }
        #endregion

        #region constructors
        public PriceExtremeStrategy() : base()
        {
            this.EmaIndicators = new Dictionary<int, List<EmaValue>>();
        }

        public PriceExtremeStrategy(StrategyConfig config) : base(config)
        {
            this.EmaIndicators = new Dictionary<int, List<EmaValue>>();
        }
        #endregion

        #region overridden methods
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

        public override void LoadCustomStrategyStuff()
        {
            var ema = new Ema();

            this.EmaIndicators.Add(10, ema.Calculate(this.Candles, 10).ToList());
            this.EmaIndicators.Add(20, ema.Calculate(this.Candles, 20).ToList());
        }

        #endregion

        #region private methods

        private decimal CalculateEmaPriceChange(CandleStick candle, decimal emaValue)
        {
            var priceChange = candle.Close > emaValue
                ? Math.Round(((candle.High - emaValue) / emaValue) * 100, 2)
                : Math.Round(((candle.Low - emaValue) / emaValue) * 100, 2);

            return priceChange;
        }

        #endregion
    }
}