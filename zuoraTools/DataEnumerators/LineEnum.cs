using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace zuoraTools.DataEnumerators
{
    /*
     * This class takes data streams, parses them into lines of text and makes them consumable in foreach loops
     * See interface for info on what public methods do.
     * 
     * This class is some complicated spaghetti, sorry about that.
     */
    class LineEnum : IDataEnumerator
    {
        //Todo: Close file handles when end of file is reached.
        public int LoopIterations { get; private set; }

        private int _bufferSize;
        private Stream _inputStream;
        private List<string> _lineBuffer = new List<string>();

        private string _textBuffer = "";
        private int _count = 1; //Non zero value for first iteration of loop.

        public List<string> Keys
        {
            get { throw new NotImplementedException("This is a simple record generator, it does not parse records and as a consequence does not have Keys."); }
            set { throw new NotImplementedException("This is a simple record generator, it does not parse records and as a consequence does not have Keys."); }
        }


        public LineEnum(Stream sourceStream, int bufferSize = 32768)
        {
            this._bufferSize = bufferSize;
            this._inputStream = sourceStream;
            LoopIterations = 0;
        }

        private bool InputStreamIsEmpty()
        {
            return !(_count > 0);
        }

        private bool TextBufferHasLine()
        {
            if (String.IsNullOrEmpty(_textBuffer))
            {
                return false;
            }
            bool hasNewlineChar = Regex.Match(_textBuffer, @"(\r|\n|\r\n)", RegexOptions.Singleline).Success;

            if (!hasNewlineChar)
            {
                return (InputStreamIsEmpty() == true && String.IsNullOrEmpty(_textBuffer));
            }
            else
            {
                return true;
            }
        }

        private bool AppendTextBuffer()
        {
            if (!InputStreamIsEmpty())
            {
                byte[] buffer = new byte[_bufferSize];
                _count = _inputStream.Read(buffer, 0, buffer.Length);
                _textBuffer = _textBuffer + System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool _textBufferLoadLine()
        {
            if (TextBufferHasLine())
            {
                return true;
            }

            while (!InputStreamIsEmpty())
            {
                if (TextBufferHasLine())
                {
                    return true;
                }
                else
                {
                    AppendTextBuffer();
                }
            }

            return TextBufferHasLine();
        }

        private string TextBufferPullLine()
        {
            //Trim leading return chars
            _textBuffer = _textBuffer.TrimStart(new char[] { '\n', '\r' });

            string line = "";
            while (Regex.Match(line, @"^\s*$", RegexOptions.Singleline).Success && _textBufferLoadLine())
            {
                //Pull all non-newline/non-return characters until newline
                line = Regex.Match(_textBuffer, @"^([^\r^\n]+)(\r|\n)").Value;
                //And remove the sequence from the source string
                _textBuffer = Regex.Replace(_textBuffer, @"^([^\r^\n]+)(\r|\n)", "", RegexOptions.Singleline);

                line = line.Trim(new char[] { '\n', '\r' });
             }
            
            return line.Trim();
        }

        public string Next()
        {
            return TextBufferPullLine();
        }

        public IEnumerable Gen()
        {
            while (true)
            {
                string s = "";

                while (_textBufferLoadLine() && String.IsNullOrWhiteSpace(s))
                {
                    s = Next();
                }
                
                if (!String.IsNullOrWhiteSpace(s))
                {
                    LoopIterations++;
                    yield return s;
                }
                else
                {
                    yield break;
                }
            }    
        }


    }
}
