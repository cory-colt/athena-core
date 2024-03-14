using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Enums;
using Athena.Domain.Models;

namespace Athena.Domain.Events
{
    public class ProfitTargetHitEventArgs
    {
        public Order? ProfitTargetOrder { get; set; }

        public Guid TradeId { get; set; }

        public decimal Profit { get; set; }

        public TradeOutcome Outcome { get; set; }
    }
}
