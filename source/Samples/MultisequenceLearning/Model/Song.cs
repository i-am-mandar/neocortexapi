using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongPredection
{
    public class Song
    {
        public String Name { get; set; }
        public String Singer1 { get; set; }
        public String Singer2 { get; set; }
        public String Genre1 { get; set; }
        public String Genre2 { get; set; }
        public String Mood { get; set; }
    }

    public class Playlist
    {
        public String Name { get; set; }
        public List<Song> Songs { get; set;}
    }
}

