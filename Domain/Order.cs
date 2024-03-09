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
        public Guid Id { get; set; }

        public DateTime OpeningDate { get; set; }

        public DateTime ClosingDate { get; set; }

        public Direction OrderDirection { get; set; }

        public decimal OrderPrice { get; set; }
    }
}
