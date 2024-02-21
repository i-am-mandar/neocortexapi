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
            bool useTestData = false;
            string datafile = useTestData ? "input-min-100.txt" : "input.txt";
            Console.WriteLine($"Using datafile: {datafile}");

            Console.WriteLine("Reading datafile...");
            var words = LLMHelperMethods.ReadInput(datafile);
            Console.WriteLine("Reading datafile done...");

            Console.WriteLine("Breaking down words...");
            var wordsBroken = LLMHelperMethods.BreakDownWords(words);
            Console.WriteLine("Breaking down words done...");

            Console.WriteLine("Creating sequences..");
            var sequences = LLMHelperMethods.CreateSequence(wordsBroken);
            Console.WriteLine("Creating sequences done...");

            Console.WriteLine("Filling database...");
            var database = LLMHelperMethods.FillDatabase(wordsBroken);
            Console.WriteLine("Filling database done...");

            Console.WriteLine("Getting word encoder...");
            var wordEncoder = LLMHelperMethods.GetWordEncoder(database);
            Console.WriteLine("Getting word encoder done...");

            Console.WriteLine("Encoding all words in sequence...");
            var encodedSequence = LLMHelperMethods.GetEncodedSequence(sequences, database, wordEncoder);
            Console.WriteLine("Encoding all words in sequence done...");

            //train in parallel => this is not implement

            //start learning the model and lets see how it goes - to do
        }
    }
}