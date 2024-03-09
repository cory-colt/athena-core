using System.Linq;
using System.Text.Json;
using TradingDataAnalytics.Application;
using TradingDataAnalytics.Domain;
using TradingDataAnalytics.Domain.Enums;
using TradingDataAnalytics.Domain.Indicators;
using TradingDataAnalytics.Domain.Strategy;

var ema = new Ema();
var ema1LookbackPeriod = 20;
var ema2LookbackPeriod = 10;
bool outputAllCandleData = true;
bool showAggregatedSessionData = false;

List<StrategyConfig> strategyConfigs = new List<StrategyConfig>();

using (StreamReader r = new StreamReader(Path.Combine(@"I:\Windows Projects\athena-core", "strategies.json")))
{
    string json = r.ReadToEnd();
    strategyConfigs = JsonSerializer.Deserialize<List<StrategyConfig>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}

    // HACK: these can probably move into the Strategy base class in the .Init() method
    string candleStickData = Path.Combine(@"I:\Windows Projects\athena-core", "market-data-sample.txt");
IEnumerable<string> lines = File.ReadLines(candleStickData);

// setup the strategy
PriceExtremeStrategy strategy = new PriceExtremeStrategy(
    3,
    new TimeOnly(8, 30),
    new TimeOnly(10, 0),
    50,
    10000);
strategy.Name = "Price Extreme Strategy";
strategy.MaxTradesPerSession = 2;

// subscrbe to strategy events
strategy.TradeClosed += Strategy_TradeClosed;

void Strategy_TradeClosed(object? sender, TradingDataAnalytics.Domain.Events.TradeClosedEventArgs e)
{
    var dateClosed = e.ClosedTrade.Outcome == TradeOutcome.Loss ? e.ClosedTrade.StopLoss.ClosingDate : e.ClosedTrade.ProfitTarget.ClosingDate;
    Console.WriteLine($"Trade Closed at {dateClosed}...{e.OldAccountBalance} {e.NewAccountBalance} {e.ClosedTrade.Outcome} {e.ClosedTrade.Profit}");
    Console.WriteLine();
}

// init the strategy
strategy.Init(lines);

// add the indicators to the strategy
strategy.EmaIndicators.Add(ema1LookbackPeriod, ema.Calculate(strategy.Candles, ema1LookbackPeriod).ToList());
strategy.EmaIndicators.Add(ema2LookbackPeriod, ema.Calculate(strategy.Candles, ema2LookbackPeriod).ToList());

Console.WriteLine($"Executing Strategy: [{strategy.Name}] - Starting Account Balance: [{strategy.AccountBalance}]");
Console.WriteLine("----------------------------------------------------------------------------------------------");


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
        if (strategy.Trades?.Where(m => m.Outcome == TradeOutcome.Win && m.DateInitiated.ToShortDateString() == session.Key).Count() > 0)
            break;

        // we're out of the market - check for long entry conditions
        if (strategy.LongEntryCondition(candle) && sessionTradeCount < strategy.MaxTradesPerSession)
        {
            strategy.ExecuteTrade(new Trade 
            {
                Id = Guid.NewGuid(),
                InitialEntryPrice = candle.Close, 
                Contracts = strategy.Contracts, 
                DateInitiated = candle.TimeOfDay, 
                Outcome = TradeOutcome.Pending, 
                TradeDirection = Direction.Long, 
                StopLoss = new Order
                {
                    Id = Guid.NewGuid(),
                    OpeningDate = candle.TimeOfDay,
                    OrderDirection = Direction.StopLoss,
                    OrderPrice = candle.Close - 30
                }, 
                ProfitTarget = new Order
                {
                    Id = Guid.NewGuid(),
                    OpeningDate = candle.TimeOfDay,
                    OrderDirection = Direction.Long,
                    OrderPrice = candle.Close + 10
                }
            });

            Console.WriteLine($"Long Trade Executed: [{candle.TimeOfDay}] Entry Price: [{candle.Close -+ 20}] Stop Loss: [{candle.Close - 20}]");
        }

        if (strategy.ShortEntryCondition(candle) && sessionTradeCount < strategy.MaxTradesPerSession)
        {
            // setup the short trade
            var shortTrade = new Trade
            {
                Id = Guid.NewGuid(),
                InitialEntryPrice = candle.Close,
                Contracts = strategy.Contracts,
                DateInitiated = candle.TimeOfDay,
                Outcome = TradeOutcome.Pending,
                TradeDirection = Direction.Short,
                StopLoss = new Order
                {
                    Id = Guid.NewGuid(),
                    OpeningDate = candle.TimeOfDay,
                    OrderDirection = Direction.StopLoss,
                    OrderPrice = candle.Close + 30
                },
                ProfitTarget = new Order
                {
                    Id = Guid.NewGuid(),
                    OpeningDate = candle.TimeOfDay,
                    OrderDirection = Direction.Short,
                    OrderPrice = candle.Close - 10
                }
            };

            // execute the trade
            strategy.ExecuteTrade(shortTrade);

            sessionTradeCount++;

            Console.WriteLine($"Short Trade Executed: [{candle.TimeOfDay}] Entry Price: [{shortTrade.InitialEntryPrice}] Stop Loss: [{shortTrade.StopLoss.OrderPrice}]");
        }
    }
}

Console.WriteLine($"---------------------- STRATEGY SUMMARY STATISTICS --------------------------");
Console.WriteLine();

Console.WriteLine($"Gain on Account: {strategy.AccountBalance - strategy.InitialAccountBalance} ({(strategy.AccountBalance - strategy.InitialAccountBalance)/strategy.InitialAccountBalance * 100}%) - Total Trades: {strategy.Trades.Count}");
var losses = Convert.ToDecimal(strategy.Trades.Where(m => m.Outcome == TradeOutcome.Loss).Count());
var wins = Convert.ToDecimal(strategy.Trades.Where(m => m.Outcome == TradeOutcome.Win).Count());
var totalTrades = Convert.ToDecimal(strategy.Trades.Count);
var winRate = Convert.ToDecimal(wins/totalTrades * 100);
Console.WriteLine(string.Format("{0,10} | {1,10} | {2, 10}", "Wins", "Losses", "Win-Rate"));
Console.WriteLine(string.Format("{0,10} | {1,10} | {2, 10}%", wins, losses, winRate.ToString("0.00")));





//Console.WriteLine($"------------------------- Sessions Range: {sessions.First().Key} to {sessions.Last().Key} -------------------------");
//if (outputAllCandleData)
//{
//    Console.WriteLine(String.Format("{0, 25} | {1, 40} | {2, 10} | {3, 12} | {4, 12}", "Date", "OHLC", "Volume", "EMA Value", "Price Change"));
//    Console.WriteLine("--------------------------------------------------------------------------------------------------------------------");
//    foreach (var candle in candlesInSession)
//    {
//        // Console.WriteLine(String.Format("{0, 25} | {1, 40} | {2, 10} | {3, 12} | {4, 12}", candle.TimeOfDay, $"[{candle.Open}, {candle.High}, {candle.Low}, {candle.Close}]", candle.Volume, candle.Ema20Value, candle.Ema20Displacement));
//    }
//}


//if (showAggregatedSessionData)
//{
//    Console.WriteLine(String.Format("{0, 12} | {1, 20} | {2, 20}", "Date", "Max Price Change (%)", "Min Price Change (%)"));
//    Console.WriteLine($"--------------------------------------------------------------------------");

//    // output session info
//    foreach (var session in sessions)
//    {
//        // get session's highest price change
//        // session.Value.SessionHighestPriceChange = session.Value.Candles.Max(m => m.Ema20Displacement);

//        // get the session's lowest price change
//        // session.Value.SessionLowestPriceChange = session.Value.Candles.Min(m => m.Ema20Displacement);

//        Console.WriteLine(String.Format("{0, 12} | {1, 20} | {2, 20}", session.Key, session.Value.SessionHighestPriceChange, session.Value.SessionLowestPriceChange));
//    }
//}
