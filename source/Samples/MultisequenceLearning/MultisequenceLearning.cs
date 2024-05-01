using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoCortexApi;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using SongPredection;
using SongPredection.Model;

namespace SongPredection
{
    public class MultiSequenceLearning
    {

        public double PredictionAccuracy { get; set; }   // Accuracy of the predicted sequence from the model
        public List<double> Accuracy { get; set; }       // Accuracy of the model while learning sequence
        public List<Dictionary<string, string>>? UserPredictedValues { get; set; }
        public long ElapsedTime { get; set; }
        public string OutputPath { get; set; }
        public string RandomUserInput { get; set; }

        public MultiSequenceLearning()
        {
            PredictionAccuracy = 0.0;
            Accuracy = new List<double>();
            UserPredictedValues = new List<Dictionary<string, string>>();
            OutputPath = "";
            RandomUserInput = "";
        }

        /// <summary>
        /// Starts the MultiSequenceLearning Experiment
        /// </summary>
        /// <param name="dataset">local full path for input dataset</param>
        public HtmPredictionEngine StartExperiment(List<Dictionary<string, int[]>> sequences, Database db, int inputBits)
        {
            int maxCycles = 5;
            int numColumns = 2048;

            MultiEncoder encoder = HelperMethods.GetSongEncoder(db);

            Stopwatch sw = new Stopwatch();

            sw.Start();

            HtmPredictionEngine trainedEngine = RunTraining(inputBits, maxCycles, numColumns, sequences, encoder);

            //RunPrediction(trainedEngine);
            sw.Stop();

            ElapsedTime = (sw.ElapsedMilliseconds) / 1000; //milliseconds to seconds

            return trainedEngine;
        }



        /// <summary>
        /// Getting trained model by MultiSequence Learning
        /// </summary>
        /// <param name="inputBits"></param>
        /// <param name="maxCycles"></param>
        /// <param name="numColumns"></param>
        /// <param name="encodedData"></param>
        private HtmPredictionEngine RunTraining(int inputBits, int maxCycles, int numColumns, List<Dictionary<string, int[]>> encodedData, EncoderBase encoder)
        {
            /*
             * Running MultiSequence Learning experiment here
             */
            var trainedHTMmodel = Run(inputBits, maxCycles, numColumns, encoder, encodedData);

            return trainedHTMmodel;
        }

        /// <summary>
        /// Takes user input and gives predicted label
        /// </summary>
        /// <param name="trainedEngine">trained object of class HtmPredictionEngine which will be used to predict</param>
        /// <param name="datafiles"></param>
        /// <param name="db"></param>
        /// <param name="listOfScalarPlaylist"></param>
        public List<Dictionary<string,string>> RunPrediction(HtmPredictionEngine trainedEngine, Database db, List<Playlist> datafiles, List<PlaylistScalarModel> listOfScalarPlaylist)
        {
            var logs = new List<String>();
            List<Tuple<Song,Song, string>> songs = new List<Tuple<Song, Song, string>>();
            List<Dictionary<string, string>> predictedValues = new List<Dictionary<string, string>>();

            //Random generated user song;
            Console.WriteLine("Genrating random user data input...");
            for(int i = 0; i<30; i++)
            {
                Tuple<Song, Song, string> userInput = HelperMethods.GenerateRandomInput(datafiles);

                if (!songs.Contains(userInput))
                    songs.Add(userInput);
                else
                    i--;
            }

            int totalPrediction = 0;
            int matchedPredictions = 0;
            int noPredictions = 0;
            double accuracy = 0.0;

            Console.WriteLine("Predicting as per inputs:");
            foreach(Tuple<Song, Song, string> userInput in songs)
            {
                Dictionary<string, string> pVal = new Dictionary<string, string>();

                Console.WriteLine($"Random User Input : {userInput.Item1.Name.ToString()}");
                logs.Add($"Random User Input : {userInput.Item1.Name.ToString()}");
                int[] sdr = HelperMethods.EncodeSingleInput(userInput.Item1, db);
                trainedEngine.Reset();
                var predictedValuesForUserInput = trainedEngine.Predict(sdr);
                if (predictedValuesForUserInput.Count > 0)
                {
                    int i = 0;
                    foreach (var predictedVal in predictedValuesForUserInput)
                    {
                        i++;
                        var playlist = predictedVal.PredictedInput.Split('_').First();
                        var song = predictedVal.PredictedInput.Split('-').Last();
                        
                        // decode the key predicted and get the playlist and song
                        PlaylistScalarModel predictedScalarPlaylist = HelperMethods.GetScalarPlaylist(listOfScalarPlaylist, playlist);
                        ScalarModel predictedScalarModel = HelperMethods.GetScalarSongByID(predictedScalarPlaylist, Int32.Parse(song[1..]));

                        // decoded playlist with datafiles reference
                        Playlist predictedPlaylist = datafiles[Int32.Parse(predictedScalarPlaylist.Name[1..])];
                        Song predictedSong = predictedScalarModel.Song;

                        pVal.Add($"{i}. Predicted: {predictedPlaylist.Name}-{predictedSong.Name}", $"Playlist: {userInput.Item3} Input: {userInput.Item1.Name} - Actual: {userInput.Item2.Name}");
                        predictedValues?.Add(pVal);
                        
                        Console.WriteLine($"SIMILARITY: {predictedVal.Similarity} PREDICTED VALUE: {playlist}-{song} Decoded PREDICTED VALUE: {predictedPlaylist.Name}-{predictedSong.Name} Actual VALUE: {userInput.Item3}-{userInput.Item2.Name}");
                        logs.Add($"SIMILARITY: {predictedVal.Similarity} PREDICTED VALUE: {playlist}-{song} Decoded PREDICTED VALUE: {predictedPlaylist.Name}-{predictedSong.Name} Actual VALUE: {userInput.Item3}-{userInput.Item2.Name}");

                        if(predictedSong.Name == userInput.Item2.Name)
                        {
                            matchedPredictions++;
                            Console.WriteLine($"{i} Perfect match for predicted song!");
                            logs.Add($"{i} Perfect match for predicted song!");
                            break;
                        }

                    }
                    totalPrediction++;
                }
                else
                {
                    Console.WriteLine("Nothing predicted :(");
                    logs.Add("Nothing predicted :(");
                    noPredictions++;
                    //totalPrediction++;
                }
            }

            /*
             * Accuracy is calculated as number of matching predictions made 
             * divided by total number of prediction made for an element of a sequence
             * 
             * accuracy = number of matching predictions/total number of prediction * 100
             */

            accuracy = (double)matchedPredictions / totalPrediction * 100;

            Console.WriteLine($"Prediction Accuracy: {accuracy}");
            logs.Add($"Prediction Accuracy: {accuracy}");
            
            File.AppendAllLines(OutputPath, logs);

            UserPredictedValues = predictedValues;
            PredictionAccuracy = accuracy;

            return UserPredictedValues;

        }

        /// <summary>
        /// Multi-sequence Learning MOdel is trained here
        /// </summary>
        /// <param name="inputBits"></param>
        /// <param name="maxCycles"></param>
        /// <param name="numColumns"></param>
        /// <param name="encoder"></param>
        /// <param name="sequences">Multi Sequence for training</param>
        /// <returns>Learned CortexLayer and HtmClassifier for prediction</returns>
        //public Dictionary<CortexLayer<object,object>, HtmClassifier<string, ComputeCycle>> Run(int inputBits, int maxCycles, int numColumns, EncoderBase encoder, List<Dictionary<string,int[]>> sequences)
        public HtmPredictionEngine Run(int inputBits, int maxCycles, int numColumns, EncoderBase encoder, List<Dictionary<string, int[]>> sequences)
        {
            /* HTM Config */
            var htmConfig = HelperMethods.FetchHTMConfig(inputBits, numColumns);

            /* Creating Connections */
            var mem = new Connections(htmConfig);

            /* Getting HTM CLassifier */
            HtmClassifier<string, ComputeCycle> cls = new HtmClassifier<string, ComputeCycle>();

            /* Get Cortex Layer */
            CortexLayer<object, object> layer1 = new CortexLayer<object, object>("L1");

            /* HPA Stable Flag */
            bool isInStableState = false;

            /* Learn Flag */
            bool learn = true;

            /* Number of new born cycles */
            int newbornCycle = 0;

            /* Logs */
            var OUTPUT_LOG_LIST = new List<Dictionary<int, string>>();
            var OUTPUT_LOG = new Dictionary<int, string>();
            var OUTPUT_trainingAccuracy_graph = new List<Dictionary<int, double>>();

            /* Minimum Cycles */
            int numUniqueInputs = GetNumberOfInputs(sequences);

            // For more information see following paper: https://www.scitepress.org/Papers/2021/103142/103142.pdf
            HomeostaticPlasticityController hpc = new HomeostaticPlasticityController(mem, numUniqueInputs, (isStable, numPatterns, actColAvg, seenInputs) =>
            {
                if (isStable)
                    // Event should be fired when entering the stable state.
                    Debug.WriteLine($"STABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");
                else
                    // Ideal SP should never enter unstable state after stable state.
                    Debug.WriteLine($"INSTABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");

                // We are not learning in instable state.
                learn = isInStableState = isStable;

            }, numOfCyclesToWaitOnChange: 50);


            /* Spatial Pooler with HomeoPlasticityController using Connections */
            SpatialPoolerMT sp = new SpatialPoolerMT();
            sp.Init(mem);

            /* Temporal Memory with Connections */
            TemporalMemory tm = new TemporalMemory();
            tm.Init(mem);

            /* Adding Encoder to Cortex Layer */
            //layer1.HtmModules.Add("encoder", encoder); /* not needed since encoded already */

            /* Adding Spatial Pooler to Cortex Layer */
            layer1.HtmModules.Add("sp", sp);

            // Container for Previous Active Columns
            int[] prevActiveCols = new int[0];

            int computeCycle = 0;
            int maxComputeCycles = maxCycles;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            /*
             * Training SP to get stable. New-born stage.
             */

            /* Stable Condition Loop --- Loop 1 */
            for (int i = 0; i < maxComputeCycles && isInStableState == false; i++)
            {
                computeCycle++;
                newbornCycle++;
                Debug.WriteLine($"-------------- Newborn Cycle {newbornCycle} ---------------");
                Console.WriteLine($"-------------- Training SP Newborn Cycle {newbornCycle} ---------------");

                /* For each sequence in multi-sequence --- Loop 2 */
                foreach (var sequence in sequences)
                {
                    /* For each element (dictionary) in sequence --- Loop 3 */
                    foreach (var element in sequence)
                    {
                        var observationClass = element.Key; // OBSERVATION LABEL or SEQUENCE LABEL
                        var elementSDR = element.Value; // ELEMENT IN GiVEN SEQUENCE

                        Debug.WriteLine($"-------------- {observationClass} ---------------");

                        var lyrOut = layer1.Compute(elementSDR, true);     /* CORTEX LAYER OUTPUT with elementSDR as INPUT and LEARN = TRUE */
                        //var lyrOut = layer1.Compute(elementSDR, learn);    /* CORTEX LAYER OUTPUT with elementSDR as INPUT and LEARN = if TRUE */

                        if (isInStableState)
                            break;
                    }

                    if (isInStableState)
                        break;
                }
            }

            int sequenceCounter = 0;
            // Clear all learned patterns in the classifier.
            //cls.ClearState();

            // We activate here the Temporal Memory algorithm.
            layer1.HtmModules.Add("tm", tm);

            //initial value which will never occur 
            var lastPredictedValue = new List<string>(new string[] { "10000" }); ;
            List<string> lastPredictedValueList = new List<string>();
            double lastCycleAccuracy = 0;
            double accuracy = 0;

            List<List<string>> possibleSequence = new List<List<string>>();

            /* Training SP+TM together */
            /* For each sequence in multi-sequence --- Loop 1 */
            foreach (var sequence in sequences)
            {
                int SequencesMatchCount = 0; // NUMBER OF MATCHES
                var tempLOGFILE = new Dictionary<int, string>();
                var tempLOGGRAPH = new Dictionary<int, double>();
                double SaturatedAccuracyCount = 0;

                int maxPrevInputs = sequence.Count;
                List<string> prevInputs = new List<string>();

                prevInputs.Add("SX");
                sequenceCounter++;
                Console.WriteLine($"-------------- Training Sequence Number: {sequenceCounter} - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                /* Loop until maxCycles --- Loop 2*/
                for (int i = 0; i < maxCycles; i++)
                {
                    Console.WriteLine($"-------------- Training SP+TM Newborn Cycle {i} ---------------");
                    List<string> ElementWiseClasses = new List<string>();

                    /* Element in sequenc match counter */
                    int elementMatches = 0;

                    /* For each element (dictionary) in sequence --- Loop 3 */
                    foreach (var Elements in sequence)
                    {

                        // key,value = PlaylistNo-SongNo,SDR(song) 
                        var observationLabel = Elements.Key;

                        var lyrOut = new ComputeCycle();

                        /* Get Compute Cycle */
                        //Compute(key)  <= this should be correct
                        lyrOut = layer1.Compute(Elements.Value, learn) as ComputeCycle;
                        Debug.WriteLine(string.Join(',', lyrOut.ActivColumnIndicies));

                        prevInputs.Add(observationLabel.Split('-')[1]);
                        if (prevInputs.Count > (maxPrevInputs + 1))
                            prevInputs.RemoveAt(0);

                        // In the pretrained SP with HPC, the TM will quickly learn cells for patterns
                        // In that case the starting sequence 4-5-6 might have the sam SDR as 1-2-3-4-5-6,
                        // Which will result in returning of 4-5-6 instead of 1-2-3-4-5-6.
                        // HtmClassifier allways return the first matching sequence. Because 4-5-6 will be as first
                        // memorized, it will match as the first one.
                        if (prevInputs.Count < maxPrevInputs)
                            continue;

                        string key = GetKey(prevInputs, observationLabel);

                        /* Get Active Cells */
                        List<Cell> actCells = (lyrOut.ActiveCells.Count == lyrOut.WinnerCells.Count) ? lyrOut.ActiveCells : lyrOut.WinnerCells;

                        /* Learn the combination of Label and Active Cells   key = PlaylistNo-SongNo */
                        cls.Learn(key, actCells.ToArray());

                        if (lastPredictedValue.Contains(key))
                        {
                            elementMatches++;
                            Debug.WriteLine($"Match. Actual value: {key} - Predicted value: {lastPredictedValue.FirstOrDefault(key)}");
                        }
                        else
                        {
                            Debug.WriteLine($"Mismatch! Actual value: {key} - Predicted values: {String.Join(',', lastPredictedValue)}");
                        }

                        Debug.WriteLine($"Col  SDR: {Helpers.StringifyVector(lyrOut.ActivColumnIndicies)}");
                        Debug.WriteLine($"Cell SDR: {Helpers.StringifyVector(actCells.Select(c => c.Index).ToArray())}");

                        if (learn == false)
                            Debug.WriteLine($"Inference mode");

                        if (lyrOut.PredictiveCells.Count > 0)
                        {
                            var predictedInputValue = cls.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 3);

                            Debug.WriteLine($"Current Input: {key}");
                            Debug.WriteLine("The predictions with similarity greater than 50% are");

                            foreach (var t in predictedInputValue)
                            {

                                if (t.Similarity >= (double)50.00)
                                {
                                    Debug.WriteLine($"Predicted Input: {string.Join(", ", t.PredictedInput)},\tSimilarity Percentage: {string.Join(", ", t.Similarity)}, \tNumber of Same Bits: {string.Join(", ", t.NumOfSameBits)}");
                                }
                            }

                            lastPredictedValue = predictedInputValue.Select(v => v.PredictedInput).ToList();

                        }
                        else
                        {
                            Debug.WriteLine($"NO CELLS PREDICTED for next cycle.");
                            lastPredictedValue = new List<string>();
                        }
                    }

                    double maxPossibleAccuraccy = (double)((double)sequence.Count - 1) / (double)sequence.Count * 100.0;

                    accuracy = (double)elementMatches / (double)sequence.Count * 100.0;

                    Debug.WriteLine($"Cycle : {i} \t Accuracy:{accuracy}");
                    tempLOGGRAPH.Add(i, accuracy);
                    if (accuracy >= maxPossibleAccuraccy)
                    {
                        SequencesMatchCount++;
                        Debug.WriteLine($"100% accuracy reached {SequencesMatchCount} times.");
                        Console.WriteLine($"100% accuracy reached {SequencesMatchCount} times.");
                        tempLOGFILE.Add(i, $"Cycle : {i} \t  Accuracy:{accuracy} as 100% \t Number of times repeated {SequencesMatchCount}");
                        Accuracy.Add(accuracy);
                        if (SequencesMatchCount >= 30)
                        {
                            SaturatedAccuracyCount++;
                            tempLOGFILE.Add(i, $"Cycle : {i} \t  SaturatedAccuracyCount : {SaturatedAccuracyCount} \t SequenceMatchCount : {SequencesMatchCount} >= 30 breaking..");
                            break;
                        }
                    }
                    else if (SequencesMatchCount >= 0)
                    {
                        SaturatedAccuracyCount = 0;
                        SequencesMatchCount = 0;
                        lastCycleAccuracy = accuracy;
                        tempLOGFILE.Add(i, $"Cycle : {i} \t Accuracy :{accuracy} \t ");
                        Accuracy.Add(accuracy);
                    }
                    lastPredictedValueList.Clear();

                }

                tm.Reset(mem);
                learn = true;
                OUTPUT_LOG_LIST.Add(tempLOGFILE);

            }

            sw.Stop();

            TimeSpan timeSpan = sw.Elapsed;

            //****************DISPLAY STATUS OF EXPERIMENT
            Debug.WriteLine("-------------------TRAINING END------------------------");
            Console.WriteLine("-----------------TRAINING END------------------------");
            string timespend = $"Training Time : {timeSpan.ToString(@"d.hh\:mm\:ss")}";
            Console.WriteLine(timespend);
            Debug.WriteLine("-------------------WRTING TRAINING OUTPUT LOGS---------------------");
            Console.WriteLine("-------------------WRTING TRAINING OUTPUT LOGS------------------------");
            //*****************

            var reportsDir = Path.Combine(HelperMethods.BasePath, "reports");
            if (!Directory.Exists(reportsDir))
                Directory.CreateDirectory(reportsDir);

            DateTime now = DateTime.Now;
            string filename = now.ToString("g");
            // remove any / or : or -
            filename = filename.Replace("/", "");
            filename = filename.Replace("-", "");
            filename = filename.Replace(":", "");
            filename = $"SongPrediction_{filename.Split(" ")[0]}_{now.Ticks.ToString()}.txt";
            string path = Path.Combine(HelperMethods.BasePath, "reports", filename);
            OutputPath = path;
            using (StreamWriter swOutput = File.CreateText(OutputPath))
            {
                swOutput.WriteLine($"{filename}");
                foreach (var SequencelogCycle in OUTPUT_LOG_LIST)
                {
                    swOutput.WriteLine("******Sequence Starting*****");
                    foreach (var cycleOutPutLog in SequencelogCycle)
                    {
                        swOutput.WriteLine(cycleOutPutLog.Value, true);
                    }
                    swOutput.WriteLine("****Sequence Ending*****");

                }
            }
            File.AppendAllText(OutputPath, $"{timespend} \n");
            Debug.WriteLine("-------------------TRAINING LOGS HAS BEEN CREATED---------------------");
            Console.WriteLine("-------------------TRAINING LOGS HAS BEEN CREATED------------------------");

            return new HtmPredictionEngine { Layer = layer1, Connections = mem, Classifier = cls };
        }

        private string GetKey(List<string> prevInputs, string observationLabel)
        {
            string playlistName = observationLabel.Split('-')[0];
            string key = String.Empty;
            for (int i = 0; i < prevInputs.Count; i++)
            {
                if (i > 0)
                    key += "-";

                key += prevInputs[i];
            }

            return $"{playlistName}_{key}";
        }

        /// <summary>
        /// Gets the number of all unique inputs.
        /// </summary>
        /// <param name="sequences">Alle sequences.</param>
        /// <returns></returns>
        private int GetNumberOfInputs(List<Dictionary<string, int[]>> sequences)
        {
            int num = 0;

            foreach (var sequence in sequences)
            {
                num += sequence.Count;
            }

            return num;
        }


    }
    public class HtmPredictionEngine
    {
        public void Reset()
        {
            var tm = this.Layer.HtmModules.FirstOrDefault(m => m.Value is TemporalMemory);
            ((TemporalMemory)tm.Value).Reset(this.Connections);
        }
        public List<ClassifierResult<string>> Predict(int[] input)
        {
            var lyrOut = this.Layer.Compute(input, false) as ComputeCycle;

            List<ClassifierResult<string>> predictedInputValues = this.Classifier.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 3);

            return predictedInputValues;
        }

        public Connections Connections { get; set; }

        public CortexLayer<object, object> Layer { get; set; }

        public HtmClassifier<string, ComputeCycle> Classifier { get; set; }
    }

}