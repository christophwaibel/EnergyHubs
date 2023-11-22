using System;
using System.IO;
using System.Collections.Generic;

using System.Linq;

namespace AdamMSc2020
{
    class Program
    {
        const int hoursPerYear = 8760;


        /// <summary>
        /// The main program. 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //ClusterRandomData();

            //ClusterLoadData();

            //SilhouetteTest();


            //try
            //{
            //    RunMultipleEhubs();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}
            //Console.ReadKey();



            // postprocessing for Sobol Indices:
            // calculate solar self sufficiency, solar fraction, and sum of all PV surfaces
            LoadEhubResultsAndComputeSolarAutonomyAndPvSurfaces();
        }


        /// <summary>
        /// Run Energy Hubs for multiple inputs and writes outputs for each
        /// </summary>
        static void RunMultipleEhubs()
        {
            int numberOfTypicalDays = 12;
            string buildingInputName = @"building_input";
            string technologyInputName = @"technology_input";

            // ask for folder with input files (pairs of demand_input and tech_params)
            Console.WriteLine();
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine(@"****************************** E N E R G Y H U B S **********************************");
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine(@"************************************* F O R *****************************************");
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine(@"********************** M U L T I P L E    B U I L D I N G S *************************");
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine(@"Please enter the path of your inputs folder in the following format: 'c:\inputs\'");
            Console.WriteLine("The folder should contain one or more pairs of csv-files in the naming convention: 'building_input_0.csv', 'technology_input_0.csv'. '0' is the integer that makes the program recognize that these two csv belong together.");
            Console.WriteLine();
            Console.Write("Enter your path now and confirm by hitting the Enter-key: ");
            string path = Console.ReadLine();
            if (!path.EndsWith(@"\"))
                path = path + @"\";


            // identify all valid input files in folder
            int filePairs = 0;
            string[] fileEntries = Directory.GetFiles(path);
            string[] fileNames = new string[fileEntries.Length];
            for (int i = 0; i < fileEntries.Length; i++)
                fileNames[i] = Path.GetFileName(fileEntries[i]);

            List<string> inputBuildingFiles = new List<string>();
            List<string> inputTechnologyFiles = new List<string>();
            List<int> inputIndices = new List<int>();
            foreach (string fileEntry in fileEntries)
            {
                string fileName = Path.GetFileName(fileEntry);
                string[] splitFileName = fileName.Split('_');
                string fileNameWithoutIndex = splitFileName[0] + '_' + splitFileName[1];
                if (string.Equals(fileNameWithoutIndex, buildingInputName))
                {
                    //find its partner. just make string search
                    string partnerFile = technologyInputName + "_" + splitFileName[splitFileName.Length - 1];
                    if (fileNames.Contains(partnerFile))
                        filePairs++;

                    inputBuildingFiles.Add(fileName);
                    inputTechnologyFiles.Add(partnerFile);
                    inputIndices.Add(Convert.ToInt32(splitFileName[splitFileName.Length - 1].Split('.')[0]));
                }
            }

            // ask, how many runs to do
            Console.WriteLine("Enter the number of Building-cases to generate Energyhubs for. There exist {0} file-pairs, hence you can select any integer number smaller equal of that:", filePairs);
            Int32.TryParse(Console.ReadLine(), out int nRuns);
            int nRunsActually = filePairs < nRuns ? filePairs : nRuns;    // if there are less file pairs in the folder than the number of runs, we actually want, only run the existing pairs in the folder

            for (int i = 0; i < nRunsActually; i++)
            {
                try
                {
                    Console.WriteLine(@"*************************************************************************************");
                    Console.WriteLine("Loading data...");
                    LoadBuildingInput(path + inputBuildingFiles[i],
                        out List<double> heating, out List<double> cooling, out List<double> electricity, out List<double> ghi, out List<double> dhw, out List<double> Tamb, out List<double[]> solar, out List<double> solarArea);
                    LoadTechParameters(path + inputTechnologyFiles[i], out Dictionary<string, double> technologyParameters);


                    /// data preparation, clustering and typical days
                    Console.WriteLine("Clustering and generating typical days...");
                    int numberOfSolarAreas = solar[0].Length;
                    int numBaseLoads = 5;                               // heating, cooling, electricity, ghi, tamb
                    int numLoads = numBaseLoads + numberOfSolarAreas;   // heating, cooling, electricity, ghi, tamb, solar. however, solar will include several profiles.
                    double[][] fullProfiles = new double[numLoads][];
                    string[] loadTypes = new string[numLoads];
                    bool[] peakDays = new bool[numLoads];
                    bool[] correctionLoad = new bool[numLoads];
                    for (int u = 0; u < numLoads; u++)
                        fullProfiles[u] = new double[hoursPerYear];
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

                    for (int u = 0; u < numberOfSolarAreas; u++)
                    {
                        useForClustering[u + numBaseLoads] = false;
                        peakDays[u + numBaseLoads] = false;
                        correctionLoad[u + numBaseLoads] = true;
                        loadTypes[u + numBaseLoads] = "solar";
                        for (int t = 0; t < hoursPerYear; t++)
                            fullProfiles[u + numBaseLoads][t] = solar[t][u];
                    }

                    // TO DO: load in GHI time series, add it to full profiles (right after heating, cooling, elec), and use it for clustering. exclude other solar profiles from clustering, but they need to be reshaped too
                    EhubMisc.HorizonReduction.TypicalDays typicalDays = EhubMisc.HorizonReduction.GenerateTypicalDays(fullProfiles, loadTypes, numberOfTypicalDays, peakDays, useForClustering, correctionLoad, false);


                    /// Running Energy Hub
                    Console.WriteLine("Solving MILP optimization model...");
                    double[][] typicalSolarLoads = new double[numberOfSolarAreas][];
                    for (int u = 0; u < numberOfSolarAreas; u++)
                        typicalSolarLoads[u] = typicalDays.DayProfiles[numBaseLoads + u];
                    int[] clustersizePerTimestep = typicalDays.NumberOfDaysPerTimestep;
                    Ehub ehub = new Ehub(typicalDays.DayProfiles[0], typicalDays.DayProfiles[1], typicalDays.DayProfiles[2],
                        typicalSolarLoads, solarArea.ToArray(),
                        typicalDays.DayProfiles[4], technologyParameters,
                        clustersizePerTimestep);
                    ehub.Solve(5);

                    /// Storing Results
                    Console.WriteLine("Saving results into {0}...", path + @"results\");
                    WriteOutput(inputIndices[i], path, numberOfSolarAreas, ehub, typicalDays);

                    Console.WriteLine("Energyhubs {0} of {1} completed...", i + 1, nRunsActually);
                    Console.WriteLine(@"......");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    WriteErrorFile(inputIndices[i], path, ex);
                }
            }



            /// Waiting for user to close window
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine(@"*************************************************************************************");
            Console.WriteLine("Program finished. Hit any key to close program...: ");
        }




        static void LoadBuildingInput(string inputFile,
            out List<double> heating, out List<double> cooling, out List<double> electricity, out List<double> ghi, out List<double> dhw, out List<double> Tamb, out List<double[]> solar, out List<double> solarArea)
        {
            heating = new List<double>();
            cooling = new List<double>();
            electricity = new List<double>();
            ghi = new List<double>();
            dhw = new List<double>();
            Tamb = new List<double>();
            solar = new List<double[]>();
            solarArea = new List<double>();


            // Headers: [0] coolingLoads; [1] dhwLoads; [2] heatingLoads; [3] electricityLoads; [4] ghi; [5] Tamb; [6] solarAreas; [7] solarLoads_0; [8] solarLoads_1; etc.
            string[] lines = File.ReadAllLines(inputFile);
            int numberOfSolarAreas = lines[0].Split(new char[2] { ',', ';' }).Length - 7;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] line = lines[i].Split(new char[2] { ',', ';' });
                cooling.Add(Convert.ToDouble(line[0]));
                dhw.Add(Convert.ToDouble(line[1]));
                heating.Add(Convert.ToDouble(line[2]));
                electricity.Add(Convert.ToDouble(line[3]));
                ghi.Add(Convert.ToDouble(line[4]));
                Tamb.Add(Convert.ToDouble(line[5]));
                if (i - 1 < numberOfSolarAreas)
                    solarArea.Add(Convert.ToDouble(line[6]));
                solar.Add(new double[numberOfSolarAreas]);
                for (int u = 0; u < numberOfSolarAreas; u++)
                    solar[i - 1][u] = Convert.ToDouble(line[u + 7]);
            }
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
                if (split.Length != 3)
                {
                    //WriteError();
                    Console.WriteLine("Reading line {0}:..... '{1}'", li + 1, lines[li]);
                    Console.Write("'{0}' contains {1} cells in line {2}, but it should contain 3 - the first two being strings and the third a number... Hit any key to abort the program: ",
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


        static void WriteOutput(int fileIndex, string path, int numberOfSolarAreas, Ehub ehub, EhubMisc.HorizonReduction.TypicalDays typicalDays)
        {
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
            header.Add("DistrictHeatingCost");
            header_units.Add("CHF/a");
            header.Add("x_Battery");
            header_units.Add("kWh");
            header.Add("x_TES");
            header_units.Add("kWh");
            header.Add("x_CHP");
            header_units.Add("kW");
            header.Add("x_Boiler");
            header_units.Add("kW");
            header.Add("x_BiomassBoiler");
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
            header.Add("x_BiomassBoiler_op");
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
            for (int i = 0; i < ehub.NumberOfBuildingsInDistrict; i++)
            {
                header.Add("x_HeatExchanger_DH_" + i);
                header_units.Add("kW");
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
            header.Add("ClusterSize");
            header_units.Add("Days");
            header.Add("BiomassConsumed");
            header_units.Add("kWh");

            for (int e = 0; e < ehub.Outputs.Length; e++)
            {
                List<List<string>> outputString = new List<List<string>>();
                if (ehub.Outputs[e].infeasible)
                {
                    outputString.Add(new List<string> { "Infeasible" });
                    Console.WriteLine("--- Infeasible Solution ---");
                }
                else
                {
                    outputString.Add(header);
                    outputString.Add(header_units);

                    List<string> firstLine = new List<string>();
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].carbon));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].cost));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].OPEX));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].CAPEX));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].cost_dh));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_boi));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bmboi));
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
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bmboi_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_hp_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_ac_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_elecpur[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_feedin[0]));
                    for (int i = 0; i < numberOfSolarAreas; i++)
                        firstLine.Add(Convert.ToString(ehub.Outputs[e].x_pv[i]));
                    for (int i = 0; i < ehub.NumberOfBuildingsInDistrict; i++)
                        firstLine.Add(Convert.ToString(ehub.Outputs[e].x_hx_dh[i]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].b_pvprod[0]));

                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[0][0]));
                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[1][0]));
                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[2][0]));
                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[3][0]));
                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[4][0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].clustersize[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].biomassConsumed));

                    outputString.Add(firstLine);

                    for (int t = 1; t < ehub.Outputs[e].x_elecpur.Length; t++)
                    {
                        List<string> newLine = new List<string>();
                        for (int skip = 0; skip < 12; skip++)
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
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bmboi_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_hp_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_ac_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_elecpur[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_feedin[t]));
                        for (int skip = 0; skip < numberOfSolarAreas; skip++)
                            newLine.Add("");
                        for (int skip = 0; skip < ehub.NumberOfBuildingsInDistrict; skip++)
                            newLine.Add("");
                        newLine.Add(Convert.ToString(ehub.Outputs[e].b_pvprod[t]));

                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[0][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[1][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[2][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[3][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[4][t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].clustersize[t]));
                        newLine.Add("");

                        outputString.Add(newLine);
                    }

                }
                using var sw = new StreamWriter(outputPath + "result_" + Convert.ToString(fileIndex) + "_epsilon" + e + ".csv");
                foreach (List<string> line in outputString)
                {
                    foreach (string cell in line)
                        sw.Write(cell + ";");
                    sw.Write(Environment.NewLine);
                }
                sw.Close();

            }
        }

        static void WriteErrorFile(int fileIndex, string path, Exception ex)
        {
            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            using var sw = new StreamWriter(outputPath + "result_" + Convert.ToString(fileIndex) + "_ERROR.csv");

            sw.Write(ex);

            sw.Close();
        }


        /// <summary>
        /// draws a ASCII art when error occurs
        /// </summary>
        static void WriteError()
        {
            string[] dog = EhubMisc.Misc.AsciiDrawing("dog");

            foreach (string d in dog)
                Console.WriteLine(d);

            Console.WriteLine();
            Console.WriteLine("                      Debug-Puppy found an error...");
            Console.WriteLine();
            //Console.WriteLine(errorMessage);
            //Console.WriteLine("Hit any key to abort program...");
            //Console.ReadKey();
        }




        static void LoadEhubResultsAndComputeSolarAutonomyAndPvSurfaces()
        {
            // get all folders:
            string path = @"\\nas22\arch_ita_schlueter\03-Research\01-Projects\29_FCLGlobal\04_Numerical\Buildings_MES_Interactions\energyhub_results";

            // Get all subdirectories
            string[] directories = Directory.GetDirectories(path);


            

            Console.WriteLine("Directories in " + path + ":");
            foreach (string dir in directories)
            {
                var sampleOutputLines = new List<string>();
                //var sampleNames = new List<string>();
                //var sampleSolarSelfSufficiency = new List<double>();
                //var sampleSolarFraction = new List<double>();
                //var sampleTotalPv = new List<double>();
                string oneLine = "File Name;"+ "Solar Fraction;" + "Solar Self-Sufficiency;" + "PV Area";
                sampleOutputLines.Add(oneLine);

                Console.WriteLine(dir);

                string[] files = Directory.GetFiles(dir);

                //Console.WriteLine("Files in " + dir + ":");
                // For each sample in this folder
                foreach (string file in files)
                {
                    //Console.WriteLine(file);
                    string filePath = file;

                    // List to hold each column as a separate list
                    List<List<string>> columns = new List<List<string>>();

                    // Read all lines from the file
                    string[] lines = File.ReadAllLines(filePath);

                    // Process each line
                    foreach (string line in lines)
                    {
                        string[] row = line.Split(new char[] { ',', ';' }, StringSplitOptions.None);

                        // Process each column in the row
                        for (int i = 0; i < row.Length; i++)
                        {
                            // If the cell is empty, skip adding it
                            if (string.IsNullOrEmpty(row[i]))
                            {
                                continue;
                            }

                            // If this is a new column, add a new list to columns
                            if (i >= columns.Count)
                            {
                                columns.Add(new List<string>());
                            }

                            // Add the item to the appropriate column
                            columns[i].Add(row[i]);
                        }
                    }

                    // Convert each List<string> in columns to a string array
                    string[][] columnArrays = new string[columns.Count][];
                    for (int i = 0; i < columns.Count; i++)
                    {
                        columnArrays[i] = columns[i].ToArray();
                    }

                    string pvString = "x_PV_";
                    var targetStrings = new string[5] { "ClusterSize", "TypicalElectricity", "x_GridPurchase", "b_PV_totalProduction", "x_FeedIn" };
                    double[] clusterSize = null;
                    double[] elecDemand = null;
                    double[] gridPurchase = null;
                    double[] pvProduction = null;
                    double[] feedIn = null;
                    double pvAreaThisSample = 0.0;
                    foreach (var targetString in targetStrings)
                    {
                        string[] targetArray = null;
                        foreach (var array in columnArrays)
                        {
                            if (array.Length > 0 && array[0].Equals(targetString, StringComparison.OrdinalIgnoreCase))
                            {
                                targetArray = array;
                                switch (targetString)
                                {
                                    case "ClusterSize":
                                        clusterSize = targetArray.Skip(2)
                                              .Select(s => double.Parse(s))
                                              .ToArray();
                                        break;
                                    case "TypicalElectricity":
                                        elecDemand = targetArray.Skip(2)
                                              .Select(s => double.Parse(s))
                                              .ToArray();
                                        break;
                                    case "x_GridPurchase":
                                        gridPurchase = targetArray.Skip(2)
                                              .Select(s => double.Parse(s))
                                              .ToArray();
                                        break;
                                    case "b_PV_totalProduction":
                                        pvProduction = targetArray.Skip(2)
                                              .Select(s => double.Parse(s))
                                              .ToArray();
                                        break;
                                    case "x_FeedIn":
                                        feedIn = targetArray.Skip(2)
                                              .Select(s => double.Parse(s))
                                              .ToArray();
                                        break;
                                }
                                break;
                            }
                        }
                    }
                    foreach (var array in columnArrays)
                    {
                        string[] targetArray = null;
                        if (array.Length > 0 && array[0].IndexOf(pvString) != -1)
                        {
                            targetArray = array;
                            pvAreaThisSample += Convert.ToDouble(targetArray[2]);
                        }
                    }

                    double[] solarAutonomy = EhubMisc.Misc.CalcSolarAutonomy(clusterSize, elecDemand, gridPurchase, pvProduction, feedIn);
                    var fileName = file.Split('\\');
                    //sampleNames.Add(fileName[fileName.Length - 1]);
                    //sampleSolarFraction.Add(solarAutonomy[0]);
                    //sampleSolarSelfSufficiency.Add(solarAutonomy[1]);
                    //sampleTotalPv.Add(pvAreaThisSample);
                    oneLine = fileName[fileName.Length - 1] + ";" + solarAutonomy[0].ToString() + ";" + solarAutonomy[1].ToString() + ";" + pvAreaThisSample.ToString();
                    sampleOutputLines.Add(oneLine);
                }
                Console.WriteLine("folder {0} done", dir);

                string outputPath = dir + "\\NewMetrics.csv";
                File.WriteAllLines(outputPath, sampleOutputLines);
            }

            Console.WriteLine("all done, save this now");
            Console.ReadKey();


            // foreach sample 1-10000



            // foreach epsilon


            //string clusterSizeFile = @"C:\temp\buildingInteractions\clusterSize.txt";
            //    string typicalElecFile = @"C:\temp\buildingInteractions\elec.txt";
            //    string typicalGridFile = @"C:\temp\buildingInteractions\gridPurchase.txt";
            //    string typicalPvFile = @"C:\temp\buildingInteractions\pvProduction.txt";
            //    string typicalFeedInFile = @"C:\temp\buildingInteractions\feedIn.txt";

            //    EhubMisc.Misc.LoadTimeSeries(clusterSizeFile, out List<double> clusterSize);
            //    EhubMisc.Misc.LoadTimeSeries(typicalElecFile, out List<double> elecDemand);
            //    EhubMisc.Misc.LoadTimeSeries(typicalGridFile, out List<double> gridPurchase);
            //    EhubMisc.Misc.LoadTimeSeries(typicalPvFile, out List<double> pvProduction);
            //    EhubMisc.Misc.LoadTimeSeries(typicalFeedInFile, out List<double> feedIn);

            //double[] solarAutonomy = EhubMisc.Misc.CalcSolarAutonomy(clusterSize, elecDemand, gridPurchase, pvProduction, feedIn);

            //Console.WriteLine("solar fraction: {0}, solar self-sufficiency: {1}", solarAutonomy[0], solarAutonomy[1]);
            //Console.ReadKey();


        }




    }
}
