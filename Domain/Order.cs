using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain.Enums;

namespace TradingDataAnalytics.Domain
{
    public class Order
    {
        /// <summary>
        /// Unique order id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Datetime the order was opened
        /// </summary>
        public DateTime OpeningDate { get; set; }

        /// <summary>
        /// Datetime the order was closed
        /// </summary>
        public DateTime? ClosingDate { get; set; }

        /// <summary>
        /// Number of contracts this order will execute
        /// </summary>
        public int Contracts { get; set; }

        /// <summary>
        /// Direction of the trade/order (THIS MAY NOT BE NEEDED)
        /// </summary>
        public TradeDirection OrderDirection { get; set; }

        /// <summary>
        /// Price the order was executed for
        /// </summary>
        public decimal OrderPrice { get; set; }

        /// <summary>
        /// Price (in points) to move the <see cref="Trade"/>'s stoploss to once this profit target gets hit
        /// </summary>
        public int TrailStopTrigger { get; set; }
    }
}
