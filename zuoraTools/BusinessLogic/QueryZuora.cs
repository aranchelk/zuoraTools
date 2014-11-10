using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using zuoraTools.ZuoraConnections;

namespace zuoraTools.BusinessLogic
{
    class QueryZuora
    {
        public static void Process(Config conf)
        {
            Dictionary<int, string> columnLegend = new Dictionary<int, string>();

            IZConnection zConn = ZConnectionFactory.Make(conf);

            List<string> selectFields = new List<string>();
            Match m = Regex.Match(conf.ZqueryString, @"(?i)select \* (?i)from");

            if (m.Success)
            {
                throw new Exception("star not implemented");
            }
            else
            {

                m = Regex.Match(conf.ZqueryString, @"\b([a-zA-z0-9*]+)\b");

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

            //Console.WriteLine("zquery:" + conf.zqueryString);
            zObject[] zList = zConn.Query(conf.ZqueryString);

            //Console.WriteLine("zlist count:" + zList.Count().ToString());
            //Console.WriteLine(Regex.Replace(JsonConvert.SerializeObject(zList), @",", "\n"));

            if (zList.Count() == 1 && zList[0] == null)             
            {
                Console.WriteLine("No records returned from query.");
            }
            else
            {
                if (writeToFile)
                {
                    outputFile = new System.IO.StreamWriter(conf.OutputFile);
                    outputFile.WriteLine(outputHeader);
                }
                else
                {
                    Console.WriteLine(outputHeader);
                }

                foreach (zObject zOb in zList)
                {
                    if (writeToFile)
                    {
                        outputFile.WriteLine(ZObjectHelper.ToCsvRecord(zOb, selectFields.ToArray()));
                        outputFile.Flush();
                    }
                    else
                    {
                        Console.WriteLine(ZObjectHelper.ToCsvRecord(zOb, selectFields.ToArray()));
                    }
                }
            }
            
        }
    }
}
