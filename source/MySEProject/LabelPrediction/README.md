# Project Name: Investigate Label Prediction from the time-series sequence

Description:
=============

1.Objective
-------------

In nature there are many events which occur periodically, here were are trying to identify such time-series sequence which is associated entity/label and the end goal is to learn time-series with label and predict the label at given time.

Examples of time-series can be retail sales, weather forecast, power consumption, peak hours of taxi and so on.

2.Approach
-------------

For our experiment we choose to work with [dataset of power consumption] (https://github.com/numenta/nupic/blob/master/src/nupic/datafiles/extra/hotgym/rec-center-hourly.csv) where the consumption of power is noted at an interval of an hour. See below for example:

```
timestamp,consumption
datetime,float
7/1/10 0:00,21.2
7/1/10 1:00,16.4
7/1/10 2:00,4.7
7/1/10 3:00,4.7
7/1/10 4:00,4.6
7/1/10 5:00,23.5
7/1/10 6:00,47.5
7/1/10 7:00,45.4
```

After reformatting the datetime to standard format and we create a series of time segment. Here the segmentation is done on hourly basis so there are 24 segment for a single day and the dataset is 6 months long.

```
01/07/10 00:00,21.2
01/07/10 01:00,16.4
01/07/10 02:00,4.7
01/07/10 03:00,4.7
01/07/10 04:00,4.6
01/07/10 05:00,23.5
01/07/10 06:00,47.5
01/07/10 07:00,45.4
```

For learning time-series and predicting it we are using Multi-sequence Learning using HTM Classifier [example] (https://github.com/i-am-mandar/neocortexapi/blob/master/source/Samples/NeoCortexApiSample/MultisequenceLearning.cs) which is been forked form [NeoCortexApi](https://github.com/ddobric/neocortexapi)

The HTM Classifier consists of Spatial Pooler and Temporal Memory which takes in encoded data to learn the sequence.

I. Create Multiple Sequences of the time-series

II. Encode the datetime as segment

III. Learn using HTM Classifier

IV. Predict the label 

3.Encoding and Learning
-------------

I.After the datetime was reformatted and created multiple sequences of the segments as below:

Sequence 1:
```
01/07/10 00,21.2
01/07/10 01,16.4
01/07/10 02,4.7
01/07/10 03,4.2
:      :      :
01/07/10 21,23.5
01/07/10 22,47.5
01/07/10 23,45.4
```

Sequence 2:
```
01/07/10 00,22.2
01/07/10 01,13.4
01/07/10 02,5.7
01/07/10 03,4.3
:      :      :
01/07/10 21,21.5
01/07/10 22,43.3
01/07/10 23,47.4
```

and so on

II. Encode the segment

The segment is broken down as Date, Month, Day of Week and Hour which is encoded individually and then concatinated. The encoder used is [Scalar Encoder] (https://github.com/i-am-mandar/neocortexapi/blob/master/source/NeoCortexApi/Encoders/ScalarEncoder.cs)

Following are the configuration used:

```csharp
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
```

```csharp
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
```

```csharp
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
```

```csharp
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
```

III. Learn using HTM Classifier

The following HTM Config was used:

```csharp
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
```

Algorithm for Mutli-sequence Learning
```
01. Get HTM Config and initialize memory of Connections 
02. Initialize HTM Classifier and Cortex Layer
03. Initialize HomeostaticPlasticityController
04. Initialize memory for Spatial Pooler and Temporal Memory
05. Add Spatial Pooler memory to Cortex Layer
	05.01 Compute the SDR of all encoded segment for Multi-sequences using Spatial Pooler
	05.02 Continue for maximum number of cycles
06. Add Temporal Memory to Cortex Layer
    06.01 Compute the SDR as Compute Cycle and get Active Cells
	06.02 Learn the Label with Active Cells
	06.03 Get the input predicted values and update the last predicted value depending upon the similarity
	06.04 Reset the Temporal Memory
	06.05 Continue all above steps for sequences of Multi-sequences for maximum cycles
07. Get the trained Cortex Layer and HTM Classifier
```

IV. Predict the label 

The trained Cortex Layer can now be used to compute the Compute Cycle and the HTM Classifier will give the predicted input values as shown below:

```csharp
public List<ClassifierResult<string>> Predict(int[] input)
{
    var lyrOut = this.Layer.Compute(input, false) as ComputeCycle;

    List<ClassifierResult<string>> predictedInputValues = this.Classifier.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 3);

    return predictedInputValues;
}
```


4.Results
-------------

In this experiment, total 40 times the project was ran to get and check on the results. On the X-axis the number of runs is mentioned. On the Y-axis there is maximum percentage of accuracy.  The results are shown in below figure 1.

![Figure 1](https://github.com/i-am-mandar/neocortexapi/blob/master/source/MySEProject/Documentation/experiment.png)


5.Discussion
-------------

Learning and predicting time-series have been experimented for decades and various methods have been used. Adapting an existing method for large and noisy data remains a challenge. In the experiment, HTM Classifier is used which is a recently developed neural network based on cortex of human brain and not just a single neuron model. 

But there were some hardware limitations running on local machine. To solve this in better way the road of using cloud can be taken to scale up the learning process.

Low accuracy is seen due to small size of dataset has been consider in the experiment. As seen while the run 744 segment took around 35 to 38 minutes to be trained. Also, long runtime was seen when more cycles where used.


6.References
-------------
a. [NeoCortexApi](https://github.com/ddobric/neocortexapi)

b. [Hierarchical Temporal Memory (HTM) Whitepaper](https://numenta.com/neuroscience-research/research-publications/papers/hierarchical-temporal-memory-white-paper/)

c. [Encoding Data for HTM Systems](https://arxiv.org/abs/1602.05925)

d. [Properties of Sparse Distributed Representations and their Application to Hierarchical Temporal Memory](https://arxiv.org/abs/1503.07469)

e. [A thousand brains: toward biologically constrained AI](https://link.springer.com/article/10.1007/s42452-021-04715-0)

f. [The HTM Spatial Pooler—A Neocortical Algorithm for Online Sparse Distributed Coding](https://numenta.com/neuroscience-research/research-publications/papers/htm-spatial-pooler-neocortical-algorithm-for-online-sparse-distributed-coding/)

