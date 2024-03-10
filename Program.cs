﻿using TradingDataAnalytics.Application;
using TradingDataAnalytics.Domain.Strategy;

// change the appearance of the console 
Console.BackgroundColor = ConsoleColor.Gray;
Console.Clear();
Console.ForegroundColor = ConsoleColor.Black;


var candleStickDataFile = Path.Combine(@"I:\Windows Projects\athena-core", "market-data-sample.txt");
var strategiesConfigFile = Path.Combine(@"I:\Windows Projects\athena-core", "strategies.json");

// setup the strategies that will be tested
List<Strategy> strategiesToTest = new List<Strategy>();
strategiesToTest.Add(new PriceExtremeStrategy());

// setup the strategy engine
StrategyEngine engine = new StrategyEngine(strategiesConfigFile, candleStickDataFile);

// run the engine against all the available strategies
engine.Run(strategiesToTest);