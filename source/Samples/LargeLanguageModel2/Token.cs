using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeLanguageModel2
{
    // Token is a collection of authentic characters from datasets.
    public class Token
    {
        // simialar things should be encoded similar way
        public SortedDictionary<char, int> Char = new SortedDictionary<char, int>();
    }
}
