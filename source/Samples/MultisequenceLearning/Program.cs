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

            /*
                //creating list of dataset files which contains playlist
                List<String> playlists = new List<String>();
                playlists.Add("playlist1.json");
                playlists.Add("romantic_playlist.json");
                playlists.Add("gym_playlist.json");
                playlists.Add("sporty_playlist.json");
                playlists.Add("energetic_playlist.json");

                //reading all dataset files and creating list of playlists
                Console.WriteLine("Reading playlists");
                var dataFiles = HelperMethods.GetPlaylists(playlists);
                Console.WriteLine("Reading playlists done..");
            */

            //read spotify playlist from csv file
            //string datasetfile = "spotify-streaming-top-50-world-og-min.csv";
            string datasetfile = "spotify-streaming-top-50-world-og-min-500.csv";
            Console.WriteLine($"Reading CSV File: {datasetfile}");
            var csvPlaylists = HelperMethods.ReadCSVDataset(datasetfile);
            Console.WriteLine("Reaing CSV File done...");

            // creating playlist as per Playlist model
            Console.WriteLine("Creating Playlist from CSV Data");
            var dataFiles = HelperMethods.GetPlaylists(csvPlaylists);
            Console.WriteLine("Creating Playist done...");

            // creating an object of Database which hold unique values of the attributes of Song
            Console.WriteLine("Filling Database");
            var database = HelperMethods.FillDatabase(dataFiles);
            Console.WriteLine("Filling Database done..");

            // creating scalarized objects of Playlist and Song
            Console.WriteLine("Getting Scalar Values");
            var scalarDataSet = HelperMethods.ScalarlizePlaylists(dataFiles, database);
            Console.WriteLine("Getting Scalar Values done..");

            // fetching song encoder
            Console.WriteLine("Getting Song Encoder");
            var songEncoder = HelperMethods.GetSongEncoder(database);
            Console.WriteLine("Getting Song Encoder done...");

            // encoding the playlist
            Console.WriteLine("Encoding Playlists");
            var encodedPlaylists = HelperMethods.EncodePlaylists(scalarDataSet, database);
            Console.WriteLine("Encoding Playlists done...");

            // running the experiment
            Console.WriteLine("Running Multisequence Learning experiment");
            int inputBits = HelperMethods.GetInputBits(database);
            MultiSequenceLearning multiSequenceLearning = new MultiSequenceLearning();
            var model = multiSequenceLearning.StartExperiment(encodedPlaylists, database, inputBits);
            Console.WriteLine("Running Multisequence Learning experiment done...");

            // decoding/reverse mapping the predicted values
            Console.WriteLine("Decoding Predictions");
            var predictions = multiSequenceLearning.RunPrediction(model,database, dataFiles, scalarDataSet);
            Console.WriteLine("Completed Predictions");

        }


    }
}