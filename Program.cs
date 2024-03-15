using Athena.Domain.Strategy;
using Athena.Application.Logging;
using Athena.Application.Strategies;
using Athena.Application.Data;

// change the appearance of the console 
Console.BackgroundColor = ConsoleColor.Gray;
Console.Clear();
Console.ForegroundColor = ConsoleColor.Black;


// setup the strategy engine
//StrategyEngine engine = new StrategyEngine(
//    new PriceExtremeStrategy(), 
//    new JsonStrategyConfiguration(), 
//    new CsvCandleDataProvider(), 
//    new ConsoleLogger());

// setup the strategy engine
StrategyEngine engine = new StrategyEngine(
    new EnterAfterPriceExtremeAndEmaCloseStrategy(),
    new JsonStrategyConfiguration(),
    new CsvCandleDataProvider(),
    new ConsoleLogger());

// run the engine against all the available strategies
var startDate = DateTime.Parse("2/12/2024");
var endDate = DateTime.Parse("2/29/2024");
engine.Run(startDate);