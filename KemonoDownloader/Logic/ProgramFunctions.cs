using Pastel;
using System;
using System.Drawing;
using System.Linq.Expressions;
using static KemonoDownloader.Program;

namespace KemonoDownloader.Logic
{
    public static class ProgramFunctions
    {
        public static string blushFace = "(".Pastel("#ffdbac") + "//".Pastel(Color.OrangeRed) + ">".Pastel(Color.DeepSkyBlue) + "/".Pastel(Color.OrangeRed) + "_".Pastel("#c9616b") + "/".Pastel(Color.OrangeRed) + "<".Pastel(Color.DeepSkyBlue) + "//".Pastel(Color.OrangeRed) + ")".Pastel("#ffdbac");
        public static string wrongInput = "B-baka! That's the wrong input! " + blushFace;

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

        public static string ReadUserInput(string unexpectedInputMessage, List<string> acceptableInputs, Action? instruction = null)
        {
            do
            {
                if (instruction != null)
                {
                    instruction();
                }
                string input = Console.ReadLine();
                if (acceptableInputs.Contains(input))
                    return input;
                Console.WriteLine(unexpectedInputMessage);
            } while (true);
        }

        public static void SaveArtistUrlsToFile(string artistUrls)
        {
            using (StreamWriter sw = new StreamWriter("artistUrls.txt"))
            {
                sw.Write(artistUrls);
            }
        }

        public static void ShowMenu()
        {
            Console.WriteLine("(1) Read artists from the file");
            Console.WriteLine("(2) Input artist urls");
            Console.WriteLine("(3) Download favourited artists");
        }

    }
}
