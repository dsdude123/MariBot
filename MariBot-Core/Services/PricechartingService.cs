using ConsoleTables;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Net;
using System.Web;

namespace MariBot.Core.Services
{
    public class PricechartingService
    {
        private ILogger<PricechartingService> logger;

        public PricechartingService(ILogger<PricechartingService> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Search Pricecharting for price data and return the data as a string table
        /// </summary>
        /// <param name="game">Game title to search</param>
        /// <returns>String formated as a table</returns>
        public async Task<string> SearchPricechartingDataAsTable(string game)
        {
            try
            {
                // Get the HTML page from Pricecharting
                var rawHtml = await SearchPricechartingRaw(game);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(rawHtml);

                // Check if we got search results or a direct match
                var resultsTable = htmlDocument.DocumentNode.SelectSingleNode("//table[1][@id=\"games_table\"]");
                var directResult = htmlDocument.DocumentNode.SelectSingleNode("//div[1][@id=\"game-page\"]");
                if (resultsTable != null)
                {
                    logger.LogInformation("Pricecharting returned multiple results.");
                    var headerColumns = resultsTable.SelectNodes("//tr/th");
                    var data = resultsTable.SelectNodes("//tr[not(th)]/td");

                    if (headerColumns.Count < 5)
                    {
                        logger.LogError("Missing header columns.");
                        return "Missing header columns.";
                    }

                    var output = new ConsoleTable();
                    output.AddColumn(new string[] { "Title", "Console", "Loose", "CIB", "New" });

                    logger.LogInformation("There are {} entries.", (data.Count / 6));

                    if ((data.Count % 6) == 0)
                    {
                        for (int i = 0; i < data.Count; i += 6)
                        {
                            output = TryAddRow(output, new string[] { Sanitize(data[i].InnerText), Sanitize(data[i + 1].InnerText), Sanitize(data[i + 2].InnerText), Sanitize(data[i + 3].InnerText), Sanitize(data[i + 4].InnerText) });
                        }
                    }
                    else if (((data.Count - 1) % 6) == 0) // Sometimes there is one extra cell at the end
                    {
                        for (int i = 0; i < data.Count - 1; i += 6)
                        {
                            output = TryAddRow(output, new string[] { Sanitize(data[i].InnerText), Sanitize(data[i + 1].InnerText), Sanitize(data[i + 2].InnerText), Sanitize(data[i + 3].InnerText), Sanitize(data[i + 4].InnerText) });
                        }
                    }
                    else
                    {
                        logger.LogError("Data has more elements than expected.");
                        return "Data has more elements than expected.";
                    }
                    return output.ToStringAlternative();


                }
                else if (directResult != null)
                {
                    logger.LogInformation("Pricecharting returned a direct match.");

                    var gameTitle = Sanitize(directResult.SelectSingleNode("//h1[1][@id=\"product_name\"]").GetDirectInnerText());
                    var console = Sanitize(directResult.SelectSingleNode("//h1[1][@id=\"product_name\"]/a").GetDirectInnerText());
                    var priceData = directResult.SelectSingleNode("//table[1][@id=\"price_data\"]");

                    var output = new ConsoleTable();
                    output.AddColumn(new string[] { "Title", "Console", "Loose", "CIB", "New", "Graded", "Box Only", "Manual Only" });

                    var loosePrice = Sanitize(priceData.SelectSingleNode("//tbody/tr[1]/td[@id=\"used_price\"]/span[1][@class=\"price js-price\"]").InnerText);
                    var completePrice = Sanitize(priceData.SelectSingleNode("//tbody/tr[1]/td[@id=\"complete_price\"]/span[1][@class=\"price js-price\"]").InnerText);
                    var newPrice = Sanitize(priceData.SelectSingleNode("//tbody/tr[1]/td[@id=\"new_price\"]/span[1][@class=\"price js-price\"]").InnerText);
                    var gradedPrice = Sanitize(priceData.SelectSingleNode("//tbody/tr[1]/td[@id=\"graded_price\"]/span[1][@class=\"price js-price\"]").InnerText);
                    var boxPrice = Sanitize(priceData.SelectSingleNode("//tbody/tr[1]/td[@id=\"box_only_price\"]/span[1][@class=\"price js-price\"]").InnerText);
                    var manualPrice = Sanitize(priceData.SelectSingleNode("//tbody/tr[1]/td[@id=\"manual_only_price\"]/span[1][@class=\"price js-price\"]").InnerText);

                    output.AddRow(gameTitle, console, loosePrice, completePrice, newPrice, gradedPrice, boxPrice, manualPrice);
                    return output.ToStringAlternative();
                }
                else
                {
                    return "No results were found.";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.Message;
            }


        }

        /// <summary>
        /// Calls Pricecharting and returns the raw HTML data
        /// </summary>
        /// <param name="game">Game to search</param>
        /// <returns>HTML string</returns>
        private async Task<string> SearchPricechartingRaw(string game)
        {
            logger.LogInformation("Querying Pricecharting for \"{}\"", game);
            var gameTitle = GameToParameter(game);
            var queryUrl = $"https://www.pricecharting.com/search-products?type=prices&q={gameTitle}&go=Go";
            var request = WebRequest.CreateHttp(queryUrl);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36";
            request.Method = "GET";
            using (var response = await request.GetResponseAsync())
            {
                StreamReader reader = new StreamReader(response.GetResponseStream(), encoding: System.Text.Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Converts the game title to a URL parameter for use with Pricecharting
        /// </summary>
        /// <param name="game">Game title</param>
        /// <returns>URL friendly string</returns>
        private string GameToParameter(string game)
        {
            var result = game.Replace(' ', '-');
            return HttpUtility.UrlEncode(result);
        }

        /// <summary>
        /// Decodes and removes excess spaces and line breaks from table cell data from Pricecharting
        /// </summary>
        /// <param name="text">Table cell</param>
        /// <returns>Sanitized string</returns>
        public string Sanitize(string text)
        {
            var result = text.Replace("\n                    ", "");
            result = result.Replace("\n", "");
            result = result.Trim();
            return HttpUtility.HtmlDecode(result);
        }

        /// <summary>
        /// Tries to add a new row to a console table while remaining with Discord message limits
        /// </summary>
        /// <param name="table">Table before adding row</param>
        /// <param name="row">New row to add</param>
        /// <returns>Result table</returns>
        public ConsoleTable TryAddRow(ConsoleTable table, string[] row)
        {
            table.AddRow(row);
            if (table.ToStringAlternative().Length < 1992) // Discord message limit, should decouple this to make the service generic
            {
                return table;
            }
            else // Too big, remove the new row and return.
            {
                table.Rows.Remove(row);
                return table;
            }
        }
    }
}
