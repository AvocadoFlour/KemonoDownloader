using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KemonoDownloaderDataModels.Models
{
    public class Media
    {
        public int Id { get; set; }

        [Key]
        public int KemonoId { get; set; }
        public string FileName { get; set; }

        /// <summary>
        /// Where the media file is located, e.g.
        /// c:/KemonoDownloader/artist/post/mediaFile.jpg 
        /// (the above is not the correct path format)
        /// </summarys
        public string Path { get; set; }
        public bool Exists { get; set; }

        /// <summary>
        /// Parent post, the post inside of which this media was found
        /// </summary>
        public Post Post { get; set; }
        public Media(int id, int kemonoId, string fileName, string path, bool exists, Post post)
        {
            Id = id;
            KemonoId = kemonoId;
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Exists = exists;
            Post = post ?? throw new ArgumentNullException(nameof(post));
        }
    }
}
