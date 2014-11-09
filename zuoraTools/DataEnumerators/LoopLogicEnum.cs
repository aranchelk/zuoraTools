using System.Collections;
using System.Collections.Generic;

namespace zuoraTools.DataEnumerators
{
    /*
     *This decorator class wraps LineEnum and similar classes
     *It adds offset and limit functionality
     */

    class LoopLogicEnum : IDataEnumerator
    {
        private IDataEnumerator _wrappedRg;
        private int _offset;
        private int _limit;

        public List<string> Keys
        {
            get { return _wrappedRg.Keys; }
            set { _wrappedRg.Keys = value; }
        }

        public int LoopIterations { get; private set; }

        public LoopLogicEnum(IDataEnumerator wrappedRg, int offset, int limit)
        {
            this._wrappedRg = wrappedRg;
            this._offset = offset;
            this._limit = limit;
            this.LoopIterations = 0;
        }

        public IEnumerable Gen()
        {
            // Advance wrappedRG to offset point
            while (_offset > 0)
            {
                _offset--;
                _wrappedRg.Next();
            }

            foreach (string s in _wrappedRg.Gen())
            { 
                if (_limit == 0) yield break;

                _limit--;

                LoopIterations++;
                yield return s;
            }
        }

        public string Next()
        {
            return _wrappedRg.Next();
        }

    }
}
