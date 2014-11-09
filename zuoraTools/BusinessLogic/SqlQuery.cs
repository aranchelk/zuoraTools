using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using zuoraTools;

namespace zuoraTools.BusinessLogic
{
    class SqlQuery
    {
        public static void Process(Config conf)
        {
            if (conf.SqlUsername == null || conf.SqlPassword == null || conf.SqlDatabase == null || conf.SqlServer == null) throw new Exception("SQL login information must be given (probably via ini file).");
            if (conf.SqlQueryString == null) throw new Exception("An sql query string is required for this action.");

            Boolean writeToFile = !conf.DryRun && !String.IsNullOrEmpty(conf.OutputFile);

            DbHelper dbh = new DbHelper(conf.SqlServer, conf.SqlDatabase, conf.SqlUsername, conf.SqlPassword);

            SqlCommand command = dbh.Conn.CreateCommand();
            command.CommandText = conf.SqlQueryString;
            command.CommandType = CommandType.Text;

            SqlDataReader reader = command.ExecuteReader();

            List<string> fieldNames = new List<string>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                fieldNames.Add(reader.GetName(i));
            }

            string outputHeader = String.Join(",", fieldNames);

            System.IO.StreamWriter outputFile = null;

            if (writeToFile)
            {
                outputFile = new System.IO.StreamWriter(conf.OutputFile);
                outputFile.WriteLine(outputHeader);
            }
            else
            {
                Console.WriteLine(outputHeader);
            }

            int loopIterations = 0;
            while (reader.Read())
            {
                loopIterations++;

                List<string> record = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string r = reader.GetValue(i).ToString();
                    if (r.Contains("\""))
                    {
                        throw new Exception("Unable to handle double quotes in data.");
                    }
                    if (r.Contains(","))
                    {
                        r = "\"" + r + "\"";
                    }
                    record.Add(r);
                }

                if (writeToFile)
                {
                    outputFile.WriteLine(String.Join(",", record));
                    outputFile.Flush();
                }
                else
                {
                    Console.WriteLine(String.Join(",", record));
                }
            }

            Console.Write("\nUtility complete, processed " + Convert.ToString(loopIterations) + " records.\n");
        }
    }
}
