using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain.Enums;
using TradingDataAnalytics.Domain.Events;
using TradingDataAnalytics.Domain.Helpers;
using TradingDataAnalytics.Domain.Indicators;
using TradingDataAnalytics.Domain.Interfaces;

namespace TradingDataAnalytics.Domain.Strategy
{
    public abstract class Strategy
    {
        #region public events
        /// <summary>
        /// TradeCreated is fired when a new trade is initially created. See <see cref="TradeCreatedEventArgs"/> for more details.
        /// </summary>
        public event EventHandler<TradeCreatedEventArgs> TradeCreated;

        /// <summary>
        /// TradeClosed is fired when a trade is closed. See <see cref="TradeClosedEventArgs"/> for more details.
        /// </summary>
        public event EventHandler<TradeClosedEventArgs> TradeClosed;
        #endregion

        #region public properties
        /// <summary>
        /// Internal unique identifier for the strategy
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the strategy
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the strategy
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// How much an executed trade is worth per-tick. MNQ = .50 cents per tick
        /// <para>
        ///     The Price-Per-Tick is used to calculate profit and loss for each trade
        /// </para>
        /// </summary>
        public decimal PricePerTick { get; set; }

        /// <summary>
        /// Timeframe the strategy will be tested against in minutes. (i.e. 3 = 3 minute timeframe; 5 = 5 minute timeframe, etc)
        /// </summary>
        public int Timeframe { get; set; }

        /// <summary>
        /// Starting time for the trade execution window. This is a window of time when trades are allowed to be executed during any given session
        /// </summary>
        public TimeOnly TradingWindowStartTime { get; set; }

        /// <summary>
        /// Ending time for the trade execution window. This is a window of time when trades are allowed to be executed during any given session
        /// </summary>
        public TimeOnly TradingWindowEndTime { get; set; }

        /// <summary>
        /// Overall win-rate of the strategy
        /// </summary>
        public decimal WinRate { get; set; }

        /// <summary>
        /// Total number of profitable trades
        /// </summary>
        public int ProfitableTrades { get; set; }

        /// <summary>
        /// Total number of losing trades
        /// </summary>
        public int LosingTrades { get; set; }

        /// <summary>
        /// Number of contracts bought or sold when a trade is executed
        /// </summary>
        public int Contracts { get; set; }

        /// <summary>
        /// Current account balance
        /// </summary>
        public decimal AccountBalance { get; set; }

        public decimal InitialAccountBalance { get; set; }

        /// <summary>
        /// Total amount of profit made from all the executed trades
        /// </summary>
        public decimal TotalProfit { get; set; }

        /// <summary>
        /// Total amount of losses made from all the executed trades
        /// </summary>
        public decimal TotalLosses { get; set; }

        /// <summary>
        /// Trades executed for this strategy
        /// </summary>
        public List<Trade>? Trades { get; set; }

        /// <summary>
        /// Determines if there is currently a trade already underway. See <see cref="StrategyStatus"/> for additional info
        /// </summary>
        public StrategyStatus Status { get; set; }

        /// <summary>
        /// Initial stop loss amount (in points) to use when calculating stop loss orders
        /// </summary>
        public decimal InitialStopLoss { get; set; }

        /// <summary>
        /// Max number of trades to take per session
        /// </summary>
        public int MaxTradesPerSession { get; set; }

        /// <summary>
        /// Collection of timeframe specific <see cref="CandleStick"/> objects used for executing this strategy.
        /// </summary>
        public List<CandleStick> Candles { get; set; }

        /// <summary>
        /// Avaialble <see cref="TradingSession"/>'s for the strategy to test against
        /// <para>
        ///     A session is grouped by date and contains its own collection of <see cref="CandleStick"/>'s for the specified trading window
        /// </para>
        /// </summary>
        public Dictionary<string, TradingSession> AvailableSessions { get; set; }
        #endregion

        #region constructors
        /// <summary>
        /// Instantiates a new Strategy with minimal information
        /// </summary>
        /// <param name="timeframe">Timeframe the strategy will be tested against in minutes. (i.e. 3 = 3 minute timeframe; 5 = 5 minute timeframe, etc)</param>
        /// <param name="tradingWindowStartTime">Starting time for the trade execution window. This is a window of time when trades are allowed to be executed during any given session</param>
        /// <param name="tradingWindowEndTime">Ending time for the trade execution window. This is a window of time when trades are allowed to be executed during any given session</param>
        /// <param name="contracts">Number of contracts being executed with each trade</param>
        /// <param name="accountBalance">Starting account balance</param>
        /// <param name="pricePerTick">How much an executed trade is worth per-tick. MNQ = .50 cents per tick</param>
        public Strategy(
            int timeframe,
            TimeOnly tradingWindowStartTime,
            TimeOnly tradingWindowEndTime,
            int contracts = 5,
            decimal accountBalance = 1000,
            decimal pricePerTick = .5m)
        {
            Timeframe = timeframe;
            TradingWindowStartTime = tradingWindowStartTime;
            TradingWindowEndTime = tradingWindowEndTime;
            Contracts = contracts;
            AccountBalance = accountBalance;
            InitialAccountBalance = accountBalance;
            PricePerTick = pricePerTick;
            Status = StrategyStatus.OutOfTheMarket;
            Trades = new List<Trade>();
            InitialStopLoss = 20;
            MaxTradesPerSession = 2;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Initializes the strategy
        /// <para>
        ///     When the strategy is initialized it will do several things: 
        ///     <list type="bullet">
        ///         <item>parse all the raw candle data and convert the data into a collection of <see cref="CandleStick"/> objects</item>
        ///         <item>convert these 1-min candlestick objects into the strategy's respective timeframe's candlesticks</item>
        ///         <item>take the newly converted timeframe candlesticks and filter them based on the trading window's start and end time</item>
        ///         <item>creates dictionary of <see cref="TradingSession"/>'s grouped by date - each trading session has its own respective collection of candlesticks</item>
        ///     </list>
        /// </para>
        /// </summary>
        /// <param name="rawCandleStickDataLines"></param>
        public void Init(IEnumerable<string> rawCandleStickDataLines)
        {
            // convert raw data to 1 minute candlesticks (raw data is a csv of 1 minute candle data)
            List<CandleStick> candles = CandleDataParser.ConvertRawDataToCandleSticks(rawCandleStickDataLines);

            // convert 1 minute candlesticks into the strategy's timeframe candlesticks
            Candles = CandleDataParser.ConvertCandleTimeframe(candles, Timeframe);

            TimeSpan start = new TimeSpan(TradingWindowStartTime.Hour + 1, TradingWindowStartTime.Minute, 0);
            TimeSpan end = new TimeSpan(TradingWindowEndTime.Hour + 1, 0, 0);

            // set the available trading sessions this strategy can use by filtering all the candles to only get those that are within the 
            // specified trading window start/end times
            AvailableSessions = CandleDataParser.SplitCandlesIntoSessions(
                Candles
                    .Where(m => m.TimeOfDay.TimeOfDay >= start && m.TimeOfDay.TimeOfDay <= end)
                    .ToList()
                );

            // .Where(m => (m.TimeOfDay.TimeOfDay >= start && m.TimeOfDay <= end.Hour >= this.TradingWindowStartTime.Hour + 1) && (m.TimeOfDay.Hour < this.TradingWindowEndTime.Hour + 1))
        }

        public void CheckLongPosition(Trade pendingTrade, CandleStick candle)
        {
            CheckLongForStopLoss(pendingTrade, candle);

            CheckLongForProfitTarget(pendingTrade, candle);
        }

        public void CheckShortPosition(Trade pendingTrade, CandleStick candle)
        {
            CheckShortForStopLoss(pendingTrade, candle);

            CheckShortForProfitTarget(pendingTrade, candle);
        }

        public void CheckLongForStopLoss(Trade pendingTrade, CandleStick candle)
        {
            // check if stop-loss was hit
            if (candle.Low <= pendingTrade?.StopLoss.OrderPrice || candle.Close <= pendingTrade?.StopLoss.OrderPrice)
            {
                // calculate loss
                var loss = 20 * 2 * pendingTrade.Contracts * -1; // TODO: the 2 needs to be dynamic based on the asset being traded

                // update the pending trade's status
                pendingTrade.StopLoss.ClosingDate = candle.TimeOfDay;
                pendingTrade.Outcome = TradeOutcome.Loss;
                pendingTrade.Profit = loss;

                var oldAccountBalance = AccountBalance;
                var newAccountBalance = AccountBalance += loss;

                // calculate stop loss
                TotalLosses += loss;
                AccountBalance = newAccountBalance;

                // publish the TradeClosed event
                OnTradeClosed(new TradeClosedEventArgs { ClosedTrade = pendingTrade, OldAccountBalance = oldAccountBalance, NewAccountBalance = newAccountBalance });
            }
        }

        public void CheckLongForProfitTarget(Trade pendingTrade, CandleStick candle)
        {
            // check if a profit target was hit
            if (candle.High >= pendingTrade?.ProfitTarget.OrderPrice || candle.Close >= pendingTrade?.ProfitTarget.OrderPrice)
            {
                // calculate profit
                var profit = 20 * 2 * pendingTrade.Contracts; // TODO: 20 and 2 needs to be dynamic - 20 is PT points and 2 is the multiplier for the asset being traded (MNQ or NQ)

                // update the pending trade's status
                pendingTrade.Profit = profit;
                pendingTrade.ProfitTarget.ClosingDate = candle.TimeOfDay;
                pendingTrade.Outcome = TradeOutcome.Win;

                var oldAccountBalance = AccountBalance;
                var newAccountBalance = AccountBalance += profit;

                // update the strategy
                TotalProfit += profit;
                AccountBalance = newAccountBalance;

                // publish the TradeClosed event
                OnTradeClosed(new TradeClosedEventArgs { ClosedTrade = pendingTrade, OldAccountBalance = oldAccountBalance, NewAccountBalance = newAccountBalance });
            }
        }

        /// <summary>
        /// Checks a short position to determine if a profit target has been hit
        /// </summary>
        /// <param name="pendingTrade">Pending <see cref="Trade"/> containing the profit target to be checked</param>
        /// <param name="candle">Current <see cref="CandleStick"/> to compare to the profit target order with</param>
        public void CheckShortForProfitTarget(Trade pendingTrade, CandleStick candle)
        {
            // check if a profit target was hit
            if (candle.Low <= pendingTrade?.ProfitTarget?.OrderPrice || candle.Close <= pendingTrade?.ProfitTarget?.OrderPrice)
            {
                // calculate profit
                // TODO: 20 and 2 needs to be dynamic - 20 is PT points and 2 is the multiplier for the asset being traded (MNQ or NQ)
                var profit = 20 * 2 * pendingTrade.Contracts;

                // update the pending trade's status
                pendingTrade.Profit = profit;
                pendingTrade.ProfitTarget.ClosingDate = candle.TimeOfDay;
                pendingTrade.Outcome = TradeOutcome.Win;

                var oldAccountBalance = AccountBalance;
                var newAccountBalance = AccountBalance += profit;

                // update the strategy metrics
                TotalProfit += profit;
                AccountBalance = newAccountBalance;

                // publish the TradeClosed event
                OnTradeClosed(new TradeClosedEventArgs { ClosedTrade = pendingTrade, OldAccountBalance = oldAccountBalance, NewAccountBalance = newAccountBalance }); ;
            }
        }

        /// <summary>
        /// Checks a short position to determine if the stoploss has been hit
        /// </summary>
        /// <param name="pendingTrade">Pending <see cref="Trade"/> containing the stoploss to be checked</param>
        /// <param name="candle">Current <see cref="CandleStick"/> to compare the stoploss order with</param>
        public void CheckShortForStopLoss(Trade pendingTrade, CandleStick candle)
        {
            // check if stop-loss was hit
            if (candle.High >= pendingTrade?.StopLoss.OrderPrice || candle.Close >= pendingTrade?.StopLoss.OrderPrice)
            {
                // calculate loss
                var loss = InitialStopLoss * 2 * pendingTrade.Contracts * -1; // TODO: the 2 needs to be dynamic based on the asset being traded

                // update the pending trade's status
                pendingTrade.StopLoss.ClosingDate = candle.TimeOfDay;
                pendingTrade.Outcome = TradeOutcome.Loss;
                pendingTrade.Profit = loss;

                var oldAccountBalance = AccountBalance;
                var newAccountBalance = AccountBalance += loss;

                // update the strategy metrics
                TotalLosses += loss;
                AccountBalance = newAccountBalance;

                // publish the TradeClosed event
                OnTradeClosed(new TradeClosedEventArgs { ClosedTrade = pendingTrade, OldAccountBalance = oldAccountBalance, NewAccountBalance = newAccountBalance });
            }
        }
        #endregion

        #region virtual methods

        /// <summary>
        /// Checks a currently open position against long and short positions to see if a stop loss or profit target(s) has been hit
        /// </summary>
        /// <param name="candle">Current <see cref="CandleStick"/> to check stop loss and/or profit targets</param>
        public virtual void CheckOpenPosition(CandleStick candle)
        {
            // get the currently pending trade
            var pendingTrade = Trades?.Where(m => m.Outcome == TradeOutcome.Pending).FirstOrDefault();

            // check if we're long
            if (pendingTrade?.TradeDirection == Direction.Long)
            {
                CheckLongPosition(pendingTrade, candle);
            }

            // check if we're short
            if (pendingTrade?.TradeDirection == Direction.Short)
            {
                CheckShortPosition(pendingTrade, candle);
            }
        }

        public virtual void ExecuteTrade(Trade trade)
        {
            // change the status so more than one trade isn't being taken at a time
            Status = StrategyStatus.InTheMarket;

            // add the trade to the collection of trades
            Trades?.Add(trade);
        }

        #region event handlers
        public virtual void OnTradeCreated(TradeCreatedEventArgs args)
        {
            TradeCreated?.Invoke(this, args);
        }

        public virtual void OnTradeClosed(TradeClosedEventArgs args)
        {
            // reset the strategy status so it can take more trades
            Status = StrategyStatus.OutOfTheMarket;

            TradeClosed?.Invoke(this, args);
        }
        #endregion

        #endregion

        #region abstract methods

        /// <summary>
        /// Determines if the conditions are correct for a short-entry order to be executed
        /// </summary>
        /// <param name="candle"><see cref="CandleStick"/> containing the data to be evaluated for a short entry condition</param>
        /// <returns>True if a short entry should be executed</returns>
        public abstract bool ShortEntryCondition(CandleStick candle);

        /// <summary>
        /// Determines if the conditions are correct for a long-entry order to be executed
        /// </summary>
        /// <param name="candle"><see cref="CandleStick"/> containing the data to be evaluated for a long entry condition</param>
        /// <returns>True if a long entry should be executed</returns>
        public abstract bool LongEntryCondition(CandleStick candle);
        #endregion
    }
}
