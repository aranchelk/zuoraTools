using System;
using System.Collections;
using System.Collections.Generic;

namespace zuoraTools.DataEnumerators
{
    interface IDataEnumerator
    {
        //An inteface used to facilitate looping through records in a foreach
        int LoopIterations{get;} //A counter used primarily for reporting how many records were processed
        List<string> Keys { get; set; }
        String Next(); //A method that dequeues the a record and does NOT increment loopIterations, used primarily for parsing headers
        IEnumerable Gen(); //Returns enumerable
        //bool _textBufferLoadLine(); //An idompotent method for making sure a new record is ready for processing, returns false when there are no records remaining, used internally and in wrappers, semi-private.
    }
}
