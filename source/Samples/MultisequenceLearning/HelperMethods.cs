using NeoCortexApi.Entities;
using NeoCortexApi;
using NeoCortexApi.Encoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SongPredection.Model;

namespace SongPredection
{
    public class HelperMethods
    {
        public static string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        public static int Debug = 1;

        public static int SongID = 1001;
        public static int SingerID = 101;
        public static int GenreID = 101;
        public static int MoodID = 11;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<Song> ReadPlaylistFile(string dataset)
        {
            string file = Path.Combine(BasePath, "dataset", dataset);
            Console.WriteLine($"Reading file {dataset}...");
            var jsonData = File.ReadAllText(file);

            if (Debug >= 2)
                Console.WriteLine($"\n\tRaw JSON Data:{jsonData}");

            List<Song> playlist = JsonSerializer.Deserialize<List<Song>>(jsonData);

            return playlist;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        public static List<Playlist> GetPlaylists(List<string> fileNames)
        {
            List<Playlist> playlists = new List<Playlist>();
            int i = 0;
            foreach (string file in fileNames)
            {
                Playlist playlist = new Playlist();
                playlist.Name = $"P{i}";
                if (Debug >= 2)
                    Console.WriteLine($"\n\tFILE: {file}\n\tCreating Playlist:{playlist.Name}");
                playlist.Songs = ReadPlaylistFile(file);
                playlists.Add(playlist);
                i++;
            }

            return playlists;
        }

        public static List<string> DecodePrediction(List<Dictionary<string, string>> predictions, List<Playlist> dataFiles)
        {
            List<string> predicted = new List<string>();

            foreach (Dictionary<string, string> predict in predictions)
            {
                var playlist = predict.Values.FirstOrDefault().Split('-').First();
                var song = predict.Values.FirstOrDefault().Split('-').Last(); ;

                Playlist predictedPlaylist = dataFiles[Int32.Parse(playlist.Substring(1, 1))];
                Song predictedSong = predictedPlaylist.Songs[Int32.Parse(song.Substring(1, 1))];

                predicted.Add($"{playlist}-{predictedSong.Name}");
            }

            return predicted;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playlists"></param>
        /// <returns></returns>
        public static Database FillDatabase(List<Playlist> playlists)
        {
            Database db = new Database();

            foreach (Playlist playlist in playlists)
            {
                foreach (Song song in playlist.Songs)
                {
                    string cleanSongName = song.Name.ToLower().Replace(" ", "_");
                    if (!db.SongNames.ContainsKey(cleanSongName) && !string.IsNullOrEmpty(cleanSongName))
                        db.SongNames.Add(cleanSongName, SongID++);

                    string cleanSinger1 = song.Singer1.ToLower().Replace(" ", "_");
                    if (!db.Singers.ContainsKey(cleanSinger1) && !string.IsNullOrEmpty(cleanSinger1))
                        db.Singers.Add(cleanSinger1, SingerID++);

                    string cleanSinger2 = song.Singer2.ToLower().Replace(" ", "_");
                    if (!db.Singers.ContainsKey(cleanSinger2) && !string.IsNullOrEmpty(cleanSinger2))
                        db.Singers.Add(cleanSinger2, SingerID++);

                    string cleanGenre1 = song.Genre1.ToLower().Replace(" ", "_");
                    if (!db.Genres.ContainsKey(cleanGenre1) && !string.IsNullOrEmpty(cleanGenre1))
                        db.Genres.Add(cleanGenre1, GenreID++);

                    string cleanGenre2 = song.Genre2.ToLower().Replace(" ", "_");
                    if (!db.Genres.ContainsKey(cleanGenre2) && !string.IsNullOrEmpty(cleanGenre2))
                        db.Genres.Add(cleanGenre2, GenreID++);

                    string cleanMood = song.Mood.ToLower().Replace(" ", "_");
                    if (!db.Moods.ContainsKey(cleanMood) && !string.IsNullOrEmpty(cleanMood))
                        db.Moods.Add(cleanMood, MoodID++);
                }
            }

            return db;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playlists"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static List<PlaylistScalarModel> ScalarlizePlaylists(List<Playlist> playlists, Database db)
        {
            List<PlaylistScalarModel> models = new List<PlaylistScalarModel>();
            int i = 0;

            foreach (Playlist playlist in playlists)
            {
                PlaylistScalarModel model = new PlaylistScalarModel();
                //M stands for modifying
                //model = ScalarlizePlaylist($"M{i}", playlist, db);
                //here assigning same name
                model = ScalarlizePlaylist(playlist.Name, playlist, db);
                models.Add(model);
                i++;
            }

            return models;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="playlist"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static PlaylistScalarModel ScalarlizePlaylist(string name, Playlist playlist, Database db)
        {
            List<ScalarModel> playlistScalarModel = new List<ScalarModel>();
            PlaylistScalarModel playlistsScalarModel = new PlaylistScalarModel();
            //adding modified name
            //playlistsScalarModel.Name = $"{name}-{playlist.Name}";
            //here assigning same name
            playlistsScalarModel.Name = $"{name}";

            int i = 0;
            foreach (Song song in playlist.Songs)
            {
                ScalarModel scalarModel = new ScalarModel();
                scalarModel.Id = i;
                scalarModel.Song = song;
                scalarModel.ScalarSong = new ScalarSong();
                scalarModel.ScalarSong.Name = GetValueByID(db.SongNames, song.Name);
                scalarModel.ScalarSong.Singer1 = GetValueByID(db.Singers, song.Singer1);
                scalarModel.ScalarSong.Singer2 = GetValueByID(db.Singers, song.Singer2);
                scalarModel.ScalarSong.Genre1 = GetValueByID(db.Genres, song.Genre1);
                scalarModel.ScalarSong.Genre2 = GetValueByID(db.Genres, song.Genre2);
                scalarModel.ScalarSong.Mood = GetValueByID(db.Moods, song.Mood);

                playlistScalarModel.Add(scalarModel);
                i++;
            }

            playlistsScalarModel.ScalarModelList = playlistScalarModel;

            return playlistsScalarModel;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static int GetValueByID(Dictionary<string, int> keyValuePairs, string key)
        {
            int value = 0;
            string cleanKey = key.ToLower().Replace(" ", "_");

            if (string.IsNullOrEmpty(cleanKey)) return value;

            bool hasValue = keyValuePairs.TryGetValue(cleanKey, out value);
            if (hasValue)
            {
                return value;
            }
            else
            {
                value = 0;
                return value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scalarDataSet"></param>
        /// <param name="songEncoder"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static List<Dictionary<string, int[]>> EncodePlaylists(List<PlaylistScalarModel> scalarDataSet, MultiEncoder songEncoder, Database db)
        {
            List<Dictionary<string, int[]>> keyValuePairsList = new List<Dictionary<string, int[]>>();

            foreach (PlaylistScalarModel scalarModelPlaylists in scalarDataSet)
            {
                Dictionary<string, int[]> keyValuePairs = new Dictionary<string, int[]>();
                int i = 0;
                foreach (ScalarModel scalarModelPlaylist in scalarModelPlaylists.ScalarModelList)
                {
                    //playlistID+SongID+counter
                    //string name = $"{scalarModelPlaylists.Name}-X{i}-S{scalarModelPlaylist.Id}";

                    //playlistID+SongID
                    string name = $"{scalarModelPlaylists.Name}-S{scalarModelPlaylist.Id}";
                    int[] sdr = GetEncodedSong(scalarModelPlaylist.ScalarSong, db);

                    keyValuePairs.Add(name, sdr);
                    i++;
                }

                keyValuePairsList.Add(keyValuePairs);
            }

            return keyValuePairsList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataFiles"></param>
        /// <returns></returns>
        public static Song GenerateRandomInput(List<Playlist> dataFiles)
        {
            Song song;

            int count = dataFiles.Count;

            if (count == 1)
            {
                song = SelectRandomSong(dataFiles[0]);
            }
            else
            {
                var rnd = new Random(DateTime.Now.Millisecond);
                int ticks = rnd.Next(0, count);
                song = SelectRandomSong(dataFiles[ticks]);
            }

            return song;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        public static Song SelectRandomSong(Playlist playlist)
        {
            Song song = new Song();
            int count = playlist.Songs.Count;

            var rnd = new Random(DateTime.Now.Millisecond);
            int ticks = rnd.Next(0, count);

            song = playlist.Songs[ticks];

            return song;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static int[] EncodeSingleInput(Song song, Database db)
        {
            ScalarSong scalarSong = new ScalarSong();
            
            scalarSong.Name = GetValueByID(db.SongNames, song.Name);
            scalarSong.Singer1 = GetValueByID(db.Singers, song.Singer1);
            scalarSong.Singer2 = GetValueByID(db.Singers, song.Singer2);
            scalarSong.Genre1 = GetValueByID(db.Genres, song.Genre1);
            scalarSong.Genre2 = GetValueByID(db.Genres, song.Genre2);
            scalarSong.Mood = GetValueByID(db.Moods, song.Mood);

            int[] sdr = GetEncodedSong(scalarSong, db);

            return sdr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scalarSong"></param>
        /// <param name="songEncoder"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private static int[] GetEncodedSong(ScalarSong scalarSong, Database db)
        {
            ScalarEncoder singerEncoder = GetSingerEncoder(db);
            ScalarEncoder genreEncoder = GetGenreEncoder(db);
            ScalarEncoder moodEncoder = GetMoodEncoder(db);

            int n_singer = singerEncoder.N;
            int n_genre = genreEncoder.N;
            int n_mood = moodEncoder.N;

            int[] sdr = new int[0];

            sdr = sdr.Concat(singerEncoder.Encode(scalarSong.Singer1)).ToArray();
            sdr = sdr.Concat(genreEncoder.Encode(scalarSong.Genre1)).ToArray();
            sdr = sdr.Concat(moodEncoder.Encode(scalarSong.Mood)).ToArray();

            return sdr;
        }

        /// <summary>
        /// Scalar encoder which returns object containg config 
        /// </summary>
        /// <param name="size">size of bits representing a value</param>
        /// <param name="minVal">minimum value</param>
        /// <param name="maxVal">maximum value</param>
        /// <returns></returns>
        public static ScalarEncoder GetScalarEncoder(int size, int minVal, int maxVal, string name)
        {
            int w = size;
            int n = size + (maxVal - minVal);
            ScalarEncoder scalarEncoder = new ScalarEncoder(new Dictionary<string, object>()
            {
                { "W", w},
                { "N", n},
                { "MinVal", (double)minVal},   // Min value = (0).
                { "MaxVal", (double)maxVal+1}, // Max value = (no of unique songs in DB).
                { "Periodic", false},
                { "Name", name},
                { "ClipInput", true},
           });

            return scalarEncoder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <returns></returns>
        public static ScalarEncoder GetSingerEncoder(int minVal, int maxVal)
        {
            //size is added to maxValue which should result in odd number
            //int size = ((maxVal - minVal) % 2 == 0) ? 7 : 8;
            int size = 15;

            ScalarEncoder singerEncoder = GetScalarEncoder(size, minVal, maxVal, "Singer");

            return singerEncoder;
        }

        public static ScalarEncoder GetSingerEncoder(Database db)
        {
            //size is added to maxValue which should result in odd number
            //int size = (db.Singers.Count % 2 == 0) ? 7 : 8;
            int size = 15;

            ScalarEncoder singerEncoder = GetScalarEncoder(size, SingerID - db.Singers.Count, SingerID, "Singer");

            return singerEncoder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <returns></returns>
        public static ScalarEncoder GetGenreEncoder(int minVal, int maxVal)
        {
            //size is added to maxValue which should result in odd number
            //int size = ((maxVal- minVal) % 2 == 0) ? 3 : 4;
            int size = 9;

            ScalarEncoder genreEncoder = GetScalarEncoder(size, minVal, maxVal, "Genre");

            return genreEncoder;
        }

        public static ScalarEncoder GetGenreEncoder(Database db)
        {
            //size is added to maxValue which should result in odd number
            //int size = (db.Moods.Count % 2 == 0) ? 3 : 4;
            int size = 9;

            ScalarEncoder genreEncoder = GetScalarEncoder(size, GenreID - db.Genres.Count, GenreID, "Genre");

            return genreEncoder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <returns></returns>
        public static ScalarEncoder GetMoodEncoder(int minVal, int maxVal)
        {
            //size is added to maxValue which should result in odd number
            //int size = ((maxVal - minVal) % 2 == 0) ? 9 : 10;
            int size = 13;

            ScalarEncoder moodEncoder = GetScalarEncoder(size, minVal, maxVal, "Mood");

            return moodEncoder;
        }

        public static ScalarEncoder GetMoodEncoder(Database db)
        {
            //size is added to maxValue which should result in odd number
            //int size = (db.Moods.Count % 2 == 0) ? 9 : 10;
            int size = 13;

            ScalarEncoder moodEncoder = GetScalarEncoder(size, MoodID - db.Moods.Count, MoodID, "Mood");

            return moodEncoder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static MultiEncoder GetSongEncoder(Database db)
        {
            EncoderBase singerEncoder = GetSingerEncoder(SingerID - db.Singers.Count, SingerID);
            EncoderBase genreEncoder = GetGenreEncoder(GenreID - db.Genres.Count, GenreID);
            EncoderBase moodEncoder = GetMoodEncoder(MoodID - db.Moods.Count, MoodID);

            List<EncoderBase> song = new List<EncoderBase>();
            song.Add(singerEncoder);
            song.Add(genreEncoder);
            song.Add(moodEncoder);

            MultiEncoder songEncoder = new MultiEncoder(song);

            return songEncoder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static int GetInputBits(Database db)
        {
            EncoderBase singerEncoder = GetSingerEncoder(SingerID - db.Singers.Count, SingerID);
            EncoderBase genreEncoder = GetGenreEncoder(GenreID - db.Genres.Count, GenreID);
            EncoderBase moodEncoder = GetMoodEncoder(MoodID - db.Moods.Count, MoodID);

            return (singerEncoder.N + genreEncoder.N + moodEncoder.N);
        }

        /// <summary>
        /// HTM Config for creating Connections
        /// </summary>
        /// <param name="inputBits"></param>
        /// <param name="numColumns"></param>
        /// <returns></returns>
        public static HtmConfig FetchHTMConfig(int inputBits, int numColumns)
        {
            HtmConfig cfg = new HtmConfig(new int[] { inputBits }, new int[] { numColumns })
            {
                Random = new ThreadSafeRandom(42),

                CellsPerColumn = 25,
                GlobalInhibition = true,
                LocalAreaDensity = -1,
                NumActiveColumnsPerInhArea = 0.02 * numColumns,
                PotentialRadius = (int)(0.15 * inputBits),
                //InhibitionRadius = 15,

                MaxBoost = 10.0,
                DutyCyclePeriod = 25,
                MinPctOverlapDutyCycles = 0.75,
                MaxSynapsesPerSegment = (int)(0.02 * numColumns),

                ActivationThreshold = 15,
                ConnectedPermanence = 0.5,

                // Learning is slower than forgetting in this case.
                PermanenceDecrement = 0.25,
                PermanenceIncrement = 0.15,

                // Used by punishing of segments.
                PredictedSegmentDecrement = 0.1,
                //StimulusThreshold = 5,
                //NumInputs = 82,

            };

            return cfg;
        }

    }
}
