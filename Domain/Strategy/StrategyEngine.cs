using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TradingDataAnalytics.Domain.Enums;
using TradingDataAnalytics.Domain.Interfaces;

namespace TradingDataAnalytics.Domain.Strategy
{
    public class StrategyEngine
    {
        private readonly List<StrategyConfig> _strategyConfigs = new List<StrategyConfig>();
        private readonly string _candleDataFile = string.Empty;

        #region constructors
        /// <summary>
        /// Instanties a new <see cref="StrategyEngine"/> by passing in a collection of <see cref="StrategyConfig"/>'s
        /// </summary>
        /// <param name="strategyConfigs">Collection of <see cref="StrategyConfig"/> objects</param>
        /// <param name="candleDataFile">Fully qualified name of the candle data csv file</param>
        public StrategyEngine(
            List<StrategyConfig> strategyConfigs, 
            string candleDataFile
            )
        {
            this._strategyConfigs = strategyConfigs;
            this._candleDataFile = candleDataFile;
        }

        /// <summary>
        /// Instantiates a new <see cref="StrategyEngine"/> by passing in the fully-qualified path to a strategies.json file
        /// </summary>
        /// <param name="strategyConfigFile">Fully qualified name of the strategies.json configuration file</param>
        /// <param name="candleDataFile">Fully qualified name of the candle data csv file</param>
        public StrategyEngine(
            string strategyConfigFile, 
            string candleDataFile
            )
        {
            this._strategyConfigs = LoadStrategyConfigs(strategyConfigFile);
            this._candleDataFile = candleDataFile;
        }
        #endregion

        #region public methods
        public void Run(List<Strategy> strategies)
        {
            // read in the raw candle data from the csv/flat file
            IEnumerable<string> rawCandleData = File.ReadLines(this._candleDataFile);

            // iterate over each strategy config
            foreach (var strategyConfig in this._strategyConfigs)
            {
                // iterate over the available strategies and load each config to test
                foreach (var strategy in strategies)
                {
                    // load the strategy configuration, parse the candle data, and load any custom strategy logic
                    strategy
                        .LoadConfiguration(strategyConfig)
                        .ParseCandleData(rawCandleData)
                        .LoadCustomStrategyStuff();

                    // subscrbe to strategy events
                    strategy.TradeClosed += Strategy_TradeClosed;
                    strategy.TradeCreated += Strategy_TradeCreated;
                    strategy.ProfitTargetHit += Strategy_ProfitTargetHit;

                    StartLookingForTradeOpportunities(strategy);
                    
                    // unsubscribe from the events
                    strategy.TradeClosed -= Strategy_TradeClosed;
                    strategy.TradeCreated -= Strategy_TradeCreated;
                    strategy.ProfitTargetHit -= Strategy_ProfitTargetHit;
                }
            }            
        }

        

        #endregion

        #region private methods

        private void StartLookingForTradeOpportunities(Strategy strategy)
        {
            // write the beginning strategy header to the console
            WriteStrategyHeaderToConsole(strategy);

            // iterate over each tradable session and execute the strategy
            foreach (var session in strategy.AvailableSessions)
            {
                var sessionTradeCount = 0;

                // iterate over the session's candles
                foreach (var candle in session.Value.Candles)
                {
                    // if we're in the market then check to see if a profit target has been hit or the stop loss has been hit
                    if (strategy.Status == StrategyStatus.InTheMarket)
                    {
                        strategy.CheckOpenPosition(candle);
                    }

                    // check if there are any trades, if so see if they're a winning trade so we can exit this session without taking any additional trades
                    if (strategy.StopTradingAfterWinning)
                        if (strategy.Trades?.Where(m => m.Outcome == TradeOutcome.Win && m.DateInitiated.ToShortDateString() == session.Key).Count() > 0)
                            break;

                    // we're out of the market - check for long entry conditions
                    if (strategy.LongEntryCondition(candle) && sessionTradeCount < strategy.MaxTradesPerSession)
                    {
                        var longTrade = CreateTrade(candle, strategy, TradeDirection.Long);

                        // execute the trade
                        strategy.ExecuteTrade(longTrade);

                        sessionTradeCount++;
                    }

                    if (strategy.ShortEntryCondition(candle) && sessionTradeCount < strategy.MaxTradesPerSession)
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
            WriteSummaryToConsole(strategy);
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

        /// <summary>
        /// Reads the strategies.json file and deserializes it into a collection of <see cref="StrategyConfig"/> objects
        /// </summary>
        /// <param name="configFile">Fully qualified name of the strategies.json file</param>
        /// <returns>Deserialized collection of <see cref="StrategyConfig"/> objects</returns>
        private List<StrategyConfig> LoadStrategyConfigs(string configFile)
        {
            using (StreamReader r = new StreamReader(configFile))
            {
                string json = r.ReadToEnd();
                var configs = JsonSerializer.Deserialize<List<StrategyConfig>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return configs;
            }
        }

        private void WriteStrategyHeaderToConsole(Strategy strategy)
        {
            Console.WriteLine($"Executing Strategy: [{strategy.Name}] - Starting Account Balance: [{strategy.AccountBalance}]");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("{0, 12} | {1, 20} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12}", "Id", "Date", "Direction", "Entry Price", "Closing Price", "P/L", "Outcome"));
            Console.WriteLine("---------------------------------------------------------------------------------------------------------------");
        }

        private static void WriteSummaryToConsole(Strategy strategy)
        {
            Console.WriteLine($"---------------------- STRATEGY SUMMARY STATISTICS --------------------------");
            Console.WriteLine();

            Console.WriteLine($"Gain on Account: {strategy.AccountBalance - strategy.InitialAccountBalance} ({(strategy.AccountBalance - strategy.InitialAccountBalance) / strategy.InitialAccountBalance * 100}%) - Total Trades: {strategy.Trades.Count}");
            var losses = Convert.ToDecimal(strategy.Trades.Where(m => m.Outcome == TradeOutcome.Loss).Count());
            var wins = Convert.ToDecimal(strategy.Trades.Where(m => m.Outcome == TradeOutcome.Win).Count());
            var totalTrades = Convert.ToDecimal(strategy.Trades.Count);
            var winRate = Convert.ToDecimal(wins / totalTrades * 100);
            Console.WriteLine(string.Format("{0,10} | {1,10} | {2, 10}", "Wins", "Losses", "Win-Rate"));
            Console.WriteLine(string.Format("{0,10} | {1,10} | {2, 10}%", wins, losses, winRate.ToString("0.00")));
        }
        #endregion

        #region event handlers
        void Strategy_TradeClosed(object? sender, TradingDataAnalytics.Domain.Events.TradeClosedEventArgs e)
        {
            var dateClosed = e.ClosedTrade.Outcome == TradeOutcome.Loss ? e.ClosedTrade.StopLoss?.ClosingDate : e.ClosedTrade.ProfitTargets.Last().ClosingDate;
            var closingPrice = e.ClosedTrade.Outcome == TradeOutcome.Loss ? e.ClosedTrade.StopLoss?.OrderPrice : e.ClosedTrade.ProfitTargets.Last().OrderPrice;

            Console.WriteLine(string.Format("{0, 12} | {1,20} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12}",
                e.ClosedTrade.Id.ToString().Substring(0, 7),
                dateClosed,
                "-",
                "-",
                closingPrice,
                string.Format("{0:C}", e.ClosedTrade.Profit),
                e.ClosedTrade.Outcome));
        }

        private void Strategy_TradeCreated(object? sender, Events.TradeCreatedEventArgs e)
        {
            Console.WriteLine(string.Format("{0, 12} | {1,20} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12}",
                e.TradeCreated.Id.ToString().Substring(0, 7),
                e.TradeCreated.ProfitTargets.First().OpeningDate,
                e.TradeCreated.TradeDirection,
                e.TradeCreated.InitialEntryPrice,
                e.TradeCreated.StopLoss?.OrderPrice,
                "-",
                "-"));
        }

        private void Strategy_ProfitTargetHit(object? sender, Events.ProfitTargetHitEventArgs e)
        {
            Console.WriteLine(string.Format("{0, 12} | {1,20} | {2, 12} | {3, 12} | {4, 12} | {5, 12} | {6, 12}",
                e.ProfitTargetOrder?.Id.ToString().Substring(0, 7),
                e.ProfitTargetOrder?.OpeningDate,
                "-",
                "-", 
                e.ProfitTargetOrder?.OrderPrice,
                string.Format("{0:C}", e.Profit),
                "PT Hit"));
        }
        #endregion
    }
}
