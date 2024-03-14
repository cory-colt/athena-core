using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Models;

namespace Athena.Domain.TradeManagement
{
    public class TradingSession
    {
        public decimal SessionHighestPriceChange { get; set; }

        public decimal SessionLowestPriceChange { get; set; }

        public CandleStick? SessionMaxVolumeCandle { get; set; }
        public CandleStick? SessionMinVolumeCandle { get; set; }


        public CandleStick? SessionHighCandle { get; set; }

        public CandleStick? SessionLowCandle { get; set; }

        public required List<CandleStick> Candles { get; set; }
    }
}
