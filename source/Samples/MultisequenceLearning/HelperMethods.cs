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
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace SongPredection
{
    public class HelperMethods
    {
        public static string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        public static int Debug = 1;

        // inital value for scalarizing attributes
        public static int SongID = 10001;
        public static int SingerID = 1001;
        public static int GenreID = 1001;
        public static int MoodID = 101;

        public static Dictionary<string, List<Dataset>> ReadCSVDataset(string dataset)
        {
            string file = Path.Combine(BasePath, "dataset", dataset);
            Dictionary<string,List<Dataset>> result = new Dictionary<string, List<Dataset>>();
            int maxCount = 50; //since each playlist is of size 50
            int playlistCount = 0;
            int lineCount = 0;
            bool isSkipHeader = true;
            if (File.Exists(file))
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    List<Dataset> playlist = new List<Dataset>();
                    while(reader.Peek() >= 0)
                    {
                        var line = reader.ReadLine();
                        string[] values = line.Split(",");
                        lineCount++;
                        Console.WriteLine($"Line Count:{lineCount}");

                        if(isSkipHeader)
                        {
                            Console.WriteLine($"Header:\n\t{values[0]},{values[1]},{values[2]},{values[3]},{values[4]}");
                            isSkipHeader= false;
                            continue;
                        }

                        Dataset song = new Dataset();
                        song.Position = int.Parse(values[0]);
                        song.Song = values[1];
                        song.Artist = values[2];
                        song.Genre = values[3];
                        song.Mood = values[4];


                        playlist.Add(song);

                        if(song.Position >= maxCount)
                        {
                            playlistCount++;
                            result.Add(playlistCount.ToString(), playlist);
                            playlist = new List<Dataset>();
                        }
                    }
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// Reads from the json file and returns object of List<Song>
        /// </summary>
        /// <param name="dataset">full path of the dataset</param>
        /// <returns>object of List<Song></returns>
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
        /// Reads from the list of json file and returns object of List<Playlist>
        /// </summary>
        /// <param name="fileNames">List of string containing full path of dataset</param>
        /// <returns>object of List<Playlist></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csvPlaylists"></param>
        /// <returns></returns>
        public static List<Playlist> GetPlaylists(Dictionary<string, List<Dataset>> csvPlaylists)
        {
            List<Playlist> playlists = new List<Playlist>();
            int i = 0;
            foreach (List<Dataset> dataset in csvPlaylists.Values)
            {
                Playlist playlist = new Playlist();
                playlist.Name = $"P{i}";
                if (Debug >= 2)
                    Console.WriteLine($"\n\tCreating Playlist:{playlist.Name}");
                playlist.Songs = FillPlaylist(dataset);
                playlists.Add(playlist);
                i++;
            }

            return playlists;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static List<Song> FillPlaylist(List<Dataset> dataset)
        {
            List<Song> list = new List<Song>(); 
            foreach(Dataset data in dataset)
            {
                Song song = new Song();
                song.Name = data.Song;
                song.Genre1 = data.Genre;
                song.Genre2 = String.Empty;
                song.Mood = data.Mood;

                string[] singers = data.Artist.Split("&");
                if(singers.Length > 1)
                {
                    song.Singer1 = singers[0].Trim();
                    song.Singer2 = singers[1].Trim();
                }
                else
                {
                    song.Singer1 = singers[0].Trim();
                    song.Singer2 = String.Empty;
                }
                list.Add(song);
            }

            return list;
        }

        /// <summary>
        /// Decodes the key generated by prediction and reverse maps name of song from playlist
        /// </summary>
        /// <param name="predictions">predicted list from Predictor</param>
        /// <param name="dataFiles">List of Playlist</param>
        /// <returns></returns>
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
        /// Takes all the playlists and create an object of Database with attributes of Song
        /// </summary>
        /// <param name="playlists">List of Playlist</param>
        /// <returns>object of Database</returns>
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
        /// Creates scalarised list of playlist from orignal list of playlists
        /// </summary>
        /// <param name="playlists">List of Playlist</param>
        /// <param name="db">object of Database</param>
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
        /// Creates scalarised playlist from orignal playlists
        /// </summary>
        /// <param name="name">name given to scalarized playlist</param>
        /// <param name="playlist">Object of Playlist to be scalarized</param>
        /// <param name="db">Object of Database</param>
        /// <returns>object of PlaylistScalarModel</returns>
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
        /// Get value from a dictonary wrt to key
        /// </summary>
        /// <param name="keyValuePairs">Dictionary of data</param>
        /// <param name="key">key of the value to be fetched</param>
        /// <returns>value of the key in interger</returns>
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
        /// Encodes the song form List of Playlist which has been scalarized using Scalar Encoder
        /// </summary>
        /// <param name="scalarDataSet">List of Playlist which is scalarized</param>
        /// <param name="db">Database made form the List of Playlists</param>
        /// <returns>List of Playlist encoded which holds name of song and the SDR representing the song</returns>
        public static List<Dictionary<string, int[]>> EncodePlaylists(List<PlaylistScalarModel> scalarDataSet, Database db)
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
        /// Selects random song, immediate next songa and playlist name from the List of Playlist
        /// </summary>
        /// <param name="dataFiles">List of Playlist</param>
        /// <returns>randomly selected tuple of Song</returns>
        public static Tuple<Song, Song, string> GenerateRandomInput(List<Playlist> dataFiles)
        {
            Tuple<Song, Song> song;
            string playlist = String.Empty;

            int count = dataFiles.Count;

            if (count == 1)
            {
                song = SelectRandomSong(dataFiles[0]);
                playlist = dataFiles[0].Name;
            }
            else
            {
                var rnd = new Random(DateTime.Now.Millisecond);
                int ticks = rnd.Next(0, count);
                song = SelectRandomSong(dataFiles[ticks]);
                playlist = dataFiles[ticks].Name;
            }

            Tuple<Song, Song, string> songWithPlaylist = Tuple.Create<Song, Song, string>(song.Item1, song.Item2, playlist);

            return songWithPlaylist;
        }

        /// <summary>
        /// Selects random playlist to choose a song and immediate next song
        /// </summary>
        /// <param name="playlist">Playlist of the Song</param>
        /// <returns>randomly selected tuple of Song</returns>
        public static Tuple<Song, Song> SelectRandomSong(Playlist playlist)
        {
            Song song1 = new Song();
            Song song2 = new Song();
            int count = playlist.Songs.Count-1;

            var rnd = new Random(DateTime.Now.Millisecond);
            int ticks = rnd.Next(0, count);

            song1 = playlist.Songs[ticks];
            song2 = playlist.Songs[ticks+1];

            Tuple<Song, Song> tuple = Tuple.Create<Song,Song>(song1, song2);

            return tuple;
        }

        /// <summary>
        /// Encodes the Song and creates a SDR of it
        /// </summary>
        /// <param name="song">Object of Song to be encoded</param>
        /// <param name="db">Database of songs</param>
        /// <returns>SDR of the Song</returns>
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
        /// Encodes the ScalarSong and creates a SDR of it 
        /// </summary>
        /// <param name="scalarSong">Object of ScalarSong to be encoded</param>
        /// <param name="db">Database of songs</param>
        /// <returns>SDR of the ScalarSong</returns>
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
        /// Scalar encoder which returns object containing config 
        /// </summary>
        /// <param name="size">size of bits representing a value</param>
        /// <param name="minVal">minimum value</param>
        /// <param name="maxVal">maximum value</param>
        /// <returns>Object of ScalarEnocder</returns>
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
        /// Creates an ScalarEncoder for Singer of Song
        /// </summary>
        /// <param name="minVal">minimum value</param>
        /// <param name="maxVal">maximum value</param>
        /// <returns>object of ScalarEncoder</returns>
        public static ScalarEncoder GetSingerEncoder(int minVal, int maxVal)
        {
            //size is added to maxValue which should result in odd number
            //int size = ((maxVal - minVal) % 2 == 0) ? 7 : 8;
            int size = 15;

            ScalarEncoder singerEncoder = GetScalarEncoder(size, minVal, maxVal, "Singer");

            return singerEncoder;
        }

        /// <summary>
        /// Creates an ScalarEncoder for Singer of Song
        /// </summary>
        /// <param name="db">Database of Song</param>
        /// <returns>object of ScalarEncoder</returns>
        public static ScalarEncoder GetSingerEncoder(Database db)
        {
            //size is added to maxValue which should result in odd number
            //int size = (db.Singers.Count % 2 == 0) ? 7 : 8;
            int size = 15;

            ScalarEncoder singerEncoder = GetScalarEncoder(size, SingerID - db.Singers.Count, SingerID, "Singer");

            return singerEncoder;
        }

        /// <summary>
        /// Creates an ScalarEncoder for Genre of Song
        /// </summary>
        /// <param name="minVal">minimum value</param>
        /// <param name="maxVal">maximum value</param>
        /// <returns>object of ScalarEncoder</returns>
        public static ScalarEncoder GetGenreEncoder(int minVal, int maxVal)
        {
            //size is added to maxValue which should result in odd number
            //int size = ((maxVal- minVal) % 2 == 0) ? 3 : 4;
            int size = 9;

            ScalarEncoder genreEncoder = GetScalarEncoder(size, minVal, maxVal, "Genre");

            return genreEncoder;
        }

        /// <summary>
        /// Creates an ScalarEncoder for Genre of Song
        /// </summary>
        /// <param name="db">Database of Song</param>
        /// <returns>object of ScalarEncoder</returns>
        public static ScalarEncoder GetGenreEncoder(Database db)
        {
            //size is added to maxValue which should result in odd number
            //int size = (db.Moods.Count % 2 == 0) ? 3 : 4;
            int size = 9;

            ScalarEncoder genreEncoder = GetScalarEncoder(size, GenreID - db.Genres.Count, GenreID, "Genre");

            return genreEncoder;
        }

        /// <summary>
        /// Creates an ScalarEncoder for Mood of Song
        /// </summary>
        /// <param name="minVal">minimum value</param>
        /// <param name="maxVal">maximum value</param>
        /// <returns>object of ScalarEncoder</returns>
        public static ScalarEncoder GetMoodEncoder(int minVal, int maxVal)
        {
            //size is added to maxValue which should result in odd number
            //int size = ((maxVal - minVal) % 2 == 0) ? 9 : 10;
            int size = 13;

            ScalarEncoder moodEncoder = GetScalarEncoder(size, minVal, maxVal, "Mood");

            return moodEncoder;
        }

        /// <summary>
        /// Creates an ScalarEncoder for Mood of Song
        /// </summary>
        /// <param name="db">Database of Song</param>
        /// <returns>object of ScalarEncoder</returns>
        public static ScalarEncoder GetMoodEncoder(Database db)
        {
            //size is added to maxValue which should result in odd number
            //int size = (db.Moods.Count % 2 == 0) ? 9 : 10;
            int size = 13;

            ScalarEncoder moodEncoder = GetScalarEncoder(size, MoodID - db.Moods.Count, MoodID, "Mood");

            return moodEncoder;
        }

        /// <summary>
        /// Creates a MultiEncoder from the Database
        /// </summary>
        /// <param name="db">Database of the songs</param>
        /// <returns>Object of MultiEncoder</returns>
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
        /// Calculates the value of the input bit in a SDR
        /// </summary>
        /// <param name="db">Database of the songs</param>
        /// <returns>value of input bits of SDR</returns>
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
        /// <returns>Object of HtmConfig</returns>
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
