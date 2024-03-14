using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Enums;
using Athena.Domain.Models;

namespace Athena.Domain.Events
{
    public class StopLossEventArgs
    {
        public Order? StopLossOrder { get; set; }

        public Guid TradeId { get; set; }

        public TradeOutcome Outcome { get; set; }

        public decimal LossAmount { get; set; }
    }
}
