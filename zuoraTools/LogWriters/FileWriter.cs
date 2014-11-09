using System.IO;

namespace zuoraTools.LogWriters
{
    class FileWriter : ScreenWriter
    {
        private StreamWriter _outputFile;

        public FileWriter(string fileLocation)
        {
            _outputFile = new StreamWriter(fileLocation);
        }

        public override void Write(string line)
        {
            _outputFile.WriteLine(line);
            _outputFile.Flush();
        }
    }
}
