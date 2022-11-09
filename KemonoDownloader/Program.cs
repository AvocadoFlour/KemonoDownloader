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
                Console.WriteLine("Would you like to download the artists to which the links are in the file \n\"artistUrls.txt\" or would you like to input the artist urls?");
                menu = ReadUserInput("Input \"1\" to read from the file, or \"2\" to input the urls.", new List<string>() { "1", "2" });

                // Read from file
                if (menu == "1")
                {
                    artistUrls = ReadArtistUrlsFromFile();
                }
                // Read console input
                if (menu == "2")
                {
                    Console.WriteLine("Input (paste or manually type in) the artist URLs. They must be seperated by a space. " +
                        "\n The program will not work properly if you don't correctly input the artist links." +
                        "\n Already existing files will not be downloaded anew.");
                    var artistUrlsRaw = Console.ReadLine();
                    SaveArtistUrlsToFile(artistUrlsRaw);
                    artistUrls = artistUrlsRaw.Split(" ", StringSplitOptions.TrimEntries).ToList();
                }

                // How to save downloaded posts
                Console.WriteLine("Do you want all of the media from a single post to also be put into post-based folder? \n");
                choice = ReadUserInput("Input \"N\" for: artistname\\artworks file hierarcy. Input \"Y\" for: artistname\\post\\artworks file hierarcy.", new List<string>() { "y", "n" });

                DownloadingArt da = new DownloadingArt();
                da.DownloadArt(artistUrls);

            }
            catch (Exception e)
            {
                logger.Error($"Unhandled exception HAPPENEDZ. Read the info: {e.Message}" +
                    $"\n {e.StackTrace}" +
                    $"\n {e.InnerException}");
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