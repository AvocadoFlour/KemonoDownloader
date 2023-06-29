using HtmlAgilityPack;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using static KemonoDownloader.Logic.ProgramFunctions;
using KemonoDownloaderDataModels.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NLog.Fluent;
using System.Data.SqlTypes;
using KemonoDownloader.Logic;
using Pastel;
using KemonoDownloader.Menu;

namespace KemonoDownloader
{
    internal class Program
    {
        public static readonly Logger logger = LogManager.GetCurrentClassLogger();
        const string kemonoBaseUrl = "https://kemono.party/";

        static void Main(string[] args)
        {
            try
            {
                List<string> artistUrls = null;
                string menu;
                string choice;

                // Where to get the artists links from
                Console.WriteLine("Master, what is it that you oh so desire?");
                menu = ReadUserInput(wrongInputMessage, new List<string>() { MenuChoices.MENU_OPTION_1, MenuChoices.MENU_OPTION_2 } );

                // Read console input
                if (menu == "1")
                {
                    Console.WriteLine("Input (paste or manually type in) the artist URLs. They must be seperated by a space. " +
                        "\n The program will not work properly if you don't correctly input the artist links." +
                        "\n Already existing files will not be downloaded anew.");
                    var artistUrlsRaw = Console.ReadLine();

                    //TODO: Spremi u BAZU, ne u .txt file
                    SaveArtistUrlsToFile(artistUrlsRaw);

                    string urlRegexPattern = "(^http?s?:\\/\\/(?:www.)?(kemono).(party)/(fanbox|patreon)\\/(user)\\/([0-9])+)";
                    Regex urlRegex = new Regex(urlRegexPattern);

                    string[] perfectList = Regex.Split(artistUrlsRaw, urlRegexPattern);

                    foreach (string perfect in perfectList) 
                    {
                        
                        Console.WriteLine(perfect);
                    }

                    artistUrls = artistUrlsRaw.Split(" ", StringSplitOptions.TrimEntries).ToList();
                }
                // Read from file
                if (menu == "2")
                {
                    artistUrls = ReadArtistUrlsFromFile();
                }

                // How to save downloaded posts
                Console.WriteLine("Do you want all of the media from a single post to also be put into post-based folder? (Y/N) \n");
                choice = ReadUserInput(wrongInputMessage + "\n Input \"N\" for: artistname\\artworks file hierarcy. Input \"Y\" for: artistname\\post\\artworks file hierarcy.", new List<string>() { "y", "n" });

                DownloadingArt da = new DownloadingArt();
                da.DownloadArt(artistUrls);

            }
            catch (Exception e)
            {
                logger.Error($"Unhandled exception HAPPENEDZ. Read the info: {e.Message}" +
                    $"\n {e.StackTrace}" +
                    $"\n {e.InnerException}" +
                    $"\n {e.GetBaseException}" +
                    $"\n {e.Source}");
            }
        }
        
        //static string GetPostName(HtmlDocument doc)
        //{
        //    return ValidatePathName(doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'post__title')]").ChildNodes.ElementAt(1).InnerText);
        //}
        //static string ValidatePathName(string input, string replacement = "")
        //{
        //    var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        //    var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        //    var fileName = r.Replace(input, replacement);
        //    if (fileName.Length > 120)
        //    {
        //        fileName = fileName.Substring(0, 119);
        //    }
        //    return fileName;
        //}
    }
}