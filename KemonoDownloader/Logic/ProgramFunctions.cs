using static KemonoDownloader.Program;

namespace KemonoDownloader.Logic
{
    public static class ProgramFunctions
    {
        public static List<string> ReadArtistUrlsFromFile()
        {
            try
            {
                return File.ReadAllText("artistUrls.txt").Split(" ", StringSplitOptions.TrimEntries).ToList();
            }
            catch (FileNotFoundException e)
            {
                logger.Error("The file with artist URLs does not exists. You need to make sure it does before you use this option.", e.Message);
                Environment.Exit(0);
                return null;
            }
        }

        public static string ReadUserInput (string message, List<string> acceptableInputs)
        {
            do
            {
                Console.WriteLine(message);
                string input = Console.ReadLine();
                if (acceptableInputs.Contains(input))
                    return input;
            } while (true);
        }

        public static void SaveArtistUrlsToFile(string artistUrls)
        {
            using (StreamWriter sw = new StreamWriter("artistUrls.txt"))
            {
                sw.Write(artistUrls);
            }
        }

    }
}
