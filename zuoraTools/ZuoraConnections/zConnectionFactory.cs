using System.Collections.Concurrent;
using zuoraTools.LogWriters;

namespace zuoraTools.ZuoraConnections
{
    class ZConnectionFactory
    {
        public static IZConnection Make(Config conf)
        {
            IZConnection zConn;

           if (conf.Concurrency == 1)
           {
                // create regular single threaded zConnection
                zConn = new ZConnection(conf.ZuoraUsername, conf.ZuoraPassword, conf.ZuoraEndpoint, conf.ZuoraExportUri);
                //Todo: implement export URI accross the board.           
           
            }
            else
            {
                //Create a synchronous parallel zConnection
                zConn = new ZConnectionParallel(conf.ZuoraUsername, 
                    conf.ZuoraPassword,
                    conf.ZuoraEndpoint, 
                    conf.Concurrency
                    );
            }
            if (!conf.DryRun)
            {
                zConn.Errors = new FileWriter(conf.LogLocation + conf.LogPrefix + "errors.log");
                zConn.FailedRecords = new FileWriter(conf.LogLocation + conf.LogPrefix + "failedRecords.log");
                zConn.Success = new FileWriter(conf.LogLocation + conf.LogPrefix + "success.log");
            }
            else
            {
                zConn.Errors = new ScreenWriter("Error: ");
                zConn.FailedRecords = new ScreenWriter("Failed: ");
                zConn.Success = new ScreenWriter("Success: ");
            }

            return zConn;
        }

        public static ZConnection MakePoolWorker(ZuoraService zExternal, ConcurrentQueue<string> success, ConcurrentQueue<string> failedRecords, ConcurrentQueue<string> errors)
        {
            ZConnection zConn = new ZConnection(zExternal);

            MultiThreadWriter successMulti = new MultiThreadWriter(success);
            MultiThreadWriter failedRecordsMulti = new MultiThreadWriter(failedRecords);
            MultiThreadWriter errorMulti = new MultiThreadWriter(errors);

            zConn.Success = successMulti;
            zConn.Errors = errorMulti;
            zConn.FailedRecords = failedRecordsMulti;

            zConn.AutoFlush = false;

            return zConn;
        }
    }
}
