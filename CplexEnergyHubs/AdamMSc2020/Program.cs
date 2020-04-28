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

            Console.WriteLine("Run Energyhub for a single set of demand profiles: Enter '1'");
            Console.WriteLine("Run Energyhubs for several sets of demand profiles: Enter '2'");
            Console.WriteLine();
            int mode = Convert.ToInt32(Console.ReadLine());
            if (mode == 2)
                RunMultipleEhubs();
            else
                RunAdamsEhub();
        }

        

        static void RunMultipleEhubs()
        {

        }

        static void RunAdamsEhub()
        {
            const int hoursPerYear = 8760;
            const int numberOfTypicalDays = 12;


            Console.WriteLine();
            Console.WriteLine("Hi Adam! This is your EnergyHub.");
            Console.WriteLine();
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine(@"Please enter the path of your inputs folder in the following format: 'c:\inputs\'");
            Console.WriteLine("The folder should contain following '.csv' files:");
            Console.WriteLine();
            string[] fileNames = new string[8] { "heatingLoads.csv", "coolingLoads.csv", "electricityLoads.csv", "ghi.csv", "dhwLoads.csv", "Tamb.csv",
                "solarLoads.csv", "solarAreas.csv" };
            foreach (string fname in fileNames)
                Console.WriteLine("- '{0}'", fname);
            int indexSolar = fileNames.Length - 3;  // solar areas, solar loads, and dhw
            Console.WriteLine();
            Console.WriteLine(@"*************************************************************************************");
            Console.Write("Enter your path now and confirm by hitting the Enter-key: ");

            string path = Console.ReadLine();
            Console.WriteLine();
            if (!path.EndsWith(@"\"))
                path = path + @"\";

            /// error checking of the input path: all 4 files existing? is their structure fine?
            Console.WriteLine(@"*************************************************************************************");
            // load data

            List<double> heating = new List<double>();
            List<double> cooling = new List<double>();
            List<double> electricity = new List<double>();
            List<double> ghi = new List<double>();
            List<double> dhw = new List<double>();
            List<double> Tamb = new List<double>();
            List<double[]> solar = new List<double[]>();
            List<double> solarArea = new List<double>();
            foreach (string fname in fileNames)
            {
                if (!File.Exists(path + fname))
                {
                    WriteError();
                    Console.Write("'{0}' does not exist in '{1}'... Hit any key to abort the program: ", fname, path);
                    Console.ReadKey();
                    return;
                }
                else
                {
                    string[] lines = File.ReadAllLines(path + fname);
                    if (lines.Length != hoursPerYear && !string.Equals(fname, fileNames[fileNames.Length - 1]))
                    {
                        WriteError();
                        Console.Write("'{0}' does not have {1} elements, but {2}... Hit any key to abort the program: ", path + fname, hoursPerYear, lines.Length);
                        Console.ReadKey();
                        return;
                    }
                    else if (string.Equals(fname, fileNames[fileNames.Length - 1]))
                        if (lines.Length != solar[0].Length)
                        {
                            WriteError();
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
                    else if (string.Equals(fname, "ghi.csv"))
                        foreach (string line in lines)
                            ghi.Add(Convert.ToDouble(line));
                    else if (string.Equals(fname, "dhwLoads.csv"))
                        foreach (string line in lines)
                            dhw.Add(Convert.ToDouble(line));
                    else if (string.Equals(fname, "Tamb.csv"))
                        foreach (string line in lines)
                            Tamb.Add(Convert.ToDouble(line));
                    else if (string.Equals(fname, "solarLoads.csv"))
                    {
                        for (int li = 0; li < lines.Length; li++)
                        {
                            string[] split = lines[li].Split(new char[2] { ';', ',' });
                            solar.Add(new double[split.Length]);
                            for (int i = 0; i < split.Length; i++)
                                solar[li][i] = Convert.ToDouble(split[i]);
                        }
                    }
                    else if (string.Equals(fname, "solarAreas.csv"))
                        foreach (string line in lines)
                            solarArea.Add(Convert.ToDouble(line));
                }
            }

            // load technology parameters
            Dictionary<string, double> technologyParameters = new Dictionary<string, double>();
            string technologyParametersFile = "technologyParameters.csv";
            if (!File.Exists(path + technologyParametersFile))
            {
                WriteError();
                Console.Write("'{0}' does not exist in '{1}'... Hit any key to abort the program: ", technologyParametersFile, path);
                Console.ReadKey();
                return;
            }
            else
            {
                string[] lines = File.ReadAllLines(path + technologyParametersFile);
                for (int li = 0; li < lines.Length; li++)
                {
                    string[] split = lines[li].Split(new char[2] { ';', ',' });
                    split = split.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    if (split.Length != 3)
                    {
                        WriteError();
                        Console.WriteLine("Reading line {0}:..... '{1}'", li+1, lines[li]);
                        Console.Write("'{0}' contains {1} cells in line {2}, but it should contain 3 - the first two being strings and the third a number... Hit any key to abort the program: ",
                            path + technologyParametersFile, split.Length, li+1); 
                        Console.ReadKey();
                        return;
                    }
                    else
                    {
                        if (technologyParameters.ContainsKey(split[0])) continue;
                        technologyParameters.Add(split[0], Convert.ToDouble(split[2]));
                    }
                }
            }

            // get length of "solar.csv", that determines how many solar profiles we have
            int numberOfSolarAreas = solar[0].Length;


            Console.WriteLine("Data read successfully...");
            Console.WriteLine();

            /// data preparation, clustering and typical days
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine("Clustering and Generating typical days...");

            int numLoads = fileNames.Length - 2 + numberOfSolarAreas - 1; // heating, cooling, electricity, solar. however, solar will include several profiles. - 1 dhw
            double[][] fullProfiles = new double[numLoads][];
            string[] loadTypes = new string[numLoads];
            bool[] peakDays = new bool[numLoads];
            bool[] correctionLoad = new bool[numLoads];
            for (int i = 0; i < numLoads; i++)
                fullProfiles[i] = new double[hoursPerYear];
            loadTypes[0] = "heating";
            loadTypes[1] = "cooling";
            loadTypes[2] = "electricity";
            loadTypes[3] = "ghi";
            loadTypes[4] = "Tamb";
            peakDays[0] = true;
            peakDays[1] = true;
            peakDays[2] = true;
            peakDays[3] = false;
            peakDays[4] = false;
            correctionLoad[0] = true;
            correctionLoad[1] = true;
            correctionLoad[2] = true;
            correctionLoad[3] = false;
            correctionLoad[4] = false;

            bool[] useForClustering = new bool[fullProfiles.Length]; // specificy here, which load is used for clustering. the others are just reshaped
            for (int t = 0; t < hoursPerYear; t++) 
            { 
                fullProfiles[0][t] = heating[t] + dhw[t];
                fullProfiles[1][t] = cooling[t];
                fullProfiles[2][t] = electricity[t];
                fullProfiles[3][t] = ghi[t];
                fullProfiles[4][t] = Tamb[t];
                useForClustering[0] = true;
                useForClustering[1] = true;
                useForClustering[2] = true;
                useForClustering[3] = true;
                useForClustering[4] = false;
            }

            for (int i = indexSolar; i < numLoads; i++) 
            {
                useForClustering[i] = false;
                peakDays[i] = false;
                correctionLoad[i] = true;
                loadTypes[i] = "solar";
                for(int t=0; t<hoursPerYear; t++)
                    fullProfiles[i][t] = solar[t][i-indexSolar];
            }

            // TO DO: load in GHI time series, add it to full profiles (right after heating, cooling, elec), and use it for clustering. exclude other solar profiles from clustering, but they need to be reshaped too
            EhubMisc.HorizonReduction.TypicalDays typicalDays = EhubMisc.HorizonReduction.GenerateTypicalDays(fullProfiles, loadTypes, numberOfTypicalDays, peakDays, useForClustering, correctionLoad);


            /// Running Energy Hub
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine("Running MILP now...");
            double[][] typicalSolarLoads = new double[solarArea.Count][];
            for (int i = 0; i < numLoads - indexSolar; i++) 
                typicalSolarLoads[i] = typicalDays.DayProfiles[indexSolar + i];
            int[] clustersizePerTimestep = typicalDays.NumberOfDaysPerTimestep;
            Ehub ehub = new Ehub(typicalDays.DayProfiles[0], typicalDays.DayProfiles[1], typicalDays.DayProfiles[2],
                typicalSolarLoads, solarArea.ToArray(),
                typicalDays.DayProfiles[4], technologyParameters,
                clustersizePerTimestep);
            ehub.Solve(5, true);


            /// Storing Results
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine("Saving results into {0}...", path+ @"results\");

            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            // write a csv for each epsilon cut, name it "_e1", "_e2", .. etc
            List<string> header = new List<string>();
            // write units to 2nd row
            List<string> header_units = new List<string>();
            header.Add("Lev.Emissions");
            header_units.Add("kgCO2eq/a");
            header.Add("Lev.Cost");
            header_units.Add("CHF/a");
            header.Add("OPEX");
            header_units.Add("CHF/a");
            header.Add("CAPEX");
            header_units.Add("CHF/a");
            header.Add("x_Battery");
            header_units.Add("kWh");
            header.Add("x_TES");
            header_units.Add("kWh");
            header.Add("x_CHP");
            header_units.Add("kW");
            header.Add("x_Boiler");
            header_units.Add("kW");
            header.Add("x_ASHP");
            header_units.Add("kW");
            header.Add("x_AirCon");
            header_units.Add("kW");
            header.Add("x_Battery_charge");
            header_units.Add("kWh");
            header.Add("x_Battery_discharge");
            header_units.Add("kWh");
            header.Add("x_Battery_soc");
            header_units.Add("kWh");
            header.Add("x_TES_charge");
            header_units.Add("kWh");
            header.Add("x_TES_discharge");
            header_units.Add("kWh");
            header.Add("x_TES_soc");
            header_units.Add("kWh");
            header.Add("x_CHP_op_e");
            header_units.Add("kWh");
            header.Add("x_CHP_op_h");
            header_units.Add("kWh");
            header.Add("x_CHP_dump");
            header_units.Add("kWh");
            header.Add("x_Boiler_op");
            header_units.Add("kWh");
            header.Add("x_ASHP_op");
            header_units.Add("kWh");
            header.Add("x_AirCon_op");
            header_units.Add("kWh");
            header.Add("x_GridPurchase");
            header_units.Add("kWh");
            header.Add("x_FeedIn");
            header_units.Add("kWh");
            for (int i = 0; i < numberOfSolarAreas; i++)
            {
                header.Add("x_PV_" + i);
                header_units.Add("sqm");
            }
            header.Add("b_PV_totalProduction");
            header_units.Add("kWh");
            header.Add("TypicalHeating");
            header_units.Add("kWh");
            header.Add("TypicalCooling");
            header_units.Add("kWh");
            header.Add("TypicalElectricity");
            header_units.Add("kWh");
            header.Add("TypicalGHI");
            header_units.Add(@"W/sqm");
            header.Add("TypicalAmbientTemp");
            header_units.Add("deg C");

            for (int e = 0; e < ehub.Outputs.Length; e++) 
            {
                List<List<string>> outputString = new List<List<string>>();
                outputString.Add(header);
                outputString.Add(header_units);

                List<string> firstLine = new List<string>();
                firstLine.Add(Convert.ToString(ehub.Outputs[e].carbon));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].cost));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].OPEX));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].CAPEX));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_boi));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_hp));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_ac));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_charge[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_discharge[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_soc[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_charge[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_discharge[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_soc[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_e[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_h[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_dump[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_boi_op[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_hp_op[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_ac_op[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_elecpur[0]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].x_feedin[0]));
                for (int i = 0; i < numberOfSolarAreas; i++)
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_pv[i]));
                firstLine.Add(Convert.ToString(ehub.Outputs[e].b_pvprod[0]));

                firstLine.Add(Convert.ToString(typicalDays.DayProfiles[0][0]));
                firstLine.Add(Convert.ToString(typicalDays.DayProfiles[1][0]));
                firstLine.Add(Convert.ToString(typicalDays.DayProfiles[2][0]));
                firstLine.Add(Convert.ToString(typicalDays.DayProfiles[3][0]));
                firstLine.Add(Convert.ToString(typicalDays.DayProfiles[4][0]));

                outputString.Add(firstLine);

                for (int t=1; t<ehub.Outputs[e].x_elecpur.Length; t++)
                {
                    List<string> newLine = new List<string>();
                    for (int skip = 0; skip < 10; skip++)
                        newLine.Add("");
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_charge[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_discharge[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_soc[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_charge[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_discharge[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_soc[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_e[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_h[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_dump[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_boi_op[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_hp_op[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_ac_op[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_elecpur[t]));
                    newLine.Add(Convert.ToString(ehub.Outputs[e].x_feedin[t]));
                    for (int skip = 0; skip < numberOfSolarAreas; skip++)
                        newLine.Add("");
                    newLine.Add(Convert.ToString(ehub.Outputs[e].b_pvprod[t]));

                    newLine.Add(Convert.ToString(typicalDays.DayProfiles[0][t]));
                    newLine.Add(Convert.ToString(typicalDays.DayProfiles[1][t]));
                    newLine.Add(Convert.ToString(typicalDays.DayProfiles[2][t]));
                    newLine.Add(Convert.ToString(typicalDays.DayProfiles[3][t]));
                    newLine.Add(Convert.ToString(typicalDays.DayProfiles[4][t]));

                    outputString.Add(newLine);
                }

                using var sw = new StreamWriter(outputPath + "inputfilename" + "_out" + "_e" + e + ".csv");
                foreach (List<string> line in outputString)
                {
                    foreach (string cell in line)
                        sw.Write(cell + ";");
                    sw.Write(Environment.NewLine);
                }
                sw.Close();

            }


            /// Waiting for user to close window
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine("Hit any key to close program...: ");
            Console.ReadKey();

        }

        static void WriteError()
        {
            string[] dog = EhubMisc.Misc.AsciiDrawing(0);

            foreach (string d in dog)
                Console.WriteLine(d);

            Console.WriteLine();
            Console.WriteLine("                      Debug-Puppy found an error...");
            Console.WriteLine();
            //Console.WriteLine(errorMessage);
            //Console.WriteLine("Hit any key to abort program...");
            //Console.ReadKey();
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
