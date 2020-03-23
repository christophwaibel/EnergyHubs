using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;

namespace EhubMisc
{
    public static class Clustering
    {
        #region K-Means
        /// <summary>
        /// K-Means clustering
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusters"></param>
        /// <param name="iterations">50 is enough: Scalable K-Means by Ranked Retrieval. Broder et al. 2014, WSDM'14; http://dx.doi.org/10.1145/2556195.2556260 </param>
        /// <param name="seed"></param>
        /// <param name="algorithm">Options: {"Simple"}</param>
        /// <returns></returns>
        public static Tuple<double[][], int[][]> KMeans(double[][] dataset, int clusters, int iterations = 50, int seed = 34, string algorithm = "Simple")
        {
            //if (String.Equals(algorithm, "Simple"))
            return KMeansSimple(dataset, clusters, iterations, seed);
        }


        /// <summary>
        /// Simple K-Means algorithm according from Andrew Ng's Machine Learning lecture on coursera 
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusters"></param>
        /// <param name="iterations"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private static Tuple<double[][], int[][]> KMeansSimple(double[][] dataset, int clusters, int iterations, int seed)
        {
            Random rnd = new Random(seed);

            int n = dataset[0].Length; // problem dimension
            int m = dataset.Length; // size of dataset
            int K = clusters;

            //// Pseudo code
            /// could be improved, e.g. https://arxiv.org/pdf/1801.02949.pdf
            ///Randomly initialize K cluster centrods, mu1, mu2, ..., mu_k \in R^n
            ///Repeat {
            ///    // cluster assignment step
            ///    for i=1 to m
            ///        c^(i) := index(from 1 to K) of clister centroid closest to x^(i)
            ///                -> c^(i) = min _k ||x^(i) - mu_k||^2
            ///
            ///    // move centroid
            ///    for k = 1 to K
            ///        mu_k := mean of points assigned to cluster k
            /// }

            // initialize random centroids
            Tuple<List<int>, double[][]> tuple = selectRandomIndices(K, n, m, rnd, dataset);
            List<int> alreadySelected = tuple.Item1;
            double[][] means = tuple.Item2;


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
        #endregion


        #region K-Medoids
        /// <summary>
        /// K-Medoids clustering
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusters"></param>
        /// <param name="iteration"></param>
        /// <param name="seed"></param>
        /// <param name="algorithm">Options: {"PAM" (default), "MeansApproximation"}</param>
        /// <returns></returns>
        public static Tuple<int[], int[][]> KMedoids(double[][] dataset, int clusters, int iteration = 50, int seed = 34, string algorithm = "PAM_Random")
        {
            if (String.Equals(algorithm, "MeansApproximation"))
                return KMedoidsMeanApproximation(dataset, clusters, iteration, seed);
            else if (String.Equals(algorithm, "PAM_Exhaustive"))
                return KMedoidsPAM(dataset, clusters, iteration, seed, "exhaustive");
            else
                return KMedoidsPAM(dataset, clusters, iteration, seed, "random");
        }


        /// <summary>
        /// K-Medoids clustering
        /// PAM - Partitioning around medoid. Kaufmann & Rousseeuw, 1987. Wiley Series in Probability and Statistics.
        /// https://doi.org/10.1002/9780470316801.ch2
        /// </summary>
        /// <param name="dataX"></param>
        /// <param name="nClusters"></param>
        /// <returns></returns>
        private static Tuple<int[], int[][]> KMedoidsPAM(double[][] dataset, int clusters, int iterations, int seed, string swapMode = "exhaustive")
        {
            Random rnd = new Random(seed);

            int n = dataset[0].Length; // problem dimension
            int m = dataset.Length; // size of dataset
            int K = clusters;

            //// Pseudocode PAM
            /// https://www.geeksforgeeks.org/ml-k-medoids-clustering-with-example/
            ///
            /// cost = sum_{c_i} sum_{p_i in c_i} |p_i - c_i|
            /// 
            /// 1. Initialize: select k random points out of the n data points as the medoids.
            /// 2. Associate each data point to the closest medoid by using any common distance metric methods
            /// 3. While the cost decreases:
            ///     For each medoid m, for each data point o which is not a medoid:
            ///         1. Swap m and o, associate each data point to the closest medoid, recompute the cost
            ///         2. if the total cost is more than that in the previous step, undo the swap
            ///         

            // 1. initialize random centroids
            Tuple<List<int>, double[][]> tuple = selectRandomIndices(K, n, m, rnd, dataset);
            int[] medoids = tuple.Item1.ToArray();
            double[][] means = tuple.Item2;

            // 2.a cluster assignment
            List<int>[] clusterItems = clusterAssignment(K, m, medoids, dataset);
            // 2.b cost
            double cost = totalCostDistance(K, clusterItems, medoids, dataset);

            // 3. iterate and swap medoids
            bool improvement = true;
            double newCost = double.MaxValue;
            int currentIteration = 0;
            do
            {
                double oldCost = cost;

                for (int _k = 0; _k < K; _k++)
                {
                    if (String.Equals(swapMode, "random"))
                    {
                        int rndSwap = rnd.Next(0, clusterItems[_k].Count);
                        while (clusterItems[_k][rndSwap] == medoids[_k])
                            rndSwap = rnd.Next(0, clusterItems[_k].Count);

                        int[] tempMedoids = new int[K];
                        medoids.CopyTo(tempMedoids, 0);
                        tempMedoids[_k] = rndSwap;

                        List<int>[] tempClusterItems = clusterAssignment(K, m, tempMedoids, dataset);
                        double tempCost = totalCostDistance(K, tempClusterItems, tempMedoids, dataset);

                        if (tempCost < cost)
                        {
                            cost = tempCost;
                            newCost = tempCost;
                            medoids[_k] = rndSwap;
                            tempClusterItems.CopyTo(clusterItems, 0);
                        }
                    }
                    // exhaustive
                    else
                    {
                        for (int i = 0; i < clusterItems[_k].Count; i++)
                        {
                            int swap = clusterItems[_k][i];
                            if (medoids[_k] == swap) continue;

                            int[] tempMedoids = new int[K];
                            medoids.CopyTo(tempMedoids, 0);
                            tempMedoids[_k] = swap;

                            List<int>[] tempClusterItems = clusterAssignment(K, m, tempMedoids, dataset);
                            double tempCost = totalCostDistance(K, tempClusterItems, tempMedoids, dataset);

                            if (tempCost < cost)
                            {
                                cost = tempCost;
                                newCost = tempCost;
                                medoids[_k] = swap;
                                tempClusterItems.CopyTo(clusterItems, 0);
                            }
                        }
                    }
                }

                if (newCost >= oldCost)
                    improvement = false;

                currentIteration++;
            } while (improvement && currentIteration < iterations);


            int[][] indices = new int[K][];
            for (int _k = 0; _k < K; _k++)
                indices[_k] = clusterItems[_k].ToArray();
            return Tuple.Create(medoids, indices);
        }


        /// <summary>
        /// Compute K-Means and take the closest existing point of the centroid as medoid
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusters"></param>
        /// <param name="iterations"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private static Tuple<int[], int[][]> KMedoidsMeanApproximation(double[][] dataset, int clusters, int iterations, int seed)
        {
            Tuple<double[][], int[][]> approximation = KMeans(dataset, clusters, iterations, seed, "Simple");

            int n = dataset[0].Length; // problem dimension
            int m = dataset.Length; // size of dataset
            int K = clusters;

            //// Pseudocode
            /// for each cluster 
            ///     compute distances from centroid to each point within that cluster
            ///     get the closest point and re-assign it as medoid 

            int[] medoids = new int[clusters];
            for (int _k = 0; _k < K; _k++)
            {
                List<double> distancesToCentroid = new List<double>();
                List<int> listIndices = new List<int>();
                double[] centroid = approximation.Item1[_k];
                for (int i = 0; i < approximation.Item2[_k].Length; i++)
                {
                    int index = approximation.Item2[_k][i];
                    distancesToCentroid.Add(Misc.Distance2Pts(dataset[index], centroid));
                    listIndices.Add(index);
                }
                int minimumValueIndex = distancesToCentroid.IndexOf(distancesToCentroid.Min());
                medoids[_k] = listIndices[minimumValueIndex];
            }

            List<int>[] clusterItems = new List<int>[K];
            for(int _k=0; _k<K; _k++)
            {
                clusterItems[_k] = new List<int>();
                for(int i=0; i<approximation.Item2[_k].Length; i++)
                {
                    clusterItems[_k].Add(approximation.Item2[_k][i]);
                }
            }

            double cost = totalCostDistance(K, clusterItems, medoids, dataset);

            return Tuple.Create(medoids, approximation.Item2);
        }
        #endregion


        #region shared functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusterItems"></param>
        /// <param name="centroids"></param>
        /// <returns>s(i), average s(i) per cluster, total average s(i)</returns>
        public static Tuple<double [], double [], double> Silhouette(double[][] dataset, int [][] clusterItems, double [][] centroids)
        {
            int m = dataset.Length;
            int n = dataset[0].Length;
            int K = centroids.Length; 

            double[] averageDistance = new double[m];
            double[] distanceToNearestCluster = new double[m];
            double []silhouetteSample = new double[m];
            double[] silhouetteCluster = new double[K];
            for (int _k=0; _k<K; _k++)
            {
                for (int i = 0; i < clusterItems[_k].Length; i++)
                {
                    int index = clusterItems[_k][i];
                    averageDistance[index] = 0.0;
                    for (int u = 0; u < clusterItems[_k].Length; u++)
                    {
                        if (i == u) continue;
                        averageDistance[index] += Misc.Distance2Pts(dataset[index], dataset[clusterItems[_k][u]]);
                    }
                    averageDistance[index] /= (clusterItems[_k].Length - 1);

                    double[] distanceToOtherClusters = new double[K - 1];
                    int otherCluster = 0;
                    for(int _q = 0; _q<K; _q++)
                    {
                        if (_q == _k) continue;
                        for(int u = 0; u<clusterItems[_q].Length; u++)
                            distanceToOtherClusters[otherCluster] += Misc.Distance2Pts(dataset[index], dataset[clusterItems[_q][u]]);
                        distanceToOtherClusters[otherCluster] /= clusterItems[_q].Length;
                        otherCluster++;
                    }
                    distanceToNearestCluster[index] = distanceToOtherClusters.Min();

                    double a = averageDistance[index];
                    double b = distanceToNearestCluster[index];
                    silhouetteSample[index] = (b - a) / (new double[2] { a, b }.Max());
                }

                silhouetteCluster[_k] = 0.0;
                for (int i = 0; i < clusterItems[_k].Length; i++)
                    silhouetteCluster[_k] += silhouetteSample[clusterItems[_k][i]];
                if (clusterItems[_k].Length > 1) silhouetteCluster[_k] /= clusterItems[_k].Length;
                else silhouetteCluster[_k] = 0.0;
            }

            double averageSilhouette = 0.0;
            for(int _k=0; _k<K; _k++)
                averageSilhouette += (clusterItems[_k].Length * silhouetteCluster[_k]);
            averageSilhouette /= m;

            return Tuple.Create(silhouetteSample, silhouetteCluster, averageSilhouette);
        }


        public static Tuple<double[], double[], double> Silhouette(double[][] dataset, int[][] clusterItems, int [] medoids)
        {
            int K = medoids.Length;
            int n = dataset[0].Length;
            double[][] centroids = new double[K][];
            for(int _k=0; _k<K; _k++)
                centroids[_k] = dataset[medoids[_k]];

            return Silhouette(dataset, clusterItems, centroids);
        }


        /// <summary>
        /// select random indices from a dataset. no double selections
        /// </summary>
        /// <param name="K">Number of selections (clusters)</param>
        /// <param name="n">vector length of a sample (dimension)</param>
        /// <param name="m">number of samples in dataset</param>
        /// <param name="rnd">random generator</param>
        /// <param name="dataset">dataset</param>
        /// <returns></returns>
        private static Tuple<List<int>, double[][]> selectRandomIndices(int K, int n, int m, Random rnd, double[][] dataset)
        {
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

            return Tuple.Create(alreadySelected, means);
        }


        private static List<int>[] clusterAssignment(int K, int m, int[] medoids, double[][] dataset)
        {
            List<int>[] clusterItems = new List<int>[K];
            for (int _k = 0; _k < K; _k++)
                clusterItems[_k] = new List<int>();
            for (int i = 0; i < m; i++)
            {
                List<double> distancesToCentroids = new List<double>();
                for (int _k = 0; _k < K; _k++)
                    distancesToCentroids.Add(Misc.Distance2Pts(dataset[i], dataset[medoids[_k]]));

                int minimumValueIndex = distancesToCentroids.IndexOf(distancesToCentroids.Min());
                clusterItems[minimumValueIndex].Add(i);
            }

            return clusterItems;
        }


        private static double totalCostDistance(int K, List<int>[] clusterItems, int[] medoids, double[][] dataset)
        {
            double cost = 0.0;
            for (int _k = 0; _k < K; _k++)
                for (int i = 0; i < clusterItems[_k].Count; i++)
                    if (clusterItems[_k][i] != medoids[_k])     // might cost more than what it saves, because it only saves one distance calculation
                        cost += Misc.Distance2Pts(dataset[medoids[_k]], dataset[clusterItems[_k][i]]);

            return cost;
        }
        #endregion
    }
}
