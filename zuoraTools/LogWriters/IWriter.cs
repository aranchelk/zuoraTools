using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace zuoraTools.LogWriters
{
    interface IWriter
    {
        void Write(string line);

        void Write(string[] lines);

        void Write(List<string> lines);

        void Write(Exception e);

        void Write(zObject zOb);

        void Write(ConcurrentQueue<string> cQueue);
    }
}
