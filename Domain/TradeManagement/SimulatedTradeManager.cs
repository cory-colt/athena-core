using Athena.Domain.Enums;
using Athena.Domain.Interfaces;
using Athena.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Domain.TradeManagement
{
    public class SimulatedTradeManager : ITradeManager
    {
        #region ITradeManager implementation
        public Order Enter(decimal entryPrice, int contracts, TradeDirection direction)
        {
            Order order = new Order
            {
                Id = Guid.NewGuid(),
                OpeningDate = DateTime.Now, 
                Contracts = contracts, 
                OrderPrice = entryPrice,
                OrderDirection = direction
            };

            return order;
        }

        public Order Exit(Order orderToExit, decimal exitPrice, int contracts)
        {
            throw new NotImplementedException();
        }

        public Trade FlattenPosition(Trade tradeToFlatten, decimal marketPrice)
        {
            throw new NotImplementedException();
        }

        public void CancelAllOrders(Trade tradeToCancel)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
