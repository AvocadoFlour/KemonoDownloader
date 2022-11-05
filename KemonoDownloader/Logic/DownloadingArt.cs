using HtmlAgilityPack;
using System.Net;
using KemonoDownloaderDataModels.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace KemonoDownloader.Logic
{
    internal class DownloadingArt
    {
        private static KemonoDbContext dbContext = new KemonoDbContext();
        private readonly string KEMONO_BASE_URL = "https://kemono.party/";
        private readonly int POSTS_PER_PAGE = 50;
        private readonly string paginationMarker = "?o=";

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
            Artist artist = GetArtistDatabaseInstance(artistName, artistUrl);

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
                foreach (string postHref in postHrefs)
                {
                    Console.Write($"Processing post {postHref}...");
                    if (SavePostIntoDatabase(postHref, artist))
                    {
                        Console.WriteLine(" -- Post saved to database");
                    }
                    else
                    {
                        Console.WriteLine("Reached post that already exists in database");
                    }
                }
            }
        }

        private void DownloadArtistMedia(Artist artist)
        {
            foreach (Post post in artist.ArtistPosts)
            {
                string choice = "n";
                GetPostAttachments(choice, post);
                GetImagesFromASinglePost(choice, post);
            } 
        }

        /// <summary>
        /// When an artist doesn't have the path value in the database
        /// this method fills it (e.g. when an artist has only just 
        /// been created)
        /// </summary>
        /// <param name="artist"></param>
        private string CreateArtistPath(Artist artist)
        {
            /// from -> https://kemono.party/fanbox/user/9016
            /// to -> fanbox/user/9016
            char[] artistUrlReversed = artist.ArtistUrl.Reverse().ToArray();
            string artistUrlReversedString = new string (artistUrlReversed);
            int charactersToSkipInUrl = 0;
            charactersToSkipInUrl += artistUrlReversedString.IndexOf("/");
            artistUrlReversedString = new string(artistUrlReversedString.Skip(artistUrlReversedString.IndexOf("/")).ToArray());
            charactersToSkipInUrl += artistUrlReversedString.IndexOf("/");
            artistUrlReversedString = new string(artistUrlReversedString.Skip(artistUrlReversedString.IndexOf("/")).ToArray());
            charactersToSkipInUrl += artistUrlReversedString.IndexOf("/");
            string rawArtistKemonoHref = new string(artist.ArtistUrl.Skip(artist.ArtistUrl.Count() - charactersToSkipInUrl).ToArray());
            string userDirectoryPath = ValidatePathName(artist.Name + "(" + artist.ArtistUrl.Remove(0, KEMONO_BASE_URL.Length) + ")");
            userDirectoryPath = userDirectoryPath.Replace("user", "-");
            return userDirectoryPath;
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

        /// <summary>
        /// Get Post from database.
        /// If the post doesn't exists must be provided with an artist
        /// as well because it needs to create the post and for that it needs
        /// the artis to assotiate it with.
        /// </summary>
        /// <param name="postHref"></param>
        /// <param name="artist"></param>
        /// <returns>Returns false if post already exists in database.</returns>
        private bool SavePostIntoDatabase(string postHref, Artist? artist = null)
        {
            int postKemonoId = int.Parse(postHref);
            Post post = dbContext.Posts.FirstOrDefault(x => x.KemonoId == postKemonoId);
            if (post == null) 
            {
                post = new Post();
                post.Artist = artist;
                post.KemonoId = postKemonoId;
                dbContext.Posts.Add(post);
                dbContext.SaveChanges();
                return true;
            }
            else 
            {
                return false;
            }
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
        public Artist GetArtistDatabaseInstance(string artistName, string artistUrl)
        {
            Artist artistsInDb = null;
            if (dbContext.Artists.Count(x => x.ArtistUrl.Equals(artistUrl)) > 0)
            {
                artistsInDb = dbContext.Artists.FirstOrDefault(x => x.ArtistUrl == artistUrl);
            }

            if (artistsInDb == null)
            {
                artistsInDb = new Artist();
                artistsInDb.Name = artistName;
                artistsInDb.ArtistUrl = artistUrl;
                artistsInDb.DateAdded = DateTime.Now;
                artistsInDb.PathOnDisk = CreateArtistPath(artistsInDb);
                dbContext.Artists.Add(artistsInDb);
                dbContext.SaveChanges();

            }
            return artistsInDb;
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
            if (post.Processed)
            {
                Console.WriteLine($"Post {post.Id} already processed.");
                return;
            }

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

        private void GetImagesFromASinglePost(string choice, Post post)
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
        private bool CheckIfFileExists(string fileName)
        {
            var workingDirectory = Environment.CurrentDirectory;
            var file = $"{workingDirectory}\\{fileName}";
            return File.Exists(file);
        }
    }
}
