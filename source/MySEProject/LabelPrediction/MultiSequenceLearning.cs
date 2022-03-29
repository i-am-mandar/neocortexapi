﻿using System;
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

namespace LabelPrediction
{
    public class MultiSequenceLearning
    {
        static readonly string PowerConsumptionCSV = Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory + @"\Dataset\rec-center-hourly-og.csv");
        static readonly string PowerConsumptionCSV_Exp = Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory + @"\Dataset\rec-center-hourly-exp.csv");


        public MultiSequenceLearning()
        {
            //needs no implementation
        }

        public void StartExperiment()
        {
            int inputBits = 100;
            int maxCycles = 15;
            int numColumns = 2048;
            string[] sequenceFormatType = { "byMonth" /* 720 */, "byWeek" /* 168 */, "byDay" /* 24 */};

            List<Dictionary<string, int[]>> encodedData;
            EncoderBase encoderDateTime;
            HtmPredictionEngine trainedEngine;

            PrepareTrainingData(sequenceFormatType, out encodedData, out encoderDateTime);

            RunTraining(inputBits, maxCycles, numColumns, encodedData, encoderDateTime, out trainedEngine);

            RunPrediction(trainedEngine);
        }

        /// <summary>
        /// Preparing Data for Learning - 1. Created MultiSequence and 2. Encoding Input
        /// </summary>
        /// <param name="sequenceFormatType"></param>
        /// <param name="encodedData"></param>
        /// <param name="encoderDateTime"></param>
        private static void PrepareTrainingData(string[] sequenceFormatType, out List<Dictionary<string, int[]>> encodedData, out EncoderBase encoderDateTime)
        {
            Console.WriteLine("Reading CSV File..");
            //var csvData = HelperMethods.ReadPowerConsumptionDataFromCSV(PowerConsumptionCSV, sequenceFormatType[0]);
            var csvData = HelperMethods.ReadPowerConsumptionDataFromCSV(PowerConsumptionCSV_Exp, sequenceFormatType[2]);
            Console.WriteLine("Completed reading CSV File..");

            Console.WriteLine("Encoding data read from CSV...");
            encodedData = HelperMethods.EncodePowerConsumptionData(csvData, true);
            encoderDateTime = HelperMethods.FetchDateTimeEncoder();
        }

        /// <summary>
        /// Getting trained model by MultiSequence Learning
        /// </summary>
        /// <param name="inputBits"></param>
        /// <param name="maxCycles"></param>
        /// <param name="numColumns"></param>
        /// <param name="encodedData"></param>
        /// <param name="encoderDateTime"></param>
        /// <param name="trainedHTMmodel"></param>
        private void RunTraining(int inputBits, int maxCycles, int numColumns, List<Dictionary<string, int[]>> encodedData, EncoderBase encoderDateTime, out HtmPredictionEngine trainedHTMmodel)
        {
            Console.WriteLine("Started Learning...");
            /*
             * Running MultiSequence Learning experiment here
             */
            trainedHTMmodel = Run(inputBits, maxCycles, numColumns, encoderDateTime, encodedData);

            Console.WriteLine("Done Learning");
        }

        /// <summary>
        /// Takes user input and gives predicted label
        /// </summary>
        /// <param name="trainedEngine"></param>
        private static void RunPrediction(HtmPredictionEngine trainedEngine)
        {
            Debug.WriteLine("PLEASE ENTER DATE FOR PREDICTING POWER CONSUMPTION:      *note format->dd/mm/yy hh:00");
            Console.WriteLine("PLEASE ENTER DATE FOR PREDICTING POWER CONSUMPTION:      *note format->dd/mm/yy hh:00");

            var userInput = Console.ReadLine();

            while (!userInput.Equals("q") && userInput != "Q")
            {
                if (userInput != null)
                {
                    var sdr = HelperMethods.EncodeSingleInput(userInput);
                    trainedEngine.Reset();
                    var predictedValuesForUserInput = trainedEngine.Predict(sdr);
                    if (predictedValuesForUserInput.Count > 0)
                    {
                        foreach (var predictedVal in predictedValuesForUserInput)
                        {
                            Console.WriteLine("SIMILARITY " + predictedVal.Similarity + " PREDICTED VALUE :" + predictedVal.PredictedInput);
                        }
                    }
                    else
                        Console.WriteLine("Nothing predicted :(");
                }
                Console.WriteLine("PLEASE ENTER DATE FOR PREDICTING POWER CONSUMPTION:      *note format->dd/mm/yy hh:00");
                userInput = Console.ReadLine();
            }
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
            int numUniqueInputs = sequences.Count;

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

                /* For each sequence in multi-sequence --- Loop 2 */
                foreach (var sequence in sequences)
                {
                    /* For each element (dictionary) in sequence --- Loop 3 */
                    foreach (var element in sequence)
                    {
                        string[] splitKeyv = element.Key.Split(",");
                        var observationClass = splitKeyv[0]; // OBSERVATION LABEL || SEQUENCE LABEL
                        var elementSDR = element.Value; // ELEMENT IN ONE SEQUENCE

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

            // Clear all learned patterns in the classifier.
            //cls.ClearState();

            // We activate here the Temporal Memory algorithm.
            layer1.HtmModules.Add("tm", tm);

            string lastPredictedValue = "-1";
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

                /* Loop until maxCycles --- Loop 2*/
                for (int i = 0; i < maxCycles; i++)
                {
                    List<string> ElementWiseClasses = new List<string>();

                    /* Element in sequenc match counter */
                    int elementMatches = 0;

                    /* For each element (dictionary) in sequence --- Loop 3 */
                    foreach (var Elements in sequence)
                    {
                        string[] splitKey = Elements.Key.Split(",");
                        var observationLabel = splitKey[0];

                        var lyrOut = new ComputeCycle();

                        /* Get Compute Cycle */
                        lyrOut = layer1.Compute(Elements.Value, learn) as ComputeCycle;
                        Debug.WriteLine(string.Join(',', lyrOut.ActivColumnIndicies));

                        /* Get Active Cells */
                        List<Cell> actCells = (lyrOut.ActiveCells.Count == lyrOut.WinnerCells.Count) ? lyrOut.ActiveCells : lyrOut.WinnerCells;

                        /* Learn the combination of Label and Active Cells */
                        cls.Learn(observationLabel, actCells.ToArray());

                        if (lastPredictedValue == observationLabel && lastPredictedValue != "")
                        {
                            elementMatches++;
                            Debug.WriteLine($"Match. Actual value: {observationLabel} - Predicted value: {lastPredictedValue}");
                        }
                        else
                        {
                            Debug.WriteLine($"Mismatch! Actual value: {observationLabel} - Predicted values: {lastPredictedValue}");
                        }

                        Debug.WriteLine($"Col  SDR: {Helpers.StringifyVector(lyrOut.ActivColumnIndicies)}");
                        Debug.WriteLine($"Cell SDR: {Helpers.StringifyVector(actCells.Select(c => c.Index).ToArray())}");

                        if (learn == false)
                            Debug.WriteLine($"Inference mode");

                        if (lyrOut.PredictiveCells.Count > 0)
                        {
                            var predictedInputValue = cls.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 3);

                            Debug.WriteLine($"Current Input: {observationLabel}");
                            Debug.WriteLine("The predictions with similarity greater than 50% are");

                            foreach (var t in predictedInputValue)
                            {

                                if (t.Similarity >= (double)50.00)
                                {
                                    Debug.WriteLine($"Predicted Input: {string.Join(", ", t.PredictedInput)},\tSimilarity Percentage: {string.Join(", ", t.Similarity)}, \tNumber of Same Bits: {string.Join(", ", t.NumOfSameBits)}");
                                }
                            }

                            lastPredictedValue = predictedInputValue.First().PredictedInput;

                        }
                    }

                    accuracy = ((double)elementMatches / (sequence.Count)) * 100;
                    Debug.WriteLine($"Cycle : {i} \t Accuracy:{accuracy}");
                    tempLOGGRAPH.Add(i, accuracy);
                    if (accuracy == 100)
                    {
                        SequencesMatchCount++;
                        if (SequencesMatchCount >= 30)
                        {
                            tempLOGFILE.Add(i, $"Cycle : {i} \t  Accuracy:{accuracy} \t Number of times repeated {SequencesMatchCount}");
                            break;
                        }
                        tempLOGFILE.Add(i, $"Cycle : {i} \t  Accuracy:{accuracy} \t Number of times repeated {SequencesMatchCount}");

                    }
                    else if (lastCycleAccuracy == accuracy && accuracy != 0)
                    {
                        SaturatedAccuracyCount++;
                        if (SaturatedAccuracyCount >= 20 && lastCycleAccuracy > 70)
                        {
                            Debug.WriteLine($"NO FURTHER ACCURACY CAN BE ACHIEVED");
                            Debug.WriteLine($"Saturated Accuracy : {lastCycleAccuracy} \t Number of times repeated {SaturatedAccuracyCount}");
                            tempLOGFILE.Add(i, $"Cycle: { i} \t Accuracy:{accuracy} \t Number of times repeated {SaturatedAccuracyCount}");
                            break;
                        }
                        else
                        {
                            tempLOGFILE.Add(i, $"Cycle: { i} \t Saturated Accuracy : {lastCycleAccuracy} \t Number of times repeated {SaturatedAccuracyCount}");
                        }
                    }
                    else
                    {
                        SaturatedAccuracyCount = 0;
                        SequencesMatchCount = 0;
                        lastCycleAccuracy = accuracy;
                        tempLOGFILE.Add(i, $"cycle : {i} \t Accuracy :{accuracy} \t ");
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
            Console.WriteLine($"Training Time : {timeSpan.Minutes} minutes and {timeSpan.Seconds} seconds");
            Debug.WriteLine("-------------------WRTING TRAINING OUTPUT LOGS---------------------");
            Console.WriteLine("-------------------WRTING TRAINING OUTPUT LOGS------------------------");
            //*****************

            DateTime now = DateTime.Now;
            string filename = now.ToString("g"); //

            filename = $"PowerConsumptionPredictionExperiment_{filename.Split(" ")[0]}_{now.Ticks.ToString()}.txt";
            string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName, $"TrainingLogs/{filename}");

            using (StreamWriter swOutput = File.CreateText(path))
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

            Debug.WriteLine("-------------------TRAINING LOGS HAS BEEN CREATED---------------------");
            Console.WriteLine("-------------------TRAINING LOGS HAS BEEN CREATED------------------------");

            return new HtmPredictionEngine { Layer = layer1, Classifier = cls, Connections = mem };
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

}