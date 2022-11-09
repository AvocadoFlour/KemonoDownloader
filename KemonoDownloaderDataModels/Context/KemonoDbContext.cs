using KemonoDownloaderDataModels.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

public class KemonoDbContext : DbContext
{
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Media> Media { get; set; }
    public DbSet<Post> Posts { get; set; }
    public string DbPath { get; } = Environment.CurrentDirectory;
    public KemonoDbContext()
    {
        //var folder = Environment.SpecialFolder.LocalApplicationData;
        //var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(DbPath, "kemonoDownloader.db");
        this.Database.EnsureCreated();
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>()
            .HasAlternateKey(p => p.KemonoId);
        modelBuilder.Entity<Artist>()
            .HasAlternateKey(a => a.ArtistUrl);
        modelBuilder.Entity<Media>()
            .HasAlternateKey(a => a.Href);
    }
}