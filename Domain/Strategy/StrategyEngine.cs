﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Athena.Domain.Models;
using Athena.Domain.Enums;
using Athena.Domain.Interfaces;
using Athena.Application.Data;
using System.Numerics;

namespace Athena.Domain.Strategy
{
    public class StrategyEngine
    {
        private readonly ILogger _logger;
        private readonly Strategy _strategy;
        private readonly ICandleDataProvider _candleDataProvider;
        private readonly IStrategyConfiguration _strategyConfiguration;

        #region constructors
        /// <summary>
        /// Instantiates a new <see cref="StrategyEngine"/> by passing in the fully-qualified path to a strategies.json file
        /// </summary>
        /// <param name="strategy">Strategy to use for backtesting</param>
        /// <param name="strategyConfiguration">Strategy configurations to use for backtesting</param>
        /// <param name="candleDataProvider">Candle data provider used to provide 1-minute <see cref="CandleStick"/> object collection</param>
        /// <param name="logger">Logging provider to use for logging and instrumentation</param>
        public StrategyEngine(
            Strategy strategy, 
            IStrategyConfiguration strategyConfiguration,
            ICandleDataProvider candleDataProvider, 
            ILogger logger
            )
        {
            this._strategy = strategy;
            this._strategyConfiguration = strategyConfiguration;
            this._candleDataProvider = candleDataProvider;
            this._logger = logger;
        }
        #endregion

        #region public methods
        public void Run(DateTime? startDate = null, DateTime? endDate = null)
        {
            var configsToTest = this._strategyConfiguration.LoadStrategies();

            // iterate over each strategy config
            foreach (var strategyConfig in configsToTest)
            {
                // load the strategy configuration, parse the candle data, and load any custom strategy logic
                this._strategy
                    .LoadConfiguration(strategyConfig)
                    .ParseCandleData(this._candleDataProvider.LoadCandleStickData(startDate, endDate).ToList())
                    .LoadStrategySpecificSettings();

                // subscrbe to strategy events
                this._strategy.TradeClosed += Strategy_TradeClosed;
                this._strategy.TradeCreated += Strategy_TradeCreated;
                this._strategy.ProfitTargetHit += Strategy_ProfitTargetHit;
                this._strategy.StopLossHit += Strategy_StopLossHit;

                StartLookingForTradeOpportunities(this._strategy);

                // unsubscribe from the events
                this._strategy.TradeClosed -= Strategy_TradeClosed;
                this._strategy.TradeCreated -= Strategy_TradeCreated;
                this._strategy.ProfitTargetHit -= Strategy_ProfitTargetHit;
                this._strategy.StopLossHit -= Strategy_StopLossHit;
            }            
        }

        #endregion

        #region private methods

        private void StartLookingForTradeOpportunities(Strategy strategy)
        {
            // write the beginning strategy header to the console
            WriteStrategyHeaderToLogger(strategy);

            // iterate over each tradable session and execute the strategy
            foreach (var session in strategy.AvailableSessions)
            {
                var sessionTradeCount = 0;

                // reset any session settings prior to testing the session
                strategy.ResetSessionSettings();

                // iterate over the session's candles
                foreach (var candle in session.Value.Candles)
                {
                    // if we're in the market then check to see if a profit target has been hit or the stop loss has been hit
                    if (strategy.Status == StrategyStatus.InTheMarket)
                    {
                        strategy.CheckOpenPosition(candle);
                    }

                    // check to make sure we're still inside the allowable time window for when new trades can be entered
                    var isInsideNewOrderAllowedWindow = TimeOnly.FromDateTime(candle.TimeOfDay) <= strategy.AllowedOrderEntryWindowEndTime;

                    // check if there are any trades, if so see if they're a winning trade so we can exit this session without taking any additional trades
                    if (strategy.StopTradingAfterWinning)
                        if (strategy.Trades?.Where(m => m.Outcome == TradeOutcome.Win && m.DateInitiated.ToShortDateString() == session.Key).Count() > 0)
                            break;

                    // we're out of the market - check for long entry conditions
                    if (strategy.LongEntryCondition(candle) && sessionTradeCount < strategy.MaxTradesPerSession && isInsideNewOrderAllowedWindow)
                    {
                        var longTrade = CreateTrade(candle, strategy, TradeDirection.Long);

                        // execute the trade
                        strategy.ExecuteTrade(longTrade);

                        sessionTradeCount++;
                    }

                    if (strategy.ShortEntryCondition(candle) && sessionTradeCount < strategy.MaxTradesPerSession && isInsideNewOrderAllowedWindow)
                    {
                        // setup the short trade
                        var shortTrade = CreateTrade(candle, strategy, TradeDirection.Short);

                        // execute the trade
                        strategy.ExecuteTrade(shortTrade);

                        sessionTradeCount++;
                    }
                }
            }

            // output strategy summary to the console
            WriteSummaryToLogger(strategy);
        }

        private Trade CreateTrade(CandleStick candle, Strategy strategy, TradeDirection direction)
        {
            var tradeToExecute = new Trade
            {
                Id = Guid.NewGuid(),
                InitialEntryPrice = candle.Close,
                Contracts = strategy.Contracts,
                DateInitiated = candle.TimeOfDay,
                Outcome = TradeOutcome.Pending,
                TradeDirection = (direction == TradeDirection.Long) ? TradeDirection.Long : TradeDirection.Short,
                StopLoss = new Order
                {
                    Id = Guid.NewGuid(),
                    OpeningDate = candle.TimeOfDay,
                    OrderDirection = TradeDirection.StopLoss,
                    OrderPrice = (direction == TradeDirection.Long) ? candle.Close - strategy.InitialStopLoss : candle.Close + strategy.InitialStopLoss
                },
                ProfitTargets = strategy.ProfitTargets
                    .Select(m => new Order
                    {
                        Id = Guid.NewGuid(),
                        Contracts = m.Contracts,
                        OrderPrice = (direction == TradeDirection.Long) ? candle.Close + m.ProfitTarget : candle.Close - m.ProfitTarget,
                        OpeningDate = candle.TimeOfDay,
                        OrderDirection = (direction == TradeDirection.Long) ? TradeDirection.Long : TradeDirection.Short
                    })
                    .ToList()
            };

            return tradeToExecute;
        }

        private void WriteStrategyHeaderToLogger(Strategy strategy)
        {
            _logger.Info($"Executing Strategy: [{strategy.Name}] - Starting Account Balance: [{string.Format("{0:C}", strategy.AccountBalance)}]");
            _logger.Info("------------------------------------------------------------------------------------------------------------------------------------------------");
            _logger.Info(string.Format("{0, 12} | {1, 25} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12} | {7, 12}", "Id", "Date", "Direction", "Entry Price", "Closing Price", "P/L", "Outcome", "Account Balance"));
            _logger.Info("------------------------------------------------------------------------------------------------------------------------------------------------");
        }

        private void WriteSummaryToLogger(Strategy strategy)
        {
            var gainOnAccount = strategy.AccountBalance - strategy.InitialAccountBalance;
            var goaString = $"({(strategy.AccountBalance - strategy.InitialAccountBalance) / strategy.InitialAccountBalance * 100}%)";
            var losses = Convert.ToDecimal(strategy.Trades.Where(m => m.Outcome == TradeOutcome.Loss).Count());
            var wins = Convert.ToDecimal(strategy.Trades.Where(m => m.Outcome == TradeOutcome.Win).Count());
            var totalTrades = Convert.ToDecimal(strategy.Trades.Count);
            var winRate = Convert.ToDecimal(wins / totalTrades * 100);

            _logger.Info("\n");
            _logger.Info($"----------------------------------------------- STRATEGY SUMMARY STATISTICS ---------------------------------------------------");
            _logger.Info($"-------------------------------------------------------------------------------------------------------------------------------");
            _logger.Info(string.Format("{0, 30} | {1, 12} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12}",
                "Gain On Account",
                "Total Trades",
                "Wins",
                "Losses",
                "Total Profit",
                "Total Losses",
                "Win-Rate"
            ));
            _logger.Info($"-------------------------------------------------------------------------------------------------------------------------------");            

            _logger.Info(string.Format("{0, 30} | {1, 12} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12}%",
                string.Format("{0:C}, {1}", gainOnAccount, goaString), 
                strategy.Trades?.Count,
                wins,
                losses,
                string.Format("{0:C}", strategy.Statistics.TotalProfit),
                string.Format("{0:C}", strategy.Statistics.TotalLosses),
                winRate.ToString("0.00")
            ));

            _logger.Info("\n");
            _logger.Info("--------------------------------------");
            _logger.Info("Strategy Details: ");
            _logger.Info("--------------------------------------");
            _logger.Info($"Contracts: {strategy.Contracts}");
            _logger.Info($"Stop Trading After Winning: {strategy.StopTradingAfterWinning}");
            _logger.Info($"Max Trades Per Session: {strategy.MaxTradesPerSession}");
            _logger.Info($"Trail Stop to Breakeven: {strategy.TrailStopToBreakeven}");
            _logger.Info($"Trail Stop to Half: {strategy.TrailStopToHalfStop}");
            _logger.Info($"Initial Stop Loss: {strategy.InitialStopLoss}");
            _logger.Info($"Profit Targets: ({strategy.ProfitTargets.Count}) ");
            foreach (var pt in strategy.ProfitTargets)
            {
                _logger.Info($"- {pt.ProfitTarget} (risk-to-reward: 1:{Convert.ToDecimal(pt.ProfitTarget / strategy.InitialStopLoss).ToString("#.##")})");
            }
        }
        #endregion

        #region event handlers
        void Strategy_TradeClosed(object? sender, Athena.Domain.Events.TradeClosedEventArgs e)
        {
            var dateClosed = e.ClosedTrade.Outcome == TradeOutcome.Loss ? e.ClosedTrade.StopLoss?.ClosingDate : e.ClosedTrade.ProfitTargets.Last().ClosingDate;
            var closingPrice = e.ClosedTrade.Outcome == TradeOutcome.Loss ? e.ClosedTrade.StopLoss?.OrderPrice : e.ClosedTrade.ProfitTargets.Last(m => m.ClosingDate != null).OrderPrice;

            _logger.Info(string.Format("{0, 12} | {1,25} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12} | {7, 12}",
                e.ClosedTrade.Id.ToString().Substring(0, 7),
                dateClosed,
                "-",
                "-",
                closingPrice,
                "-", 
                e.ClosedTrade.Outcome, 
                string.Format("{0:C}", e.NewAccountBalance)));
        }

        private void Strategy_TradeCreated(object? sender, Events.TradeCreatedEventArgs e)
        {
            _logger.Info("------------------------------------------------------------------------------------------------------------------------------------------------");
            _logger.Info(string.Format("{0, 12} | {1,25} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12} | {7, 12}",
                e.TradeCreated.Id.ToString().Substring(0, 7),
                e.TradeCreated.ProfitTargets.First().OpeningDate,
                e.TradeCreated.TradeDirection,
                e.TradeCreated.InitialEntryPrice,
                e.TradeCreated.StopLoss?.OrderPrice,
                "-",
                "-", 
                "-"));
        }

        private void Strategy_ProfitTargetHit(object? sender, Events.ProfitTargetHitEventArgs e)
        {
            _logger.Info(string.Format("{0, 12} | {1,25} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12} | {7, 12}",
                e.TradeId.ToString().Substring(0, 7),
                e.ProfitTargetOrder?.ClosingDate,
                "-",
                "-", 
                e.ProfitTargetOrder?.OrderPrice,
                string.Format("{0:C}", e.Profit),
                "PT Hit", 
                "-"));
        }

        private void Strategy_StopLossHit(object? sender, Events.StopLossEventArgs e)
        {
            if (e.Outcome == TradeOutcome.StoppedOut)
                Console.ForegroundColor = ConsoleColor.Red;

            _logger.Info(string.Format("{0, 12} | {1,25} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12} | {7, 12}",
                e.TradeId.ToString().Substring(0, 7),
                e.StopLossOrder?.ClosingDate,
                "-",
                "-",
                e.StopLossOrder?.OrderPrice,
                (e.Outcome == TradeOutcome.StoppedOut) ? string.Format("{0:C}", e.LossAmount) : "$0.00",
                e.Outcome,
                "-"));

            Console.ForegroundColor = ConsoleColor.Black;
        }
        #endregion
    }
}
