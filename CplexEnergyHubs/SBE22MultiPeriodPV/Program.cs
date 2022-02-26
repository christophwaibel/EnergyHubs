using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using EhubMisc;


namespace SBE22MultiPeriodPV
{
    class Program
    {
        static void Main(string[] args)
        {
            RunMultiPeriodEHub();
        }

        static void RunMultiPeriodEHub()
        {
            const int minusIndex = 1; //1 for the smaller dataset...
            const int epsilonCuts = 3;
            const int periodInterval = 5;
            int[] periods = new int[(2050 - 2020) / periodInterval];
            for (int p = 0; p < periods.Length; p++)
                periods[p] = 2020 + p * periodInterval;

            Console.WriteLine("___________________________________________________________________ \n ");
            Console.WriteLine("SBE22 Multi-Period Energy Hub");
            Console.WriteLine("For stages...");
            foreach(int period in periods)
                Console.Write("{0}  ", period);
            Console.WriteLine("\n___________________________________________________________________ \n");

            Console.WriteLine(@"Specify file path, e.g.: 'c:\sbe22\'");
            Console.WriteLine(@"Or just hit ENTER and it will use the current file directory \n");
            Console.WriteLine(@"That folder needs to contain a subfolder called '\inputs'");
            string path = Console.ReadLine();
            if (path.Length == 0)
                path = System.AppDomain.CurrentDomain.BaseDirectory;
            if (!path.EndsWith(@"\"))
                path = path + @"\";

            string pathInputs = path + @"inputs\";

            if (!Directory.Exists(pathInputs))
            {
                Console.WriteLine("*********************\nWARNING: {0} folder missing!!! Hit any key to quit program", pathInputs);
                Console.ReadKey();
                return;
            }


            // load loads
            // load peak loads

            var dhwAllPeriods = new List<List<double>>();
            var htgAllPeriods = new List<List<double>>();
            var clgAllPeriods = new List<List<double>>();
            var elecAllPeriods = new List<List<double>>();

            var peakHtgAllPeriods = new List<List<double>>();
            var peakClgAllPeriods = new List<List<double>>();

            var ghiAllPeriods = new List<List<double>>();
            var dryBulbAllPeriods = new List<List<double>>();

            var techParamsAllPeriods = new List<Dictionary<string, double>>();
            
            for (int p = 0; p < periods.Length; p++)
            {
                string period = periods[p].ToString();
                LoadBuildingInput(pathInputs + "ZH_" + period + "_BAU_demand.csv", out var htg, out var dhw, out var clg, out var elec);
                htgAllPeriods.Add(htg);
                dhwAllPeriods.Add(dhw);
                clgAllPeriods.Add(clg);
                elecAllPeriods.Add(elec);

                LoadPeakLoads(pathInputs + "ZH_" + period + "_BAU_PeakLoads.csv", out var numBuildings, out var peakHtg, out var peakClg);
                peakHtgAllPeriods.Add(peakHtg);
                peakClgAllPeriods.Add(peakClg);

                Misc.LoadTimeSeries(pathInputs + "ZH_" + period + "_BAU_GHI.csv", out var ghi);
                Misc.LoadTimeSeries(pathInputs + "ZH_" + period + "_BAU_DryBulb.csv", out var dryBulb);
                ghiAllPeriods.Add(ghi);
                dryBulbAllPeriods.Add(dryBulb);

                LoadTechParameters(pathInputs + "ZH_" + period + "_BAU_technology.csv", out var technologyParameters);
                technologyParameters.Add("NumberOfBuildingsInEHub", Convert.ToDouble(numBuildings));
                for (int i = 0; i < numBuildings; i++)
                {
                    technologyParameters.Add("Peak_Htg_" + Convert.ToString(i), peakHtg[i]);
                    technologyParameters.Add("Peak_Clg_" + Convert.ToString(i), peakClg[i]);
                }
                techParamsAllPeriods.Add(technologyParameters);
            }

            //load surface areas - the same for all periods
            LoadSurfaceAreasInput(pathInputs + "ZH_SurfaceAreas.csv", out var solarAreas);
            // load sensor points - the same for all periods in this model
            LoadSolarPotentialsInput(pathInputs + "ZH_solar_SP0.csv", out var irradiance, minusIndex);

            Console.WriteLine("___________________________________________________________________ \n ");
            Console.WriteLine("All Inputs Loaded...");





            /// data preparation, clustering and typical days
            int numberOfSolarAreas = solarAreas.Count;
            int numBaseLoads = 5;                               // heating, cooling, electricity, ghi, tamb
            int numLoads = numBaseLoads + numberOfSolarAreas;   // heating, cooling, electricity, ghi, tamb, solar. however, solar will include several profiles.
            const int hoursPerYear = 8760;
            string[] loadTypes = new string[numLoads];
            bool[] peakDays = new bool[numLoads];
            bool[] correctionLoad = new bool[numLoads];

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

            // specificy here, which load is used for clustering. the others are just reshaped
            bool[] useForClustering = new bool[numLoads]; 
            for (int t = 0; t < hoursPerYear; t++)
            {
                useForClustering[0] = true;
                useForClustering[1] = true;
                useForClustering[2] = true;
                useForClustering[3] = true;
                useForClustering[4] = false;
            }

            for (int u = 0; u < numberOfSolarAreas; u++)
            {
                useForClustering[u + numBaseLoads] = false;
                peakDays[u + numBaseLoads] = false;
                correctionLoad[u + numBaseLoads] = true;
                loadTypes[u + numBaseLoads] = "solar";
            }


            // looping through all periods and create typical days for each
            var allTypicalHtgDays = new List<double[]>();
            var allTypicalClgDays = new List<double[]>();
            var allTypicalElecDays = new List<double[]>();
            var allTypicalAmbientTemp = new List<double[]>();
            var allTypicalSolarLoads = new List<double[][]>();
            var allClusterSizes = new List<int[]>();
            for (int p=0; p<periods.Length; p++)
            {
                // Clustering typical days for each period
                Console.WriteLine("Creating Typical Days Profiles for period {0}...", periods[p]);
                double[][] fullProfiles = new double[numLoads][];
                for (int u = 0; u < numLoads; u++)
                    fullProfiles[u] = new double[hoursPerYear];
                for (int t = 0; t < hoursPerYear; t++)
                {
                    fullProfiles[0][t] = htgAllPeriods[p][t] + dhwAllPeriods[p][t];
                    fullProfiles[1][t] = clgAllPeriods[p][t];
                    fullProfiles[2][t] = elecAllPeriods[p][t];
                    fullProfiles[3][t] = ghiAllPeriods[p][t];
                    fullProfiles[4][t] = dryBulbAllPeriods[p][t];
                    for(int u=0; u<numberOfSolarAreas; u++)
                        fullProfiles[u + numBaseLoads][t] = irradiance[u][t];
                }
                HorizonReduction.TypicalDays typicalDays = HorizonReduction.GenerateTypicalDays(fullProfiles, loadTypes,
                    12, peakDays, useForClustering, correctionLoad, false);
                allClusterSizes.Add(typicalDays.NumberOfDaysPerTimestep);

                // solar profiles negative or very small numbers. rounding floating numbers thing?
                double[][] typicalSolarLoads = new double[numberOfSolarAreas][];
                for (int u = 0; u < numberOfSolarAreas; u++)
                {
                    typicalSolarLoads[u] = typicalDays.DayProfiles[numBaseLoads + u];
                    for (int t = 0; t < typicalSolarLoads[u].Length; t++)
                    {
                        if (typicalSolarLoads[u][t] < 0.1)
                            typicalSolarLoads[u][t] = 0.0;
                    }
                }
                // same for heating, cooling, elec demand... round very small numbers
                for (int t = 0; t < typicalDays.DayProfiles[0].Length; t++)
                    for (int i = 0; i < 3; i++)
                        if (typicalDays.DayProfiles[i][t] < 0.001) typicalDays.DayProfiles[i][t] = 0.0;


                allTypicalSolarLoads.Add(typicalSolarLoads);
                allTypicalHtgDays.Add(typicalDays.DayProfiles[0]);
                allTypicalClgDays.Add(typicalDays.DayProfiles[1]);
                allTypicalElecDays.Add(typicalDays.DayProfiles[2]);
                allTypicalAmbientTemp.Add(typicalDays.DayProfiles[4]);
            }

            // Solve Multi-Period Ehub
            Console.WriteLine("___________________________________________________________________ \n ");
            Console.WriteLine("Solving MILP optimization model...");
            MultiPeriodEHub ehub = new MultiPeriodEHub(allTypicalHtgDays, allTypicalClgDays, allTypicalElecDays,
                allTypicalSolarLoads,solarAreas.ToArray(),allTypicalAmbientTemp,
                techParamsAllPeriods, allClusterSizes, periodInterval);
            ehub.Solve(epsilonCuts, false);



            //WriteOutput("result_scenario_" + s.ToString(), path, numberOfSolarAreas, ehub, typicalDays, numBaseLoads);

            Console.ReadKey();
        }



        static void WriteOutput(string scenario, string path, int numberOfSolarAreas, MultiPeriodEHub ehub, 
            List<double[][]> allTypicalSolarLoads, List<double[][]> allTypicalHtgDays, List<double[][]> allTypicalClgDays, 
            List<double[][]> allTypicalElecDays, List<double[][]> allTypicalAmbientTemp, int numBaseLoads)
        {
            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            // write a csv for each period, name it "_p1", "_p2", .. etc
            List<string> header = new List<string>();
            // write units to 2nd row
            List<string> header_units = new List<string>();
            header.Add("Total Emissions");
            header_units.Add("kgCO2eq");
            header.Add("Total Cost");
            header_units.Add("CHF");
            header.Add("OPEX");
            header_units.Add("CHF");
            header.Add("CAPEX");

            header.Add("x_new_PV_mono");
            header_units.Add("sqm");
            header.Add("x_new_PV_cdte");
            header_units.Add("sqm");

            header.Add("PV_Electricity");
            header_units.Add("kWh");

            header.Add("TypicalHeating");
            header_units.Add("kWh");
            header.Add("TypicalCooling");
            header_units.Add("kWh");
            header.Add("TypicalElectricity");
            header_units.Add("kWh");
            header.Add("TypicalAmbientTemp");
            header_units.Add("deg C");
            header.Add("ClusterSize");
            header_units.Add("Days");
            for (int i = 0; i < numberOfSolarAreas; i++)
            {
                header.Add("TypicalPotentialsSP_" + i);
                header_units.Add("W/m^2");
            }

            for (int p = 0; p < ehub.NumPeriods; p++)
            {
                List<List<string>> outputString = new List<List<string>>();
                if (ehub.Outputs[0].infeasible)
                {
                    outputString.Add(new List<string> { "Infeasible" });
                    Console.WriteLine("--- Infeasible Solution ---");
                }
                else
                {
                    outputString.Add(header);
                    outputString.Add(header_units);

                    List<string> firstLine = new List<string>();
                    firstLine.Add(Convert.ToString(ehub.Outputs[0].Carbon));
                    firstLine.Add(Convert.ToString(ehub.Outputs[0].Cost));
                    firstLine.Add(Convert.ToString(ehub.Outputs[0].Opex));
                    firstLine.Add(Convert.ToString(ehub.Outputs[0].Capex));

                    firstLine.Add(Convert.ToString(ehub.Outputs[0].XNewPvMono[p][0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[0].XNewPvCdte[p][0]));

                    firstLine.Add(Convert.ToString(ehub.Outputs[0].XOperationPvElectricity[p][0]));

                    firstLine.Add(Convert.ToString(allTypicalHtgDays[p][0]));
                    firstLine.Add(Convert.ToString(allTypicalClgDays[p][0]));
                    firstLine.Add(Convert.ToString(allTypicalElecDays[p][0]));
                    firstLine.Add(Convert.ToString(allTypicalAmbientTemp[p][0]));

                    firstLine.Add(Convert.ToString(ehub.Outputs[0].Clustersize[p][0]));
     

                    for (int i = 0; i < numberOfSolarAreas; i++)
                        firstLine.Add(Convert.ToString(allTypicalSolarLoads[p][0]));

                    outputString.Add(firstLine);

                    for (int t = 1; t < ehub.Outputs[0].XOperationPvElectricity[p].Length; t++)
                    {
                        List<string> newLine = new List<string>();
                        for (int skip = 0; skip < 4; skip++)
                            newLine.Add("");
                        newLine.Add(Convert.ToString(ehub.Outputs[p].x_bat_charge[t]));

                        for (int skip = 0; skip < numberOfSolarAreas; skip++)
                            newLine.Add("");
                        for (int skip = 0; skip < ehub.NumberOfBuildingsInDistrict; skip++)
                            newLine.Add("");
                        newLine.Add(Convert.ToString(ehub.Outputs[p].b_pvprod[t]));

                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[0][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[1][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[2][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[3][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[4][t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[p].clustersize[t]));
                        newLine.Add("");
                        for (int i = 0; i < numberOfSolarAreas; i++)
                            newLine.Add(Convert.ToString(typicalDays.DayProfiles[numBaseLoads + i][t]));

                        outputString.Add(newLine);
                    }

                }
                using var sw = new StreamWriter(outputPath + scenario + "_result_epsilon_" + p + ".csv");
                foreach (List<string> line in outputString)
                {
                    foreach (string cell in line)
                        sw.Write(cell + ";");
                    sw.Write(Environment.NewLine);
                }
                sw.Close();

            }
        }



        static void LoadBuildingInput(string inputFile,
            out List<double> heating, out List<double> dhw, out List<double> cooling, out List<double> electricity)
        {
            heating = new List<double>();
            dhw = new List<double>();
            cooling = new List<double>();
            electricity = new List<double>();


            // Headers: [0] -; [1] space heating; [2] dhw; [3] cooling; [4] electricity
            string[] lines = File.ReadAllLines(inputFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] line = lines[i].Split(new char[2] { ',', ';' });
                cooling.Add(Convert.ToDouble(line[3]));
                dhw.Add(Convert.ToDouble(line[2]));
                heating.Add(Convert.ToDouble(line[1]));
                electricity.Add(Convert.ToDouble(line[4]));
            }
        }


        static void LoadPeakLoads(string inputFile,
            out int numBuildings, out List<double> peakHeatingLoads, out List<double> peakCoolingLoads)
        {
            peakHeatingLoads = new List<double>();
            peakCoolingLoads = new List<double>();
            var lines = File.ReadAllLines(inputFile);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Split(new char[2] { ',', ';' });
                peakHeatingLoads.Add(Convert.ToDouble(line[3])); // sh_and_dhw at [3]
                peakCoolingLoads.Add(Convert.ToDouble(line[4]));
            }

            numBuildings = peakHeatingLoads.Count;
        }


        static void LoadTechParameters(string inputFile, out Dictionary<string, double> technologyParameters)
        {
            // load technology parameters
            technologyParameters = new Dictionary<string, double>();


            string[] lines = File.ReadAllLines(inputFile);
            for (int li = 0; li < lines.Length; li++)
            {
                string[] split = lines[li].Split(new char[2] { ';', ',' });
                split = split.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (split.Length < 3)
                {
                    //WriteError();
                    Console.WriteLine("Reading line {0}:..... '{1}'", li + 1, lines[li]);
                    Console.Write("'{0}' contains {1} cells in line {2}, but it should contain at least 3 lines - the first two being strings and the third a number... Hit any key to abort the program: ",
                        inputFile, split.Length, li + 1);
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


        /// <summary>
        /// one surface area per sensor point. get value from second last index
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="solarArea"></param>
        static void LoadSurfaceAreasInput(string inputFile, out List<double> solarArea)
        {
            solarArea = new List<double>();

            string[] solarAreaLines = File.ReadAllLines(inputFile);
            for (int i = 1; i < solarAreaLines.Length; i++)
            {
                string[] line = solarAreaLines[i].Split(new char[2] { ',', ';' });
                double area = Convert.ToDouble(line[^2]);  // ^2 get's me the 2nd last index of an array. That's the 'usefulearea' column in the input csv
                solarArea.Add(area);
            }
        }


        /// <summary>
        /// one sensor point per profile
        /// </summary>
        static void LoadSolarPotentialsInput(string inputFile, out double[][] solarPotentials, int minusIndex)
        {
            int horizon = 8760;

            string[] linesSp = File.ReadAllLines(inputFile);
            string[] firstLine = linesSp[0].Split(new char[2] { ',', ';' });
            solarPotentials = new double[firstLine.Length - minusIndex][];
            for (int i = 0; i < solarPotentials.Length; i++)
                solarPotentials[i] = new double[horizon];

            for (int t = 1; t < linesSp.Length; t++) // first line is header, so skip
            {
                string[] currentHour = linesSp[t].Split(new char[2] { ',', ';' });
                for (int i = 0; i < solarPotentials.Length; i++)
                    solarPotentials[i][t - 1] = Convert.ToDouble(currentHour[i]);
            }
        }
    }
}
