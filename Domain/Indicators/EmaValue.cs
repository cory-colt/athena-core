using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Domain.Indicators
{
    public record EmaValue
    {
        /// <summary>
        /// Time of day the EMA/candle value has taken place
        /// </summary>
        public DateTime TimeOfDay { get; set; }

        /// <summary>
        /// Value of the EMA
        /// </summary>
        public decimal Value { get; set; }
    }
}
