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
        public static string wrongInputMessage = "B-baka! That's the wrong input! " + blushFace;

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

        /// <summary>
        /// Define input parameters (acceptableInputs), 
        /// the clarification of actions associated with each input as a method (instruction) 
        /// and the feedback message for when the inputted input was invalid.
        /// </summary>
        /// <param name="unexpectedInputMessage">Invalid input feedback message</param>
        /// <param name="acceptableInputs">Valid inputs</param>
        /// <param name="instruction">Explanations for what to input</param>
        /// <returns></returns>
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
        public static string ReadUserInput(string unexpectedInputMessage, List<string> options)
        {
            do
            {
                foreach (string option in options)
                {
                    Console.WriteLine($"({options.IndexOf(option) + 1}) {option}");
                }
                string input = Console.ReadLine();
                int intInput = 0;
                bool inputCheck = int.TryParse(input, out intInput);
                if (inputCheck)
                {
                    intInput -= 1;
                    if (intInput > -1 && options.Count() >= intInput)
                    {
                        return input;
                    }
                }
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

    }
}
