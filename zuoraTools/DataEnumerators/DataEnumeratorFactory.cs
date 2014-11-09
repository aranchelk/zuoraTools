using System.IO;

namespace zuoraTools.DataEnumerators
{
    class DataEnumeratorFactory
    {
        public static IDataEnumerator Make(Stream inputStream, int offset = 0, int limit = -1)
        {
            LineEnum le = new LineEnum(inputStream);

            if (offset == 0 && limit == -1)
            {
                return le;
            }

            return new LoopLogicEnum(le, offset, limit);
        }

        public static IDataEnumerator MakeZObjectGen(Stream inputStream, Config conf)
        {
            IDataEnumerator lineEnum = Make(inputStream, conf.Offset, conf.Limit);
            CsvParserEnum csvDict = new CsvParserEnum(lineEnum);
            ZObjectEnum zGen = new ZObjectEnum(innerGen: csvDict, zObjectType: conf.ZObjectType, additionalProperties: conf.PropertyValues);

            return zGen;
        }

 
    }
}

        //Old zip logic, needs to go somewhere
        /*
public static System.Collections.IEnumerable zip(string sourceFile)
{
    if (String.IsNullOrEmpty(sourceFile))
    {
        throw new System.ArgumentException("Source file cannot be empty", "sourceFile");
    }
    else
    {
        //http://dotnetzip.codeplex.com/wikipage?title=CS-Examples
        using (ZipFile zip = ZipFile.Read(sourceFile))
        {
            if (zip.Entries.Count != 1)
            {
                throw new System.IO.FileLoadException("Selected zip archives must have exactly one file inside.");
            }

            ZipEntry entry = zip[zip.Entries.First().FileName];
            using (var stream = entry.OpenReader())
            {
                var buffer = new byte[bufferSize]; //64MB chunks
                int n;
                List<string> myList = new List<string>();
                string line = "";

                while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    line = line + System.Text.Encoding.UTF8.GetString(buffer);

                    if (Regex.Match(line, @"(\r|\n|\r\n)", RegexOptions.Multiline).Success || buffer.Length == 0)
                    {
                        myList = new List<string>(Regex.Split(line, "(\r\n|\n|\r)"));
                        line = "";

                        if (myList.Last() == "")
                        {
                            myList.RemoveAt(myList.Count-1);
                        }

                        if(!Regex.Match(myList.Last(), @".*(\r\n|\r|\n)", RegexOptions.Multiline).Success && buffer.Length > 0)
                        {
                            line = myList.Last();
                            myList.RemoveAt(myList.Count - 1);
                        }

                        foreach (string s in myList)
                        {
                            if (!Regex.Match(s, @"^\s*$", RegexOptions.Multiline).Success)
                            {
                                yield return s.Trim();
                            }
                        }
                    }

                }
                yield break;
            }
        }

    }
}*/

