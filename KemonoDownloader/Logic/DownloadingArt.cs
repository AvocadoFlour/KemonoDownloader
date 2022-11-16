using HtmlAgilityPack;
using System.Net;
using KemonoDownloaderDataModels.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Drawing;
using System;
using NLog;

namespace KemonoDownloader.Logic
{
    internal class DownloadingArt
    {
        public static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static readonly string KEMONO_BASE_URL = "https://kemono.party/";
        private readonly int POSTS_PER_PAGE = 50;
        private readonly string paginationMarker = "?o=";
        private readonly MediaStorageOperations storageOps = new MediaStorageOperations();

        private CookieContainer _cookiesContainer => SetCookieContainer();
        public CookieContainer SetCookieContainer(string sessionString = "eyJfcGVybWFuZW50Ijp0cnVlLCJhY2NvdW50X2lkIjoxNzc0Nn0.Y0IIxA.blaHuCz9ddG7TEUw70X-oxMP1bo")
        {
            CookieContainer cookiesContainer = new CookieContainer();
            cookiesContainer.Add(new Cookie("session", sessionString) { Domain = "https://kemono.party/" });
            return cookiesContainer;
        }

        public void DownloadArt(List<string> artistUrls)
        {
            // One by one artist
            foreach (var artistUrl in artistUrls)
            {
                ProcessSingleArtistUrl(artistUrl);
            }
        }

        /// <summary>
        /// This gets all of the artists posts,
        /// saves them to the database,
        /// then proceeds to download their art from each post.
        /// </summary>
        /// <param name="artistUrl"></param>
        private void ProcessSingleArtistUrl(string artistUrl)
        {
            HtmlDocument firstArtistPage = TryLoop(() =>
            {
                return LoadPageHtmlFromUrl(artistUrl);
            }
            );

            string artistName = GetArtistsName(firstArtistPage);
            Artist artist = storageOps.GetArtistDatabaseInstance(artistName, artistUrl);

            int numberOfPosts = GetArtistsNumberOfPages(firstArtistPage);
            GetAllArtistPosts(artist, numberOfPosts);
            Console.WriteLine("Proceeding to download the artist's media from their " +
                "posts saved in the database.");
            DownloadArtistMedia(artist);
        }

        /// <summary>
        /// Fetched all artist's posts from Kemono and saves them in the database
        /// </summary>
        /// <param name="artist"></param>
        /// <param name="numberOfPosts"></param>
        private void GetAllArtistPosts(Artist artist, int numberOfPosts)
        {
            var fullPages = numberOfPosts / POSTS_PER_PAGE;
            if (numberOfPosts % POSTS_PER_PAGE == 0)
            {
                fullPages -= 1;
            }
            for (int i = 0; i <= fullPages; i++)
            {
                var postHrefs = TryLoop(() =>
                {
                    return GetAllPostOnAPage(artist.ArtistUrl + paginationMarker + i * POSTS_PER_PAGE);
                });

                Console.WriteLine($"Processing {artist.Name} - ({artist.ArtistUrl}) posts");
                ProcessPostHrefBatch(postHrefs, artist);
            }
        }

        /// <summary>
        /// The logic below is extracted into a seperate method because of the need
        /// to break out of a two loops.
        /// </summary>
        /// <param name="postHrefs"></param>
        /// <param name="artist"></param>
        /// <returns></returns>
        private void ProcessPostHrefBatch(List<string> postHrefs, Artist artist)
        {
            foreach (string postHref in postHrefs)
            {
                Console.Write($"Processing post {postHref}...");
                if (storageOps.SavePostIntoDatabase(postHref, artist))
                {
                    Console.WriteLine(" -- Post saved to database");
                }
                else
                {
                    Console.WriteLine("Post already exists in database");
                }
            }
        }

        private void GetImagesFromASinglePost(string choice, Post post)
        {
            HtmlWeb hw = new HtmlWeb();
            HtmlDocument doc = new HtmlDocument();
            doc = TryLoop(() =>
            {
                return hw.Load(post.Url());
            });

            if (doc.DocumentNode.SelectNodes("//div[contains(@class, 'post__files')]") != null)
            {
                foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//div[contains(@class, 'post__files')]"))
                {
                    var counter = 0;
                    // Get the value of the HREF attribute
                    foreach (HtmlNode url in div.SelectNodes("//a[contains(@class, 'fileThumb')]"))
                    {
                        Sleep();
                        string hrefValue = url.GetAttributeValue("href", string.Empty);
                        string extension = hrefValue.Split(".").Last();
                        if (extension.Equals("jpe"))
                        {
                            extension = "jpg";
                        }
                        var fileName = storageOps.ValidatePathName(post.Url().Split("post/")[1] + "_" + counter + "." + extension);

                        string postFolder = post.Artist.Name;

                        Media mediaInDb = storageOps.GetMediaInDatabase(post, hrefValue, fileName);

                        if (mediaInDb is null)
                        {
                            continue;
                        }

                        //// create a folder for each post as well
                        //if (choice.Equals("y"))
                        //{
                        //    var postName = GetPostName(doc);
                        //    postFolder = postFolder + "\\" + postName;
                        //}

                        // Make sure that the directory exists
                        System.IO.Directory.CreateDirectory(postFolder);

                        if (!storageOps.CheckIfFileExists(postFolder + "\\" + fileName))
                        {
                            Console.WriteLine($"Saving: {fileName}");
                            if (extension.Equals("gif"))
                            {
                                SaveGif(KEMONO_BASE_URL + mediaInDb.Href, postFolder + "\\" + fileName);
                            }
                            else SaveImage(KEMONO_BASE_URL + mediaInDb.Href, postFolder + "\\" + fileName);
                        }
                        else Console.WriteLine($"File exists, skipping: {fileName}");
                        counter += 1;
                        storageOps.MarkMediaExistsInDb(mediaInDb);
                    }
                }
            }
        }

        private void DownloadArtistMedia(Artist artist)
        {
            foreach (Post post in artist.ArtistPosts)
            {
                if (post.Processed)
                {
                    Console.WriteLine($"\n {post.KemonoId} post fully processed.");
                    continue;
                }
                Directory.CreateDirectory(artist.Name);
                string choice = "n";
                GetPostAttachments(choice, post);
                GetImagesFromASinglePost(choice, post);
                storageOps.MarkPostProcessedInDb(post);
            }
        }

        private string GetPostHtmlContentAsString(Post post)
        {
            HtmlWeb hw = new HtmlWeb();
            var doc = hw.Load(post.Url());
            return doc.Text;
        }

        //private void ProcessArtistUrlWithCocksies(string artistUrl)
        //{
        //    HttpClientHandler httpClientHandler = new HttpClientHandler();
        //    httpClientHandler.CookieContainer
        //    HttpClient httpClient = new HttpClient();
        //    httpClient.BaseAddress = new Uri("https://kemono.party/");
        //    httpClient.coo
        //    // get all posts
        //    HttpWebRequest  hw = new HtmlWebRequest();
        //    hw.UseCookies = true;
        //    hw.PreRequest += request =>
        //    {
        //        request.CookieContainer = _cookiesContainer;
        //        return true;
        //    };
        //}
        public static HtmlDocument LoadPageHtmlFromUrl(string artistUrl)
        {
            HtmlDocument doc = new HtmlDocument();
            HtmlWeb hw = new HtmlWeb();
            doc = hw.Load(artistUrl);
            return doc;
        }
        public int GetArtistsNumberOfPages(HtmlDocument doc)
        {
            var posts = doc.DocumentNode.SelectSingleNode("//small");
            if (posts == null)
            {
                return 1;
            }
            int numberOfPosts = int.Parse(posts.InnerHtml.Split("\n")[1].Split(" ").Last());
            return numberOfPosts;
        }
        public string GetArtistsName(HtmlDocument doc)
        {
            var artistNameator = doc.DocumentNode.SelectSingleNode("//*[@itemprop='name']");
            string artistName = artistNameator.GetDirectInnerText();
            return artistName;
        }
        /// <summary>
        /// Checks if the artists already exists in the database and
        /// returnts its instance. If it doesn't exists, creates is
        /// and returns it.
        /// </summary>
        /// <param name="artistName"></param>
        /// <param name="artistUrl"></param>
        /// <returns></returns>

        static List<string> GetAllPostOnAPage(string pageUrl)
        {
            HtmlWeb hw = new HtmlWeb();
            List<string> postHrefs = new List<string>();
            var doc = hw.Load(pageUrl);
            Program.logger.Info($"Processing page {pageUrl}");
            foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//article[contains(@class, 'post-card')]"))
            {
                // Get the values of the href attribute of each post on the page
                string postHref = div.GetAttributeValue("data-id", string.Empty);
                postHrefs.Add(postHref);
            }
            return postHrefs;
        }

        private void GetPostAttachments(string choice, Post post)
        {
            string postUrl = post.Url();
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
                    var fullUrl = KEMONO_BASE_URL + url;
                    var fileName = attachment.InnerText;
                    fileName = storageOps.ValidatePathName(postUrl.Split("post/")[1] + "_" + fileName.Split("\n")[1].TrimStart().Split("\n")[0]);

                    Media mediaInDb = storageOps.GetMediaInDatabase(post, url, fileName);

                    if (mediaInDb is null)
                    {
                        break;
                    }

                    if (!storageOps.CheckIfFileExists(mediaInDb.Path()))
                    {
                        Console.WriteLine("Downloading: " + fullUrl);
                        WebClient webClient = new WebClient();
                        Console.WriteLine($"Downloading attachment: {fileName}");

                        // Create post folder
                        //if (choice.Equals("y"))
                        //{
                        //    System.IO.Directory.CreateDirectory(media.Post.Artist.Name);
                        //}

                        TryLoopAction(() =>
                        {
                            webClient.DownloadFile(new Uri(fullUrl), mediaInDb.Post.Artist.Name + "\\" + fileName);
                        });
                        Console.WriteLine("Download done.");
                        webClient.Dispose();
                    }
                    else
                    {
                        Console.WriteLine($"File exists, skipping: {fileName}");
                    }
                    storageOps.MarkMediaExistsInDb(mediaInDb);
                }
            }
        }

        private void TryLoopAction(Action anyAction)
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
                    Console.Write(e.Message + "\n"
                        + e.StackTrace + "\n"
                        + e.InnerException);
                    System.Threading.Thread.Sleep(2000); // *
                }
            }
        }

        private void SaveGif(string gifUrl, string filePath)
        {
            Console.WriteLine("Downloading: " + gifUrl);
            WebClient webClient = new WebClient();
            Console.WriteLine($"Downloading attachment: {filePath}");
            string nullString = TryLoop(() =>
                {
                    webClient.DownloadFile(new Uri(gifUrl), filePath);
                    return String.Empty;
                }
            );
            webClient.Dispose();
        }

        private bool SaveImage(string imageUrl, string filePath)
        {
            WebClient client = new WebClient();

            Console.WriteLine("Downloading: " + imageUrl);
            TryLoop(() =>
                {
                    client.DownloadFile(new Uri(imageUrl), filePath);
                    return string.Empty;
                }
            );
            client.Dispose();
            return true;
        }

        static void Sleep(int length = 1)
        {
            Random rnd = new Random();
            var randInt = 0;
            if (length == 0)
            {
                randInt = rnd.Next(1354, 5987);
                Console.WriteLine($"Next post, slept for {randInt} miliseconds so as to not overburden the site.");
            }
            else
            {
                randInt = rnd.Next(585, 3576);
                Console.WriteLine($"Slept for {randInt} miliseconds so as to not overburden the site.");
            }

            Thread.Sleep(randInt);
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
    }
}
