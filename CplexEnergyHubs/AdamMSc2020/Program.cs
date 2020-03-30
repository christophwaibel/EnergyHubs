using System;
using System.IO;
using System.Collections.Generic;

using System.Linq;

namespace AdamMSc2020
{
    class Program
    {
        static void Main(string[] args)
        {
            //ClusterRandomData();

            //ClusterLoadData();

            //SilhouetteTest();

            RunAdamsEhub();
        }


        static void RunAdamsEhub()
        {
            const int daysPerYear = 365;
            const int hoursPerDay = 24;
            const int hoursPerYear = 8760;
            const int numberOfTypicalDays = 12;

            string[] dog = EhubMisc.Misc.AsciiDrawing(0);
            foreach (string d in dog)
                Console.WriteLine(d);

            Console.WriteLine();
            Console.WriteLine("Hi Adam! This is your EnergyHub. Also, above is a puppy.");
            Console.WriteLine();
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine(@"Please enter the path of your inputs folder in the following format: 'c:\inputs\'");
            Console.WriteLine("The folder should contain following '.csv' files:");
            Console.WriteLine();
            string[] fileNames = new string[5] { "heatingLoads.csv", "coolingLoads.csv", "electricityLoads.csv", "solarLoads.csv", "solarAreas.csv" };
            foreach (string fname in fileNames)
                Console.WriteLine("- '{0}'", fname);
            Console.WriteLine();
            Console.WriteLine(@"*************************************************************************************");
            Console.Write("Enter your path now and confirm by hitting the Enter-key: ");

            string path = Console.ReadLine();
            Console.WriteLine();
            if (!path.EndsWith(@"\"))
                path = path + @"\";

            // error checking of the input path: all 4 files existing? is their structure fine?
            Console.WriteLine(@"*************************************************************************************");
            // load data

            List<double> heating = new List<double>();
            List<double> cooling = new List<double>();
            List<double> electricity = new List<double>();
            List<double[]> solar = new List<double[]>();
            List<double> solarArea = new List<double>();
            foreach (string fname in fileNames)
            {
                if (!File.Exists(path + fname))
                {
                    Console.Write("'{0}' does not exist in '{1}'... Hit any key to abort the program: ", fname, path);
                    Console.ReadKey();
                    return;
                }
                else
                {
                    string[] lines = File.ReadAllLines(path + fname);
                    if (lines.Length != hoursPerYear && !string.Equals(fname, fileNames[fileNames.Length - 1]))
                    {
                        Console.Write("'{0}' does not have {1} elements, but {2}... Hit any key to abort the program: ", path + fname, hoursPerYear, lines.Length);
                        Console.ReadKey();
                        return;
                    }
                    else if (string.Equals(fname, fileNames[fileNames.Length - 1]))
                        if (lines.Length != solar[0].Length)
                        {
                            Console.Write("'{0}' contains {1} surface area elements, but {2} contains {3} irradiance profiles... Hit any key to abort the program: ",
                                path + fname, lines.Length, path + fileNames[fileNames.Length - 2], solar[0].Length);
                            Console.ReadKey();
                            return;
                        }
                    if (string.Equals(fname, "heatingLoads.csv"))
                        foreach (string line in lines)
                            heating.Add(Convert.ToDouble(line));
                    else if (string.Equals(fname, "coolingLoads.csv"))
                        foreach (string line in lines)
                            cooling.Add(Convert.ToDouble(line));
                    else if (string.Equals(fname, "electricityLoads.csv"))
                        foreach (string line in lines)
                            electricity.Add(Convert.ToDouble(line));
                    else if (string.Equals(fname, "solarLoads.csv"))
                    {
                        for (int li = 0; li < lines.Length; li++)
                        {
                            string[] split = lines[li].Split(new char[2] { ';', ',' });
                            solar.Add(new double[split.Length]);
                            for (int i = 0; i < split.Length; i++)
                            {
                                solar[li][i] = Convert.ToDouble(split[i]);
                            }
                        }
                    }
                    else if (string.Equals(fname, "solarAreas.csv"))
                        foreach (string line in lines)
                            solarArea.Add(Convert.ToDouble(line));
                }
            }

            // get length of "solar.csv", that determines how many solar profiles we have
            int numberOfSolarAreas = solar[0].Length;


            Console.WriteLine("Data read successfully...");
            Console.WriteLine();

            // data preparation, clustering and typical days
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine("Clustering and Generating typical days...");

            int numLoads = fileNames.Length - 2 + numberOfSolarAreas; // heating, cooling, electricity, solar. however, solar will include several profiles
            double[][] fullProfiles = new double[numLoads][];
            for (int i = 0; i < numLoads; i++)
                fullProfiles[i] = new double[hoursPerYear];               
            string[] loadTypes = new string[numLoads]; // { "heating", "cooling", "electricity", "solar" };
            loadTypes[0] = "heating";
            loadTypes[1] = "cooling";
            loadTypes[2] = "electricity";
            bool[] peakDays = new bool[numLoads]; // { true, true, true, false };
            peakDays[0] = true;
            peakDays[1] = true;
            peakDays[2] = true;

            for (int t = 0; t < hoursPerYear; t++) 
            { 
                fullProfiles[0][t] = heating[t];
                fullProfiles[1][t] = cooling[t];
                fullProfiles[2][t] = electricity[t];
            }

            for (int i = 3; i < numLoads; i++) 
            {
                peakDays[i] = false;
                loadTypes[i] = "solar";
                for(int t=0; t<hoursPerYear; t++)
                    fullProfiles[i][t] = solar[t][i-3];
            }

            EhubMisc.DemandParameterization.TypicalDays typicalDays = EhubMisc.DemandParameterization.GenerateTypicalDays(fullProfiles, loadTypes, numberOfTypicalDays, peakDays);



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

            Tuple<double[][], int[][], double> tuple = EhubMisc.Clustering.KMeans(randomData, clusters);
            //Tuple<int[], int[][], double> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters, 50, 34, "MeansApproximation");
            //Tuple<int[], int[][], double> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters, 50, 34, "PAM_Random");
            //Tuple<int[], int[][], double> tuple = EhubMisc.Clustering.KMedoids(randomData, clusters);

            Tuple<double[], double[], double> silhouette = EhubMisc.Clustering.Silhouette(randomData, tuple.Item2);
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
            for(int i=1; i<replicates.Count; i++)
            {
                if(replicates[i].Item3 < minCost)
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
                    text += ",#id,"+ index.ToString() + ",medoid," + clusteredData.Item1[_k].ToString();
                    writeSamples.Add(text);

                    idx.Add(index, _k);
                }
                writeSamples.Add(""); 
            }
            string outPath = @"c:\temp\ecostest\testsilhouette.csv";
            File.WriteAllLines(outPath, writeSamples.ToArray());

            idx = idx.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            List<string> idxString = new List<string>();
            for (int i=0; i<days; i++)
                idxString.Add(idx[i].ToString());
            string outPath2 = @"c:\temp\ecostest\idx.csv";
            File.WriteAllLines(outPath2, idxString.ToArray());

        }


        static void SilhouetteTest()
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
    }
}
