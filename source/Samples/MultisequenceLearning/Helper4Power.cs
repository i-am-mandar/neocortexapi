using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoCortexApi;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;

namespace MultisequenceLearning
{
    public class Helper4Power
    {
        public Helper4Power()
        {
            //needs no implementation
        }

        /// <summary>
        /// Reads PowerConsumption CSV file and pre-processes the data and returns it into List of Dictionary
        /// </summary>
        /// <param name="csvFilePath">CSV file</param>
        /// <returns></returns>
        public static List<Dictionary<string, string>> ReadPowerConsumptionDataFromCSV(string csvFilePath, string sequenceFormat)
        {
            List<Dictionary<string, string>> sequencesCollection = new List<Dictionary<string, string>>();

            int keyForUniqueIndexes = 0;
            string[] sequenceFormatType = { "byMonth", "byWeek", "byDay" };

            int[] daysOfMonth = { -1, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

            int count = 0, maxCount = 0;

            bool firstTime = true;

            if (File.Exists(csvFilePath))
            {
                using (StreamReader reader = new StreamReader(csvFilePath))
                {
                    // [Power, "dd/MM hh SEG"]
                    Dictionary<string, string> sequence = new Dictionary<string, string>();
                    while (reader.Peek() >= 0)
                    {
                        var line = reader.ReadLine();
                        string[] values = line.Split(",");

                        keyForUniqueIndexes++;
                        count++;

                        var columnDateTime = values[0];
                        var columnPower = values[1];

                        /* 
                         * This is a bit mess. The DateTime in date set is M/d/yy h:mm or MM/dd/yy hh:mm 
                         * parsing with ParseExact seemed difficult, reformatting to dd/MM/yy hh:mm 
                         */
                        string[] splitDateTime = columnDateTime.Split(" ");

                        string[] date = splitDateTime[0].Split("/");
                        int dd = int.Parse(date[1]);
                        int MM = int.Parse(date[0]);
                        int yy = int.Parse(date[2]);

                        /* 
                         * Parse only one month of data
                         */
                        if (firstTime)
                        {
                            if (sequenceFormatType[0].Equals(sequenceFormat))        /* byMonth */
                                maxCount = daysOfMonth[MM] * 24;
                            else if (sequenceFormatType[1].Equals(sequenceFormat))   /* byWeek  */
                                maxCount = 7 * 24;
                            else if (sequenceFormatType[2].Equals(sequenceFormat))   /* byDay   */
                                maxCount = 24;

                            firstTime = false;
                        }

                        string[] time = splitDateTime[1].Split(":");
                        int hh = int.Parse(time[0]);
                        int mm = int.Parse(time[1]);

                        /*
                         * Recreating date as dd/MM/yy hh:mm
                         */
                        string dateTime = $"{dd.ToString("00")}/{MM.ToString("00")}/{yy.ToString("00")} {hh.ToString("00")}:{mm.ToString("00")}";

                        /*
                         * If the label(key) is same then add a unique number 
                         * to key before adding to sequence to create multiple sequences
                         */
                        if (sequence.ContainsKey(columnPower))
                        {
                            var newKey = columnPower + "," + keyForUniqueIndexes;
                            sequence.Add(newKey, dateTime);
                        }
                        else
                            sequence.Add(columnPower, dateTime);

                        /*
                         * Creating multiple sequences for each month
                         */
                        if (count >= maxCount)
                        {
                            count = 0;
                            maxCount = 0;
                            firstTime = true;

                            sequencesCollection.Add(sequence);

                            sequence = new Dictionary<string, string>();
                        }
                    }
                }

                return sequencesCollection;
            }

            return null;
        }

        public static List<Dictionary<string,string>> CreatePowerConsumptionDataset(string sequenceFormat, DateTime startDate, DateTime endDate)
        {
            int keyForUniqueIndexes = 0;
            string[] sequenceFormatType = { "byMonth", "byWeek", "byDay" };
            int[] daysOfMonth = { -1, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            double totalCount = 0;
            double actualCount = 0;
            int count = 0;
            int maxCount = 0;
            bool firstTime = true;
            double power = 0;
            List<Dictionary<string, string>> sequencesCollection = new List<Dictionary<string, string>>();
            var random = new ThreadSafeRandom();
            TimeSpan ts = endDate - startDate;
            totalCount = ts.TotalHours;
            Dictionary<string, string> sequence = new Dictionary<string, string>();


            while (true)
            {
                if (firstTime)
                {
                    if (sequenceFormatType[0].Equals(sequenceFormat))        /* byMonth */
                        maxCount = daysOfMonth[startDate.Month] * 24;
                    else if (sequenceFormatType[1].Equals(sequenceFormat))   /* byWeek  */
                        maxCount = 7 * 24;
                    else if (sequenceFormatType[2].Equals(sequenceFormat))   /* byDay   */
                        maxCount = 24;

                    firstTime = false;
                }

                string dateTime = $"{startDate.Day.ToString("00")}/{startDate.Month.ToString("00")}/{startDate.Year.ToString("00")} {startDate.Hour.ToString("00")}:{startDate.Minute.ToString("00")}";
                power = random.Next(500,5000)/1000;

                if (sequence.ContainsKey(power.ToString()))
                    continue;
                else
                    sequence.Add(power.ToString(), dateTime);

                startDate = startDate.AddHours(1);
                count++;
                actualCount++;
                /*
                 * Creating multiple sequences for each month
                 */
                if (count >= maxCount)
                {
                    count = 0;
                    maxCount = 0;
                    firstTime = true;

                    sequencesCollection.Add(sequence);

                    sequence = new Dictionary<string, string>();
                }

                if (actualCount >= totalCount)
                    break;
            }

            return sequencesCollection;
        }

        /// <summary>
        /// HTM Config for creating Connections
        /// </summary>
        /// <param name="inputBits"></param>
        /// <param name="numColumns"></param>
        /// <returns></returns>
        public static HtmConfig FetchHTMConfig(int inputBits, int numColumns)
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
                ConnectedPermanence = 0.5,

                // Learning is slower than forgetting in this case.
                PermanenceDecrement = 0.25,
                PermanenceIncrement = 0.15,

                // Used by punishing of segments.
                PredictedSegmentDecrement = 0.1,

                //NumInputs = 88
            };

            return cfg;
        }

        /// <summary>
        /// Takes in user input and return encoded SDR for prediction
        /// </summary>
        /// <param name="userInput"></param>
        /// <returns></returns>
        public static int[] EncodeSingleInput(string userInput)
        {
            DateTime date = DateTime.Parse(userInput);
            var day = date.Day;
            var month = date.Month;
            var week = date.DayOfWeek;
            var segment = date.Hour;

            EncoderBase dayEncoder = FetchDayEncoder();
            EncoderBase monthEncoder = FetchMonthEncoder();
            EncoderBase weekEncoder = FetchWeekEncoder();
            EncoderBase segmentEncoder = FetchSegmentEncoder();

            int[] sdr = new int[0];

            sdr = sdr.Concat(dayEncoder.Encode(day)).ToArray();
            sdr = sdr.Concat(monthEncoder.Encode(month)).ToArray();
            sdr = sdr.Concat(segmentEncoder.Encode(segment)).ToArray();
            sdr = sdr.Concat(weekEncoder.Encode(week)).ToArray();

            return sdr;
        }

        /// <summary>
        /// Encodes the DateTime from the List of Dictionary (which was CSV File) using ScalerEncoder 
        /// and return SDR of DateTime in List of Dictionary
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<Dictionary<string, int[]>> EncodePowerConsumptionData(List<Dictionary<string, string>> data, bool trace = false)
        {
            List<Dictionary<string, int[]>> listOfSDR = new List<Dictionary<string, int[]>>();

            ScalarEncoder segmentEncoder = FetchSegmentEncoder();
            ScalarEncoder dayEncoder = FetchDayEncoder();
            ScalarEncoder monthEncoder = FetchMonthEncoder();
            ScalarEncoder weekEncoder = FetchWeekEncoder();

            foreach (var sequence in data)
            {
                var tempDic = new Dictionary<string, int[]>();

                foreach (var keyValuePair in sequence)
                {
                    var label = keyValuePair.Key;
                    var value = keyValuePair.Value;

                    string[] formats = { "MM/dd/yy hh:mm" };
                    //DateTime dateTime = DateTime.ParseExact(value, formats, CultureInfo.InvariantCulture);
                    DateTime dateTime = DateTime.Parse(value);
                    var day = dateTime.Day;
                    var month = dateTime.Month;
                    var week = dateTime.DayOfWeek;
                    var hour = dateTime.Hour;

                    int[] sdr = new int[0];

                    sdr = sdr.Concat(dayEncoder.Encode(day)).ToArray();
                    sdr = sdr.Concat(monthEncoder.Encode(month)).ToArray();
                    sdr = sdr.Concat(segmentEncoder.Encode(hour)).ToArray();
                    sdr = sdr.Concat(weekEncoder.Encode(week)).ToArray();

                    //logger.WriteInformation(Helpers.StringifyVector(sdr));

                    tempDic.Add(label, sdr);
                }

                listOfSDR.Add(tempDic);
            }


            return listOfSDR;
        }

        /// <summary>
        /// ScalarEncoder which returns config for Day of Month
        /// </summary>
        /// <returns></returns>
        public static ScalarEncoder FetchDayEncoder()
        {
            ScalarEncoder dayEncoder = new ScalarEncoder(new Dictionary<string, object>()
            {
                { "W", 7},
                { "N", 38},
                { "MinVal", (double)1},  // Min value = (1).
                { "MaxVal", (double)32}, // Max value = (31).
                { "Periodic", true},
                { "Name", "Date"},
                { "ClipInput", true},
           });

            return dayEncoder;
        }

        /// <summary>
        /// MonthEncoder which returns config for Month of Year
        /// </summary>
        /// <returns></returns>
        public static ScalarEncoder FetchMonthEncoder()
        {
            ScalarEncoder monthEncoder = new ScalarEncoder(new Dictionary<string, object>()
            {
                { "W", 5},
                { "N", 17},
                { "MinVal", (double)1},  // Min value = (1).
                { "MaxVal", (double)13}, // Max value = (12).
                { "Periodic", true},
                { "Name", "Month"},
                { "ClipInput", true},
            });
            return monthEncoder;
        }

        /// <summary>
        /// SegmentEncoder which returns config for Segment of Day
        /// </summary>
        /// <returns></returns>
        public static ScalarEncoder FetchSegmentEncoder()
        {
            ScalarEncoder segmentEncoder = new ScalarEncoder(new Dictionary<string, object>()
            {
                { "W", 9},
                { "N", 34},
                { "MinVal", (double)0},      // Min value = (0)
                { "MaxVal", (double)23 + 1}, //Max value = (23)
                { "Periodic", true},
                { "Name", "Hour of the day."},
                { "ClipInput", true},
            });
            return segmentEncoder;
        }

        /// <summary>
        /// YearEncoder which returns config for Year
        /// </summary>
        /// <returns></returns>
        public static ScalarEncoder FetchYearEncoder()
        {
            ScalarEncoder yearEncoder = new ScalarEncoder(new Dictionary<string, object>()
            {
                { "W", 5},
                { "N", 9},
                { "MinVal", (double)2009}, // Min value = (2009).
                { "MaxVal", (double)2012}, // Max value = (2012).
                { "Periodic", false},
                { "Name", "Year"},
                { "ClipInput", true},
            });
            return yearEncoder;
        }

        /// <summary>
        /// WeekEncoder which return config for Day of the Week
        /// </summary>
        /// <returns></returns>
        public static ScalarEncoder FetchWeekEncoder()
        {
            ScalarEncoder weekEncoder = new ScalarEncoder(new Dictionary<string, object>()
            {
                { "W", 3},
                { "N", 11},
                { "MinVal", (double)0}, // Min value = 0.
                { "MaxVal", (double)7}, // Max value = 6.
                { "Periodic", false},
                { "Name", "Year"},
                { "ClipInput", true},
            });
            return weekEncoder;
        }

        /// <summary>
        /// MultiEncoder for encoding DateTime
        /// </summary>
        /// <returns></returns>
        public static MultiEncoder FetchDateTimeEncoder()
        {
            EncoderBase segmentEncoder = FetchSegmentEncoder();
            EncoderBase dayEncoder = FetchDayEncoder();
            EncoderBase monthEncoder = FetchMonthEncoder();
            EncoderBase weekEncoder = FetchWeekEncoder();

            List<EncoderBase> datetime = new List<EncoderBase>();
            datetime.Add(segmentEncoder);
            datetime.Add(dayEncoder);
            datetime.Add(monthEncoder);
            datetime.Add(weekEncoder);

            MultiEncoder datetimeEncoder = new MultiEncoder(datetime);

            return datetimeEncoder;
        }
    }
}
