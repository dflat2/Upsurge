using System.IO;

namespace MCGalaxy
{
    public class RoundID
    {
        private static string path = "RoundID.txt";
        public static void IncrementRoundID()
        {
            UpdateRoundID(path);
        }

        public static uint GetRoundID()
        {
            ConditionalCreateFile(path);
            string IDString = File.ReadAllText(path);
            uint ID = uint.Parse(IDString);
            return ID;
        }

        private static void ConditionalCreateFile(string path)
        {
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                string[] input = { "1" };
                File.WriteAllLines(path, input);
            }
        }

        private static void UpdateRoundID(string path)
        {
            uint oldID = GetRoundID();
            uint newID = oldID + 1;
            File.WriteAllText(path, newID.ToString());
        }
    }
}