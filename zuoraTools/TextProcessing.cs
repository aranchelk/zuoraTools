using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace zuoraTools
{
    class TextProcessing
    {
        public static void VarDump(object o)
        {
            Console.WriteLine(Regex.Replace(JsonConvert.SerializeObject(o), @",", "\n"));
        }

        public static List<string> ParseCsvLine(string line)
        {
            List<string> retList = new List<string>();
            //http://stackoverflow.com/questions/3268622/regex-to-split-line-csv-file
            var regex = new Regex("(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");

            foreach (Match m in regex.Matches(line))
            {
                string cleanString = m.Value.Replace("\"\"", "\"");

                if (cleanString.EndsWith("\""))
                {
                    cleanString = cleanString.Substring(0, cleanString.Length - 1);
                }

                if (cleanString.StartsWith("\""))
                {
                    cleanString = cleanString.Substring(1, cleanString.Length - 1);
                }

                retList.Add(cleanString);
            }

            return retList;
        }
       
        public static Dictionary<int, string> ParseHeaders(string headers)
        {
            List<string> parsedInput = new List<string>(Regex.Split(headers, @"(\s?,\s?)"));
            Dictionary<int, string> output = new Dictionary<int, string>();
            int count = 0;

            foreach (string s in parsedInput.Where(s => !Regex.Match(s, @".*,.*", RegexOptions.Multiline).Success))
            {
                output.Add(count, s);
                count++;
            }

            return output;
        }

        public static Dictionary<string, string> ParseLine(string line, Dictionary<int, string> legend)
        {
            int i = 0;
            Dictionary<string, string> retOb = new Dictionary<string, string>();

            if (line.Contains("\""))
            {
                throw new Exception("Unable to handle records with double quotes.");
            }

            while (Regex.Match(line, @",").Success)
            {
                string record = Regex.Match(line, @"^(.*?,)").Value;
                line = Regex.Replace(line, @"^(.*?,)", "");

                record = Regex.Replace(record, @",", "");
                retOb.Add(legend[i], record);

                i++;
            }
            line = Regex.Replace(line, @",", "");
            retOb.Add(legend[i], line);

            return retOb;
        }
         
    }
}
