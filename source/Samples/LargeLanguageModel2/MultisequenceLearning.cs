using NeoCortexApi;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;


namespace LargeLanguageModel2
{
    /// <summary>
    /// Implements an experiment that demonstrates how to learn sequences.
    /// </summary>
    public class MultiSequenceLearning
    {
        public string OutputPath { get; set; }

        /// <summary>
        /// Runs the learning of sequences and create a trained model
        /// </summary>
        /// <param name="multiSequences"></param>
        /// <param name="db"></param>
        /// <param name="inputBits"></param>
        /// <returns>Object of Predictor class which is trained model</returns>
        public Predictor Run(List<EncodedMultisequence> multiSequences, Token db, int inputBits)
        {
            Console.WriteLine($"Hello NeocortexApi! Experiment {nameof(MultiSequenceLearning)}");

            int numColumns = 1024;

            HtmConfig cfg = GetHtmConfig(inputBits, numColumns);


            return RunExperiment(inputBits, cfg, multiSequences);
        }

        /// <summary>
        /// Runs the Multisequence Learning algorithm for the experiment
        /// </summary>
        /// <param name="inputBits"></param>
        /// <param name="cfg"></param>
        /// <param name="multiSequences"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private Predictor RunExperiment(int inputBits, HtmConfig cfg, List<EncodedMultisequence> multiSequences)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int maxMatchCnt = 0;

            var mem = new Connections(cfg);

            bool isInStableState = false;

            HtmClassifier<string, ComputeCycle> cls = new HtmClassifier<string, ComputeCycle>();

            var numUniqueInputs = GetNumberOfInputs(multiSequences);

            CortexLayer<object, object> layer1 = new CortexLayer<object, object>("L1");

            TemporalMemory tm = new TemporalMemory();

            // For more information see following paper: https://www.scitepress.org/Papers/2021/103142/103142.pdf
            HomeostaticPlasticityController hpc = new HomeostaticPlasticityController(mem, numUniqueInputs * 150, (isStable, numPatterns, actColAvg, seenInputs) =>
            {
                if (isStable)
                    // Event should be fired when entering the stable state.
                    Debug.WriteLine($"STABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");
                else
                    // Ideal SP should never enter unstable state after stable state.
                    Debug.WriteLine($"INSTABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");

                // We are not learning in instable state.
                isInStableState = isStable;

                // Clear active and predictive cells.
                //tm.Reset(mem);
            }, numOfCyclesToWaitOnChange: 50);


            SpatialPoolerMT sp = new SpatialPoolerMT(hpc);
            sp.Init(mem);
            tm.Init(mem);

            // Please note that we do not add here TM in the layer.
            // This is omitted for practical reasons, because we first eneter the newborn-stage of the algorithm
            // In this stage we want that SP get boosted and see all elements before we start learning with TM.
            // All would also work fine with TM in layer, but it would work much slower.
            // So, to improve the speed of experiment, we first ommit the TM and then after the newborn-stage we add it to the layer.
            /* already encoeded data so no need to encode again */
            layer1.HtmModules.Add("sp", sp);

            //file for saving logs
            string logFile = GetLogFile();
            OutputPath = logFile;

            //list for logs
            List<string> logs = new List<string>();

            //double[] inputs = inputValues.ToArray();
            int[] prevActiveCols = new int[0];

            int cycle = 0;
            int matches = 0;
            int maxCycles = 300;
            int sequenceCounter = 0;
            int totalSequences = multiSequences.Count();


            //
            // Training SP to get stable. New-born stage.
            //

            using (StreamWriter writeLogs = new StreamWriter(logFile))
            {
                writeLogs.WriteLine($"-------------- Number of sequences: {totalSequences}---------------");
                writeLogs.WriteLine($"-------------- Training SP Starts - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                Console.WriteLine($"-------------- Training SP Starts - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                Debug.WriteLine($"-------------- Training SP Starts - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                for (int i = 0; i < maxCycles && isInStableState == false; i++)
                {
                    cycle++;

                    writeLogs.WriteLine($"-------------- Training SP Newborn Cycle {cycle} - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                    Debug.WriteLine($"-------------- Training SP Newborn Cycle {cycle} - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                    Console.WriteLine($"-------------- Training SP Newborn Cycle {cycle} - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");

                    foreach (var sequences in multiSequences)
                    {
                        foreach (var input in sequences.EncodedSequences)
                        {
                            Debug.WriteLine($"-- Sequence: {sequences.Name} - Input: {string.Join("", input.SubSequence.ToArray())} --");

                            var lyrOut = layer1.Compute(input.SDR, true);

                            if (isInStableState)
                                break;
                        }

                        if (isInStableState)
                            break;
                    }
                }
                writeLogs.WriteLine($"-------------- Training SP Done - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                Console.WriteLine($"-------------- Training SP Done - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                Debug.WriteLine($"-------------- Training SP Done - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                // Clear all learned patterns in the classifier.
                cls.ClearState();

                // We activate here the Temporal Memory algorithm.
                layer1.HtmModules.Add("tm", tm);

                cycle = 0;
                var lastPredictedValues = new List<string>(new string[] { "0" });
                

                writeLogs.WriteLine($"-------------- Training SP+TM Starts - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                Console.WriteLine($"-------------- Training SP+TM Starts - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                Debug.WriteLine($"-------------- Training SP+TM Starts - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                //
                // Loop over all sequences.
                foreach (var sequences in multiSequences)
                {
                    sequenceCounter++;
                    writeLogs.WriteLine($"-------------- Training Sequence Number: {sequenceCounter} of {totalSequences} Name: {sequences.Name} - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                    Debug.WriteLine($"-------------- Training Sequence Number: {sequenceCounter} of {totalSequences} Name: {sequences.Name} - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                    Console.WriteLine($"-------------- Training Sequence Number: {sequenceCounter} of {totalSequences} Name: {sequences.Name} - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");

                    int maxPrevInputs = sequences.EncodedSequences.Count - 1;

                    List<string> previousInputs = new List<string>();

                    previousInputs.Add("-1.0");

                    // Set on true if the system has learned the sequence with a maximum acurracy.
                    bool isLearningCompleted = false;

                    cycle = 0;

                    //
                    // Now training with SP+TM. SP is pretrained on the given input pattern set.
                    for (int i = 0; i < maxCycles; i++)
                    {
                        matches = 0;

                        cycle++;

                        Debug.WriteLine("");

                        writeLogs.WriteLine($"-------------- Training SP+TM Newborn Cycle {cycle} ---------------");
                        Debug.WriteLine($"-------------- Training SP+TM Newborn Cycle {cycle} ---------------");
                        Console.WriteLine($"-------------- Training SP+TM Newborn Cycle {cycle} ---------------");
                        Debug.WriteLine("");

                        foreach (var input in sequences.EncodedSequences)
                        {
                            Debug.WriteLine($"-- SubSequence: {sequences.Name} - Input: {string.Join("", input.SubSequence)} --");
                            writeLogs.WriteLine($"-- SubSequence: {sequences.Name} - Input: {string.Join("", input.SubSequence)} --");

                            var lyrOut = layer1.Compute(input.SDR, true) as ComputeCycle;

                            var activeColumns = layer1.GetResult("sp") as int[];

                            previousInputs.Add(input.EncodedSubSequence.Last().ToString());
                            if (previousInputs.Count > (maxPrevInputs + 1))
                                previousInputs.RemoveAt(0);

                            // In the pretrained SP with HPC, the TM will quickly learn cells for patterns
                            // In that case the starting sequence 4-5-6 might have the sam SDR as 1-2-3-4-5-6,
                            // Which will result in returning of 4-5-6 instead of 1-2-3-4-5-6.
                            // HtmClassifier allways return the first matching sequence. Because 4-5-6 will be as first
                            // memorized, it will match as the first one.
                            if (previousInputs.Count < maxPrevInputs)
                                continue;

                            string key = GetKey(previousInputs);

                            /* Get Active Cells */
                            List<Cell> actCells = (lyrOut.ActiveCells.Count == lyrOut.WinnerCells.Count) ? lyrOut.ActiveCells : lyrOut.WinnerCells;

                            /* Learn the combination of key and Active Cells   key = Sequence of char encoded as number */
                            cls.Learn(key, actCells.ToArray());

                            //Debug.WriteLine($"Col  SDR: {Helpers.StringifyVector(lyrOut.ActivColumnIndicies)}");
                            //Debug.WriteLine($"Cell SDR: {Helpers.StringifyVector(actCells.Select(c => c.Index).ToArray())}");

                            //
                            // If the list of predicted values from the previous step contains the currently presenting value,
                            // we have a match.
                            if (lastPredictedValues.Contains(key))
                            {
                                matches++;
                                Debug.WriteLine($"Match. Actual value: {key} - Predicted value: {lastPredictedValues.FirstOrDefault(key)}.");
                            }
                            else
                                Debug.WriteLine($"Missmatch! Actual value: {key} - Predicted values: {String.Join(',', lastPredictedValues)}");

                            if (lyrOut.PredictiveCells.Count > 0)
                            {
                                //var predictedInputValue = cls.GetPredictedInputValue(lyrOut.PredictiveCells.ToArray());
                                var predictedInputValues = cls.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 3);

                                foreach (var item in predictedInputValues)
                                {
                                    Debug.WriteLine($"Current Input: {string.Join('-', input.EncodedSubSequence)} \t| Predicted Input: {item.PredictedInput} - {item.Similarity}");
                                    writeLogs.WriteLine($"Current Input: {string.Join('-', input.EncodedSubSequence)} \t| Predicted Input: {item.PredictedInput} - {item.Similarity}");
                                }

                                lastPredictedValues = predictedInputValues.Select(v => v.PredictedInput).ToList();
                            }
                            else
                            {
                                Debug.WriteLine($"NO CELLS PREDICTED for next cycle.");
                                writeLogs.WriteLine($"NO CELLS PREDICTED for next cycle.");
                                lastPredictedValues = new List<string>();
                            }
                        }

                        // The first element (a single element) in the sequence cannot be predicted
                        double maxPossibleAccuraccy = (double)((double)sequences.EncodedSequences.Count - 1) / (double)sequences.EncodedSequences.Count * 100.0;

                        double accuracy = (double)matches / (double)sequences.EncodedSequences.Count * 100.0;

                        writeLogs.WriteLine($"Cycle: {cycle}\tMatches={matches} of {sequences.EncodedSequences.Count}\t {accuracy}%");
                        Console.WriteLine($"Cycle: {cycle}\tMatches={matches} of {sequences.EncodedSequences.Count}\t {accuracy}%");
                        Debug.WriteLine($"Cycle: {cycle}\tMatches={matches} of {sequences.EncodedSequences.Count}\t {accuracy}%");

                        if (accuracy >= maxPossibleAccuraccy)
                        {
                            maxMatchCnt++;
                            writeLogs.WriteLine($"100% accuracy reached {maxMatchCnt} times.");
                            Console.WriteLine($"100% accuracy reached {maxMatchCnt} times.");
                            Debug.WriteLine($"100% accuracy reached {maxMatchCnt} times.");

                            //
                            // Experiment is completed if we are 30 cycles long at the 100% accuracy.
                            if (maxMatchCnt >= 30)
                            {
                                writeLogs.WriteLine($"Sequence learned. The algorithm is in the stable state after 30 repeats with with accuracy {accuracy} of maximum possible {maxMatchCnt}. Elapsed sequence {sequences.Name} learning time: {sw.Elapsed}.");
                                Console.WriteLine($"Sequence learned. The algorithm is in the stable state after 30 repeats with with accuracy {accuracy} of maximum possible {maxMatchCnt}. Elapsed sequence {sequences.Name} learning time: {sw.Elapsed}.");
                                Debug.WriteLine($"Sequence learned. The algorithm is in the stable state after 30 repeats with with accuracy {accuracy} of maximum possible {maxMatchCnt}. Elapsed sequence {sequences.Name} learning time: {sw.Elapsed}.");
                                isLearningCompleted = true;
                                break;
                            }
                        }
                        else if (maxMatchCnt > 0)
                        {
                            writeLogs.WriteLine($"At 100% accuracy after {maxMatchCnt} repeats we get a drop of accuracy with accuracy {accuracy}. This indicates instable state. Learning will be continued.");
                            Console.WriteLine($"At 100% accuracy after {maxMatchCnt} repeats we get a drop of accuracy with accuracy {accuracy}. This indicates instable state. Learning will be continued.");
                            Debug.WriteLine($"At 100% accuracy after {maxMatchCnt} repeats we get a drop of accuracy with accuracy {accuracy}. This indicates instable state. Learning will be continued.");
                            maxMatchCnt = 0;
                        }

                        // This resets the learned state, so the first element starts allways from the beginning.
                        tm.Reset(mem);
                    }

                    if (isLearningCompleted == false)
                    {
                        //throw new Exception($"The system didn't learn with expected acurracy!"); 
                        writeLogs.WriteLine($"The system didn't learn with expected acurracy!");
                    }
                }
                writeLogs.WriteLine($"-------------- Training SP+TM Done - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                Console.WriteLine($"-------------- Training SP+TM Done - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");
                Debug.WriteLine($"-------------- Training SP+TM Done - {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}---------------");

                sw.Stop();
                Console.WriteLine("-----------------TRAINING END------------------------");
                Debug.WriteLine("-----------------TRAINING END------------------------");
                string timespend = $"Training Time : {sw.Elapsed}";
                Console.WriteLine(timespend);
                writeLogs.WriteLine(timespend);

                writeLogs.WriteLine("-----------------Learning completed------------------------");
            }
            //WriteLogs(OutputPath, logs);

            return new Predictor(layer1, mem, cls);
        }

        /// <summary>
        /// Runs the Prediction on model learned from Multisequence Learning algorithm for the experiment
        /// </summary>
        /// <param name="model"></param>
        /// <param name="corpus"></param>
        /// <param name="testSequences"></param>
        /// <param name="wordEncoder"></param>
        /// <returns></returns>
        public List<List<string>> RunPrediction(Predictor model, Token corpus, List<EncodedMultisequence> testSequences, ScalarEncoder wordEncoder)
        {
            //List<EncodedMultisequence> encodedTestSequences = LLMCharHelperMethods.GetEncodedSequence(testSequences, corpus, wordEncoder);
            List<List<string>>? predictedValuesList = new List<List<string>>();


            int totalPrediction = 0;
            int matchedPredictions = 0;
            int noPredictions = 0;
            double accuracy = 0.0;
            bool first = true;
            EncodedSequence prev = new EncodedSequence(); // initialize with null
            EncodedSequence next = new EncodedSequence(); // initialize with null

            foreach (EncodedMultisequence encodedMultisequence in testSequences)
            {
                model.Reset();
                List<string>? predictedValues = new List<string>();
                Console.WriteLine("-----------------------");
                foreach (EncodedSequence encodedSequence in encodedMultisequence.EncodedSequences)
                {
                    next = encodedSequence;
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        Console.WriteLine($"Sequence: {prev.Name} Test Input: {string.Join("",prev.SubSequence)}");
                        var predictedValuesForInput = model.Predict(prev.SDR);
                        if (predictedValuesForInput.Count > 0)
                        {
                            int i = 0;
                            foreach (var predictedVal in predictedValuesForInput)
                            {
                                i++;
                                //var pSequence = predictedVal.PredictedInput.Split('_').First();
                                var pCharKey = predictedVal.PredictedInput.Split('-').Last();

                                // decode the predicted work key and find actual word from corpus

                                string pVal = $"Input: {string.Join("", prev.SubSequence)} Ouput: {pCharKey} - SIMILARITY: {predictedVal.Similarity}";
                                predictedValues.Add(pVal);

                                Console.WriteLine($"{pVal}");

                                if (((int)next.SubSequence.Last()) == Int32.Parse(pCharKey))
                                {
                                    matchedPredictions++;
                                    Console.WriteLine($"{i} Perfect match for predicted song!");
                                    break;
                                }

                            }
                            totalPrediction++;
                        }
                        else
                        {
                            Console.WriteLine("Nothing predicted :(");
                            noPredictions++;
                            //totalPrediction++;
                        }
                    }

                    prev = next;
                }
                predictedValuesList.Add(predictedValues);
            }

            accuracy = matchedPredictions/ totalPrediction * 100;
            List<string> accuracyData = new List<string>();
            accuracyData.Add($"Matched Predictions: {matchedPredictions}");
            accuracyData.Add($"Total Predictions: {totalPrediction}");
            accuracyData.Add($"Accuracy: {accuracy}%");
            
            predictedValuesList.Add(accuracyData);

            WritePredictonLogs(OutputPath, predictedValuesList);

            return predictedValuesList;

        }

        /// <summary>
        /// Gets the number of all unique inputs.
        /// </summary>
        /// <param name="multiSequences">All sequences.</param>
        /// <returns></returns>
        private int GetNumberOfInputs(List<EncodedMultisequence> multiSequences)
        {
            int num = 0;

            foreach (var sequences in multiSequences)
            {
                num += sequences.EncodedSequences.Count;
            }

            return num;
        }


        /// <summary>
        /// Constracts the unique key of the element of an sequece. This key is used as input for HtmClassifier.
        /// It makes sure that alle elements that belong to the same sequence are prefixed with the sequence.
        /// The prediction code can then extract the sequence prefix to the predicted element.
        /// </summary>
        /// <param name="prevInputs"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private static string GetKey(List<string> prevInputs)
        {
            string key = String.Empty;

            for (int i = 0; i < prevInputs.Count; i++)
            {
                if (i > 0)
                    key += "-";

                key += (prevInputs[i]);
            }

            return $"{key}";
        }

        /// <summary>
        /// Gets an object of HTM Config as per input bits and num of columns
        /// </summary>
        /// <param name="inputBits">input bits for config</param>
        /// <param name="numColumns">number of columns for config</param>
        /// <returns>object of the HTM Config</returns>
        private static HtmConfig GetHtmConfig(int inputBits, int numColumns)
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
                SynPermConnected = 0.5,
                // Learning is slower than forgetting in this case.
                PermanenceDecrement = 0.25,
                PermanenceIncrement = 0.15,

                // Used by punishing of segments.
                PredictedSegmentDecrement = 0.1
            };

            return cfg;
        }

        /// <summary>
        /// Gets the Scalar Encoder with given configs
        /// </summary>
        /// <param name="max">max number of value to encode</param>
        /// <param name="inputBits">number of input bits as per HTM Config</param>
        /// <returns>object of Scalar Encoder</returns>
        private static ScalarEncoder GetScalarEncoder(int max, int inputBits)
        {
            Dictionary<string, object> settings = new Dictionary<string, object>()
            {
                { "W", 15},
                { "N", inputBits},
                { "Radius", -1.0},
                { "MinVal", 0.0},
                { "Periodic", false},
                { "Name", "scalar"},
                { "ClipInput", false},
                { "MaxVal", max}
            };

            ScalarEncoder encoder = new ScalarEncoder(settings);

            return encoder;
        }

        /// <summary>
        /// Write logs in the given file name with full path
        /// </summary>
        /// <param name="filePath">full path of log</param>
        /// <param name="logs">logs to be written</param>
        public void WritePredictonLogs(string filePath, List<List<string>> logs)
        {
            foreach (var log in logs)
            { File.AppendAllLines(filePath, log); }
        }

        /// <summary>
        /// Create a unique file name for writing logs
        /// </summary>
        /// <returns>returns name of log file</returns>
        public string GetLogFile()
        {
            string BasePath = AppDomain.CurrentDomain.BaseDirectory;
            string reportFolder = Path.Combine(BasePath, "report");
            if (!Directory.Exists(reportFolder))
                Directory.CreateDirectory(reportFolder);

            var ticks = DateTime.Now.Ticks;
            string reportPath = Path.Combine(reportFolder, $"reports_{ticks}.txt");

            if (!File.Exists(reportPath))
            {
                using (StreamWriter sw = File.CreateText(reportPath))
                {
                    sw.WriteLine($"Log file created at {ticks}...");
                }
            }

            return reportPath;
        }
    }
}
