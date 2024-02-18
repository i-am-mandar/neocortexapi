using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeLanguageModel
{
    public class Sentence
    {
        public string[] Word { get; set; }
    }
    
    public class Conversation
    {
        public string Speaker { get; set; }
        public string[] Words { get; set; }
    }

}
