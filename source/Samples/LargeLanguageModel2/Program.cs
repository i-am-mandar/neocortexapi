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

            string datafile = useMinData ? "input-min-100.txt" : "input.txt";
            Console.WriteLine($"Using datafile: {datafile}");

            Console.WriteLine("Breaking down chars...");
            List<char> charsBroken = LLMCharHelperMethods.BreakDownToChar(LLMCharHelperMethods.BreakDownToWords(LLMCharHelperMethods.ReadInput(datafile)));
            Console.WriteLine("Breaking down chars done...");

            Console.WriteLine("Filling database...");
            Token tokens = LLMCharHelperMethods.FillTokenDictonary(charsBroken);
            Console.WriteLine("Filling database done...");

            Console.WriteLine("Getting word encoder...");
            ScalarEncoder charEncoder = LLMCharHelperMethods.GetCharEncoder(tokens);
            Console.WriteLine("Getting word encoder done...");

            Console.WriteLine("Creating sequences..");
            Multisequence sequences = LLMCharHelperMethods.CreateSequence(charsBroken);
            Console.WriteLine("Creating sequences done...");

            Console.WriteLine("Encoding all words in sequence...");
            List<EncodedMultisequence> encodedSequence = LLMCharHelperMethods.GetEncodedSequence(sequences, tokens, charEncoder);
            Console.WriteLine("Encoding all words in sequence done...");

            MultiSequenceLearning multiSequenceLearning = new MultiSequenceLearning();

            //train in parallel => this is not implement
            Console.WriteLine("Running Multisequence Learning experiment");
            int inputBits = LLMCharHelperMethods.GetInputBits(charEncoder);
            var model = multiSequenceLearning.Run(encodedSequence, tokens, inputBits);
            Console.WriteLine("Running Multisequence Learning experiment done...");

            Console.WriteLine("Save sequences...");
            var trainDatasetFilePath = LLMCharHelperMethods.SaveSequences(multiSequenceLearning.OutputPath, "train", encodedSequence);
            var tokenFilePath = LLMCharHelperMethods.SaveToken(multiSequenceLearning.OutputPath, tokens);
            Console.WriteLine("Save sequences done...");

            // decoding/reverse mapping the predicted values
            Console.WriteLine("Running completion model..");
            multiSequenceLearning.RunCompletionModel(model, tokens, charEncoder);
            Console.WriteLine("Running completion model..");

        }
    }
}