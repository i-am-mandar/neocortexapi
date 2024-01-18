using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongPredection.Model
{
    public class Dataset
    {
        public int Position { get; set; }
        public string Song { get; set; }
        public string Artist { get; set; }
        public string Genre { get; set; }
        public string Mood { get; set; }
    }
}
