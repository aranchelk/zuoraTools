using System.IO;
using zuoraTools.LogWriters;
using zuoraTools.zuora;

namespace zuoraTools.ZuoraConnections
{
    interface IZConnection
    {
        //A wrapper for Zuora API calls, implemented as an interface as there are single and multi-threaded versions.
        IWriter FailedRecords { get; set; } //An object to record output of IDs of records that failed to update
        IWriter Errors { get; set; } //An object to record erorr messages from the server
        IWriter Success { get; set; } //An object to record the Ids of successful records
        int BatchSize { get; set; } //Batch size, will pretty much always be set to the max, 50, but it's configurable.

        void Flush();
        void Flush(string queueToFlush);

        void Update(zObject[] z);
        void UpdateAsync(zObject z);

        void Create(zObject[] z);
        void CreateAsync(zObject z);

        void Delete(zObject[] z);
        void DeleteAsync(zObject z);

        Stream ExportQuery(string queryString);

        zObject[] Query(string queryString);
    }
}
