using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading;
//using System.Web.UI.WebControls.Expressions;
using zuoraTools.LogWriters;
using zuoraTools.zuora;

namespace zuoraTools.ZuoraConnections
{
    class ZConnection : IZConnection
    {
        /*Notes on this class
         * Async methods are made to be used to batch/glob records when run in a loop.
         * I.e. updateAsync is placed in a loop processing records line by line, when enough records are added to
         * 
         *Todos:
         * Cleanly close connection on exportQuery on destruct
         * Destructor that throws an error if there are still object in queue

         *Implement results accessor
         */

        public Queue<zObject> UpdateQueue = new Queue<zObject>();
        public Queue<zObject> CreateQueue = new Queue<zObject>();
        public Queue<zObject> DeleteQueue = new Queue<zObject>();

        //public string Mode;
        //public Action<zObject> ModeAction;

        public ZuoraService Connection;

        public Boolean AutoFlush = true;

        public IWriter FailedRecords { get; set; }
        public IWriter Errors { get; set; }
        public IWriter Success { get; set; }

        public int BatchSize { get; set; }

        //These are hard coded to 1, not yet in use

        public string ExportUri { get; set; }

        //public results 

        public static ZuoraService CreateService(string username, string password, string endpoint)
        {
            ZuoraService z = new ZuoraService() { Url = endpoint };
            z.PreAuthenticate = true;
            LoginResult lr = z.login(username, password);
            SessionHeader newSession = new SessionHeader { session = lr.Session };
            z.SessionHeaderValue = newSession;

            return z;
        }

        public void SetLogging(IWriter failed, IWriter successful, IWriter errorInfo)
        {
            FailedRecords = failed;
            Success = Success;
            Errors = errorInfo;
        }

        public ZConnection(ZuoraService zExternal)
        {
            this.Connection = zExternal;
            BatchSize = 50;
        }

        public ZConnection(string username, string password, string endpoint, string exportUri = null)
        {
            this.Connection = CreateService(username, password, endpoint);
            this.ExportUri = exportUri;
            BatchSize = 50;

            FailedRecords = new ScreenWriter();
            Errors = new ScreenWriter();
            Success = new ScreenWriter();
        }

        public zObject[] Query(string queryString)
        {
            QueryResult qr = Connection.query(queryString);
            return qr.records;
        }

        public Stream ExportQuery(string queryString)
        {
            Export exp = new Export { Format = "csv", Query = queryString, Zip = false, ZipSpecified = true, Encrypted = false, EncryptedSpecified = true };
            SaveResult[] sr = Connection.create(new zObject[] { exp });
            
            string fileId;
            QueryResult qr = new QueryResult();

            int attempt = 0;
            const int maxAttempts = 200;
            const int millisecondsWait = 100;

            do
            {
                if (attempt > maxAttempts)
                {
                    throw new Exception("Exceeded maximum attempts to perform export.");
                }
                Thread.Sleep(millisecondsWait);
                attempt++;

                qr = Connection.query("SELECT status, fileId, query, size FROM Export WHERE Id='" + sr[0].Id + "'");
                Export ex = (Export)qr.records[0];
                //TextProcessing.VarDump(qr);
                //TextProcessing.VarDump();
                fileId = (string)ex.FileId;
            } while (String.IsNullOrEmpty(fileId));

            WebRequest req = WebRequest.Create(ExportUri + fileId);
            req.Headers.Add("Authorization", "ZSession " + Connection.SessionHeaderValue.session.ToString());
            //If Zuora ever enables stream compression, enable this: req.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");

            WebResponse res = req.GetResponse();
            return res.GetResponseStream();
        }

        //todo, overload a single record version of this method
        public void Update(zObject[] zArray)
        {
            SaveResult[] results = Connection.update(zArray);

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].Success)
                {
                    Success.Write(results[i].Id);
                }
                else
                {
                    FailedRecords.Write(zArray[i].Id);
                    foreach (Error e in results[i].Errors)
                    {
                        Errors.Write(results[i].Id + " failed with code:" + e.Code + " and message:" + e.Message);
                    }
                }
            }
        }

        public void Create(zObject[] zArray)
        {
            SaveResult[] results = Connection.create(zArray);
            
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].Success)
                {
                    Success.Write(results[i].Id);
                }
                else
                {
                    FailedRecords.Write(zArray[i].Id);
                    foreach (Error e in results[i].Errors)
                    {
                        Errors.Write(results[i].Id + " failed with code:" + e.Code + " and message:" + e.Message);
                    }
                }
            }
            
        }

        public void Delete(zObject[] zArray)
        {           
            //Convert zobject array into string array
            string[] delList = new List<zObject>(zArray).Select(zob => zob.Id).ToArray();
            string zType = zArray[0].GetType().Name.ToString().ToLower();

            DeleteResult[] results = Connection.delete(zType, delList);

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].success)
                {
                    Success.Write(results[i].id);
                }
                else
                {
                    FailedRecords.Write(zArray[i].Id);
                    foreach (Error e in results[i].errors)
                    {
                        Errors.Write(results[i].id + " failed with code:" + e.Code + " and message:" + e.Message);
                    }
                }
            }
        }
        
        public void UpdateAsync(zObject z)
        {
            QueueAndFlush(z, UpdateQueue, Update);
        }

        public void CreateAsync(zObject z)
        {
            QueueAndFlush(z, CreateQueue, Create);
        }

        public void DeleteAsync(zObject z)
        {
            Console.WriteLine("Queued for delete {0}", z.Id);
            QueueAndFlush(z, DeleteQueue, Delete);
        }

        public void Flush()
        {
            FlushQueue(UpdateQueue, Update);
            FlushQueue(CreateQueue, Create);
            FlushQueue(DeleteQueue, Delete);
        }

        public void Flush(string queueToFlush)
        {
            if (queueToFlush == "update")
            {
                FlushQueue(UpdateQueue, Update);
            }
            else if (queueToFlush == "create")
            {
                FlushQueue(CreateQueue, Create);
            }
            else if (queueToFlush == "delete")
            {
                FlushQueue(DeleteQueue, Delete);
            }
            else
            {
                throw new Exception("A queue has been specified for Flush() that does not exist!");
            }
        }

        public void QueueAndFlush(zObject z, Queue<zObject> targetQueue, Action<zObject[]> flushAction)
        {
            if (targetQueue.Count > BatchSize)
            {
                throw new Exception("Queue has exceeded batch size, there must be a flow control issue.");
            }

            if (targetQueue.Count == BatchSize && AutoFlush)
            {
                FlushQueue(targetQueue, flushAction);
            }

            targetQueue.Enqueue(z);
        }

        public void FlushQueue(Queue<zObject> targetQueue, Action<zObject[]> flushAction )
        {
            Console.WriteLine("Flushing queue.");
            try
            {
                if (targetQueue.Count > 0)
                {
                    flushAction(targetQueue.ToArray());
                    targetQueue.Clear();
                }                  
            }
            catch (Exception e)
            {
                Errors.Write(e);
                foreach(zObject zOb in targetQueue){
                    FailedRecords.Write(zOb);
                }
                targetQueue.Clear();
            }
        }
    }
}
