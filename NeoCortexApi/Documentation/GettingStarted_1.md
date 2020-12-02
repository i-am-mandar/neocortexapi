# Getting started

First, input is given as a list.

```cs
            int inputBits = 100;

            double max = 20;
            int numColumns = 2048;

            List<double> inputValues = new List<double>(new double[] { 0.0, 1.0, 0.0, 2.0, 3.0, 4.0, 5.0, 6.0, 5.0, 4.0, 3.0, 7.0, 1.0, 9.0, 12.0, 11.0, 12.0, 13.0, 14.0, 11.0, 12.0, 14.0, 5.0, 7.0, 6.0, 9.0, 3.0, 4.0, 3.0, 4.0, 3.0, 4.0 });
            int numInputs = inputValues.Distinct().ToList().Count;

            var inputs = inputValues.ToArray();
```

## Encoder

Encoder is chosen according to the type of the inputs. There are some encoder available for popular input type:

- Scalar Encoder
- Datetime Encoder
- Boolean Encoder
- Category Encoder
- Geo-Spatial Encoder

In this example, ScalarEncoder is preferred as inputs are all numbers. The encoder is instantiated with predefined settings. The inputs will be encoded as series of '0's and '1's so that the spatial pooler will understand and proceed with its own computation.

```cs
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

            EncoderBase encoder = new ScalarEncoder(settings);
```

## Spatial Pooler

Type of Spatial Pooler (SP) that is used in this example is the multithreaded version that utilize multicore of the machine to run the spatial pooler algorithm. The `HomeostaticPlasticityController` is included in the spatial pooler algorithm to implement the "new born" effect. This effect tracks the learning process of the SP and switches-off the boosting mechanism (new-born effect) after the SP has entered a stable state for all seen input patterns.

```cs
            HtmConfig htmConfig = Connections.GetHtmConfigDefaultParameters();
            htmConfig.Random = new ThreadSafeRandom(42);
            htmConfig.InputDimensions = new int[] { inputBits };
            htmConfig.ColumnDimensions = new int[] { numColumns };
            htmConfig.CellsPerColumn = 25;
            htmConfig.GlobalInhibition = true;
            htmConfig.LocalAreaDensity = -1;
            htmConfig.NumActiveColumnsPerInhArea = 0.02 * numColumns;
            htmConfig.PotentialRadius = 50;
            htmConfig.InhibitionRadius = 15;
            htmConfig.MaxBoost = 10.0;
            htmConfig.DutyCyclePeriod = 25;
            htmConfig.MinPctOverlapDutyCycles = 0.75;
            htmConfig.MaxNewSynapseCount = (int)(0.02 * numColumns);
            htmConfig.ActivationThreshold = 15;
            htmConfig.ConnectedPermanence = 0.5;
            htmConfig.PermanenceDecrement = 0.25;
            htmConfig.PermanenceIncrement = 0.15;
            htmConfig.PredictedSegmentDecrement = 0.1;

            Connections memory = new Connections(htmConfig);

            HomeostaticPlasticityController hpa = new HomeostaticPlasticityController(memory, numInputs * 55,
            (isStable, numPatterns, actColAvg, seenInputs) =>
            {
                if (isStable)
                    // Event should be fired when entering the stable state.
                    Debug.WriteLine($"STABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");
                else
                    // Ideal SP should never enter unstable state after stable state.
                    Debug.WriteLine($"INSTABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");
            }, numOfCyclesToWaitOnChange: 25);

            SpatialPoolerMT spatialPooler = new SpatialPoolerMT(hpa);
            spatialPooler.Init(memory, UnitTestHelpers.GetMemory());
```

## Temporal Memory

Initialization of Temporal Memory.

```cs
            TemporalMemory temporalMemory = new TemporalMemory();
            temporalMemory.Init(memory);

```

## Combine components

After initializing components, they are chained together in `CortexLayer` as HtmModule pipelines in the following order:

Input &#8594; Encoder &#8594; Spatial Pooler &#8594; Temporal Memory &#8594; Output (`ComputeCycle`)

```cs
            List<CortexRegion> regions = new List<CortexRegion>();
            CortexRegion region0 = new CortexRegion("1st Region");

            regions.Add(region0);

            CortexLayer<object, object> layer1 = new CortexLayer<object, object>("L1");
            region0.AddLayer(layer1);
            layer1.HtmModules.Add("encoder", encoder);
            layer1.HtmModules.Add("sp", spatialPooler);
            layer1.HtmModules.Add("tm", temporalMemory);

            bool learn = true;

            int maxCycles = 3500;

            for (int i = 0; i < maxCycles; i++)
            {
                foreach (var input in inputs)
                {
                    var lyrOut = layer1.Compute(input, learn) as ComputeCycle;
                }
            }
```