using HtmlAgilityPack;
using KemonoDownloaderDataModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KemonoDownloader.Logic
{
    public class MediaStorageOperations
    {
        private KemonoDbContext dbContext = new KemonoDbContext();

        public MediaStorageOperations()
        {
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
            string artistUrlReversedString = new string(artistUrlReversed);
            int charactersToSkipInUrl = 0;
            charactersToSkipInUrl += artistUrlReversedString.IndexOf("/");
            artistUrlReversedString = new string(artistUrlReversedString.Skip(artistUrlReversedString.IndexOf("/")).ToArray());
            charactersToSkipInUrl += artistUrlReversedString.IndexOf("/");
            artistUrlReversedString = new string(artistUrlReversedString.Skip(artistUrlReversedString.IndexOf("/")).ToArray());
            charactersToSkipInUrl += artistUrlReversedString.IndexOf("/");
            //string rawArtistKemonoHref = new string(artist.ArtistUrl.Skip(artist.ArtistUrl.Count() - charactersToSkipInUrl).ToArray());
            string userDirectoryPath = ValidatePathName(artist.Name + "(" + artist.ArtistUrl.Remove(0, DownloadingArt.KEMONO_BASE_URL.Length) + ")");
            userDirectoryPath = userDirectoryPath.Replace("user", "-");
            return userDirectoryPath;
        }

        public string ValidatePathName(string input, string replacement = "")
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

        public bool CheckIfFileExists(string fileName)
        {
            var workingDirectory = Environment.CurrentDirectory;
            var file = $"{workingDirectory}\\{fileName}";
            return File.Exists(file);
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
        /// 
        public bool SavePostIntoDatabase(string postHref, Artist? artist = null)
        {
            int postKemonoId = int.Parse(postHref);
            Post post = dbContext.Posts.FirstOrDefault(x => x.KemonoId == postKemonoId);
            if (post == null)
            {
                post = new Post();
                post.Artist = artist;
                post.KemonoId = postKemonoId;
                // Makes the database 99+% larger
                //post.HtmlContent = GetPostHtmlContentAsString(post);
                dbContext.Posts.Add(post);
                dbContext.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
        }

        public Media GetMediaInDatabase(Post post, string url, string fileName)
        {
            Media media = dbContext.Media.FirstOrDefault(x => x.Href.Equals(url));

            if (media == null)
            {
                media = new Media();
                media.Post = post;
                media.FileName = fileName;
                media.Href = url;
                media.DateAdded = DateTime.Now;
                dbContext.Media.Add(media);
                dbContext.SaveChanges();
                return media;
            }
            else if (media.Exists)
            {
                Console.WriteLine($"{media.Href} already processed.");
                return null;
            }
            else return media;
        }

        public Artist GetArtistDatabaseInstance(string artistName, string artistUrl)
        {
            Artist artistsInDb = artistsInDb = dbContext.Artists.Include(a => a.ArtistPosts).FirstOrDefault(x => x.ArtistUrl == artistUrl);

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

        public void MarkMediaExistsInDb(Media media)
        {
            media.Exists = true;
            dbContext.SaveChanges();
        }

        public void MarkPostProcessedInDb(Post post)
        {
            post.Processed = true;
            dbContext.SaveChanges();
        }
    }
}
