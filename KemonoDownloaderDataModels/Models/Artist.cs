using System.ComponentModel.DataAnnotations;

namespace KemonoDownloaderDataModels.Models
{
    public class Artist
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ArtistUrl> ArtistUrls { get; set; }
    }
}