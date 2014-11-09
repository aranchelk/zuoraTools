using System;
using System.Collections.Generic;
using zuoraTools.BusinessLogic;
using System.Diagnostics;

namespace zuoraTools
{
	class Program
	{
		public static void Main(string[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			ConfigCliFactory confFactory = new ConfigCliFactory(args);

			//  try
			//{
			Config conf = confFactory.Make();
			Logv("\nData tools started, running in mode: " + conf.Mode + "\n", conf.Verbosity);

			var actionDict = new Dictionary<string, Action>()
			{
				{ "ids-from-sql-query", () => AugmentFile.Process(conf)},
				{ "sql-query", () =>  SqlQuery.Process(conf)},
				{"zquery", () => QueryZuora.Process(conf)},
				{"zxquery", () => ExportQueryZuora.Process(conf)},
				{"update-zobjects", () => ModifyZuoraData.Process(conf)},
				{"touch-zobjects", () => ModifyZuoraData.Process(conf)},
				{"delete-zobjects", () => ModifyZuoraData.Process(conf)},
				{"create-zobjects", () => ModifyZuoraData.Process(conf)},
				{"describe-zobject", () => DescribeZuoraObject.Process(conf)},
				{"list-zobjects", ()  => DescribeZuoraObject.Process()},
				{"help", confFactory.ShowHelp},
			};

			actionDict[conf.Mode]();

			stopwatch.Stop();
			Logv("Processed in " + stopwatch.Elapsed + " seconds", conf.Verbosity);
			/*
            }
            catch (Exception ex)
            {
                confFactory.ShowHelp();

                http://stackoverflow.com/questions/20991233/get-file-name-and-line-number-from-exception
                StackTrace trace = new StackTrace(ex, true);
                string fileName = trace.GetFrame(0).GetFileName();
                int lineNo = trace.GetFrame(0).GetFileLineNumber();

                Console.Error.WriteLine("\n*** Error: {0} ***", ex.Message);
                Console.Error.WriteLine("*** Line: {1}, File: {0}, ***\n", fileName, lineNo);
            }
             */
		}

		public static void Logv(string str, int verbosity)
		{
			if (verbosity > 0)
			{
				Console.Write(str);
			}
		}
	}
}

