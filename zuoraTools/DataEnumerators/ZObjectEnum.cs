using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using zuoraTools.zuora;

namespace zuoraTools.DataEnumerators
{
    class ZObjectEnum : IDataEnumerator
    {        
        private Dictionary<string, string> _additionalProperties = new Dictionary<string, string>();
        private Dictionary<string, string> _badKeyGoodKeyDict = new Dictionary<string, string>();
        private IDataEnumerator _innerGen;
        private string _zObjectType;
        public int LoopIterations { get; set;}
        private List<string> _keys;

        public List<string> Keys
        {
            get
            {
                if (this._keys == null) this._keys = _badKeyGoodKeyDict.Values.ToList();
                return _keys;
            }
            set
            {
                Dictionary<string, string> newDict = new Dictionary<string, string>();
                Dictionary<string, string> reverseBkgk = _badKeyGoodKeyDict.ToDictionary(el => el.Value, el => el.Key);

                foreach (string prop in value)
                {
                    string correctedProp = ZObjectHelper.FixPropertyCases(prop, _zObjectType);
                    newDict.Add(reverseBkgk[correctedProp], prop);

                    _badKeyGoodKeyDict = newDict;
                }
            }
        }

        public ZObjectEnum(IDataEnumerator innerGen, String zObjectType, Dictionary<string, string> additionalProperties)
        {
            _innerGen = innerGen;
            _zObjectType = zObjectType;
            _additionalProperties = additionalProperties;

            List<string> compositeKeys = new List<string>();

            compositeKeys.AddRange(this._innerGen.Keys);
            compositeKeys.AddRange(additionalProperties.Keys);

            // Todo: move this into a function in zObjectHelper
            foreach (string key in compositeKeys)
            {
                if (!key.Contains("."))
                {
                    //Intentionally not using the .Add() method, may be duplicate values on data merges, that's not a problem
                    _badKeyGoodKeyDict[key] = ZObjectHelper.FixPropertyCases(key, _zObjectType);
                }
                else
                {
                    string[] typeProp = key.Split(new Char[] { '.' });

                    if (typeProp.Length != 2) throw new Exception("Bad property label:" + key);

                    if (String.Equals(typeProp[0], this._zObjectType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        string temp = ZObjectHelper.FixPropertyCases(typeProp[1], _zObjectType);
                        _badKeyGoodKeyDict.Add(key, temp);
                    }
                }
            }
        }

        public IEnumerable Gen()
        {
            foreach (Dictionary<string, string> rowDict in _innerGen.Gen())
            {
                zObject zOb = null;

                //Todo: replace with linq
                //Todo: replace reference to conf with passed value
                if (_additionalProperties.Count > 0)
                {
                    //Merge user added data with CSV data
                    foreach (KeyValuePair<string, string> kv in _additionalProperties)
                    {
                        rowDict[kv.Key] = kv.Value;
                    }
                }

                //Todo: replace with linq
                Dictionary<string, string> fixedDict = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> kv in rowDict)
                {
                    if (_badKeyGoodKeyDict.Keys.Contains(kv.Key))
                    {
                        //Intentionally not using the .Add() method, may be duplicate values on data merges, that's not a problem
                        fixedDict[_badKeyGoodKeyDict[kv.Key]] = kv.Value;
                    } 
                }

                zOb = ZObjectHelper.Compose(fixedDict, _zObjectType, true);

                yield return zOb;
            }

            yield break;
        }

        public string Next()
        {
            throw new Exception("This method was not designed to return results as string.");
        }

    }
}
