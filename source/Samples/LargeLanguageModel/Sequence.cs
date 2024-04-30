using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeLanguageModel
{
    public class Sequence
    {
        public string Name { get; set; }
        public List<string> Words { get; set; }

        public Sequence() 
        {
            this.Name = string.Empty; 
            this.Words = new List<string>();
        }
    }

    public class EncodedWord
    {
        public string Word { get; set; }
        public int Key { get; set; }
        public int[] SDR { get; set; }
    }

    public class EncodedSequence
    {
        public string Name { get; set; }
        //string = word, int = key, int[] SDR
        public List<EncodedWord> encodedWords { get; set; }

        public EncodedSequence()
        {
            this.Name = string.Empty;
            this.encodedWords = new List<EncodedWord>();
        }
    }
}
