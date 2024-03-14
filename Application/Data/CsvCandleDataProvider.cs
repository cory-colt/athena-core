using Athena.Application.Interfaces;
using Athena.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Application.Data
{
    public class CsvCandleDataProvider : ICandleDataProvider
    {
        public IEnumerable<CandleStick> LoadCandleStickData()
        {
            throw new NotImplementedException();
        }
    }
}
