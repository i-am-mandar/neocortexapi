using NeoCortexApi;
using NeoCortexApi.Encoders;
using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static LargeLanguageModel2.MultiSequenceLearning;
using LargeLanguageModel2;


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
            var words = LLMCharHelperMethods.ReadInput(datafile);
            Console.WriteLine("Reading datafile done...");

            Console.WriteLine("Breaking down chars...");
            var charsBroken = LLMCharHelperMethods.BreakDownToChar(LLMCharHelperMethods.BreakDownToWords(words));
            Console.WriteLine("Breaking down chars done...");

            Console.WriteLine("Filling database...");
            var tokens = LLMCharHelperMethods.FillTokenDatabase(charsBroken);
            Console.WriteLine("Filling database done...");

            Console.WriteLine("Getting word encoder...");
            var charEncoder = LLMCharHelperMethods.GetCharEncoder(tokens);
            Console.WriteLine("Getting word encoder done...");

            Console.WriteLine("Creating sequences..");
            var sequences = LLMCharHelperMethods.CreateSequence(charsBroken);
            Console.WriteLine("Creating sequences done...");

            Console.WriteLine("Encoding all words in sequence...");
            var encodedSequence = LLMCharHelperMethods.GetEncodedSequence(sequences, tokens, charEncoder);
            Console.WriteLine("Encoding all words in sequence done...");

            Console.WriteLine("Split sequences...");
            (var trainSequences, var testSequences) = LLMCharHelperMethods.SplitSequence(encodedSequence);
            Console.WriteLine("Split sequences done...");

            Console.WriteLine("Save sequences...");
            //needs implementation
            Console.WriteLine("Save sequences done...");

            //train in parallel => this is not implement
            //start learning the model and lets see how it goes - to do
            Console.WriteLine("Running Multisequence Learning experiment");
            int inputBits = LLMCharHelperMethods.GetInputBits(charEncoder);
            MultiSequenceLearning multiSequenceLearning = new MultiSequenceLearning();
            var model = multiSequenceLearning.Run(trainSequences, tokens, inputBits);
            Console.WriteLine("Running Multisequence Learning experiment done...");

            // decoding/reverse mapping the predicted values
            Console.WriteLine("Decoding Predictions");
            var logs = multiSequenceLearning.RunPrediction(model, tokens, testSequences, charEncoder);
            Console.WriteLine("Completed Predictions");
            
        }
    }
}