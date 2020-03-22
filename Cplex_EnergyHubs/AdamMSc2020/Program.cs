using System;

namespace AdamMSc2020
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Cplex test");

            Random rnd = new Random(34);

            double[][] randomData = new double[100][];
            for(int i=0; i<100; i++)
            {
                randomData[i] = new double[2];
                randomData[i][0] = rnd.NextDouble();
                randomData[i][1] = rnd.NextDouble();
            }

            //Tuple<double[][], int[][]> tuple = EhubMisc.Clustering.KMeans(randomData, 3);
            Tuple<int[], int[][]> tuple = EhubMisc.Clustering.KMedoids(randomData, 3, 50, 34, "MeansApproximation");
          
            Console.ReadKey();


        }
    }
}
