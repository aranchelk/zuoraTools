using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using zuoraTools.DataEnumerators;
using zuoraTools.ZuoraConnections;

namespace zuoraTools.BusinessLogic
{
    class ExportQueryZuora
    {
        public static void Process(Config conf)
        {
            //Todo: check for necessary config values and throw exceptions if they are not present.
            IZConnection zConn = ZConnectionFactory.Make(conf);
            string zqueryString = conf.ZqueryString;

            List<string> selectFields = new List<string>();
            Match m = Regex.Match(zqueryString, @"(?i)select \* (?i)from");

            if (m.Success)
            {
                //Console.WriteLine("select " + string.Join(", ", ZObjectHelper.describeZobject()));
                string zObjectType = Regex.Match(zqueryString, @"from[\s]+([\S]*)").Groups[1].Value;
                string suffix = Regex.Match(zqueryString, @"from\s[\S]+\s(.*)$").Groups[1].Value;
                string generatedFields = String.Join(", ", ZObjectHelper.DescribeZobject(zObjectType));

                zqueryString = "select " + generatedFields + " from " + zObjectType + " " + suffix;
            }
            else
            {
                //Populate select fields for output
                m = Regex.Match(zqueryString, @"\b([a-zA-z0-9*]+)\b");

                while (m.Success && m.Value.ToLower() != "from")
                {
                    if (m.Value.ToLower() != "select")
                    {
                        selectFields.Add(m.Value);
                    }
                    m = m.NextMatch();
                }
            }

            string outputHeader = String.Join(",", selectFields);

            Boolean writeToFile = !conf.DryRun && !String.IsNullOrEmpty(conf.OutputFile);

            System.IO.StreamWriter outputFile = null;

            //Console.WriteLine(zqueryString);
            Stream exportDataStream = zConn.ExportQuery(zqueryString);
            IDataEnumerator le = DataEnumeratorFactory.Make(exportDataStream, offset: conf.Offset, limit: conf.Limit);

            String csvHeaders = le.Next();
            Dictionary<int, string> columnLegend = TextProcessing.ParseHeaders(csvHeaders);
            Console.WriteLine(csvHeaders);

            foreach (string s in le.Gen())
            {
                Console.WriteLine(s);
            }
        }
    }
}
