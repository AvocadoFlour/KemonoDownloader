using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KemonoDownloaderDataModels.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// This is an alternate key; For each Post the KemonoId is unique
        /// </summary>
        public int KemonoId { get; set; }
        public string? HtmlContent { get; set; }
        [Required]
        public Artist Artist { get; set; }
        public List<Media> PostMedia { get; set; }
        public bool Processed { get; set; } = false;
        /// <summary>
        /// This has to be a method and not a property
        /// so as not to cause an entity framework error
        /// </summary>
        /// <returns></returns>
        public string Url()
        {
            return Artist.ArtistUrl + "/post/" + KemonoId;
        }
    }
}
