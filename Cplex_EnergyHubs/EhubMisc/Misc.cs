using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace EhubMisc
{
    public static class Misc
    {
        public static bool IsNullOrDefault<T>(this Nullable<T> value) where T : struct
        {
            return default(T).Equals(value.GetValueOrDefault());
        }


        public static double Distance2Pts(double[] x1, double[] x2)
        {
            int n = x1.Length;
            if (x2.Length < n) n = x2.Length;

            double distance = 0.0;
            for (int i = 0; i < n; i++)
                distance += Math.Pow(x1[i] - x2[i], 2);
            distance = Math.Sqrt(distance);

            return distance;
        }
    }


    /// <summary>
    /// Outputs. new struct per epsilon cut
    /// </summary>
    public struct EhubOutputs
    {
        internal double carbon;             // annual carbon.
        internal double cost;               // cost. levelized.
        internal double OPEX;               // annual operation cost.
        internal double CAPEX;              // capital cost. levelized.

        // Technology sizing
        internal double[] x_pv;             // pv sizing [m2]
        internal double x_bat;              // battery 
        internal double x_hp;               // heat pump. assume it reaches peak heat temperatures as simplification.
        internal double x_tes;              // thermal storage
        internal double x_chp;              // combined heat and power
        internal double x_ac;               // air condition
        internal double x_boi;              // gas boiler

        // Operation. Time resolved.
        internal double[] x_elecpur;        // purchase from grid
        internal double[] x_feedin;         // feedin
        internal double[] x_batdischarge;   // battery discharge
        internal double[] x_batcharge;      // battery charge
        internal double[] x_batsoc;         // battery state of charge
        internal double[] x_tesdischarge;   // thermal energy storage (tes) discharge
        internal double[] x_tescharge;      // tes charge
        internal double[] x_tessoc;         // tes state of charge
        internal double[] x_hp_op;          // heat pump operation
        internal double[] x_boi_op;         // boiler operation
        internal double[] x_chp_op_e;       // chp operation electricity
        internal double[] x_chp_op_h;       // chp operation heat
        internal double[] x_chp_dump;       // chp heat dumped

        internal double[] b_pvprod;     // total pv production
        internal double[] b_pvprod_Roof;// pv production roof
        internal double[] b_pvprod_E;   // pv prod East
        internal double[] b_pvprod_S_a; // pv prod South A
        internal double[] b_pvprod_S_b; // pv prod South B
        internal double[] b_pvprod_W_a; // pv prod West A
        internal double[] b_pvprod_W_b; // pv prod West B
    }


    public static class Clustering
    {
        /// <summary>
        /// K-Means clustering
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusters"></param>
        /// <param name="iterations">50 is enough: Scalable K-Means by Ranked Retrieval. Broder et al. 2014, WSDM'14; http://dx.doi.org/10.1145/2556195.2556260 </param>
        /// <param name="seed"></param>
        /// <returns></returns>
        public static Tuple<double[][], int[][]> KMeans(double[][] dataset, int clusters, int iterations = 50, int seed = 34)
        {
            Random rnd = new Random(seed);

            int n = dataset[0].Length; // problem dimension
            int m = dataset.Length; // size of dataset
            int K = clusters;

            //// Pseudo code, from Andrew Ng's Machine Learning lecture on coursera
            /// could be improved, e.g. https://arxiv.org/pdf/1801.02949.pdf
            //Randomly initialize K cluster centrods, mu1, mu2, ..., mu_k \in R^n
            //Repeat {
            //    // cluster assignment step
            //    for i=1 to m
            //        c^(i) := index(from 1 to K) of clister centroid closest to x^(i)
            //                -> c^(i) = min _k ||x^(i) - mu_k||^2

            //    // move centroid
            //    for k = 1 to K
            //        mu_k := mean of points assigned to cluster k
            //}

            // initialize random centroids
            List<int> alreadySelected = new List<int>();
            double[][] means = new double[K][];
            for (int _k = 0; _k < K; _k++)
            {
                int selectedIndex = rnd.Next(0, m);
                if (_k > 0)
                {
                    while (alreadySelected.IndexOf(selectedIndex) != -1)
                        selectedIndex = rnd.Next(0, m);
                    alreadySelected.Add(selectedIndex);
                }
                else alreadySelected.Add(selectedIndex);

                means[_k] = new double[n];
                for (int _n = 0; _n < n; _n++)
                    means[_k][_n] = dataset[selectedIndex][_n];
            }

            // iterate
            List<int>[] clusterItems = new List<int>[K];
            for (int _iter = 0; _iter < iterations; _iter++)
            {
                // cluster assignment
                clusterItems = new List<int>[K];
                for (int _k = 0; _k < K; _k++)
                    clusterItems[_k] = new List<int>();

                for (int i = 0; i < m; i++)
                {
                    List<double> distancesToCentroids = new List<double>();
                    foreach (double[] centroid in means)
                        distancesToCentroids.Add(Misc.Distance2Pts(dataset[i], centroid));

                    int minimumValueIndex = distancesToCentroids.IndexOf(distancesToCentroids.Min());
                    clusterItems[minimumValueIndex].Add(i);
                }

                // move centroids
                for (int _k = 0; _k < K; _k++)
                {
                    means[_k] = new double[n];
                    foreach (int item in clusterItems[_k])
                        for (int _n = 0; _n < n; _n++)
                            means[_k][_n] += dataset[item][_n];

                    for (int _n = 0; _n < n; _n++)
                        means[_k][_n] /= clusterItems[_k].Count;
                }
            }

            int[][] indices = new int[K][];
            for (int _k = 0; _k < K; _k++)
                indices[_k] = clusterItems[_k].ToArray();
            return Tuple.Create(means, indices);
        }


        /// <summary>
        /// K-Medoids clustering
        /// PAM - Partitioning around medoid. Kaufmann & Rousseeuw, 1987. Wiley Series in Probability and Statistics.
        /// https://doi.org/10.1002/9780470316801.ch2
        /// </summary>
        /// <param name="dataX"></param>
        /// <param name="nClusters"></param>
        /// <returns></returns>
        public static Tuple<double[,], int[][]> KMedoids(double[,] dataX, int nClusters)
        {
            int dim = dataX.GetLength(1);

            double[,] means = new double[nClusters, dim];
            int[][] indices = new int[nClusters][];
            return Tuple.Create(means, indices);
        }
    }
}
