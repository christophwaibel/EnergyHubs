using System;

namespace AdamMSc2020
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Cplex test");

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
    }
}
