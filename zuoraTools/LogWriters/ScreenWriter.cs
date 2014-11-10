using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace zuoraTools.LogWriters
{
    class ScreenWriter : IWriter
    {
        private string _prefix = "";

        public ScreenWriter() { }

        public ScreenWriter(string prefix)
        {
            _prefix = prefix;
        }

        public List<string> ZObjectFields = new List<string>{"id"};

        public virtual void Write(string line)
        {
            Console.WriteLine(_prefix + line);
        }
        public void Write(string[] lines)
        {
            foreach (string line in lines)
            {
                Write(line);
            }

        }
        public void Write(List<string> lines)
        {
            Write(lines.ToArray());
        }

        public void Write(Exception e)
        {
            Write(Regex.Replace(JsonConvert.SerializeObject(e), @",", ", "));
        }

        public void Write(zObject zOb)
        {
            //zObjectFields.ToArray()
            Write(ZObjectHelper.ToCsvRecord(zOb, new string[] {"id"} ));
        }

        public void Write(ConcurrentQueue<string> cQueue)
        {
            String item;
            while (cQueue.TryDequeue(out item))
            {
                Write(item);
            }
        }

        public static void Write(object o){
            Console.WriteLine(Regex.Replace(JsonConvert.SerializeObject(o), @",", "\n"));
        }
    }
}
