using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongPredection.Model
{
    //equivalent to song class
    public class ScalarModel
    {
        public int Id { get; set; }
        public Song Song { get; set; }
        public ScalarSong ScalarSong { get; set; }
    }

    //equivalent to playlist class
    public class PlaylistScalarModel
    {
        public String Name { get; set; }
        public List<ScalarModel> ScalarModelList { get; set; }
    }
}
