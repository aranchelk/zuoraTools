using System;
using System.IO;
using NDesk.Options;
using IniParser;

namespace zuoraTools
{
    class ConfigCliFactory
    {
        private OptionSet _optSet;
        private string[] _args;
        private string _iniFilePath;
        private IniParser.Model.KeyDataCollection _iniData;

        public ConfigCliFactory(string[] args)
        {
            //Need to populate optset to generate help file.
            _args = args;
            PopulateOptSet(new Config());
        }

        public Config Make()
        {
            var conf = new Config();

            conf = PopulateOptSet(conf);
            //Set defaults
            conf.LogLocation = GetUserHomeDir();
            conf.LogPrefix = GetBigEndianTimeString();
            this._iniFilePath = GetDefaultConfigPath();

            //Apply commandline arguments
            var invalidArgs = _optSet.Parse(_args);
            if (invalidArgs.Count != 0)
            {
                throw new Exception("One or more unsupported arguments: " + string.Join(", ", invalidArgs.ToArray()));          
            }

            //Apply iniFile
            PopulateIniData();
            if(this._iniData != null) conf = ApplyIniData(conf);


            return conf;
        }

        private Config PopulateOptSet(Config conf)
        {
            this._optSet = new OptionSet() {

                //Generic config options
                { "s|source-file=", "If data is being pulled from a file, specify the file's location.",
                  v => conf.SourceFile = v },
                { "o|output-file=",  "location to write file, specify the file's location.",
                   v => conf.OutputFile = v },

                { "limit=", "Limit, only run the first n records",
                   v => conf.Limit = Convert.ToInt32(v) },
                { "offset=", "Offset, skip the first n records",
                   v => conf.Offset = Convert.ToInt32(v) },
                { "concurrency=", "update zobjects can run in parallel threads.",
                   v => conf.Concurrency = Convert.ToInt32(v) },
                { "i|ini=", "ini file for personalize settings.",
                   v => this._iniFilePath = v},

                { "log-location=", "A path to directory where logs will be stored.",
                   v => conf.LogLocation = v },
                { "log-prefix=", "A string that will prepend log file names",
                   v => conf.LogPrefix = v },

                { "v", "increase debug message verbosity",
                  v => { if (v != null) conf.Verbosity++; } },

                { "n|dry-run",  "Don't make callouts or save to file.", 
                  v => conf.DryRun = v != null },
                { "h|help",  "show this message and exit", 
                   v => { if (v != null){
                       conf.Mode = "help";}}},

                //Methods that interact with Zuora
                { "zcreate",  ".",
                   v => { if (v != null) conf.Mode="create-zobjects"; } },
                { "zxquery=",  "Query data from Zuora using export syntax.",
                   v => { if (v != null){ 
                       conf.ZqueryString = v;
                       conf.Mode = "zxquery";
                   }} },
                { "zquery=",  "Perform a query against the Zuora API, needs a lot of work.",
                   v => { if (v != null){ 
                       conf.ZqueryString = v;
                       conf.Mode = "zquery";
                   }} },
                { "zupdate",  "Pull data from source file, update data on server.",
                   v => { if (v != null){
                       conf.Mode = "update-zobjects";
                   }} },
                { "ztouch",  "Set updated date to now on zObjects, but don't modify.",
                   v => { if (v != null) conf.Mode="touch-zobjects"; } },
                { "zdelete",  "Delete zobjects.",
                   v => { if (v != null) conf.Mode="delete-zobjects"; } },
                { "zdescribe=",  "List all fields for specified zobject type.",
                   v => { if (v != null) conf.Mode="describe-zobject"; conf.ZObjectType = v;} },
                { "zobjects",  "List all availabel zobject types.",
                   v => { if (v != null) conf.Mode="list-zobjects"; } },

                { "t|zobject-type=", "Zuora object type, necessary for any operation that generates zObjects",
                   v => conf.ZObjectType = v },

                //SQL methods
                { "sql-query=",  "Perform a sql-query, format as CSV, if outputfile is set, write to ouput.",
                   v => { if (v != null){ 
                       conf.SqlQueryString = v;
                       conf.Mode = "sql-query";
                   }} },
                { "field-from-sql-query=",  "Peform a sql query, merge data with source-file and write to output-file. The query should only select one field and end with 'where <something> ='{}' The braces will be populated with --where-column",
                   v => { if (v != null){ 
                       conf.SqlQueryString = v;
                       conf.Mode = "ids-from-sql-query";
                   }} },
                { "where-column=",  "Column name in source column used for where clause in sql queries.",
                   v => conf.WhereColumn = v },
                { "property-value=", "Comma separated key value, that can be applied to a zObject, e.g. to change account batch --property-value 'batch=Batch1'. It can be used multiple times and overrides columns in source files.",
                   v => conf.PropertyValues.Add(v.Split(new Char[] { '=' })[0], v.Split(new Char[] { '=' })[1])
                },

            };

            return conf;
        }

        public void PopulateIniData()
        {
            var parser = new FileIniDataParser();

            try
			{
				using (StreamReader sr = new StreamReader(_iniFilePath))
				{
					//string line;
					//while ((line = sr.ReadLine()) != null) 
					//{
					//	Console.WriteLine(line);
					//}
					this._iniData = parser.ReadData(sr).Global;
				}
            }
            catch(Exception e)
            {
				Console.WriteLine ("Looked for ini file at: {0}", _iniFilePath);
				Console.WriteLine ("Error: {0}", e.Message);
                Console.WriteLine("No ini file location present, some operations may not work. Example ini file at zuoraTools.ini.dist");
            }
        }

        public Config ApplyIniData(Config conf)
        {
            //Todo: make this dynamic, with dash separated to camel/Pascal case conversion
            try
            {
                conf.SqlServer = _iniData["sql-server"];
                conf.SqlDatabase = _iniData["sql-database"];
                conf.SqlUsername = _iniData["sql-username"];
                conf.SqlPassword = _iniData["sql-password"];
            }
            catch (Exception e)
            {
                Console.WriteLine("ini file did not contain sql credentails");
				Console.WriteLine (e.Message);
            }

            try
            {
                conf.ZuoraEndpoint = _iniData["zuora-endpoint"];
                conf.ZuoraUsername = _iniData["zuora-username"];
                conf.ZuoraPassword = _iniData["zuora-password"];
                conf.ZuoraExportUri = _iniData["zuora-export-uri"];   
            }
            catch (Exception e)
            {
                Console.WriteLine("ini file did not contain Zuora credentails");
				Console.WriteLine (e.Message);
            }

            return conf;
        }

        public void ShowHelp()
        {
            //Todo: Add usage with new command structure.
            /*Console.Write("\nUsage examples:\n" +
                "\tAdd Ids to a csv file:\n" +
                "\t\tzuoratools --ini dev-env --ids-from-sql-query \"select id from account where accountnumber = '{}'\" -s test.csv --where-column AccountNumber --o test.out\n\n" +
                "\tUpdate zuora records overriding a field from the source file:\n" +
                "\t\tzuoratools --ini dev-env --update-zobjects --source-file accounts.csv --zobject-type account --property-value Name=NewName\n" +
                "\n");*/
            Console.WriteLine("\nOptions:");
            _optSet.WriteOptionDescriptions(Console.Out);
            Console.Write("\n\n*Default config path is: " + GetDefaultConfigPath() + "\n");
        }

        public static string GetUserHomeDir()
        {
            string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
            Environment.OSVersion.Platform == PlatformID.MacOSX)
            ? Environment.GetEnvironmentVariable("HOME") + @"/"
            : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") + @"\";

            return homePath;

        }

        public static string GetDefaultConfigPath()
        {
            string configPath = (Environment.OSVersion.Platform == PlatformID.Unix ||
            Environment.OSVersion.Platform == PlatformID.MacOSX)
            ? Environment.GetEnvironmentVariable("HOME") + @"/.zuoraTools.ini"
            : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") + @"\_zuoraTools.ini";

			//Console.WriteLine ("config file at{0}", configPath);
            return configPath;
        }

        public static string GetEpochString()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;
            return secondsSinceEpoch.ToString();
        }

        public static string GetBigEndianTimeString()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_");
        }
    }
}
