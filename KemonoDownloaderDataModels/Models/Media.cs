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
        [Key]
        public int Id { get; set; }
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
        public DateTime DateAdded { get; set; }
    }
}
