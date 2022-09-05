using KemonoDownloaderDataModels.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KemonoDownloaderDataModels.Models
{
    public class ArtistUrl
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public ArtistUrlTypes UrlType {get; set;}
        public Artist Artist { get; set; }
    }
}
