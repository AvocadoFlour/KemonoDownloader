using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KemonoDownloaderDataModels.Models
{
    public class Post
    {
        public int Id { get; set; }
        public int KemonoId { get; set; }
        public string Url { get; set; }
        public string? HtmlContent { get; set; }
        public Artist Artist { get; set; }
        public Post(int id, int kemonoId, string url, string? htmlContent, Artist artist)
        {
            Id = id;
            KemonoId = kemonoId;
            Url = url ?? throw new ArgumentNullException(nameof(url));
            HtmlContent = htmlContent;
            Artist = artist ?? throw new ArgumentNullException(nameof(artist));
        }
    }
}
