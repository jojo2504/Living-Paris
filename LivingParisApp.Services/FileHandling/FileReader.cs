namespace LivingParisApp.Services.FileHandling {
    public class FileReader {
        public string FilePath { get; private set; }
        public FileReader(string filePath) {
            FilePath = filePath;
        }

        public IEnumerable<string> ReadLines() {
            using (StreamReader sr = new StreamReader(FilePath)) {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    yield return line;
                }
            }
        }
    }
}