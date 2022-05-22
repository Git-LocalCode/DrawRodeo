namespace Draw.Rodeo.Server.Services
{
    public class WordManager
    {
        private Random _RandomGen;
        private List<string> _Words;

        public WordManager()
        {
            _RandomGen = new Random();
            string words = Properties.Resources.Words;
            _Words = words.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

        public async Task<List<string>> GetWords()
        {
            List<string> words = new();

            while(words.Count < 3)
            {
                words.Add(_Words[_RandomGen.Next(_Words.Count)].ToUpper());
                words = words.Distinct().ToList();
            }

            return words;
        }

#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

    }
}
