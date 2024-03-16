using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Models;
using Athena.Domain.Enums;
using Athena.Domain.Events;
using Athena.Domain.Indicators;
using Athena.Domain.Interfaces;
using Athena.Domain.TradeManagement;
using Athena.Domain.Data;

namespace Athena.Domain.Strategy
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

        /// <summary>
        /// ProfitTargetHit is fired every time a profit target is hit. See <see cref="ProfitTargetHitEventArgs"/> for more details
        /// </summary>
        public event EventHandler<ProfitTargetHitEventArgs> ProfitTargetHit;

        /// <summary>
        /// StopLossHit is fired every time a trade was closed as a result of a stoploss getting triggered. <see cref="StopLossEventArgs"/> for more details
        /// </summary>
        public event EventHandler<StopLossEventArgs> StopLossHit;
        #endregion

        #region public properties
        /// <summary>
        /// Internal unique identifier for the strategy
        /// </summary>
        public Guid Id { get; set; }

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
        /// Starting time for the trading window. This is the beginning of when trade logic will begin to start looking for trade entries.
        /// </summary>
        public TimeOnly TradingWindowStartTime { get; set; }

        /// <summary>
        /// Ending time for the trading window. This is the end time for the session. Any existing trades will be automatically closed at this time.
        /// </summary>
        public TimeOnly TradingWindowEndTime { get; set; }

        /// <summary>
        /// Determines the starting time of when new order entries are allowed to be executed - this is only applicable when there are currently no pending trades
        /// <para>
        ///     The allowed order entry start/end windows are here to determine when new order entries are able to be placed within the strategy's trading window
        /// </para>
        /// </summary>
        public TimeOnly AllowedOrderEntryWindowStartTime { get; set; }

        /// <summary>
        /// Determines the ending time of when new order entries are allowed to be executed - no new orders (except for closing existing orders) will be allowed after this time
        /// <para>
        ///     The allowed order entry start/end windows are here to determine when new order entries are able to be placed within the strategy's trading window
        /// </para>
        /// </summary>
        public TimeOnly AllowedOrderEntryWindowEndTime { get; set; }

        /// <summary>
        /// Contains the overall running statistics for the strategy's key performance indicators. See <see cref="StrategyStatistics"/> for more info
        /// </summary>
        public StrategyStatistics Statistics { get; set; } = new StrategyStatistics();

        /// <summary>
        /// Number of contracts bought or sold when a trade is executed
        /// </summary>
        public int Contracts { get; set; }

        /// <summary>
        /// Current account balance
        /// </summary>
        public decimal AccountBalance { get; set; }

        /// <summary>
        /// Sets the initial account balance
        /// </summary>
        public decimal InitialAccountBalance { get; set; }

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
        /// Determines if no more trades should be taken during a session as soon as there is a winning/profitable trade. Default is false.
        /// </summary>
        public bool StopTradingAfterWinning { get; set; } = false;

        /// <summary>
        /// Determines if a stop can be trailed to breakeven based on the TrailStopTrigger
        /// </summary>
        public bool TrailStopToBreakeven { get; set; } = false;

        /// <summary>
        /// Determines if a stop can be trailed to half a stop loss
        /// </summary>
        public bool TrailStopToHalfStop { get; set; } = false;

        /// <summary>
        /// Collection of profit targets used for scaling out of a trade. See <see cref="StrategyProfitTarget"/> for more details
        /// <para>
        ///     Each profit target is comprised of an exit price, number of contracts to be closed at this price, 
        ///     and an optional stop price to move the stoploss to once the profit target has been hit
        /// </para>
        /// </summary>
        public List<StrategyProfitTarget> ProfitTargets { get; set; } = new List<StrategyProfitTarget>();

        /// <summary>
        /// Collection of timeframe specific <see cref="CandleStick"/> objects used for executing this strategy.
        /// </summary>
        public List<CandleStick> Candles { get; set; }

        /// <summary>
        /// <see cref="StrategyConfig"/> being used while executing this strategy
        /// </summary>
        public StrategyConfig StrategyConfig { get; set; }

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
        /// Instantiates a new Strategy with a <see cref="StrategyConfig"/>
        /// </summary>
        /// <param name="config"><see cref="StrategyConfig"/> object to use when setting all the properties</param>
        public Strategy(StrategyConfig config) : this()
        {
            StrategyConfig = config;
            Timeframe = config.Timeframe;
            PricePerTick = config.PricePerTick;
            Contracts = config.ExecutionSettings.Contracts;
            AccountBalance = config.StartingAccountBalance;
            MaxTradesPerSession = config.MaxTradesPerSession;
            InitialAccountBalance = config.StartingAccountBalance;
            InitialStopLoss = config.ExecutionSettings.InitialStopLoss;
            TradingWindowStartTime = new TimeOnly(config.TradingWindowStartTime.Hour, config.TradingWindowStartTime.Minute);
            TradingWindowEndTime = new TimeOnly(config.TradingWindowEndTime.Hour, config.TradingWindowEndTime.Minute);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Strategy()
        {
            Status = StrategyStatus.OutOfTheMarket;
            Trades = new List<Trade>();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Initializes the strategy
        /// <para>
        ///     When the strategy is initialized it will do several things: 
        ///     <list type="bullet">
        ///         <item>convert 1-min candlestick objects into the strategy's respective timeframe's candlesticks</item>
        ///         <item>take the newly converted timeframe candlesticks and filter them based on the trading window's start and end time</item>
        ///         <item>creates dictionary of <see cref="TradingSession"/>'s grouped by date - each trading session has its own respective collection of candlesticks</item>
        ///     </list>
        /// </para>
        /// </summary>
        public Strategy ParseCandleData(List<CandleStick> candles)
        {
            // convert 1 minute candlesticks into the strategy's timeframe candlesticks, this is so the strategy will always have access to the original
            // collection of timeframe-specific candles that have NOT been split into individual sessions
            this.Candles = CandleDataProcessor.ConvertCandleTimeframe(candles, Timeframe);

            // set the trading session start and end time - used split the candles into only candles that are within this trading window
            TimeSpan start = new TimeSpan(TradingWindowStartTime.Hour, TradingWindowStartTime.Minute, 0);
            TimeSpan end = new TimeSpan(TradingWindowEndTime.Hour, 0, 0);

            // set the available trading sessions this strategy can use by filtering all the candles to only get those that are within the 
            // specified trading window start/end times
            AvailableSessions = CandleDataProcessor.SplitCandlesIntoSessions(
                this.Candles
                    .Where(m => m.TimeOfDay.TimeOfDay >= start && m.TimeOfDay.TimeOfDay <= end)
                    .ToList()
                );

            return this;
        }

        public Strategy LoadConfiguration(StrategyConfig config)
        {
            this.Id = config.Id;
            this.InitialAccountBalance = config.StartingAccountBalance;
            this.InitialStopLoss = config.ExecutionSettings.InitialStopLoss;
            this.AccountBalance = config.StartingAccountBalance;
            this.Contracts = config.ExecutionSettings.Contracts;
            this.StrategyConfig = config;
            this.PricePerTick = config.PricePerTick;
            this.Name = config.Name;
            this.Description = config.Description;
            this.MaxTradesPerSession = config.MaxTradesPerSession;
            this.Timeframe = config.Timeframe;
            this.StopTradingAfterWinning = config.StopTradingAfterWinning;
            this.TrailStopToBreakeven = config.ExecutionSettings.TrailStopToBreakeven;
            this.TrailStopToHalfStop = config.ExecutionSettings.TrailStopToHalfStop;
            this.ProfitTargets = config.ExecutionSettings.ProfitTargets;
            this.TradingWindowEndTime = new TimeOnly(config.TradingWindowEndTime.Hour + 2, config.TradingWindowEndTime.Minute); // + 2 is because the candle data is in EST, not MST
            this.TradingWindowStartTime = new TimeOnly(config.TradingWindowStartTime.Hour + 2, config.TradingWindowStartTime.Minute); // + 2 is because the candle data is in EST, not MST
            this.AllowedOrderEntryWindowStartTime = new TimeOnly(config.ExecutionSettings.AllowedOrderEntryWindowStartTime.Hour + 2, config.ExecutionSettings.AllowedOrderEntryWindowStartTime.Minute); // + 2 is because the candle data is in EST, not MST
            this.AllowedOrderEntryWindowEndTime = new TimeOnly(config.ExecutionSettings.AllowedOrderEntryWindowEndTime.Hour + 2, config.ExecutionSettings.AllowedOrderEntryWindowEndTime.Minute); // + 2 is because the candle data is in EST, not MST

            return this;
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

        public void CheckLongForProfitTarget(Trade pendingTrade, CandleStick candle)
        {
            foreach (var profitTargetOrder in pendingTrade.ProfitTargets.Where(m => m.ClosingDate == null))
            {
                var oldAccountBalance = AccountBalance;

                // check if a profit target was hit
                if (candle.High >= profitTargetOrder.OrderPrice || candle.Close >= profitTargetOrder.OrderPrice)
                {
                    // pt hit - process it
                    ProcessProfitTargetHit(profitTargetOrder, pendingTrade, candle);
                }

                // check if there are any outstanding contracts to close, if not, close the trade out
                if (pendingTrade.Contracts <= 0)
                {
                    // update the pending trade's status
                    pendingTrade.Outcome = TradeOutcome.Win;

                    // publish the TradeClosed event
                    OnTradeClosed(new TradeClosedEventArgs { ClosedTrade = pendingTrade, NewAccountBalance = this.AccountBalance }); ;
                }
            }
        }

        public void CheckLongForStopLoss(Trade pendingTrade, CandleStick candle)
        {
            // check if stop-loss was hit
            if (candle.Low <= pendingTrade.StopLoss?.OrderPrice || candle.Close <= pendingTrade.StopLoss?.OrderPrice)
            {
                ProcessStopLossHit(pendingTrade, candle);

                // publish the TradeClosed event
                OnTradeClosed(new TradeClosedEventArgs { ClosedTrade = pendingTrade, NewAccountBalance = this.AccountBalance });
            }
        }

        /// <summary>
        /// Checks a short position to determine if a profit target has been hit
        /// </summary>
        /// <param name="pendingTrade">Pending <see cref="Trade"/> containing the profit target to be checked</param>
        /// <param name="candle">Current <see cref="CandleStick"/> to compare to the profit target order with</param>
        public void CheckShortForProfitTarget(Trade pendingTrade, CandleStick candle)
        {
            foreach (var profitTargetOrder in  pendingTrade.ProfitTargets.Where(m => m.ClosingDate == null))
            {
                var oldAccountBalance = AccountBalance;

                // check if a profit target was hit
                if (candle.Low <= profitTargetOrder.OrderPrice || candle.Close <= profitTargetOrder.OrderPrice)
                {
                    ProcessProfitTargetHit(profitTargetOrder, pendingTrade, candle);
                }

                // check if there are any outstanding contracts to close, if not, close the trade out
                if (pendingTrade.Contracts <= 0)
                {
                    // update the pending trade's status
                    pendingTrade.Outcome = TradeOutcome.Win;

                    // publish the TradeClosed event
                    OnTradeClosed(new TradeClosedEventArgs { ClosedTrade = pendingTrade, NewAccountBalance = this.AccountBalance }); ;
                }
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
            if (candle.High >= pendingTrade.StopLoss?.OrderPrice || candle.Close >= pendingTrade.StopLoss?.OrderPrice)
            {
                ProcessStopLossHit(pendingTrade, candle);

                // publish the TradeClosed event
                OnTradeClosed(new TradeClosedEventArgs { ClosedTrade = pendingTrade, NewAccountBalance = this.AccountBalance });
            }
        }
        #endregion

        #region private methods

        /// <summary>
        /// Process what happens when a stop loss is hit
        /// </summary>
        /// <param name="pendingTrade"><see cref="Order"/> containing the stop loss information</param>
        /// <param name="candle">The current <see cref="CandleStick"/> used for comparison to see if the stop loss was hit</param>
        private void ProcessStopLossHit(Trade pendingTrade, CandleStick candle)
        {
            var isBreakEven = pendingTrade.StopLoss.OrderPrice == pendingTrade.InitialEntryPrice;

            // calculate loss
            var loss = (isBreakEven) ? 0 : Math.Abs(pendingTrade.StopLoss.OrderPrice - pendingTrade.InitialEntryPrice) * (this.PricePerTick * 4) * pendingTrade.Contracts * -1;

            // update the pending trade's status
            pendingTrade.StopLoss.ClosingDate = candle.TimeOfDay;
            pendingTrade.Outcome = (isBreakEven) ? TradeOutcome.Breakeven : TradeOutcome.Loss;
            pendingTrade.Profit += loss;

            // update the strategy metrics
            UpdateAccountBalances(loss);

            // publish the StopLossHit event
            OnStopLossHit(new StopLossEventArgs { TradeId = pendingTrade.Id, StopLossOrder = pendingTrade.StopLoss, LossAmount = loss, Outcome = (loss == 0) ? TradeOutcome.Breakeven : TradeOutcome.StoppedOut });
        }

        /// <summary>
        /// Processes what happens when a profit target is hit
        /// </summary>
        /// <param name="profitTargetOrder"><see cref="Order"/> containing the profit target information</param>
        /// <param name="pendingTrade">The currently pending <see cref="Trade"/> used for calculating the profit</param>
        /// <param name="candle">The current <see cref="CandleStick"/> used for comparison to see if the profit target was hit</param>
        private void ProcessProfitTargetHit(Order profitTargetOrder, Trade pendingTrade, CandleStick candle)
        {
            // calculate profit for this profit target
            var profit = Math.Abs(profitTargetOrder.OrderPrice - pendingTrade.InitialEntryPrice) * (this.PricePerTick * 4) * profitTargetOrder.Contracts;

            // update the outstanding contracts and profit for the pending trade
            pendingTrade.Contracts -= profitTargetOrder.Contracts;
            pendingTrade.Profit += profit;

            // set the closing date for this profit target
            profitTargetOrder.ClosingDate = candle.TimeOfDay;

            // update the strategy's metrics
            UpdateAccountBalances(profit);

            // check to see if we're trailing the stop to break even
            if (this.TrailStopToBreakeven)
            {
                // trail stop to break even
                pendingTrade.StopLoss.OrderPrice = pendingTrade.InitialEntryPrice;
            } else if (this.TrailStopToHalfStop)
            {
                if (pendingTrade.TradeDirection == TradeDirection.Long)
                    pendingTrade.StopLoss.OrderPrice = pendingTrade.StopLoss.OrderPrice + (this.InitialStopLoss / 2);
                else
                    pendingTrade.StopLoss.OrderPrice = pendingTrade.StopLoss.OrderPrice - (this.InitialStopLoss / 2);
            }

            // publish the ProfitTargetHit event
            OnProfitTargetHit(new ProfitTargetHitEventArgs { TradeId = pendingTrade.Id, ProfitTargetOrder = profitTargetOrder, Profit = profit, Outcome = TradeOutcome.Win });
        }

        /// <summary>
        /// Updates the <see cref="Strategy"/>'s account balance and running profit and loss
        /// </summary>
        /// <param name="profitAndLoss">Amount of profit and loss to update the account balance by</param>
        private void UpdateAccountBalances(decimal profitAndLoss)
        {
            // update the strategy's running account balance
            this.AccountBalance += profitAndLoss;

            // update the strategy's running total profit and total losses
            this.Statistics.TotalProfit += (profitAndLoss > 0) ? profitAndLoss : 0;
            this.Statistics.TotalLosses += (profitAndLoss < 0) ? profitAndLoss : 0;
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
            if (pendingTrade?.TradeDirection == TradeDirection.Long)
            {
                CheckLongPosition(pendingTrade, candle);
            }

            // check if we're short
            if (pendingTrade?.TradeDirection == TradeDirection.Short)
            {
                CheckShortPosition(pendingTrade, candle);
            }
        }

        public virtual void ExecuteTrade(Trade trade)
        {
            // add the trade to the collection of trades
            Trades?.Add(trade);

            // publish the TradeCreated event
            OnTradeCreated(new TradeCreatedEventArgs { TradeCreated = trade });
        }

        /// <summary>
        /// This allows derived classes to reset their own session settings when a new session has started
        /// </summary>
        public virtual void ResetSessionSettings()
        {
            // intentionally left blank incase we n
        }

        #region event handlers
        public virtual void OnTradeCreated(TradeCreatedEventArgs args)
        {
            // change the status so more than one trade isn't being taken at a time
            Status = StrategyStatus.InTheMarket;

            TradeCreated?.Invoke(this, args);
        }

        public virtual void OnTradeClosed(TradeClosedEventArgs args)
        {
            this.ResetSessionSettings();

            // reset the strategy status so it can take more trades
            Status = StrategyStatus.OutOfTheMarket;

            // if this trade was closed early after a profit target was hit then it still needs to be counted as a win
            if (args.ClosedTrade.Profit > 0)
                args.ClosedTrade.Outcome = TradeOutcome.Win;

            TradeClosed?.Invoke(this, args);
        }

        public virtual void OnProfitTargetHit(ProfitTargetHitEventArgs args)
        {
            ProfitTargetHit?.Invoke(this, args);
        }

        public virtual void OnStopLossHit(StopLossEventArgs args)
        {
            StopLossHit?.Invoke(this, args);
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

        /// <summary>
        /// Allows derived classes to implement their own custom logic such as adding moving averages or other indicators
        /// </summary>
        public abstract void LoadStrategySpecificSettings();
        #endregion
    }
}
