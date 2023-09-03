using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongPredection.Model
{
    public class Database
    {
        public Dictionary<string, int> SongNames = new Dictionary<string, int>();
        public Dictionary<string, int> Singers = new Dictionary<string, int>();
        public Dictionary<string, int> Genres = new Dictionary<string, int>();
        public Dictionary<string, int> Moods = new Dictionary<string, int>();
    }

    public class SongName
    {
        public String Name { get; set; }
        public int ID { get; set; }
    }

    public class Singer
    {
        public String Name { get; set; }
        public int ID { get; set; }
    }

    public class Genre
    {
        public String Name { get; set; }
        public int ID { get; set; }
    }

    public class Mood
    {
        public String Name { get; set; }
        public int ID { get; set; }
    }
}
