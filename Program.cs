using HtmlAgilityPack;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using static KemonoDownloader.Logic.PostManipulation;
using KemonoDownloader.Logic;
using KemonoDownloaderDataModels.Models;
using KemonoDownloaderDataModels.Models.Enum;

namespace KemonoDownloader
{
    internal class Program
    {
        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        const string pagination = "?o=";
        const string kemonoBaseUrl = "https://kemono.party/";
        private static KemonoDbContext dbContext = new KemonoDbContext();

        static void Main(string[] args)
        {
            try
            {
                List<string> artistUrls = null;

                //Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine("Start ");
                //Console.ResetColor();
                //Console.Write("up. \n");
                Console.WriteLine("Would you like to download the artists to which the links are in the file \n\"artistUrls.txt\" or would you like to input the artist urls?");
                string menu;
                do
                {
                    Console.WriteLine("Input \"1\" to read from the file, or \"2\" to input the urls.");
                    menu = Console.ReadLine();
                    if (menu == "1" || menu == "2")
                        break;
                } while (true);

                if (menu == "1")
                {
                    artistUrls = ReadArtistUrlsFromFile();
                }
                if (menu == "2")
                {
                    Console.WriteLine("Input (paste or manually type in) the artist URLs. They must be seperated by a space. " +
                        "\n The program will not work properly if you don't correctly input the artist links." +
                        "\n Already existing files will not be downloaded anew.");
                    var artistUrlsRaw = Console.ReadLine();
                    SaveArtistUrlsToFile(artistUrlsRaw);
                    artistUrls = artistUrlsRaw.Split(" ", StringSplitOptions.TrimEntries).ToList();
                }

                Console.WriteLine("Do you want all of the media from a single post to also be put into post-based folder? \n");
                string choice;
                do
                {
                    Console.WriteLine("Input \"N\" for: artistname\\artworks file hierarcy. Input \"Y\" for: artistname\\post\\artworks file hierarcy.");
                    choice = Console.ReadLine();
                    var choiceLower = choice?.ToLower();
                    if ((choiceLower == "y") || (choiceLower == "n"))
                        break;
                } while (true);

                DownloadArt(choice, artistUrls);

            }
            catch (Exception e)
            {
                Logger.Error("Unhandled exception HAPPENEDZ. Read the info:", e.Message, e.StackTrace);
            }
        }
        static void SaveArtistUrlsToFile(string artistUrls)
        {
            using (StreamWriter sw = new StreamWriter("artistUrls.txt"))
            {
                sw.Write(artistUrls);
            }
        }
        static void DownloadArt(string choice, List<string> artistUrls)
        {
            // One by one artist
            foreach (var artistUrl in artistUrls)
            {
                // TODO: Check if artists already exists in DB

                // Checks if the current artist url already exists in the database,
                // indicating that the url was already at least partially processed 
                var alreadyProcessed = dbContext.Artists.FirstOrDefault(x => x.ArtistUrls.Any(y => y.Url == artistUrl));

                var artistIndex = TryLoop(() =>
                {
                    return GetAllPagesAndArtistName(artistUrl);
                }
                );
                var fullPages = artistIndex.Item1 / 25;
                if (artistIndex.Item1 % 25 == 0)
                {
                    fullPages -= 1;
                }
                Directory.CreateDirectory(artistIndex.Item2);
                for (int i = 0; i <= fullPages; i++)
                {
                    var postIds = TryLoop(() =>
                    {
                        return GetAllPostOnAPage(artistUrl + pagination + i * 25);
                    });

                    foreach (string postId in postIds)
                    {
                        Sleep(1);

                        GetPostAttachments(choice, artistUrl + "/post/" + postId, artistIndex);
                        GetImagesFromASinglePost(choice, artistUrl + "/post/" + postId, artistIndex);

                    }
                }
            }         
        }
        static Tuple<int, string> GetAllPagesAndArtistName(string artistUrl)
        {
            HtmlDocument doc = new HtmlDocument();
            HtmlWeb hw = new HtmlWeb();
            doc = hw.Load(artistUrl);
            var paginator = doc.DocumentNode.SelectSingleNode("//small");
            int pages = int.Parse(paginator.InnerHtml.Split("\n")[1].Split(" ").Last());
            var artistNameator = doc.DocumentNode.SelectSingleNode("//*[@itemprop='name']");
            string artistName = artistNameator.GetDirectInnerText();
            Artist artist = new Artist();

            artist.Name = artistName;

            ArtistUrl theArtistUrl = new ArtistUrl();
            theArtistUrl.Artist = artist;
            theArtistUrl.Url = artistUrl;
            string[] artistUrlType = artistUrl.Split("/");
            switch (artistUrlType[1])
            {
                case "fanbox":
                    theArtistUrl.UrlType = ArtistUrlTypes.PixivFanbox;
                    break;
                case "patreon":
                    theArtistUrl.UrlType = ArtistUrlTypes.Patreon;
                    break;
                case "gumroad":
                    theArtistUrl.UrlType = ArtistUrlTypes.Gumroad;
                    break;
                case "subscribestar":
                    theArtistUrl.UrlType = ArtistUrlTypes.SubscribeStar;
                    break;
                case "dlsite":
                    theArtistUrl.UrlType = ArtistUrlTypes.DLSite;
                    break;
                case "discord":
                    theArtistUrl.UrlType = ArtistUrlTypes.Discord;
                    break;
                case "fantia":
                    theArtistUrl.UrlType = ArtistUrlTypes.Fantia;
                    break;
                case "boosty":
                    theArtistUrl.UrlType = ArtistUrlTypes.Boosty;
                    break;
                case "afdian":
                    theArtistUrl.UrlType = ArtistUrlTypes.Afdian;
                    break;
            }
            artist.ArtistUrls.Add(theArtistUrl);
            dbContext.Artists.Add(artist);
            dbContext.SaveChanges();

            return Tuple.Create(pages, artistName);
        }

        // get all posts of an artist on one page
        static List<string> GetAllPostOnAPage(string pageUrl)
        {
            HtmlWeb hw = new HtmlWeb();
            List<string> postIds = new List<string>();
            var doc = hw.Load(pageUrl);
            foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//article[contains(@class, 'post-card')]"))
            {
                // Get the values of the href attribute of each post on the page
                string postId = div.GetAttributeValue("data-id", string.Empty);
                postIds.Add(postId);
            }
            return postIds;
        }
        static void GetImagesFromASinglePost(string choice, string postUrl, Tuple<int, string> artistIndex)
        {
            HtmlWeb hw = new HtmlWeb();
            HtmlDocument doc = new HtmlDocument();
            doc = TryLoop(() =>
            {
                return hw.Load(postUrl);
            });

            if (doc.DocumentNode.SelectNodes("//div[contains(@class, 'post__files')]") != null)
            {
                foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//div[contains(@class, 'post__files')]"))
                {
                    var counter = 0;
                    // Get the value of the HREF attribute
                    foreach (HtmlNode url in div.SelectNodes("//a[contains(@class, 'fileThumb')]"))
                    {
                        string hrefValue = url.GetAttributeValue("href", string.Empty);
                        string extension = hrefValue.Split(".").Last();
                        if (extension.Equals("jpe"))
                        {
                            extension = "jpg";
                        }
                        var fileName = ValidatePathName(postUrl.Split("post/")[1] + "_" + counter + "." + extension);

                        string postFolder = artistIndex.Item2;

                        // create a folder for each post as well
                        if (choice.Equals("y"))
                        {
                            var postName = GetPostName(doc);
                            postFolder = postFolder + "\\" + postName;
                        }

                        // Make sure that the directory exists
                        System.IO.Directory.CreateDirectory(postFolder);


                        if (!CheckIfFileExists(postFolder + "\\" + fileName))
                        {
                            Console.WriteLine($"Saving: {fileName}");
                            if (extension.Equals("gif"))
                            {
                                SaveGif(kemonoBaseUrl + hrefValue, postFolder + "\\" + fileName);
                            }
                            else SaveImage(kemonoBaseUrl + hrefValue, postFolder + "\\" + fileName);

                            Sleep();
                        }
                        else Console.WriteLine($"File exists, skipping: {fileName}");
                        counter += 1;
                    }
                }
            }
        }

        // https://stackoverflow.com/a/23103561/10299831
        static T TryLoop<T>(Func<T> anyMethod)
        {
            while (true)
            {
                try
                {
                    return anyMethod();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    System.Threading.Thread.Sleep(2000); // *
                }
            }
            return default(T);
        }
        static void GetPostAttachments(string choice, string postUrl, Tuple<int, string> artistIndex)
        {
            HtmlWeb hw = new HtmlWeb();
            var doc = TryLoop(() =>
            {
                return hw.Load(postUrl);
            });

            if (doc.DocumentNode.SelectNodes("//a[contains(@class, 'post__attachment-link')]") != null)
            {
                foreach (HtmlNode attachment in doc.DocumentNode.SelectNodes("//a[contains(@class, 'post__attachment-link')]"))
                {
                    var url = attachment.GetAttributeValue("href", string.Empty);
                    var fileName = attachment.InnerText;
                    fileName = ValidatePathName(postUrl.Split("post/")[1] + "_" + fileName.Split("\n")[1].TrimStart().Split("\n")[0]);
                    if (!CheckIfFileExists(artistIndex.Item2 + "\\" + fileName))
                    {
                        var fullUrl = kemonoBaseUrl + url;
                        Console.WriteLine("Downloading: " + fullUrl);
                        WebClient webClient = new WebClient();
                        Console.WriteLine($"Downloading attachment: {fileName}");

                        // Create post folder
                        if (choice.Equals("y"))
                        {
                            System.IO.Directory.CreateDirectory(artistIndex.Item2);
                        }

                        TryLoopAction(() =>
                        {
                            webClient.DownloadFile(new Uri(fullUrl), artistIndex.Item2 + "\\" + fileName);
                        });
                        Console.WriteLine("Download done.");
                        webClient.Dispose();
                        Sleep();
                    }
                    else Console.WriteLine($"File exists, skipping: {fileName}");
                }
            }
        }
        static void TryLoopAction(Action anyAction)
        {
            while (true)
            {
                try
                {
                    anyAction();
                    break;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Error: ");
                    Console.ResetColor();
                    Console.Write(e.Message + "\n");
                    System.Threading.Thread.Sleep(2000); // *
                }
            }
        }
        static string GetPostName(HtmlDocument doc)
        {
            return ValidatePathName(doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'post__title')]").ChildNodes.ElementAt(1).InnerText);
        }
        static string ValidatePathName(string input, string replacement = "")
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            var fileName = r.Replace(input, replacement);
            if (fileName.Length > 120)
            {
                fileName = fileName.Substring(0, 119);
            }
            return fileName;
        }
        static void Sleep(int length = 1)
        {
            Random rnd = new Random();
            var randInt = 0;
            if (length == 0)
            {
                randInt = rnd.Next(1354, 5987);
                Console.WriteLine($"Next post, slept for {randInt} miliseconds so as not to overburden the site.");
            }
            else
            {
                randInt = rnd.Next(585, 3576);
                Console.WriteLine($"Slept for {randInt} miliseconds so as not to overburden the site.");
            }

            Thread.Sleep(randInt);
        }
        static bool CheckIfFileExists(string fileName)
        {
            var workingDirectory = Environment.CurrentDirectory;
            var file = $"{workingDirectory}\\{fileName}";
            return File.Exists(file);
        }
        static void SaveGif(string gifUrl, string filePath)
        {
            Console.WriteLine("Downloading: " + gifUrl);
            WebClient webClient = new WebClient();
            Console.WriteLine($"Downloading attachment: {filePath}");
            TryLoopAction(() =>
            {
                webClient.DownloadFile(new Uri(gifUrl), filePath);
            });
            webClient.Dispose();
        }
        static void SaveImage(string imageUrl, string filePath)
        {
            WebClient client = new WebClient();

            Console.WriteLine("Downloading: " + imageUrl);
            Stream stream = Stream.Null;
            Bitmap bitmap = null;
            TryLoopAction(() =>
            {
                stream = TryLoop(() =>
                {
                    return client.OpenRead(imageUrl);
                });
                bitmap = new Bitmap(stream);
            });

            if (bitmap != null)
            {
                bitmap.Save(filePath);
            }

            stream.Flush();
            stream.Close();
            client.Dispose();
        }
    }
}