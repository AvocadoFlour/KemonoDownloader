using static KemonoDownloader.Program;

namespace KemonoDownloader.Logic
{
    public static class PostManipulation
    {
        public static List<string> ReadArtistUrlsFromFile()
        {
            try
            {
                return File.ReadAllText("artistUrls.txt").Split(" ", StringSplitOptions.TrimEntries).ToList();
            }
            catch (FileNotFoundException e)
            {
                Logger.Error("The file with artist URLs does not exists. You need to make sure it does before you use this option.", e.Message);
                Environment.Exit(0);
                return null;
            }
        }
    }
}
