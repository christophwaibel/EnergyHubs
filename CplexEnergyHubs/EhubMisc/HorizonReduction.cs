using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EhubMisc
{
    public static class HorizonReduction
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
            /// Same as DayProfiles, but uncorrected (see part 7 in algorithm)
            /// </summary>
            public double[][] UncorrectedDayProfiles;
            /// <summary>
            /// Indicating, how many days one typical day represents. In other words, the cluster sizes. Note: The peak days will NOT necessarly be DaysPerTypicalDay[peakday] = 1, because it is sorted according to day of the year. Refer to ClusterID
            /// </summary>
            public int[] DaysPerTypicalDay;
            /// <summary>
            /// Number of days that each timestep represents, i.e. the clustersize of the cluster that this timestep belongs to. Peak days included, but they will have a value of 1
            /// </summary>
            public int[] NumberOfDaysPerTimestep;
            /// <summary>
            /// Sorted cluster IDs, sorted according to day of the year.
            /// </summary>
            public int[] ClusterID;
            /// <summary>
            /// Indicates whether this typical day is a peak day. If so, then it also only has DayPerTypicalDay[day] = 1
            /// </summary>
            public bool[] IsPeakDay;
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
            /// Scaling factor per load and per timestep. Note: if IsTypicalDay[day], then ScalingFactorPerTimestep[load][timestep->day] = 1.0; else, it can be any factor. Needs to be used in energy hub to account for total carbon emissions and cost, to match original total loads
            /// </summary>
            public double[][] ScalingFactorPerTimestep;
            /// <summary>
            /// Total loads per LoadType. The sum of the original 8760 timeseries should match the sum of DayProfiles x DaysPerTypicalDay.
            /// </summary>
            public double[] TotalLoads;
            public int Horizon;
        }


        /// <summary>
        /// Generating typical demand profile days from an annual hourly time series.
        /// Loosely based on: Dominguez-Munoz, Cejudo-Lopez, Carrillo-Andres (2011). "Selection of typical demand days for CHP optimization"
        /// Energy and Buildings 43(11), pp. 3036-3043. doi: 10.1016/j.enbuild.2011.07.024
        /// as well as Matlabscript 'Demand_parameterization_ECOS_NEW.m' by gmavroma@ethz.ch
        /// </summary>
        /// <param name="fullProfiles">Full annual hourly demand profiles. First index for demand types, second index for timesteps.</param>
        /// <param name="loadTypes">Description for each demand type, e.g. {"cooling", "heating", "electricity"}.</param>
        /// <param name="numberOfTypicalDays">Number of typical days.</param>
        /// <param name="peakDays">Adding the peak day per demand type? One boolean per demand type. Days will be added to the regular typical days. E.g. 12 typical days + peak days.</param>
        /// <param name="useForClustering">Specifying which load type is used in the clustering. e.g. it might be wise to not include 20 solar profiles in the clustering ,because they get too much emphasize. instead, just use heating, cooling, electricity, and one profile for global horizontal irradiance. this array correspond to the loadTypes string array</param>
        ///<param name="verbose"></param>
        ///<param name="dataScaling">data scaling mode: "standardization" (default), "normalization"</param>
        /// <returns>Returns a TypicalDays structure</returns>
        public static TypicalDays GenerateTypicalDays(double[][] fullProfiles, string[] loadTypes, int numberOfTypicalDays, bool[] peakDays, bool[] useForClustering, bool verbose = true, string dataScaling = "standardization")
        {
            TypicalDays typicalDays = new TypicalDays();

            int[] seeds = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            const int days = 365;
            const int hoursPerDay = 24;
            int numberOfLoadTypes = loadTypes.Length;
            typicalDays.LoadTypes = loadTypes;
            typicalDays.NumOfPeakDays = peakDays.Count(c => c);
            typicalDays.NumOfTypicalDays = numberOfTypicalDays;
            typicalDays.NumOfDays = numberOfTypicalDays + typicalDays.NumOfPeakDays;
            typicalDays.Horizon = typicalDays.NumOfDays * hoursPerDay;
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
            double[] mean = new double[numberOfLoadTypes];
            double[] sigma = new double[numberOfLoadTypes];
            for (int load = 0; load < numberOfLoadTypes; load++)
            {
                lowerBounds[load] = fullProfiles[load].Min();
                upperBounds[load] = fullProfiles[load].Max();

                mean[load] = fullProfiles[load].Sum() / fullProfiles[load].Length;
                sigma[load] = 0.0;
                int N = fullProfiles[load].Length;
                for(int i=0; i<N; i++)
                    sigma[load] += Math.Pow(fullProfiles[load][i] - mean[load], 2);
                sigma[load] /= N;
                sigma[load] = Math.Sqrt(sigma[load]);
            }

            double[][] Xcomplete = new double[days][];
            double[][] X = new double[days - typicalDays.NumOfPeakDays][];
            double[][] Xclustering = new double[days - typicalDays.NumOfPeakDays][];
            _dayCounter = 0;
            for (int d = 0; d < days; d++)
            {
                if (!typicalDays.DayOfTheYear.Contains(d + 1))
                {
                    X[_dayCounter] = new double[hoursPerDay * numberOfLoadTypes];
                    Xclustering[_dayCounter] = new double[hoursPerDay * useForClustering.Count(c => c)];
                    _dayCounter++;
                }
                Xcomplete[d] = new double[hoursPerDay * numberOfLoadTypes];
                for (int h = 0; h < hoursPerDay; h++)
                {
                    for (int load = 0; load < numberOfLoadTypes; load++)
                    {
                        double _value;
                        if (string.Equals(dataScaling, "normalization")) _value = (fullProfiles[load][h + (d * hoursPerDay)] - lowerBounds[load]) / (upperBounds[load] - lowerBounds[load]);
                        else _value = (fullProfiles[load][h + (d * hoursPerDay)] - mean[load]) / sigma[load];
                        int _hour = h + (load * hoursPerDay);
                        if (!typicalDays.DayOfTheYear.Contains(d + 1))
                        {
                            X[_dayCounter - 1][_hour] = _value;
                            if (useForClustering[load])
                                Xclustering[_dayCounter - 1][_hour] = _value;
                        }
                        Xcomplete[d][_hour] = _value;
                    }
                }
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
                replicates[seed] = EhubMisc.Clustering.KMedoids(Xclustering, clusters, 50, seed);
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

            typicalDays.ClusterID = new int[typicalDays.NumOfDays];
            for (int i = 0; i < typicalDays.NumOfDays; i++)
                typicalDays.ClusterID[i] = -1;

            Dictionary<int, int> idx = new Dictionary<int, int>();
            for (int _k = 0; _k < clusters; _k++)
            {
                for (int i = 0; i < clusteredData.Item2[_k].Length; i++)
                {
                    int index = clusteredData.Item2[_k][i];
                    idx.Add(index, _k);
                }
                typicalDays.ClusterID[_k] = _k;
            }
            idx = idx.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            typicalDays.ClusterIdPerDay = new int[idx.Count];
            for (int i = 0; i < idx.Count; i++)
                typicalDays.ClusterIdPerDay[i] = idx[i];


            /// 4. Sort DayOfTheYear (key) and DayPerTypicalDay (value)
            ///      make a bool array for peakdays, sorted also
            typicalDays.IsPeakDay = new bool[typicalDays.NumOfDays];
            for (int d = typicalDays.NumOfTypicalDays; d < typicalDays.NumOfDays; d++)
                typicalDays.IsPeakDay[d] = true;
            int[] _copyDaysOfTheYear = new int[typicalDays.NumOfDays];
            int[] _copyDaysOfTheYear2 = new int[typicalDays.NumOfDays];
            typicalDays.DayOfTheYear.CopyTo(_copyDaysOfTheYear, 0);
            typicalDays.DayOfTheYear.CopyTo(_copyDaysOfTheYear2, 0);
            Array.Sort(_copyDaysOfTheYear, typicalDays.IsPeakDay);
            Array.Sort(_copyDaysOfTheYear2, typicalDays.ClusterID);
            Array.Sort(typicalDays.DayOfTheYear, typicalDays.DaysPerTypicalDay);


            /// 5. fill the final profiles for the MILP with the medoids and peak day profiles
            ///     reverse normalization at the same time
            typicalDays.DayProfiles = new double[numberOfLoadTypes][];
            for (int load = 0; load < numberOfLoadTypes; load++)
            {
                typicalDays.DayProfiles[load] = new double[hoursPerDay * typicalDays.NumOfDays];
                for (int d = 0; d < typicalDays.NumOfDays; d++)
                {
                    for (int h = 0; h < hoursPerDay; h++)
                    {
                        int _hourA = h + d * hoursPerDay;
                        int _hourB = h + load * hoursPerDay;
                        if (string.Equals(dataScaling, "normalization"))
                        {
                            if (typicalDays.IsPeakDay[d]) // get it from original data, where peak days have not been culled from 
                                typicalDays.DayProfiles[load][_hourA] = Xcomplete[typicalDays.DayOfTheYear[d] - 1][_hourB] * (upperBounds[load] - lowerBounds[load]) + lowerBounds[load];
                            else // get it from X
                                typicalDays.DayProfiles[load][_hourA] = X[typicalDays.DayOfTheYear[d] - 1][_hourB] * (upperBounds[load] - lowerBounds[load]) + lowerBounds[load];
                        }
                        else
                        {
                            if (typicalDays.IsPeakDay[d]) // get it from original data, where peak days have not been culled from 
                                typicalDays.DayProfiles[load][_hourA] = Xcomplete[typicalDays.DayOfTheYear[d] - 1][_hourB] * sigma[load] + mean[load];
                            else // get it from X
                                typicalDays.DayProfiles[load][_hourA] = X[typicalDays.DayOfTheYear[d] - 1][_hourB] * sigma[load] + mean[load];
                        }
                    }
                }
            }


            /// 6. NumberOfDaysPerTimestep
            /// 
            typicalDays.NumberOfDaysPerTimestep = new int[typicalDays.Horizon];
            for (int t = 0; t < typicalDays.Horizon; t++)
            {
                int day = (int)Math.Floor((double)(t / hoursPerDay));
                typicalDays.NumberOfDaysPerTimestep[t] = typicalDays.DaysPerTypicalDay[day];
            }


            /// 7. Correcting each medoid day to make it proportional to the total loads of all elements in its cluster
            ///     necessary, because the medoid is not the perfect mean that can be multiplied with the number of days in this cluster
            //eqt in Dominguez munoz paper. then, scale up loads by that correction factor
            /// 8. (OPTIONAL) Calculation of scaling factors
            ///     for each load type
            ///         for each cluster
            ///             sum of loads of all days in that cluster must match the sum of loads of the medoid
            ///             this factor will be used in the energyhub to scale up the carbon emissions and operational cost, so all the days of that cluster are accounted for
            ///             NOTE! numOfDays approach as in the Matlab code is inprecise? Because the medoid is not the perfect mean. Also, the factor should differ for each load type
            typicalDays.UncorrectedDayProfiles = new double[numberOfLoadTypes][];
            for(int load = 0; load<numberOfLoadTypes; load++)
            {
                typicalDays.UncorrectedDayProfiles[load] = new double[typicalDays.Horizon];
                typicalDays.DayProfiles[load].CopyTo(typicalDays.UncorrectedDayProfiles[load], 0);
            }
            typicalDays.ScalingFactorPerTimestep = new double[numberOfLoadTypes][];

            for (int load = 0; load < numberOfLoadTypes; load++)
            {
                double[] sumOfAllClusterDays = new double[typicalDays.NumOfDays];
                double[] sumOfMedoids = new double[typicalDays.NumOfDays];
                double[] scalingFactor = new double[typicalDays.NumOfDays];
                double[] correctionFactor = new double[typicalDays.NumOfDays];

                typicalDays.ScalingFactorPerTimestep[load] = new double[typicalDays.NumOfDays * hoursPerDay];
                for (int d = 0; d < typicalDays.NumOfDays; d++)
                {
                    double _scalingFactor = 0.0;
                    double _sumOfAllClusterDays = 0.0;
                    double _sumOfMedoid = 0.0;
                    for (int h = 0; h < hoursPerDay; h++)
                    {
                        if (!typicalDays.IsPeakDay[d])
                        {
                            if (string.Equals(dataScaling, "normalization"))
                                _sumOfMedoid +=( X[typicalDays.DayOfTheYear[d] - 1][h + load * hoursPerDay] * (upperBounds[load] - lowerBounds[load]) + lowerBounds[load]);
                            else
                                _sumOfMedoid += (X[typicalDays.DayOfTheYear[d] - 1][h + load * hoursPerDay] * sigma[load] + mean[load]);
                            for (int i = 0; i < typicalDays.ClusterIdPerDay.Length; i++)
                            {
                                if (typicalDays.ClusterIdPerDay[i] == typicalDays.ClusterID[d])
                                {
                                    if (string.Equals(dataScaling, "normalization"))
                                        _sumOfAllClusterDays += (X[i][h + load * hoursPerDay] * (upperBounds[load] - lowerBounds[load]) + lowerBounds[load]);
                                    else
                                        _sumOfAllClusterDays += (X[i][h + load * hoursPerDay] * sigma[load] + mean[load]);
                                }
                            }
                        }
                        else
                        {
                            if(string.Equals(dataScaling, "normalization"))
                            {
                                _sumOfMedoid += (Xcomplete[typicalDays.DayOfTheYear[d] - 1][h + load * hoursPerDay] * (upperBounds[load] - lowerBounds[load]) + lowerBounds[load]);
                                _sumOfAllClusterDays += (Xcomplete[typicalDays.DayOfTheYear[d] - 1][h + load * hoursPerDay] * (upperBounds[load] - lowerBounds[load]) + lowerBounds[load]);
                            }
                            else
                            {
                                _sumOfMedoid += (Xcomplete[typicalDays.DayOfTheYear[d] - 1][h + load * hoursPerDay] * sigma[load] + mean[load]);
                                _sumOfAllClusterDays += (Xcomplete[typicalDays.DayOfTheYear[d] - 1][h + load * hoursPerDay] * sigma[load] + mean[load]);
                            }
                        }
                    }

                    sumOfMedoids[d] = _sumOfMedoid;
                    sumOfAllClusterDays[d] = _sumOfAllClusterDays;

                    _scalingFactor = _sumOfAllClusterDays / _sumOfMedoid;
                    double _correctionFactor = _sumOfAllClusterDays / (_sumOfMedoid * typicalDays.DaysPerTypicalDay[d]);
                    if (double.IsInfinity(_scalingFactor) || double.IsNaN(_scalingFactor)) _scalingFactor = 1.0;
                    if (double.IsInfinity(_correctionFactor) || double.IsNaN(_correctionFactor)) _correctionFactor = 1.0;

                    scalingFactor[d] = _scalingFactor;
                    correctionFactor[d] = _correctionFactor;
                }
                for (int t = 0; t < typicalDays.Horizon; t++)
                {
                    int day = (int)Math.Floor((double)(t / hoursPerDay));
                    typicalDays.DayProfiles[load][t] *= correctionFactor[day];
                }


                // (OPTIONAL) ALSO, can't apply to DayProfiles anymore, because they have been corrected. Use UncorrectedDayProfiles for it
                // store all the lost data of _sumOfAllClusterDays
                //    distributing equally amongst all days that have medoid != 0
                if (sumOfMedoids.Contains(0))
                {
                    double missing = 0.0;
                    for (int d = 0; d < typicalDays.NumOfDays; d++)
                    {
                        if (sumOfMedoids[d] == 0)
                        {
                            missing += sumOfAllClusterDays[d];
                        }
                    }

                    int countDaysToDistribute = 0;
                    bool[] distributeHere = new bool[typicalDays.NumOfDays];
                    for (int d = 0; d < typicalDays.NumOfDays; d++)
                    {
                        if (sumOfMedoids[d] != 0)
                        {
                            countDaysToDistribute++;
                            distributeHere[d] = true;
                        }
                    }
                    double distributePerTypicalDay = missing / countDaysToDistribute;
                    for (int d = 0; d < typicalDays.NumOfDays; d++)
                    {
                        if (distributeHere[d])
                            scalingFactor[d] = (sumOfAllClusterDays[d] + distributePerTypicalDay) / sumOfMedoids[d];
                    }

                }

                for (int d = 0; d < typicalDays.NumOfDays; d++)
                {
                    for (int h = 0; h < hoursPerDay; h++)
                    {
                        typicalDays.ScalingFactorPerTimestep[load][h + d * hoursPerDay] = scalingFactor[d];
                    }
                }
            }


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
