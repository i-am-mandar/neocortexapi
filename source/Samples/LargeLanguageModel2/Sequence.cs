using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeLanguageModel2
{
    public class Sequence
    {
        public string Name { get; set; }
        public List<char> Chars { get; set; }

        public Sequence()
        {
            this.Name = string.Empty;
            this.Chars = new List<char>();
        }

    }

    public class EncodedSequence
    {
        public string Name { get; set; }
        public List<char> SubSequence { get; set; }
        public List<int> EncodedSubSequence { get; set; }
        public char NextChar { get; set; }
        public int EncodedNextChar { get; set; }
        public int[] SDR { get; set; }

        public EncodedSequence()
        {
            this.Name = string.Empty;
            this.SubSequence= new List<char>();
            this.EncodedSubSequence = new List<int>();
            this.NextChar = new char();
            this.EncodedNextChar = new int();
            this.SDR = new int[] { };
        }

    }

    public class Multisequence
    {
        public string Name { get; set; }
        public List<Sequence> Sequences { get; set; }

        public Multisequence()
        {
            this.Name = string.Empty;
            this.Sequences = new List<Sequence>();
        }
    }

    public class EncodedMultisequence
    {
        public string Name { get; set; }
        public List<EncodedSequence> EncodedSequences { get; set; }

        public EncodedMultisequence()
        {
            this.Name = string.Empty;
            this.EncodedSequences = new List<EncodedSequence>();
        }
    }
}
