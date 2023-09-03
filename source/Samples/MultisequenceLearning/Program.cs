using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SongPredection;

namespace MultisequenceLearning
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Song Prediction experiment...");

            //crating list of files which contains playlist
            List<String> playlists = new List<String>();
            playlists.Add("playlist1.json");
            playlists.Add("romantic_playlist.json");
            playlists.Add("gym_playlist.json");
            playlists.Add("sporty_playlist.json");
            playlists.Add("energetic_playlist.json");

            //reading all files and creating list of playlists
            Console.WriteLine("Reading playlists");
            var dataFiles = HelperMethods.GetPlaylists(playlists);
            Console.WriteLine("Reading playlists done..");

            Console.WriteLine("Filling Database");
            var database = HelperMethods.FillDatabase(dataFiles);
            Console.WriteLine("Filling Database done..");

            Console.WriteLine("Getting Scalar Values");
            var scalarDataSet = HelperMethods.ScalarlizePlaylists(dataFiles, database);
            Console.WriteLine("Getting Scalar Values done..");

            Console.WriteLine("Getting Song Encoder");
            var songEncoder = HelperMethods.GetSongEncoder(database);
            Console.WriteLine("Getting Song Encoder done...");

            Console.WriteLine("Encoding Playlists");
            var encodedPlaylists = HelperMethods.EncodePlaylists(scalarDataSet, songEncoder, database);
            Console.WriteLine("Encoding Playlists done...");

            Console.WriteLine("Running Multisequence Learning experiment");
            int inputBits = HelperMethods.GetInputBits(database);
            MultiSequenceLearning multiSequenceLearning = new MultiSequenceLearning();
            var model = multiSequenceLearning.StartExperiment(encodedPlaylists, database, inputBits);
            Console.WriteLine("Running Multisequence Learning experiment done...");

            Console.WriteLine("Decoding Predictions");
            var predictions = multiSequenceLearning.RunPrediction(model,database, dataFiles);
            var decodedPredictions = HelperMethods.DecodePrediction(predictions, dataFiles);
            Console.WriteLine("Completed Predictions");

            if(decodedPredictions.Count>0)
            {
                foreach(var item in decodedPredictions)
                {
                    Console.WriteLine($"Song: {item.Split('-')[1]}");
                }
            }
            else
            {
                Console.WriteLine("Nothing to predict");
            }


        }


    }
}