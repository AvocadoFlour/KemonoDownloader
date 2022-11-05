using System.ComponentModel.DataAnnotations;

namespace KemonoDownloaderDataModels.Models
{
    public class Artist
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string ArtistUrl { get; set; }
        public string PathOnDisk { get; set; }
        public DateTime DateAdded { get; set; }
        public List<Post> ArtistPosts { get; set; }
    }
}