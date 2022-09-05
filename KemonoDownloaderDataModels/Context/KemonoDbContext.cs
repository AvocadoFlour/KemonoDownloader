using KemonoDownloaderDataModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

public class KemonoDbContext : DbContext
{
    public DbSet<Artist> Artists { get; set; }
    public DbSet<ArtistUrl> ArtistUrls { get; set; }
    public DbSet<Media> Media { get; set; }
    public DbSet<Post> Posts { get; set; }
    public string DbPath { get; }
    public KemonoDbContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "kemonoDownloader.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

}