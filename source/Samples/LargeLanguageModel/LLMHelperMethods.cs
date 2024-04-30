using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoCortexApi.Encoders;

namespace LargeLanguageModel
{
    public class LLMHelperMethods
    {
        public static int DEBUG = 2;
        public static int UNIQUE_WORD = 1;
        public LLMHelperMethods() { }

        public static string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string[] ReadInput(string fileName)
        {
            fileName = Path.Combine(BasePath, "dataset", fileName);
            if(File.Exists(fileName))
            {
                //return File.ReadAllLines(fileName);

                string content = File.ReadAllText(fileName).Replace("\r","");

                string[] words = content.Split(new string[] { " " }, StringSplitOptions.None);

                return words;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        public static List<string> BreakDownWords(string[] words)
        {
            List<string> cleanWords = new List<string>();

            foreach(string word in words)
            {
                if(word.Contains("\n\n") || word.Contains(",") || word.Contains("\n") || word.Contains("?") || word.Contains("!") || word.Contains(":") || word.Contains(".") || word.Contains(";") || word.Contains("-"))
                {
                    if (DEBUG > 2)
                    {
                        Console.WriteLine($"{word}");
                    }
                    string wordToBeBroken = word;
                    wordToBeBroken = wordToBeBroken.Replace("\n\n", " \n\n ");
                    wordToBeBroken = wordToBeBroken.Replace("\n", " \n ");
                    wordToBeBroken = wordToBeBroken.Replace(",", " , ");
                    wordToBeBroken = wordToBeBroken.Replace("?", " ? ");
                    wordToBeBroken = wordToBeBroken.Replace("!", " ! ");
                    wordToBeBroken = wordToBeBroken.Replace(":", " : ");
                    wordToBeBroken = wordToBeBroken.Replace(".", " . ");
                    wordToBeBroken = wordToBeBroken.Replace(";", " ; ");
                    wordToBeBroken = wordToBeBroken.Replace("-", " - ");
                    
                    string[] brokenWords = wordToBeBroken.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                    cleanWords.AddRange(brokenWords);
                    continue;
                }

                cleanWords.Add(word);
            }

            return cleanWords;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        [Obsolete]
        public static string[] GetDelimiter(string sentence)
        {
            string[] delimiter = new string[] { ",", ".", "?", "!", ":", ";", "-", };

            return delimiter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        public static List<Sequence> CreateSequence(List<string> words)
        {
            List<Sequence> sequences = new List<Sequence>();

            int count = 0;
            int maxCount = 10; //configure - to do
            int sequenceCount = 1;

            Sequence sequence = new Sequence();
            sequence.Name = $"S{sequenceCount}";

            foreach (string word in words)
            {
                sequence.Words.Add(word);
                count++;

                if(count >= maxCount)
                {
                    count = 0;
                    sequences.Add(sequence);
                    sequence = new Sequence();
                    sequenceCount++;
                    sequence.Name = $"S{sequenceCount}";
                }

            }

            return sequences;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wordsBroken"></param>
        /// <returns></returns>
        public static Corpus FillDatabase(List<string> wordsBroken)
        {
            int count = 0;
            Corpus db = new Corpus();
            foreach(string word in wordsBroken)
            {
                string cleanWord = word.ToLower();
                if (!db.Word.ContainsKey(cleanWord))
                {
                    db.Word.Add(cleanWord, count++);
                    if (DEBUG > 4)
                    {
                        Console.WriteLine($"{cleanWord}");
                        
                    }
                }
            }

            Corpus dbSorted = new Corpus();
            SortedDictionary<string, int>.KeyCollection sortedKeys = db.Word.Keys;
            foreach (string key in sortedKeys)
            {
                dbSorted.Word.Add(key, UNIQUE_WORD++);
            }

            return dbSorted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static ScalarEncoder GetWordEncoder(Corpus db)
        {
            int size = 3;
            
            ScalarEncoder wordEncoder = GetScalarEncoder(size, UNIQUE_WORD - db.Word.Count, UNIQUE_WORD, "Word");

            return wordEncoder;
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

        public static List<EncodedSequence> GetEncodedSequence(List<Sequence> sequences, Corpus db, ScalarEncoder wordEncoder)
        {
            List<EncodedSequence> encodedSequences = new List<EncodedSequence>();

            foreach (Sequence sequence in sequences) 
            {
                EncodedSequence encodedSequence = new EncodedSequence();
                encodedSequence.Name = sequence.Name;
                foreach(string word in sequence.Words)
                {
                    int[] sdr = new int[0];
                    int key = GetValueByID(db.Word, word);
                    sdr = sdr.Concat(wordEncoder.Encode(key)).ToArray();
                    Tuple<string, int, int[]> encodedWord = Tuple.Create<string, int, int[]>(word, key, sdr);
                    encodedSequence.encodedWords.Add(encodedWord);
                }
                encodedSequences.Add(encodedSequence);
            }

            return encodedSequences;
        }

        /// <summary>
        /// Get value from a dictonary wrt to key
        /// </summary>
        /// <param name="keyValuePairs">Dictionary of data</param>
        /// <param name="key">key of the value to be fetched</param>
        /// <returns>value of the key in interger</returns>
        public static int GetValueByID(SortedDictionary<string, int> keyValuePairs, string key)
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
    }
}
