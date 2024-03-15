using Athena.Domain.Enums;
using Athena.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Domain.Interfaces
{
    public interface ITradeManager
    {
        /// <summary>
        /// Executes a new market order
        /// </summary>
        /// <param name="entryPrice">Market price the order should be filled at</param>
        /// <param name="contracts">Number of contracts to open with this order</param>
        /// <param name="direction">Which direction the order is. See <see cref="TradeDirection"/> for more</param>
        /// <returns>An <see cref="Order"/> object will be returns once the order was successfully executed</returns>
        Order Enter(decimal entryPrice, int contracts, TradeDirection direction);

        /// <summary>
        /// Exits an existing <see cref="Order"/>
        /// </summary>
        /// <param name="orderToExit"><see cref="Order"/> that needs to be closed/exited</param>
        /// <param name="exitPrice">Price to exit the order</param>
        /// <param name="contracts">Number of contracts to close along with the order</param>
        /// <returns>Same as passed in <see cref="Order"/> only with new updated closing details</returns>
        Order Exit(Order orderToExit, decimal exitPrice, int contracts);

        /// <summary>
        /// Cancels all unfilled orders associated with a <see cref="Trade"/>
        /// </summary>
        /// <param name="tradeToCancel"><see cref="Trade"/> containing all the orders to cancel</param>
        void CancelAllOrders(Trade tradeToCancel);

        /// <summary>
        /// Flattens all filled orders at the current market price effectively exiting all open positions
        /// </summary>
        /// <param name="tradeToFlatten"><see cref="Trade"/> containing any open orders that need to be exited/closed</param>
        /// <param name="marketPrice">Price to close the orders at. This is used to calcualte any profit and loss</param>
        Trade FlattenPosition(Trade tradeToFlatten, decimal marketPrice);
    }
}
