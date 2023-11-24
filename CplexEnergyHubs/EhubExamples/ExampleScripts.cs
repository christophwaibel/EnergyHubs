using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EhubExamples
{
    internal static class ExampleScripts
    {

        internal static void ClusterRandomData()
        {
            Random rnd = new Random(1);
            int days = 365;
            int hours = 24;
            int clusters = 12;

            double[][] randomData = new double[days][];
            for (int i = 0; i < days; i++)
            {
                randomData[i] = new double[hours];
                for (int h = 0; h < hours; h++)
                {
                    randomData[i][h] = rnd.NextDouble();
                }
            }

            Tuple<double[][], int[][], double> tuple = EhubMisc.Clustering.KMeans(randomData, clusters);
            //Tuple<int[], int[][], double> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters, 50, 34, "MeansApproximation");
            //Tuple<int[], int[][], double> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters, 50, 34, "PAM_Random");
            //Tuple<int[], int[][], double> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters);

            Tuple<double[], double[], double> silhouette = EhubMisc.Clustering.Silhouette(randomData, tuple.Item2);
        }


        /// <summary>
        /// Clustering some load profile data
        /// </summary>
        internal static void ClusterLoadData()
        {
            // load some profiles and cluster them
            string path = @"c:\temp\ecostest\heating.csv";
            string[] lines = File.ReadAllLines(path);
            List<double> heating = new List<double>();
            foreach (string line in lines)
                heating.Add(Convert.ToDouble(line));


            // clustering 
            int[] seeds = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            int days = 365;
            int hours = 24;
            int clusters = 12;

            double[][] heating_perDay = new double[days][];
            for (int i = 0; i < days; i++)
            {
                heating_perDay[i] = new double[hours];
                for (int h = 0; h < hours; h++)
                {
                    heating_perDay[i][h] = heating[h + (i * hours)];
                }
            }

            List<Tuple<int[], int[][], double>> replicates = new List<Tuple<int[], int[][], double>>();
            foreach (int seed in seeds)
                replicates.Add(EhubMisc.Clustering.KMedoids(heating_perDay, clusters, 50, seed, "PAM_Exhaustive"));

            ////Tuple<double[][], int[][]> clusteredData = EhubMisc.Clustering.KMeans(heating_perDay, clusters);
            ////Tuple<int[], int[][]> clusteredData = EhubMisc.Clustering.KMedoids(heating_perDay, clusters, 50, 34, "MeansApproximation");
            //Tuple<int[], int[][], double> clusteredData = EhubMisc.Clustering.KMedoids(heating_perDay, clusters, 50, 34, "PAM_Exhaustive");
            ////Tuple<int[], int[][]> clusteredData = EhubMisc.Clustering.KMedoids(heating_perDay, clusters);

            int minIndex = 0;
            double minCost = replicates[0].Item3;
            for (int i = 1; i < replicates.Count; i++)
            {
                if (replicates[i].Item3 < minCost)
                {
                    minIndex = i;
                    minCost = replicates[i].Item3;
                }
            }

            Tuple<int[], int[][], double> clusteredData = replicates[minIndex];
            Tuple<double[], double[], double> silhouette = EhubMisc.Clustering.Silhouette(heating_perDay, clusteredData.Item2);


            // write cluster points. make a blank line between two clusters
            // columns 1 to n are the parameters, n+1 is s(i)
            Dictionary<int, int> idx = new Dictionary<int, int>();

            List<string> writeSamples = new List<string>();
            for (int _k = 0; _k < clusters; _k++)
            {
                for (int i = 0; i < clusteredData.Item2[_k].Length; i++)
                {
                    int index = clusteredData.Item2[_k][i];
                    double[] sample = heating_perDay[index];
                    double s_i = silhouette.Item1[index];
                    string text = null;
                    foreach (double parameter in sample)
                        text += parameter.ToString("0.#0") + ",";
                    text += "s_i," + s_i.ToString("0.#0");
                    text += ",#id," + index.ToString() + ",medoid," + clusteredData.Item1[_k].ToString();
                    writeSamples.Add(text);

                    idx.Add(index, _k);
                }
                writeSamples.Add("");
            }
            string outPath = @"c:\temp\ecostest\testsilhouette.csv";
            File.WriteAllLines(outPath, writeSamples.ToArray());

            idx = idx.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            List<string> idxString = new List<string>();
            for (int i = 0; i < days; i++)
                idxString.Add(idx[i].ToString());
            string outPath2 = @"c:\temp\ecostest\idx.csv";
            File.WriteAllLines(outPath2, idxString.ToArray());

        }


        /// <summary>
        /// compute Silhouette coefficients for some numbers
        /// </summary>
        internal static void SilhouetteTest()
        {
            double[][] X = new double[9][];
            X[0] = new double[2] { 1, 0 };
            X[1] = new double[2] { 1, 1 };
            X[2] = new double[2] { 1, 2 };
            X[3] = new double[2] { 2, 3 };
            X[4] = new double[2] { 2, 2 };
            X[5] = new double[2] { 1, 2 };
            X[6] = new double[2] { 3, 1 };
            X[7] = new double[2] { 3, 3 };
            X[8] = new double[2] { 2, 1 };

            int[][] clusters = new int[3][];
            clusters[0] = new int[2] { 0, 1 };
            clusters[1] = new int[4] { 2, 3, 4, 5 };
            clusters[2] = new int[3] { 6, 7, 8 };


            Tuple<double[], double[], double> silhouette = EhubMisc.Clustering.Silhouette(X, clusters);
            foreach (double si in silhouette.Item1)
                Console.WriteLine(si.ToString("0.#00"));
            Console.ReadKey();
        }
    
        

        internal static void LoadBuildingDemandAndCalcSolarAutonomy()
        {
            //double[] clusterSize, double[] typicalElecDemand, double[] typicalGridPurchase, double[] typicalPvProduction, double[] typicalFeedIn
            
            string clusterSizeFile = @"C:\temp\buildingInteractions\clusterSize.txt";
            string typicalGridFile = @"C:\temp\buildingInteractions\gridPurchase.txt";
            string typicalPvFile = @"C:\temp\buildingInteractions\pvProduction.txt";
            string typicalFeedInFile = @"C:\temp\buildingInteractions\feedIn.txt";
            string typicalElecFile = @"C:\temp\buildingInteractions\elec.txt";      // this needs to be TOTAL electricity demand, including for heat pumps, aircon, battery, ...

            EhubMisc.Misc.LoadTimeSeries(clusterSizeFile, out List<double> clusterSize);
            EhubMisc.Misc.LoadTimeSeries(typicalGridFile, out List<double> gridPurchase);
            EhubMisc.Misc.LoadTimeSeries(typicalPvFile, out List<double> pvProduction);
            EhubMisc.Misc.LoadTimeSeries(typicalFeedInFile, out List<double> feedIn);
            EhubMisc.Misc.LoadTimeSeries(typicalElecFile, out List<double> elecDemand);

            double [] solarAutonomy = EhubMisc.Misc.CalcSolarAutonomy(clusterSize.ToArray(), gridPurchase.ToArray(), pvProduction.ToArray(), feedIn.ToArray(), elecDemand.ToArray());

            Console.WriteLine("solar fraction: {0}, solar self-consumption: {1}", solarAutonomy[0], solarAutonomy[1]);
            Console.ReadKey();

        }
    }
}
