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

    public class EncodedSequence
    {
        public string Name { get; set; }
        public List<Tuple<string, int[]>> encodedWords { get; set; }

        public EncodedSequence()
        {
            this.Name = string.Empty;
            this.encodedWords = new List<Tuple<string, int[]>>();
        }
    }
}
