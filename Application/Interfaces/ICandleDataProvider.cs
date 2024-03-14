using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Models;

namespace Athena.Application.Interfaces
{
    public interface ICandleDataProvider
    {
        IEnumerable<CandleStick> LoadCandleStickData();
    }
}
