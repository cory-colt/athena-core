using Athena.Domain.Data;
using Athena.Domain.Interfaces;
using Athena.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Application.Data
{
    /// <summary>
    /// Loads <see cref="CandleStick"/> data from a CSV file
    /// </summary>
    public class CsvCandleDataProvider : ICandleDataProvider
    {
        // TODO: this should not be hard-coded
        private string _csvFile = Path.Combine(@"I:\Windows Projects\athena-core", "market-data-sample.txt");

        #region ICandleDataProvider implementation
        /// <summary>
        /// Loads <see cref="CandleStick"/> into collection of 1-minute candles
        /// <para>
        ///     Candle stick data is returned as a 1 minute timeframe and arranged in chronological order
        /// </para>
        /// </summary>
        /// <returns>Collection of <see cref="CandleStick"/> objects</returns>
        public IEnumerable<CandleStick> LoadCandleStickData()
        {
            // read in the raw candle data from the csv/flat file
            IEnumerable<string> rawCandleData = File.ReadLines(this._csvFile);

            // convert raw data to 1 minute candlesticks (raw data is a csv of 1 minute candle data)
            List<CandleStick> candles = CandleDataParser.ConvertRawDataToCandleSticks(rawCandleData);

            return candles;
        }
        #endregion
    }
}
