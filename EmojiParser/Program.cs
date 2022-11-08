using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace EmojiParser
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            try
            {
                Directory.CreateDirectory("Emoticons");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Access denied, try to \"Run as Administrator\".");
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            #region Get categories

            var smileysAndPeople = await GetEmojiCategory("https://emojipedia.org/people/","//html/body/div[2]/div[1]/ul");
            File.WriteAllLines("Emoticons\\SmileysAndPeople.txt", smileysAndPeople);

            var animalsAndNature = await GetEmojiCategory("https://emojipedia.org/nature/","//html/body/div[2]/div[1]/ul");
            File.WriteAllLines("Emoticons\\AnimalsAndNature.txt", animalsAndNature);

            var foodAndDrink = await GetEmojiCategory("https://emojipedia.org/food-drink/","//html/body/div[2]/div[1]/ul");
            File.WriteAllLines("Emoticons\\FoodAndDrink.txt", foodAndDrink);

            var activity = await GetEmojiCategory("https://emojipedia.org/activity/","//html/body/div[2]/div[1]/ul");
            File.WriteAllLines("Emoticons\\Activity.txt", activity);

            var travelAndPlaces = await GetEmojiCategory("https://emojipedia.org/travel-places/","//html/body/div[2]/div[1]/ul");
            File.WriteAllLines("Emoticons\\TravelAndPlaces.txt", travelAndPlaces);

            var objects = await GetEmojiCategory("https://emojipedia.org/objects/","//html/body/div[2]/div[1]/ul");
            File.WriteAllLines("Emoticons\\Objects.txt", objects);

            var symbols = await GetEmojiCategory("https://emojipedia.org/symbols/","//html/body/div[2]/div[1]/ul");
            File.WriteAllLines("Emoticons\\Symbols.txt", symbols);

            var flags = await GetEmojiCategory("https://emojipedia.org/flags/","//html/body/div[2]/div[1]/ul");
            File.WriteAllLines("Emoticons\\Flags.txt", flags);

            #endregion

            #region Get full emoticons collection

            var url = "https://emojipedia.org/apple/";
            var web = new HtmlWeb();

            Console.WriteLine("Image sources loading...");
            
            // Load Html document
            var doc = await web.LoadFromWebAsync(url);

            // Find specific node in the document
            var node = doc.DocumentNode.SelectSingleNode("//html/body/div[2]/div[1]/article/ul");
            // Get the list of nodes with images
            var nodeList = node.SelectNodes("//img/@src");

            Console.WriteLine("Image sources loaded");
            Console.WriteLine("Downloading images...");

            var emojiList = new List<string>();
            var client = new WebClient();

            foreach (var htmlNode in nodeList)
            {
                Console.Write($"Downloading {htmlNode.Attributes["title"].Value}...");

                //Get the download link for the file
                var source = htmlNode.Attributes["class"] == null
                    ? htmlNode.Attributes["src"].Value
                    : htmlNode.Attributes["data-src"].Value;

                //Get file name from Title attribute
                var fileName = Regex.Replace(htmlNode.Attributes["title"].Value.ToLower().Replace(" ", "_"), @"[^\w_]", string.Empty);

                //Get the substring of source that contains unicode data
                var sourcePartWithHexValue = source.Substring(source.LastIndexOf('/') + 1, source.LastIndexOf('.') - (source.LastIndexOf('/') + 1));
                //Split the above substring into parts for further analysis
                var possibleHexValue = Regex.Split(sourcePartWithHexValue.Substring(sourcePartWithHexValue.IndexOf('_') + 1), "_");

                string finalHexValue;

                // This if else statement is made on an assumption that we can safely discard the end of the string
                // if it repeats the end of the previous part of the string. Example:
                // 1f91a-1f3fb_1f3fb - here the end is _1f3fb and it gets thrown away because 1f91a-1f3fb already contains that value
                if (possibleHexValue.Length > 2)
                {
                    finalHexValue = possibleHexValue[2].Equals(possibleHexValue[1].Substring(possibleHexValue[1].Length - possibleHexValue[2].Length)) 
                            ? possibleHexValue[1]
                            : possibleHexValue[2];
                }
                else
                {
                    finalHexValue = possibleHexValue.LastOrDefault();
                }

                //Convert unicode values into different format
                if (finalHexValue != null && finalHexValue.Contains('-'))
                {
                    var hexValues = Regex.Split(finalHexValue, "-");

                    var convertedValue = hexValues.Select(UnicodeFormatConverter).ToArray();

                    var convertedChar = convertedValue.Aggregate(string.Empty, (current, value) => current + char.ConvertFromUtf32(value));

                    // Create a list with unicode and filepath values
                    emojiList.Add($"{convertedChar},Images\\Emoticons\\{fileName}.png");
                }
                else
                {
                    var convertedValue = UnicodeFormatConverter(finalHexValue);

                    // Create a list with unicode and filepath values
                    emojiList.Add($"{char.ConvertFromUtf32(convertedValue)},Images\\Emoticons\\{fileName}.png");
                }


                // Download file
                client.DownloadFile(source, $"Emoticons\\{fileName}.png");

                Console.WriteLine("done");
            }

            // Save file with unicode and filepath values
            File.WriteAllLines("Emoticons\\emoji.txt", emojiList);

            stopwatch.Stop();

            Console.WriteLine($"Download complete in {stopwatch.Elapsed}");
            Console.ReadLine();

            #endregion
        }

        private static int UnicodeFormatConverter(string hexValue)
        {
            return Convert.ToInt32($"0x{hexValue}", 16);
        }

        private static async Task<List<string>> GetEmojiCategory(string url, string xPath)
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);
            var node = doc.DocumentNode.SelectSingleNode(xPath);
            var nodeList = node.SelectNodes("//li").Where(n => n.ParentNode.HasClass("emoji-list"));

            foreach (var htmlNode in nodeList)
            {
                var emoji = 0;

                if (htmlNode.InnerText.IndexOf(' ') > 1)
                {
                    var buffer = string.Empty;

                    for (var i = 0; i < htmlNode.InnerText.IndexOf(' '); i++)
                    {
                        buffer += char.ConvertToUtf32(htmlNode.InnerText, i);
                    }
                }
                
                emoji = char.ConvertToUtf32(htmlNode.InnerText, 0);
                var name = htmlNode.InnerText.Substring(3).ToLower().Replace(' ', '_');
            }

            return (from htmlNode in nodeList let emoji = char.ConvertToUtf32(htmlNode.InnerText, 0) let name = htmlNode.InnerText.Substring(3).ToLower().Replace(' ', '_') select char.ConvertFromUtf32(emoji) + "," + name + "\n").ToList();
        }
    }
}
