using System;
using System.Collections.Generic;
using System.Linq;

namespace zuoraTools
{
    class Config
    {
        public Boolean DryRun { get; set; }

        public int Limit { get; set; }
        public int Offset { get; set; }
        public string SourceFile { get; set; }
        public string OutputFile { get; set; }

        public int Verbosity { get; set; }

        public string ZObjectType { get; set; }
        public string LogLocation { get; set; }
        public string LogPrefix { get; set; }

        public string IniFilePath { get; set; }

        public string ZuoraUsername { get; set; }
        public string ZuoraPassword { get; set; }
        public string ZuoraEndpoint { get; set; }
        public string ZuoraExportUri { get; set; }

        public string SqlServer { get; set; }
        public string SqlDatabase { get; set; }
        public string SqlUsername { get; set; }
        public string SqlPassword { get; set; }

        public Dictionary<String, String> PropertyValues { get; set; }

        public string SqlQueryString { get; set; }
        public string ZqueryString { get; set; }

        public string WhereColumn { get; set; }
        public int Concurrency { get; set; }

        public string Mode { get; set; }

        public Config()
        {
            //Default values
            OutputFile = null;
            DryRun = false;
            PropertyValues = new Dictionary<String, String>();

            Concurrency = 1;
            Verbosity = 0;

            Offset = 0;
            Limit = -1;

            Mode = "help";
        }

        public void RequiredProperty(params string[] properties)
        {
            List<string> configProps = this.GetType().GetProperties().Select(prop => prop.Name).ToList();

            foreach (string reqProp in properties)
            {
                //Make sure properties supplied are valid
                if (!configProps.Contains(reqProp))
                {
                    Console.WriteLine("Props:\n" + String.Join("\n", configProps));
                    throw new Exception("An invalid config property was required:" + reqProp);
                }

                var propertyValue = this.GetType().GetProperty(reqProp).GetValue(this);

                if (propertyValue == null)
                {
                    throw new Exception("You must provide a value for '" + reqProp + "' to perform this action.");
                }
            }
        }

    }

}
