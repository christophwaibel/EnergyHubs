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
        /// Simple K-Means algorithm according from Andrew Ng's Machine Learning lecture on coursera 
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusters"></param>
        /// <param name="iterations">50 is enough: Scalable K-Means by Ranked Retrieval. Broder et al. 2014, WSDM'14; http://dx.doi.org/10.1145/2556195.2556260 </param>
        /// <param name="seed"></param>
        /// <param name="algorithm">Options: {"KMeans++"(default), "Simple"}</param>
        /// <returns></returns>
        public static Tuple<double[][], int[][], double> KMeans(double[][] dataset, int clusters, int iterations = 50, int seed = 34, 
            string algorithm = "KMeans++", string distanceMeasure = "SqEuclidean")
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
            Tuple<List<int>, double[][]> tuple;
            if (String.Equals(algorithm, "Simple"))
                tuple = selectRandomIndices(K, n, m, rnd, dataset);
            else
                tuple = selectKMeansPlusPlusIndices(K, n, m, rnd, dataset, distanceMeasure);
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
                        distancesToCentroids.Add(Misc.Distance2Pts(dataset[i], centroid, distanceMeasure));

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
            return Tuple.Create(means, indices, totalCostDistance(K, clusterItems, means, dataset, distanceMeasure));

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
        public static Tuple<int[], int[][], double> KMedoids(double[][] dataset, int clusters, int iteration = 50, int seed = 34, string algorithm = "PAM_Exhaustive", string startMode = "KMeans++", string distanceMeasure = "SqEuclidean")
        {
            if (String.Equals(algorithm, "MeansApproximation"))
                return KMedoidsMeanApproximation(dataset, clusters, iteration, seed, distanceMeasure);
            else if (String.Equals(algorithm, "PAM_Exhaustive"))
                return KMedoidsPAM(dataset, clusters, iteration, seed, "exhaustive", startMode, distanceMeasure);
            else
                return KMedoidsPAM(dataset, clusters, iteration, seed, "random", startMode, distanceMeasure);
        }


        /// <summary>
        /// K-Medoids clustering
        /// PAM - Partitioning around medoid. Kaufmann & Rousseeuw, 1987. Wiley Series in Probability and Statistics.
        /// https://doi.org/10.1002/9780470316801.ch2
        /// </summary>
        /// <param name="dataX"></param>
        /// <param name="nClusters"></param>
        /// <returns></returns>
        private static Tuple<int[], int[][], double> KMedoidsPAM(double[][] dataset, int clusters, int iterations, int seed, 
            string swapMode = "exhaustive", string startMode = "KMeans++", string distanceMeasure = "SqEuclidean")
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
            Tuple<List<int>, double[][]> tuple;
            if (string.Equals(startMode, "Random"))
                tuple = selectRandomIndices(K, n, m, rnd, dataset);
            else
                tuple = selectKMeansPlusPlusIndices(K, n, m, rnd, dataset, distanceMeasure);
            int[] medoids = tuple.Item1.ToArray();
            double[][] means = tuple.Item2;

            // 2.a cluster assignment
            List<int>[] clusterItems = clusterAssignment(K, m, medoids, dataset, distanceMeasure);
            // 2.b cost
            double cost = totalCostDistance(K, clusterItems, medoids, dataset, distanceMeasure);

            // 3. iterate and swap medoids
            bool improvement = true;
            int currentIteration = 0;
            do
            {
                double currentLowestCost = cost;
                int[] storeNewMedoids = new int[K];

                for (int _k = 0; _k < K; _k++)
                {
                    storeNewMedoids[_k] = medoids[_k];
                    if (String.Equals(swapMode, "random"))
                    {
                        int rndSwap = rnd.Next(0, clusterItems[_k].Count);
                        while (clusterItems[_k][rndSwap] == medoids[_k])
                            rndSwap = rnd.Next(0, clusterItems[_k].Count);

                        int[] tempMedoids = new int[K];
                        medoids.CopyTo(tempMedoids, 0);
                        tempMedoids[_k] = rndSwap;

                        double tempCost = totalCostDistance(K, clusterItems, tempMedoids, dataset, distanceMeasure);

                        if (tempCost < currentLowestCost)
                        {
                            currentLowestCost = tempCost;
                            storeNewMedoids[_k] = rndSwap;
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

                            double tempCost = totalCostDistance(K, clusterItems, tempMedoids, dataset, distanceMeasure);

                            if (tempCost < currentLowestCost)
                            {
                                currentLowestCost = tempCost;
                                storeNewMedoids[_k] = swap;
                            }
                        }
                    }
                }

                // only re-assign cluster items here
                if (currentLowestCost >= cost)
                    improvement = false;
                else
                { 
                    storeNewMedoids.CopyTo(medoids, 0);
                    clusterItems = clusterAssignment(K, m, medoids, dataset, distanceMeasure);
                    cost = totalCostDistance(K, clusterItems, medoids, dataset, distanceMeasure);
                }

                currentIteration++;
            } while (improvement && currentIteration < iterations);


            int[][] indices = new int[K][];
            for (int _k = 0; _k < K; _k++)
                indices[_k] = clusterItems[_k].ToArray();
            return Tuple.Create(medoids, indices, cost);
        }


        /// <summary>
        /// Compute K-Means and take the closest existing point of the centroid as medoid
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusters"></param>
        /// <param name="iterations"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private static Tuple<int[], int[][], double> KMedoidsMeanApproximation(double[][] dataset, int clusters, int iterations, int seed, string distanceMeasure = "SqEuclidean")
        {
            Tuple<double[][], int[][], double> approximation = KMeans(dataset, clusters, iterations, seed, "Simple");

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
                    distancesToCentroid.Add(Misc.Distance2Pts(dataset[index], centroid, distanceMeasure));
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

            double cost = totalCostDistance(K, clusterItems, medoids, dataset, distanceMeasure);
            return Tuple.Create(medoids, approximation.Item2, cost);
        }
        #endregion


        #region Quality metrics
        /// <summary>
        /// Computes Silhouette coefficients for clustered items
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="clusterItems"></param>
        /// <returns>s(i) for each sample, average s(i) per cluster, total average s(i) of all clusters</returns>
        public static Tuple<double [], double [], double> Silhouette(double[][] dataset, int [][] clusterItems, string distanceMeasure = "SqEuclidean")
        {
            int m = dataset.Length;
            int n = dataset[0].Length;
            int K = clusterItems.Length; 

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
                        averageDistance[index] += Misc.Distance2Pts(dataset[index], dataset[clusterItems[_k][u]], distanceMeasure);
                    }
                    averageDistance[index] /= (clusterItems[_k].Length - 1);

                    double[] distanceToOtherClusters = new double[K - 1];
                    int otherCluster = 0;
                    for(int _q = 0; _q<K; _q++)
                    {
                        if (_q == _k) continue;
                        for(int u = 0; u<clusterItems[_q].Length; u++)
                            distanceToOtherClusters[otherCluster] += Misc.Distance2Pts(dataset[index], dataset[clusterItems[_q][u]], distanceMeasure);
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
        #endregion


        #region shared functions
        /// <summary>
        /// Source: 
        /// Arthur & Vassilvitskii 2007. K-means++: The Advantages of Careful Seeding. 
        /// SOGA '07: Proceedings of the Eigteenth Annual ACM-SIAM Symposium on Discrete Algorithms. pp. 1027-1035
        /// </summary>
        /// <param name="K"></param>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <param name="rnd"></param>
        /// <param name="dataset"></param>
        /// <returns></returns>
        private static Tuple<List<int>, double[][]> selectKMeansPlusPlusIndices(int K, int n, int m, Random rnd, double[][] dataset, string distanceMeasure = "SqEuclidean")
        {
            //// Pseudo code
            /// From the Matlab 2017b documentation
            /// k-means++ algorithm chooses seeds for k-means clustering. I.e., after the seeds are chosen, regular k-means is performed
            /// 
            /// 1. Select an observation uniformly at random from the data set X. The chosen observation is the first centroid, and is denoted c_1
            /// 2. Compute distances from each observation to c_1. Denote the distance between c_j and the observation m as d(x_m, c_j)
            /// 3. Select the next centroid, c_2 at random from X with probability
            ///     d^2(x_m, c_1) / sum_{j=1}^n d^2(x_j, c_1)
            /// 4. To choose center j:
            ///     a. Compute the distances from each observation to each centroid, and assign each observation to its closest centroid
            ///     b. For m = 1, ..., n and p = 1, ..., j-1, select centroid j at random from X with probability
            ///         d^2(x_m, c_p) / sum_{h; x_h \in C_p} d^2(x_h, c_p)
            ///         where C_p is the set of all observations closest to centroid c_p and x_m belongs to C_p.
            ///         That is, select each subsequent center with a probability proportional to the distance from itself to the closest center that you already chose.
            /// 5. Repeat step 4 until k centroids are chosen.
            /// Arthur and Vassilvitskii demonstrate, using a simulation study for several cluster orientations, that k-means++ achieves faster convergence to a lower
            /// sum of within-cluster, sum-of-squares point-to-cluster-centroid distances thatn Lloyd's algorithm


            List<int> alreadySelected = new List<int>();
            double[][] means = new double[K][];
            for (int _k = 0; _k < K; _k++)
                means[_k] = new double[n];

            // 1. initialize first centroid at uniform random
            int c1 = rnd.Next(0, m);
            alreadySelected.Add(c1);
            for (int _n = 0; _n < n; _n++)
                means[0][_n] = dataset[c1][_n];

            // 2. compute distances from each oberstaion to c1
            double[] distancesToC1 = new double[m];
            double sumDistancesToC1 = 0.0;
            for (int i = 0; i < m; i++)
            {
                distancesToC1[i] = Misc.Distance2Pts(dataset[i], dataset[c1], distanceMeasure);
                sumDistancesToC1 += distancesToC1[i];
            }

            // 3. select next centroid according to probability
            Dictionary<int, double> probabilitiesDC1 = new Dictionary<int, double>();
            for (int i = 0; i < m; i++)
                probabilitiesDC1.Add(i, distancesToC1[i]);

            probabilitiesDC1 = probabilitiesDC1.OrderBy(x => x.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
            double tempProbability = 0.0;
            for (int i = 0; i < m; i++)
            {
                tempProbability += probabilitiesDC1[probabilitiesDC1.Keys.ElementAt(i)];
                probabilitiesDC1[probabilitiesDC1.Keys.ElementAt(i)] = tempProbability;
            }

            int c2 = 0;
            do
            {
                double rndC2 = rnd.NextDouble() * sumDistancesToC1;
                for (int i = 0; i < m; i++)
                {
                    if (probabilitiesDC1[probabilitiesDC1.Keys.ElementAt(i)] > rndC2)
                    {
                        // take previous element
                        c2 = probabilitiesDC1.Keys.ElementAt(i - 1);
                        break;
                    }

                    if (i == n)
                    {
                        //last element
                        c2 = probabilitiesDC1.Keys.ElementAt(i);
                    }
                }
            } while (c2 == c1);
            alreadySelected.Add(c2);
            for(int _n=0; _n<n; _n++)
                means[1][_n] = dataset[c2][_n];

            // 4. choose any more centroid j in K
            if (K > 2)
            {
                // compute distances from each observation to each centroid c1 and c2 and assign each observation to closest centroid
                List<int>[] clusterItems = clusterAssignment(2, m, new int[2] { c1, c2 }, dataset);
                int[] clusterC1OrC2 = new int[m];
                for(int _k=0; _k<2; _k++)
                    foreach(int item in clusterItems[_k])
                        clusterC1OrC2[item] = _k;

                double[] distancesToC1OrC2 = new double[m];
                double sumDistancesToC1OrC2 = 0.0;
                for(int i=0; i<m; i++)
                {
                    int index = alreadySelected[clusterC1OrC2[i]];
                    distancesToC1OrC2[i] = Misc.Distance2Pts(dataset[i], dataset[alreadySelected[clusterC1OrC2[i]]], distanceMeasure);
                    sumDistancesToC1OrC2 += distancesToC1OrC2[i];
                }
                Dictionary<int, double> probabilitiesDCJ = new Dictionary<int, double>();
                for (int i = 0; i < m; i++)
                    probabilitiesDCJ.Add(i, distancesToC1OrC2[i]);

                probabilitiesDCJ = probabilitiesDCJ.OrderBy(x => x.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                tempProbability = 0.0;
                for (int i = 0; i < m; i++)
                {
                    tempProbability += probabilitiesDCJ[probabilitiesDCJ.Keys.ElementAt(i)];
                    probabilitiesDCJ[probabilitiesDCJ.Keys.ElementAt(i)] = tempProbability;
                }

                for (int _k=2; _k<K; _k++)
                {
                    int selectedIndex = alreadySelected[0];
                    while (alreadySelected.IndexOf(selectedIndex) != -1)
                    {
                        double rndCj = rnd.NextDouble() * sumDistancesToC1OrC2;
                        for (int i = 0; i < m; i++)
                        {
                            if (probabilitiesDCJ[probabilitiesDCJ.Keys.ElementAt(i)] > rndCj)
                            {
                                // take previous element
                                selectedIndex = probabilitiesDCJ.Keys.ElementAt(i - 1);
                                break;
                            }

                            if (i == n)
                            {
                                //last element
                                selectedIndex = probabilitiesDCJ.Keys.ElementAt(i);
                            }
                        }
                    }
                    alreadySelected.Add(selectedIndex);
                    for (int _n = 0; _n < n; _n++)
                        means[_k][_n] = dataset[selectedIndex][_n];
                }
            }


            return Tuple.Create(alreadySelected, means);
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


        private static List<int>[] clusterAssignment(int K, int m, int[] medoids, double[][] dataset, string distanceMeasure = "SqEuclidean")
        {
            List<int>[] clusterItems = new List<int>[K];
            for (int _k = 0; _k < K; _k++)
                clusterItems[_k] = new List<int>();
            for (int i = 0; i < m; i++)
            {
                List<double> distancesToCentroids = new List<double>();
                for (int _k = 0; _k < K; _k++)
                    distancesToCentroids.Add(Misc.Distance2Pts(dataset[i], dataset[medoids[_k]], distanceMeasure));

                int minimumValueIndex = distancesToCentroids.IndexOf(distancesToCentroids.Min());
                clusterItems[minimumValueIndex].Add(i);
            }

            return clusterItems;
        }


        private static double totalCostDistance(int K, List<int>[] clusterItems, int[] medoids, double[][] dataset, string distanceMeasure = "SqEuclidean")
        {
            double cost = 0.0;
            for (int _k = 0; _k < K; _k++)
                for (int i = 0; i < clusterItems[_k].Count; i++)
                    if (clusterItems[_k][i] != medoids[_k])     // might cost more than what it saves, because it only saves one distance calculation
                        cost += Misc.Distance2Pts(dataset[medoids[_k]], dataset[clusterItems[_k][i]], distanceMeasure);

            return cost;
        }


        private static double totalCostDistance(int K, List<int>[] clusterItems, double[][] centroids, double [][] dataset, string distanceMeasure = "SqEuclidean")
        {
            double cost = 0.0;
            for (int _k = 0; _k < K; _k++)
                for (int i = 0; i < clusterItems[_k].Count; i++)
                    cost += Misc.Distance2Pts(centroids[_k], dataset[clusterItems[_k][i]], distanceMeasure);

            return cost;
        }
        #endregion
    }
}
