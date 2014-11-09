using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace zuoraTools.DataEnumerators
{
    // Decorator class to parse csv data from LineEnum, returns a dictionary with header fileds as keys and row data as values
    class CsvParserEnum : IDataEnumerator
    {
        public int LoopIterations { get; set; }
        public List<string> Keys { get; set; }
        private IDataEnumerator _innerGen;

        public CsvParserEnum (IDataEnumerator innerGen){
            _innerGen = innerGen;
            String csvHeaders = _innerGen.Next();
            Keys = TextProcessing.ParseCsvLine(csvHeaders);
        }

        public IEnumerable Gen()
        {
            foreach (string line in _innerGen.Gen())
            {
                List<string> lineData = TextProcessing.ParseCsvLine(line);
                Dictionary<string, string> returnDict = Keys.Zip(lineData, (key, val) => new { key, val }).ToDictionary(x => x.key, x => x.val);

                yield return returnDict;

            }
            yield break;
        }

        public string Next()
        {
            throw new Exception("This method was not designed to return results as string.");
        }


    }
}
