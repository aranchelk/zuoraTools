using System.Collections.Concurrent;

namespace zuoraTools.LogWriters
{
    class MultiThreadWriter : ScreenWriter
    {
        private ConcurrentQueue<string> _outputQueue = new ConcurrentQueue<string>();

        public MultiThreadWriter(ConcurrentQueue<string> poolQueue)
        {
            _outputQueue = poolQueue;
        }

        public override void Write(string line)
        {
            _outputQueue.Enqueue(line);
        }
    }
}