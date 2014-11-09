using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using zuoraTools.LogWriters;
using zuoraTools.zuora;

namespace zuoraTools.ZuoraConnections
{
    class ZConnectionParallel : IZConnection
    {
        public Queue<zObject> UpdateQueue = new Queue<zObject>();
        public Queue<zObject> CreateQueue = new Queue<zObject>();
        public Queue<zObject> DeleteQueue = new Queue<zObject>();

        public IWriter FailedRecords { get; set; }
        public IWriter Errors { get; set; }
        public IWriter Success { get; set; }
        public int BatchSize { get; set; }
        public string ExportUri { get; set; }

        public ConcurrentQueue<string> SuccessQueue = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> FailedRecordsQueue = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> ErrorQueue = new ConcurrentQueue<string>();

        public int Concurrency;
        public int MaxConcurrencyPerSession;
        public List<ZConnection> ZconnPool = new List<ZConnection>();

        public void SetLogging()
        {
            FailedRecords = new ScreenWriter();
            Errors = new ScreenWriter();
            Success = new ScreenWriter();
        }

        public ZConnectionParallel(string username, string password, string endpoint, int concurrency = 6, int maxConcurrencyPerSession = 3)
        {
            Concurrency = concurrency;
            int uniqueSessionCount = concurrency / maxConcurrencyPerSession;

            ZuoraService zS;
            BatchSize = 50;

            for (int i = 0; i < uniqueSessionCount; i++)
            {
                zS = ZConnection.CreateService(username, password, endpoint);
                for (int j = 0; j < maxConcurrencyPerSession; j++)
                {
                    ZconnPool.Add(ZConnectionFactory.MakePoolWorker(zS, SuccessQueue, FailedRecordsQueue, ErrorQueue));
                }
            }
            if (concurrency % maxConcurrencyPerSession != 0)
            {
                zS = ZConnection.CreateService(username, password, endpoint);
                for (int j = 0; j < concurrency % maxConcurrencyPerSession; j++)
                {
                    ZconnPool.Add(ZConnectionFactory.MakePoolWorker(zS, SuccessQueue, FailedRecordsQueue, ErrorQueue));
                }
            }

            SetLogging();
        }

        public void Flush()
        {
            
        }

        public void Flush(string qName)
        {
            var zObjQueues = new Dictionary<string, Queue<zObject>>
            {
                {"update", UpdateQueue},
                {"create", CreateQueue},
                {"delete", DeleteQueue},
            };

            var methods = new Dictionary<string, string>
            {
                {"update", "UpdateAsync"},
                {"create", "CreateAsync"},
                {"delete", "DeleteAsync"}
            };

            while (zObjQueues[qName].Any())
            {
                foreach (ZConnection z in ZconnPool)
                {
                    try
                    {
                        z.GetType().GetMethod(methods[qName]).Invoke(z, new[] { zObjQueues[qName].Dequeue() }); 
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                }
            }

            //Make calls 
            Parallel.ForEach(ZconnPool, zconn => zconn.Flush(qName));

            Success.Write(SuccessQueue);
            FailedRecords.Write(FailedRecordsQueue);
            Errors.Write(ErrorQueue);
        }

        public void Create(zObject[] z)
        {
            ZconnPool[0].Create(z);
        }

        public void CreateAsync(zObject z)
        {
            if (BatchSize * Concurrency == CreateQueue.Count)
            {
                Flush("create");
            }
            CreateQueue.Enqueue(z);
        }

        public void Update(zObject[] z)
        {
            ZconnPool[0].Update(z);
        }

        public void UpdateAsync(zObject z)
        {
            if (BatchSize * Concurrency == UpdateQueue.Count)
            {
                Flush("update");
            }
            UpdateQueue.Enqueue(z);
        }

        public zObject[] Query(string queryString)
        {
            return ZconnPool[0].Query(queryString);
        }

        public Stream ExportQuery(String queryString)
        {
            return ZconnPool[0].ExportQuery(queryString);
        }

        public void Delete(zObject[] z)
        {
            ZconnPool[0].Delete(z);
        }

        public void DeleteAsync(zObject z)
        {
            if (BatchSize * Concurrency == DeleteQueue.Count)
            {
                Flush("delete");
            }
            DeleteQueue.Enqueue(z);
        }
    }
}
