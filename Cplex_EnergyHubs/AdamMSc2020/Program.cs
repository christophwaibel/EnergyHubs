using System;
using System.IO;
using System.Collections.Generic;

namespace AdamMSc2020
{
    class Program
    {
        static void Main(string[] args)
        {
            //ClusterRandomData();

            ClusterLoadData();

        }


        static void ClusterRandomData()
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

            //Tuple<double[][], int[][]> tuple = EhubMisc.Clustering.KMeans(randomData, clusters);
            //Tuple<int[], int[][]> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters, 50, 34, "MeansApproximation");
            //Tuple<int[], int[][]> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters, 50, 34, "PAM_Exhaustive");
            Tuple<int[], int[][]> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters);

            Tuple<double[], double[], double> silhouette = EhubMisc.Clustering.Silhouette(randomData, tuple.Item2, tuple.Item1);
        }


        static void ClusterLoadData()
        {
            // load some profiles and cluster them
            string path = @"c:\temp\ecostest\heating.csv";
            string [] lines = File.ReadAllLines(path);
            List<double> heating = new List<double>();
            foreach (string line in lines)
                heating.Add(Convert.ToDouble(line));


            // clustering 
            Random rnd = new Random(1);
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

            //Tuple<double[][], int[][]> clusteredData = EhubMisc.Clustering.KMeans(heating_perDay, clusters);
            //Tuple<int[], int[][]> clusteredData = EhubMisc.Clustering.KMedoids(heating_perDay, clusters, 50, 34, "MeansApproximation");
            Tuple<int[], int[][]> clusteredData = EhubMisc.Clustering.KMedoids(heating_perDay, clusters, 50, 34, "PAM_Exhaustive");
            //Tuple<int[], int[][]> clusteredData = EhubMisc.Clustering.KMedoids(heating_perDay, clusters);

            Tuple<double[], double[], double> silhouette = EhubMisc.Clustering.Silhouette(heating_perDay, clusteredData.Item2, clusteredData.Item1);


            // write cluster points. make a blank line between two clusters
            // columns 1 to n are the parameters, n+1 is s(i)
            List<string> writeSamples = new List<string>();
            for(int _k=0; _k<clusters; _k++) 
            {
                for(int i=0; i<clusteredData.Item2[_k].Length; i++)
                {
                    int index = clusteredData.Item2[_k][i];
                    double [] sample = heating_perDay[index];
                    double s_i = silhouette.Item1[index];
                    string text = null;
                    foreach (double parameter in sample)
                        text += parameter.ToString("0.#0") + ",";
                    text += "s_i," + s_i.ToString("0.#0");

                    writeSamples.Add(text);
                }
                writeSamples.Add(""); 
            }
            string outPath = @"c:\temp\ecostest\testsilhouette.csv";
            File.WriteAllLines(outPath, writeSamples.ToArray());


            // write centroids / medoids




        }
    }
}
