using NeoCortexApi;
using NeoCortexApi.Encoders;
using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static LargeLanguageModel.MultiSequenceLearning;


namespace LargeLanguageModel
{
    class Program
    {
        /// <summary>
        /// This is an experiment to build LLM using HTM
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            bool useMinData = true;
            bool useMinTestData = useMinData;

            string datafile = useMinData ? "input-min-100.txt" : "input.txt";
            Console.WriteLine($"Using datafile: {datafile}");

            Console.WriteLine("Reading datafile...");
            var words = LLMWordHelperMethods.ReadInput(datafile);
            Console.WriteLine("Reading datafile done...");

            Console.WriteLine("Breaking down words...");
            var wordsBroken = LLMWordHelperMethods.BreakDownWords(words);
            Console.WriteLine("Breaking down words done...");

            Console.WriteLine("Creating sequences..");
            var sequences = LLMWordHelperMethods.CreateSequence(wordsBroken);
            Console.WriteLine("Creating sequences done...");

            Console.WriteLine("Filling database...");
            var corpus = LLMWordHelperMethods.FillDatabase(wordsBroken);
            Console.WriteLine("Filling database done...");

            Console.WriteLine("Getting word encoder...");
            var wordEncoder = LLMWordHelperMethods.GetWordEncoder(corpus);
            Console.WriteLine("Getting word encoder done...");

            Console.WriteLine("Encoding all words in sequence...");
            var encodedSequence = LLMWordHelperMethods.GetEncodedSequence(sequences, corpus, wordEncoder);
            Console.WriteLine("Encoding all words in sequence done...");

            string testDatafile = useMinTestData ? "test-input-min-100.txt" : "test-input.txt";
            Console.WriteLine($"Using datafile: {testDatafile}");

            Console.WriteLine("Reading test datafile...");
            var testWords = LLMWordHelperMethods.ReadInput(testDatafile);
            Console.WriteLine("Reading test datafile done...");

            Console.WriteLine("Breaking down test words...");
            var testWordsBroken = LLMWordHelperMethods.BreakDownWords(testWords);
            Console.WriteLine("Breaking down test words done...");

            Console.WriteLine("Creating test sequences..");
            var testSequences = LLMWordHelperMethods.CreateTestSequence(testWordsBroken);
            Console.WriteLine("Creating test sequences done...");

            //train in parallel => this is not implement
            //start learning the model and lets see how it goes - to do
            Console.WriteLine("Running Multisequence Learning experiment");
            int inputBits = LLMWordHelperMethods.GetInputBits(corpus);
            MultiSequenceLearning multiSequenceLearning = new MultiSequenceLearning();
            var model = multiSequenceLearning.Run(encodedSequence, corpus, inputBits);
            Console.WriteLine("Running Multisequence Learning experiment done...");

            // decoding/reverse mapping the predicted values
            Console.WriteLine("Decoding Predictions");
            var logs = multiSequenceLearning.RunPrediction(model, corpus, testSequences, wordEncoder);
            Console.WriteLine("Completed Predictions");

        }
    }
}