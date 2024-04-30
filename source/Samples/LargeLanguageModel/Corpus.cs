using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace LargeLanguageModel
{
    // Corpus is a collection of authentic text or audio organized into datasets.
    public class Corpus
    {
        // simialar things should be encoded similar way
        public SortedDictionary<string, int> Word = new SortedDictionary<string, int>();
    }
}
