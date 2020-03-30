using System;
using System.Collections.Generic;
using System.Text;

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
            /// Indicating the day of the year [1, 365] that each typical day represents. In other words, the cluster medoids. The peak days are not medoids (no medoids) are at the end of the array.
            /// </summary>
            public int[] DayOfTheYear;
            /// <summary>
            /// Indicating, for which load type (index of `this.DemandTypes`) the respective peak day stands for. Length of this array is `this.NumOfPeakDays`. 
            /// </summary>
            public int[] PeakDayLoadType;
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
        public static TypicalDays GenerateTypicalDays(double[][] fullProfiles, string[] loadTypes, int numberOfTypicalDays, bool[] peakDays)
        {
            int[] seeds = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            int days = 365;
            int hours = 24;
            int clusters = numberOfTypicalDays;

            /// 1. reshape profiles into 365 x (24 * demand types) dataset, called X
            /// e.g. with elec and cooling, we have 365 x 48
            /// 
            double[][] X = new double[days][];
            for (int i = 0; i < days; i++)
            {
                X[i] = new double[hours * loadTypes.Length];
                for (int h = 0; h < hours; h++)
                    for (int d = 0; d < loadTypes.Length; d++)
                        X[i][h + (d*hours)] = fullProfiles[d][h + (i * hours)];
            }

            /// 2. normalize X for clustering
            ///     demand types can have different scales! e.g. solar, heating, elec, etc.
            ///     but the clustering indends to cluster according to similarity of their profiles
            /// I don't normalize fullProfiles, because that's a reference, so I don't wanna alter the original data




            /// 3. remove peak days from main dataset X
            /// only for those demands with peakDays[demand] == true
            /// because they will be re-inserted later as separate day


            /// 4. perform clustering with normalized X, excluding peak days
            /// 
            List<Tuple<int[], int[][], double>> replicates = new List<Tuple<int[], int[][], double>>();
            foreach (int seed in seeds)
                replicates.Add(EhubMisc.Clustering.KMedoids(X, clusters, 50, seed));
            int minIndex = 0;
            double minCost = replicates[0].Item3;
            for (int i = 1; i < replicates.Count; i++)
                if (replicates[i].Item3 < minCost)
                {
                    minIndex = i;
                    minCost = replicates[i].Item3;
                }
            Tuple<int[], int[][], double> clusteredData = replicates[minIndex];


            /// 5. Calculation of scaling factors
            ///     summing total loads for all observations in a cluster
            ///     summing total loads




            return new TypicalDays();
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
