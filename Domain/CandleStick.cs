using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingDataAnalytics.Domain
{
    public record CandleStick
    {
        #region public properties
        public DateTime TimeOfDay { get; set; }

        public decimal Open { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Close { get; set; }

        public decimal Volume { get; set; }
        #endregion

        #region constructors
        public CandleStick()
        {
            TimeOfDay = DateTime.Now;
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            Volume = 0;
        }
        public CandleStick(DateTime timeOfDay, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            TimeOfDay = timeOfDay;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
        #endregion
    }
}
