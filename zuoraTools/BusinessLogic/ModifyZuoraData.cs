using System;
using System.Collections.Generic;
using zuoraTools.DataEnumerators;
using zuoraTools.ZuoraConnections;
using System.IO;
using zuoraTools.zuora;

namespace zuoraTools.BusinessLogic
{
    class ModifyZuoraData
    {
        public static void Process(Config conf)
        {
            conf.RequiredProperty("LogLocation", "ZuoraUsername", "ZObjectType");

            IZConnection zConn = null;

            Stream fs = new FileStream(conf.SourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            IDataEnumerator zGen = DataEnumeratorFactory.MakeZObjectGen(fs, conf);

            if (conf.Mode == "touch-zobjects" || conf.Mode == "delete-zobjects")
            {
                //For these requests to Zuora we want an empty object with only the Id populated.
                zGen.Keys = new List<string>{"id"};
            }

            Action preLoopLogic;
            Action<zObject> innerLoopLogic;
            Action postLoopLogic;

            if (conf.DryRun)
            {
                preLoopLogic = () => Console.WriteLine(String.Join(",", zGen.Keys));
                innerLoopLogic = z => Console.WriteLine(ZObjectHelper.ToCsvRecord(z, zGen.Keys.ToArray()));
                postLoopLogic = () => Console.Write("\n*** Dry run, nothing to do ***\n\n");
            }
            else
            {
                preLoopLogic = () => zConn = ZConnectionFactory.Make(conf);
                postLoopLogic = () => { zConn.Flush(); }; //Final flush as zconnection object doesn't know the loop has finished and has queued records

                if (conf.Mode == "touch-zobjects" || conf.Mode == "update-zobjects")
                {
                    innerLoopLogic = zOb => zConn.UpdateAsync(zOb);
                }
                else if (conf.Mode == "create-zobjects")
                {
                    innerLoopLogic = zOb => zConn.CreateAsync(zOb);
                }
                else if (conf.Mode == "delete-zobjects")
                { 
                    innerLoopLogic = zOb => zConn.DeleteAsync(zOb);                   
                }
                else
                {
                    throw new Exception("Invalid mode!");
                } 
            }

            preLoopLogic();
            foreach (zObject z in zGen.Gen())
            {
                innerLoopLogic(z);
            }
            postLoopLogic();
        }
    }
}
