using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using zuoraTools.DataEnumerators;

namespace zuoraTools.BusinessLogic
{
    class AugmentFile
    {
        public static void Process(Config conf)
        {
            conf.RequiredProperty("OutputFile", "SourceFile", "SourceFile", "SqlQueryString", "WhereColumn", "SqlUsername", "SqlPassword", "SqlDatabase", "SqlServer");

            FileStream fs = new FileStream(conf.SourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            IDataEnumerator sourceFile = DataEnumeratorFactory.Make(fs, offset: conf.Offset, limit: conf.Limit);

            DbHelper dbh = new DbHelper(conf.SqlServer, conf.SqlDatabase, conf.SqlUsername, conf.SqlPassword);

            string sourceHeader = sourceFile.Next();

            string outputHeader = Regex.Split(conf.SqlQueryString, @"\s").GetValue(1).ToString() + ", " + sourceHeader;
            Console.WriteLine(outputHeader);

            Dictionary<int, string> columnLegend = TextProcessing.ParseHeaders(sourceHeader);

            System.IO.StreamWriter outputFile = null;

            if (!conf.DryRun)
            {
                outputFile = new System.IO.StreamWriter(conf.OutputFile);
                outputFile.WriteLine(outputHeader);
            }

            foreach (string s in sourceFile.Gen())
            {

                Dictionary<string, string> lineData = TextProcessing.ParseLine(s, columnLegend);
                string query = Regex.Replace(conf.SqlQueryString, @"{}", lineData[conf.WhereColumn]);
                
                string output = dbh.QuerySingleValue(query) + ", " + s;
                output = output.Trim();
                if (!conf.DryRun)
                {
                    Console.WriteLine(output);
                    outputFile.WriteLine(output);
                    outputFile.Flush();
                }
                else
                {
                    Console.WriteLine(query);
                    Console.WriteLine(output);
                }
            }
            //Post processing loop

            Console.Write("\nUtility complete, processed {0} records.\n", sourceFile.LoopIterations);
        }
    }
}
