using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoCortexApi.Encoders;

namespace LargeLanguageModel2
{
    public class LLMCharHelperMethods
    {
        public static int DEBUG = 2;
        public static int UNIQUE_WORD = 1;
        public static int TOKEN_SIZE = 16;
        public static double OVERLAP_SIZE = 0.65;
        public static double SPLIT_SIZE = 0.1;
        public LLMCharHelperMethods() { }

        public static string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string[] ReadInput(string fileName)
        {
            fileName = Path.Combine(BasePath, "dataset", fileName);
            if (File.Exists(fileName))
            {
                //return File.ReadAllLines(fileName);

                string content = File.ReadAllText(fileName).Replace("\r", "");

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
        public static List<string> BreakDownToWords(string[] words)
        {
            List<string> cleanWords = new List<string>();

            foreach (string word in words)
            {
                if (word.Contains("\n\n") || word.Contains(",") || word.Contains("\n") || word.Contains("?") || word.Contains("!") || word.Contains(":") || word.Contains(".") || word.Contains(";") || word.Contains("-"))
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
        /// <param name="words"></param>
        /// <returns></returns>
        public static List<char> BreakDownToChar(List<string> words)
        {
            List<char> chars = new List<char>();

            foreach (string word in words)
            {
                if (DEBUG > 2)
                {
                    Console.WriteLine($"{word}");
                }

                char[] arr = word.ToCharArray();

                for (int i = 0; i < arr.Length; i++)
                {
                    chars.Add(arr[i]);
                }

                // neeed to come back here ----------------------------------------
                // chars.Add(' ');

            }

            return chars;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="charsBroken"></param>
        /// <returns></returns>
        public static Token FillTokenDatabase(List<char> charsBroken)
        {
            int count = 0;
            Token db = new Token();
            foreach (char chars in charsBroken)
            {
                char cleanChar = chars;
                if (!db.Char.ContainsKey(cleanChar))
                {
                    db.Char.Add(cleanChar, count++);
                    if (DEBUG > 4)
                    {
                        Console.WriteLine($"{cleanChar}");

                    }
                }
            }

            Token dbSorted = new Token();
            SortedDictionary<char, int>.KeyCollection sortedKeys = db.Char.Keys;
            foreach (char key in sortedKeys)
            {
                dbSorted.Char.Add(key, UNIQUE_WORD++);
            }

            return dbSorted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tk"></param>
        /// <returns></returns>
        public static ScalarEncoder GetCharEncoder(Token tk)
        {
            int size = 31;

            ScalarEncoder charEncoder = GetScalarEncoder(size, UNIQUE_WORD - tk.Char.Count, UNIQUE_WORD, "Char");

            return charEncoder;
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
                { "MaxVal", (double)maxVal+1}, // Max value = (no of unique songs in Corpus).
                { "Periodic", false},
                { "Name", name},
                { "ClipInput", true},
           });

            return scalarEncoder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static Multisequence CreateSequence(List<char> chars)
        {
            Multisequence sequences = new Multisequence();

            int count = 0;
            int maxCount = TOKEN_SIZE + 1; //configure - to do
            int sequenceCount = 1;

            int overlap = (int)(TOKEN_SIZE * OVERLAP_SIZE);

            Sequence sequence = new Sequence();
            sequence.Name = $"S{sequenceCount}";

            //creating overlapping sequences

            for(int i=0; i <chars.Count; i++)
            {
                sequence.Chars.Add(chars[i]);
                count++;

                if (count >= maxCount)
                {
                    count = 0;
                    i = i - overlap;
                    sequences.Sequences.Add(sequence);
                    sequence = new Sequence();
                    sequenceCount++;
                    sequence.Name = $"S{sequenceCount}";
                }

            }

            return sequences;

        }

        /// <summary>
        /// Core logic for creating multisequences which are encoded
        /// </summary>
        /// <param name="sequences"></param>
        /// <param name="tokens"></param>
        /// <param name="charEncoder"></param>
        /// <returns></returns>
        public static List<EncodedMultisequence> GetEncodedSequence(Multisequence sequences, Token tokens, ScalarEncoder charEncoder)
        {
            List<EncodedMultisequence> encodedMultisequences = new List<EncodedMultisequence>();
            int encodedMultisequenceCount = 1;
            foreach (Sequence seq in sequences.Sequences)
            {
                EncodedMultisequence ms = new EncodedMultisequence();
                ms.Name = $"E{encodedMultisequenceCount}";

                // each sequence of TOKEN_SIZE+1 creates multisequence of TOKEN_SIZE
                var x_context = seq.Chars.ToArray()[0..TOKEN_SIZE];
                var y_target = seq.Chars.ToArray()[1..(TOKEN_SIZE+1)];

                int encodedSequenceCount = 1;

                for (int count = 0; count < TOKEN_SIZE; count++)
                {
                    EncodedSequence encodedSequence = new EncodedSequence();
                    encodedSequence.Name = $"S{encodedSequenceCount}";

                    encodedSequence.SubSequence = x_context.ToList()[0..(count + 1)];
                    encodedSequence.NextChar = y_target[count];

                    encodedSequence.EncodedSubSequence = GetEncodedSubSequence(encodedSequence.SubSequence, tokens);
                    encodedSequence.EncodedNextChar = GetEncodedChar(encodedSequence.NextChar, tokens);

                    encodedSequence.SDR = charEncoder.Encode(encodedSequence.EncodedNextChar);

                    encodedSequenceCount++;
                    ms.EncodedSequences.Add(encodedSequence);
                }

                encodedMultisequenceCount++;
                encodedMultisequences.Add(ms);
            }

            return encodedMultisequences;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subSequence"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private static List<int> GetEncodedSubSequence(List<char> subSequence, Token tokens)
        {
            List<int> encodedSubSequence = new List<int>();
            foreach (char c in subSequence)
            {
                encodedSubSequence.Add(GetEncodedChar(c, tokens));
            }
            
            return encodedSubSequence;
        }

        /// <summary>
        /// Get value from a dictonary wrt to key
        /// </summary>
        /// <param name="keyValuePairs">Dictionary of data</param>
        /// <param name="key">key of the value to be fetched</param>
        /// <returns>value of the key in interger</returns>
        private static int GetValueByID(SortedDictionary<char, int> keyValuePairs, char key)
        {
            int value = 0;
            
            bool hasValue = keyValuePairs.TryGetValue(key, out value);
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

        /// <summary>
        /// Wrapper function for GetValueByID()
        /// </summary>
        /// <param name="nextChar"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private static int GetEncodedChar(char nextChar, Token tokens)
        {
            return GetValueByID(tokens.Char, nextChar);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoder"></param>
        /// <returns></returns>
        public static int GetInputBits(ScalarEncoder encoder)
        {
            return encoder.N;
        }

        /// <summary>
        /// Splits the sequences into train and test dataset as 9:1
        /// </summary>
        /// <param name="encodedSequence"></param>
        /// <returns></returns>
        public static (List<EncodedMultisequence>, List<EncodedMultisequence>) SplitSequence(List<EncodedMultisequence> encodedSequence)
        {
            List<EncodedMultisequence> trainSequences = new List<EncodedMultisequence>();
            List<EncodedMultisequence> testSequences = new List<EncodedMultisequence>();

            Random rng = new Random();

            List<EncodedMultisequence> shuffledSequence = encodedSequence.OrderBy(_ => rng.Next()).ToList();

            int trainSize = (int)(shuffledSequence.Count * (1 - SPLIT_SIZE));

            trainSequences.AddRange(shuffledSequence[0..trainSize]);
            testSequences.AddRange(shuffledSequence[trainSize..]);

            return (trainSequences, testSequences);
        }
    }
}
