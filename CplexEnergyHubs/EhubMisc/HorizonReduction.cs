using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EhubMisc
{
    public static class DemandParameterization
    {
        /// <summary>
        /// Typical days of a full annual hourly time series
        /// </summary>
        public struct TypicalDays
        {
            /// <summary>
            /// Number of total typical days, including peak days.
            /// </summary>
            public int NumOfDays;
            /// <summary>
            /// Number of total peak days, excluding regular typical days.
            /// </summary>
            public int NumOfPeakDays;
            /// <summary>
            /// Number of total typical days, excluding peak days.
            /// </summary>
            public int NumOfTypicalDays;
            /// <summary>
            /// Typical days demand profiles. First index representing demand type, second index is the full timeseries for all days, e.g. 24 x NumOfDays. Note: the peak days are appended to the end in the order as indicated in `this.PeakDayDemandType`.
            /// </summary>
            public double[][] DayProfiles;
            /// <summary>
            /// Indicating, how many days one typical day represents. In other words, the cluster sizes. Note: The peak days will always only be DaysPerTypicalDay[peakday] = 1.
            /// </summary>
            public int[] DaysPerTypicalDay;
            /// <summary>
            /// Description of each load type, e.g. {"heating", "cooling", "electricity"}
            /// </summary>
            public string[] LoadTypes;
            /// <summary>
            /// Indicating the day of the year [1, 365] that each typical day represents. In other words, the cluster medoids. The peak days are not medoids (no medoids) are at the end of the array. Length if this array equals TypicalDays.LoadTypes.Length
            /// </summary>
            public int[] DayOfTheYear;
            /// <summary>
            /// Index of the cluster that each day belongs to [0, NumOfTypicalDays]. Length of this array is 365 - NumOfPeakDays. It refers to the reduced timeseries set, where the peak days have been removed from.
            /// </summary>
            public int[] ClusterIdPerDay;
            /// <summary>
            /// Scaling factor for entire DayProfiles, such that DayProfiles x DaysPerTypicalDay matches the total annual loads of the original load profiles. ScalingFactor[LoadType] already applied to DayProfiles[LoadType][]
            /// </summary>
            public double[] ScalingFactor;
            /// <summary>
            /// Total loads per LoadType. The sum of the original 8760 timeseries should match the sum of DayProfiles x DaysPerTypicalDay.
            /// </summary>
            public double[] TotalLoads;
            /// <summary>
            /// Sum of DayProfiles x DaysPerTypicalDay, if no scaling had been applied. Will likely not match the original 8760 timeseries.
            /// </summary>
            public double[] TotalLoadsWithoutScaling;
        }


        /// <summary>
        /// Generating typical demand profile days from an annual hourly time series.
        /// Source: Dominguez-Munoz, Cejudo-Lopez, Carrillo-Andres (2011). "Selection of typical demand days for CHP optimization"
        /// Energy and Buildings 43(11), pp. 3036-3043. doi: 10.1016/j.enbuild.2011.07.024
        /// </summary>
        /// <param name="fullProfiles">Full annual hourly demand profiles. First index for demand types, second index for timesteps.</param>
        /// <param name="loadTypes">Description for each demand type, e.g. {"cooling", "heating", "electricity"}.</param>
        /// <param name="numberOfTypicalDays">Number of typical days.</param>
        /// <param name="peakDays">Adding the peak day per demand type? One boolean per demand type. Days will be added to the regular typical days. E.g. 12 typical days + peak days.</param>
        /// <returns>Returns a TypicalDays structure</returns>
        public static TypicalDays GenerateTypicalDays(double[][] fullProfiles, string[] loadTypes, int numberOfTypicalDays, bool[] peakDays, bool verbose = true)
        {
            TypicalDays typicalDays = new TypicalDays();

            //int[] seeds = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            int[] seeds = new int[1] { 42 };
            int days = 365;
            int hoursPerDay = 24;
            int numberOfLoadTypes = loadTypes.Length;
            typicalDays.LoadTypes = loadTypes;
            typicalDays.NumOfPeakDays = peakDays.Count(c => c);
            typicalDays.NumOfTypicalDays = numberOfTypicalDays;
            typicalDays.NumOfDays = numberOfTypicalDays + typicalDays.NumOfPeakDays;
            typicalDays.TotalLoads = new double[numberOfLoadTypes];
            for (int load = 0; load < numberOfLoadTypes; load++)
                typicalDays.TotalLoads[load] = fullProfiles[load].Sum();
            int clusters = numberOfTypicalDays;

            /// 1. Identify peak days from main dataset X
            ///         only for those demands with peakDays[demand] == true
            ///         because they will be re-inserted later as separate day
            /// 
            typicalDays.DayOfTheYear = new int[typicalDays.NumOfDays];
            typicalDays.DaysPerTypicalDay = new int[typicalDays.NumOfDays];
            for (int d = 0; d < typicalDays.NumOfTypicalDays; d++)
                typicalDays.DayOfTheYear[d] = 0;
            int _dayCounter = 0;
            for (int load = 0; load < numberOfLoadTypes; load++)
            {
                if (peakDays[load])
                {
                    int dayOfPeak = Array.IndexOf(fullProfiles[load], fullProfiles[load].Max());
                    dayOfPeak = (int)Math.Ceiling((double)(dayOfPeak + 1) / hoursPerDay);
                    typicalDays.DayOfTheYear[typicalDays.NumOfTypicalDays + _dayCounter] = dayOfPeak;
                    typicalDays.DaysPerTypicalDay[typicalDays.NumOfTypicalDays + _dayCounter] = 1;
                    _dayCounter++;
                }
            }


            /// 2. reshape profiles into 365 x (24 * demand types) dataset, called X
            ///         e.g. with elec and cooling, we have 365 x 48
            /// 
            /// normalize X for clustering
            ///     demand types can have different scales! e.g. solar, heating, elec, etc.
            ///     but the clustering indends to cluster according to similarity of their profiles
            /// remove peak days
            /// 
            double[] lowerBounds = new double[numberOfLoadTypes];
            double[] upperBounds = new double[numberOfLoadTypes];
            for (int d = 0; d < numberOfLoadTypes; d++)
            {
                lowerBounds[d] = fullProfiles[d].Min();
                upperBounds[d] = fullProfiles[d].Max();
            }

            double[][] Xcomplete = new double[days][];
            double[][] X = new double[days - typicalDays.NumOfPeakDays][];
            _dayCounter = 0;
            for (int d = 0; d < days; d++)
            {
                X[_dayCounter] = new double[hoursPerDay * numberOfLoadTypes];
                Xcomplete[d] = new double[hoursPerDay * numberOfLoadTypes];
                for (int h = 0; h < hoursPerDay; h++)
                {
                    for (int load = 0; load < numberOfLoadTypes; load++)
                    {
                        double _value = (fullProfiles[load][h + (d * hoursPerDay)] - lowerBounds[load]) / (upperBounds[load] - lowerBounds[load]);
                        int _hour = h + (load * hoursPerDay);
                        if (!typicalDays.DayOfTheYear.Contains(d + 1))
                            X[_dayCounter][_hour] = _value;
                        Xcomplete[d][_hour] = _value;
                    }
                }
                if (!typicalDays.DayOfTheYear.Contains(d + 1)) 
                    _dayCounter++;
            }


            /// 3. perform clustering with normalized X, excluding peak days
            /// 
            List<Tuple<int[], int[][], double>> replicates = new List<Tuple<int[], int[][], double>>();
            foreach (int seed in seeds)
            {
                replicates.Add(null);
            }
            if (verbose)
            {
                Console.WriteLine("");
                Console.WriteLine("{0} available threads for clustering...", Environment.ProcessorCount);
                Console.WriteLine("");
            }
            Parallel.For(0, seeds.Length, seed =>
            {
                if (verbose) Console.WriteLine("Performing clustering for seed {0} of {1} total seeds...", seed + 1, seeds.Length);
                replicates[seed] = EhubMisc.Clustering.KMedoids(X, clusters, 50, seed);
                if (verbose) Console.WriteLine("...clustering finished for seed {0} of {1} total seeds.", seed + 1, seeds.Length);
            });

            int minIndex = 0;
            double minCost = replicates[0].Item3;
            for (int i = 1; i < replicates.Count; i++)
                if (replicates[i].Item3 < minCost)
                {
                    minIndex = i;
                    minCost = replicates[i].Item3;
                }
            Tuple<int[], int[][], double> clusteredData = replicates[minIndex];
            if (verbose)
            {
                Console.WriteLine("");
                Console.WriteLine("Choosing cluster solution {0} for the MILP", minIndex);
                Console.WriteLine("");
            }

            for (int d = 0; d < typicalDays.NumOfTypicalDays; d++)
            {
                typicalDays.DayOfTheYear[d] = clusteredData.Item1[d];
                typicalDays.DaysPerTypicalDay[d] = clusteredData.Item2[d].Length;
            }

            Dictionary<int, int> idx = new Dictionary<int, int>();
            for (int _k = 0; _k < clusters; _k++)
                for (int i = 0; i < clusteredData.Item2[_k].Length; i++)
                {
                    int index = clusteredData.Item2[_k][i];
                    idx.Add(index, _k);
                }
            idx = idx.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            typicalDays.ClusterIdPerDay = new int[idx.Count];
            for (int i = 0; i < idx.Count; i++)
                typicalDays.ClusterIdPerDay[i] = idx[i];


            // sort DayOfTheYear (key) and DayPerTypicalDay (value)
            //make a bool array for peakdays, sorted also
            bool[] _peakDays = new bool[typicalDays.NumOfDays];
            for(int d=typicalDays.NumOfTypicalDays; d<typicalDays.NumOfDays; d++)
                _peakDays[d] = true;
            int[] _copyDaysOfTheYear = new int[typicalDays.NumOfDays];
            typicalDays.DayOfTheYear.CopyTo(_copyDaysOfTheYear, 0);
            Array.Sort(_copyDaysOfTheYear, _peakDays);
            Array.Sort(typicalDays.DayOfTheYear, typicalDays.DaysPerTypicalDay);


            typicalDays.DayProfiles = new double[numberOfLoadTypes][];
            for (int load = 0; load < numberOfLoadTypes; load++)
            {
                // fill the final profiles for the MILP with the medoids profiles
                typicalDays.DayProfiles[load] = new double[hoursPerDay * typicalDays.NumOfDays];
                for (int d = 0; d < typicalDays.NumOfDays; d++)
                    for (int h = 0; h < hoursPerDay; h++)
                    {
                        if (_peakDays[d])
                        {
                            // get it from original data, where peak days have not been culled from 
                            typicalDays.DayProfiles[load][h + d * hoursPerDay] = Xcomplete[typicalDays.DayOfTheYear[d]][h + load * hoursPerDay];
                        }
                        else
                        {
                            // get it from X
                            typicalDays.DayProfiles[load][h + d * hoursPerDay] = X[typicalDays.DayOfTheYear[d]][h + load * hoursPerDay];
                        }

                    }

                //for (int d = 0; d < numberOfTypicalDays; d++)
                //    for (int h = 0; h < hoursPerDay; h++)
                //        typicalDays.DayProfiles[load][h + d * hoursPerDay] = X[clusteredData.Item1[d]][h + load * hoursPerDay]; 

                //// fill it with the peak days
                //for(int d=0; d<typicalDays.NumOfPeakDays; d++)
                //    for(int h=0; h<hoursPerDay; h++)
                //        typicalDays.DayProfiles[load][h + d * hoursPerDay + numberOfTypicalDays * hoursPerDay] = X[typicalDays.DayOfTheYear[d + numberOfTypicalDays]][h + load * hoursPerDay];
            }

            
            /// 4. revert normalization for DayProfiles (and X and Xcomplete?)


            /// 5. Calculation of scaling factors
            ///     summing total loads for all observations in a cluster
            ///     summing total loads


            // ScalingFactor
            // TotalLoadsWithoutScaling


            return typicalDays;
        }


        /*
        /// <summary>
        /// 
        /// </summary>
        /// <param name="demandProfiles"></param>
        /// <param name="numberOfStochasticProfiles"></param>
        /// <param name="variationFromMean">Variation from nominal mean in %. I.e. if mean of heating is 20kW, and we have 10% variation, then for each time step it can vary up to +/- 2kW, normally distributed</param>
        /// <param name="constantShift"></param>
        /// <returns></returns>
        public static double[][][] GenerateStochasticDemands(double[][] demandProfiles, int numberOfStochasticProfiles, double variationFromMean, bool constantShift = false)
        {
            double[][][] newProfiles = new double[numberOfStochasticProfiles][][];
            
            return newProfiles;
        }
        */
    }
}
