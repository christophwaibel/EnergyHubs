using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
            int minusIndex = 0; //put a number >0 to reduce number of sensor points... put in 0 for whole dataset
            int epsilonCuts = 3;
            int periodInterval = 10;

            // assuming fully electrified thermals
            const double copHtg = 3.0;
            const double copClg = 3.0;
            const double copDhw = 3.0;



            Console.WriteLine("___________________________________________________________________ \n ");
            Console.WriteLine("SBE22 THESSALONIKI Multi-Period PV-Hub \n");
            Console.WriteLine("___________________________________________________________________ \n ");
            Console.WriteLine("Select mode: \n [1] = 10 year periods \n [2] = 5 year periods");
            string periodConsole = Console.ReadLine();

            try
            {
                var periodConsoleInt = Convert.ToInt16(periodConsole);
                if (periodConsoleInt != 1 && periodConsoleInt != 2)
                {
                    Console.WriteLine("WARNING --- Enter either '1' or '2'! Hit any key to exit program");
                    Console.ReadKey();
                    return;
                }

                periodInterval = periodConsoleInt == 1 ? 10 : 5;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            int[] periods = new int[(2050 - 2020) / periodInterval];
            for (int p = 0; p < periods.Length; p++)
                periods[p] = 2020 + p * periodInterval;
            Console.WriteLine("\nOK! Mode [{0}] selected... Running the model for stages...", periodConsole);
            foreach (int period in periods)
                Console.Write("{0}  ", period);



            Console.WriteLine("\n\n\n___________________________________________________________________ \n ");
            Console.WriteLine("Select epsilon cuts. '3' is a good value (you'll get 5 solutions). min '1', and max about '8' \n");
            string epsilonConsole = Console.ReadLine();
            try
            {
                var epsilonConsoleInt = Convert.ToInt16(epsilonConsole);
                if (epsilonConsoleInt < 1)
                {
                    Console.WriteLine("WARNING --- Epsilon cuts must be >= 1! Hit any key to exit program");
                    Console.ReadKey();
                    return;
                }
                Console.WriteLine("\nOK! Epsilon [{0}] selected... Creating in total 2+{0} solutions (carbon min, cost min, and {0} cuts)...\n", epsilonConsole);
                epsilonCuts = epsilonConsoleInt;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            Console.WriteLine("\n___________________________________________________________________ \n ");
            Console.WriteLine("Reduce length of considered PV surfaces by 'x' (for debugging). \n " +
                              "I.e., if your solar file has 15000 surfaces & profiles, enter 14000 to only use 1000 surfaces. \n" +
                              "Enter '0' to consider whole dataset");
            string minusIndexConsole = Console.ReadLine();
            try
            {
                minusIndex = Convert.ToInt16(minusIndexConsole);
                if(minusIndex < 0)
                {
                    Console.WriteLine("WARNING --- Value must be >= 0! Hit any key to exit program");
                    Console.ReadKey();
                    return;
                }
                if(minusIndex == 0)
                    Console.WriteLine("\nOK! [{0}] entered. Running with the full dataset...\n", minusIndexConsole);
                else
                    Console.WriteLine("\nOK! [{0}] entered. Reducing the solar dataset by {0} entries...\n", minusIndexConsole);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            Console.WriteLine("\n___________________________________________________________________ \n ");
            Console.WriteLine("Run with [0] or without [1] batteries?\n");
            string batteryConsole = Console.ReadLine();
            var batteryConsoleInt = Convert.ToInt16(batteryConsole);
            try
            {
                if (batteryConsoleInt != 0 && batteryConsoleInt != 1)
                {
                    Console.WriteLine("WARNING --- Enter either [0] or [1]! Hit any key to exit program");
                    Console.ReadKey();
                    return;
                }
                if (batteryConsoleInt == 0)
                    Console.WriteLine("\nOK! [{0}] entered. Running model WITH batteries...\n", batteryConsole);
                else
                    Console.WriteLine("\nOK! [{0}] entered. Running model WITHOUT batteries...\n", batteryConsole);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }





            Console.WriteLine("\n___________________________________________________________________ \n");

            Console.WriteLine(@"Specify file path, e.g.: 'c:\sbe22\'");
            Console.WriteLine("Or just hit ENTER and it will use the current file directory \n");
            Console.WriteLine(@"That folder needs to contain a subfolder called '\inputs'");
            string path = Console.ReadLine();
            if (path.Length == 0)
                path = System.AppDomain.CurrentDomain.BaseDirectory;
            if (!path.EndsWith(@"\"))
                path = path + @"\";

            Console.WriteLine("using path: {0}", path);

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

                Misc.LoadTimeSeries(pathInputs + "ZH_" + period + "_BAU_GHI.csv", out var ghi);
                Misc.LoadTimeSeries(pathInputs + "ZH_" + period + "_BAU_DryBulb.csv", out var dryBulb);
                ghiAllPeriods.Add(ghi);
                dryBulbAllPeriods.Add(dryBulb);

                LoadTechParameters(pathInputs + "ZH_" + period + "_BAU_technology.csv", out var technologyParameters);
                if (batteryConsoleInt == 1)
                    technologyParameters["b_MaxBattery"] = 0.0;
                techParamsAllPeriods.Add(technologyParameters);
            }

            //load surface areas - the same for all periods
            LoadSurfaceAreasInput(pathInputs + "ZH_SurfaceAreas.csv", out var solarAreas, minusIndex);
            // load sensor points - the same for all periods in this model
            LoadSolarPotentialsInput(pathInputs + "ZH_solar_SP0.csv", out var irradiance, minusIndex);

            Console.WriteLine("___________________________________________________________________ \n ");
            Console.WriteLine("All Inputs Loaded...");



            /// data preparation, clustering and typical days
            int numberOfSolarAreas = solarAreas.Count;
            int numBaseLoads = 3;                               // electricity, ghi, tamb
            int numLoads = numBaseLoads + numberOfSolarAreas;   // electricity, ghi, tamb, solar. however, solar will include several profiles.
            const int hoursPerYear = 8760;
            string[] loadTypes = new string[numLoads];
            bool[] peakDays = new bool[numLoads];
            bool[] correctionLoad = new bool[numLoads];

            loadTypes[0] = "electricity";
            loadTypes[1] = "ghi";
            loadTypes[2] = "Tamb";

            peakDays[0] = true;
            peakDays[1] = true;
            peakDays[2] = true;

            // scaling by cluster size (how many days belong to each typical day. important so the sum over the year remains the same)
            correctionLoad[0] = true;
            correctionLoad[1] = false;
            correctionLoad[2] = false;

            // specificy here, which load is used for clustering. the others are just reshaped
            bool[] useForClustering = new bool[numLoads];
            for (int t = 0; t < hoursPerYear; t++)
            {
                useForClustering[0] = true;
                useForClustering[1] = true;
                useForClustering[2] = true;
            }

            for (int u = 0; u < numberOfSolarAreas; u++)
            {
                useForClustering[u + numBaseLoads] = false;
                peakDays[u + numBaseLoads] = false;
                correctionLoad[u + numBaseLoads] = true;
                loadTypes[u + numBaseLoads] = "solar";
            }


            // looping through all periods and create typical days for each
            var allTypicalElecDays = new List<double[]>();
            var allTypicalAmbientTemp = new List<double[]>();
            var allTypicalGhi = new List<double[]>();
            var allTypicalSolarLoads = new List<double[][]>();
            var allClusterSizes = new List<int[]>();
            for (int p = 0; p < periods.Length; p++)
            {
                // Clustering typical days for each period
                Console.WriteLine("Creating Typical Days Profiles for period {0}...", periods[p]);
                double[][] fullProfiles = new double[numLoads][];
                for (int u = 0; u < numLoads; u++)
                    fullProfiles[u] = new double[hoursPerYear];
                for (int t = 0; t < hoursPerYear; t++)
                {
                    // Adding thermal demands to electricity demand, assuming fully electrified system
                    fullProfiles[0][t] = elecAllPeriods[p][t] +
                                         htgAllPeriods[p][t] / copHtg +
                                         dhwAllPeriods[p][t] / copDhw +
                                         clgAllPeriods[p][t] / copClg;
                    fullProfiles[1][t] = ghiAllPeriods[p][t];
                    fullProfiles[2][t] = dryBulbAllPeriods[p][t];
                    for (int u = 0; u < numberOfSolarAreas; u++)
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
                // same for elec demand and GHI, round very small numbers to 0.0
                // NOT for ambient temp tho [i=2], because temp can have - °C
                for (int t = 0; t < typicalDays.DayProfiles[0].Length; t++)
                    for (int i = 0; i < 2; i++)
                        if (typicalDays.DayProfiles[i][t] < 0.001) typicalDays.DayProfiles[i][t] = 0.0;


                allTypicalSolarLoads.Add(typicalSolarLoads);
                allTypicalElecDays.Add(typicalDays.DayProfiles[0]);
                allTypicalGhi.Add(typicalDays.DayProfiles[1]);
                allTypicalAmbientTemp.Add(typicalDays.DayProfiles[2]);
            }


            // Solve Multi-Period Ehub
            Console.WriteLine("___________________________________________________________________ \n ");
            Console.WriteLine("Solving MILP optimization model...");
            MultiPeriodEHub ehub = new MultiPeriodEHub(allTypicalElecDays,
                allTypicalSolarLoads, solarAreas.ToArray(), allTypicalAmbientTemp,
                techParamsAllPeriods, allClusterSizes, periodInterval);
            ehub.Solve(epsilonCuts, true);


            WriteTypicalLoads(path, allTypicalElecDays, allTypicalGhi, allTypicalAmbientTemp, allTypicalSolarLoads);
            for (int i = 0; i < ehub.Outputs.Length; i++)
            {
                if (!ehub.Outputs[i].infeasible)
                {
                    WriteCapacities(path, ehub.Outputs[i], i);
                    WriteObjectiveValues(path, ehub.Outputs[i], i);
                    WriteOperation(path, ehub.Outputs[i], i);
                }
            }

            Console.WriteLine("Done. hit any key to close...");
            Console.ReadKey();
        }




        // for each epsilon
        static void WriteObjectiveValues(string path, MultiPeriodEhubOutput ehubResults, int epsilonCut)
        {
            double kgToTonnes = 0.001;
            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            var header = new List<string>();
            var header_units = new List<string>();

            // objectives
            header.Add("Epsilon");
            header_units.Add("integer");
            header.Add("Total Cost");
            header_units.Add("CHF");
            header.Add("CAPEX");
            header_units.Add("CHF");
            header.Add("OPEX");
            header_units.Add("CHF");
            header.Add("Emissions");
            header_units.Add("t CO2");

            var outputString = new List<List<string>>();
            outputString.Add(header);
            outputString.Add(header_units);

            var newLine = new List<string>();
            newLine.Add(Convert.ToString(epsilonCut));
            newLine.Add(Convert.ToString(ehubResults.Cost));
            newLine.Add(Convert.ToString(ehubResults.Capex));
            newLine.Add(Convert.ToString(ehubResults.Opex));
            newLine.Add(Convert.ToString(ehubResults.Carbon * kgToTonnes));
            outputString.Add(newLine);

            Misc.WriteTextFile(outputPath, "ObjectiveValues_epsilon_" + Convert.ToString(epsilonCut) + ".csv", outputString);
        }

        static void WriteCapacities(string path, MultiPeriodEhubOutput ehubResults, int epsilonCut)
        {
            int numberOfSolarAreas = ehubResults.XTotalPvMono[0].Length;    // number of solar profiles in period 0. should be the same in all periods.
            int NumPeriods = ehubResults.XTotalPvMono.Length;                // number of periods

            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);



            var newLine = new List<string>();
            newLine.Add("epsilon="+Convert.ToString(epsilonCut));
            newLine.Add("Technology");
            newLine.Add("Unit");
            for (int p = 0; p < NumPeriods; p++)
                newLine.Add("Capacity at p=" + Convert.ToString(p));

            //this one goes out
            var outputString = new List<List<string>>();
            outputString.Add(newLine);

            newLine = new List<string>();
            newLine.Add("");
            newLine.Add("Battery");
            newLine.Add("kWh");
            for (int p = 0; p < NumPeriods; p++)
                newLine.Add(Convert.ToString(ehubResults.XNewBattery[p]));
            outputString.Add(newLine);


            for (int i = 0; i < numberOfSolarAreas; i++)
            {
                var newLineMono = new List<string>();
                var newLineCdte = new List<string>();
                newLineMono.Add("");
                newLineMono.Add("Total PV Mono" + Convert.ToString(i));
                newLineMono.Add("m^2");
                newLineCdte.Add("");
                newLineCdte.Add("Total PV Cdte" + Convert.ToString(i));
                newLineCdte.Add("m^2");
                for (int p = 0; p < NumPeriods; p++)
                {
                    newLineMono.Add(Convert.ToString(ehubResults.XTotalPvMono[p][i]));
                    newLineCdte.Add(Convert.ToString(ehubResults.XTotalPvCdte[p][i]));
                }
                outputString.Add(newLineMono);
                outputString.Add(newLineCdte);
            }
        
            Misc.WriteTextFile(outputPath, "Capacities_epsilon_" + Convert.ToString(epsilonCut) + ".csv", outputString);
        }

        // for each epsilon
        static void WriteOperation(string path, MultiPeriodEhubOutput ehubResults, int epsilonCut)
        {
            int numPeriods = ehubResults.XOperationElecPurchase.Length;     // number of periods
            int horizon = ehubResults.XOperationElecPurchase[0].Length;     // horizon per period

            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            var header = new List<string>();
            var header_units = new List<string>();

            header.Add("Period");
            header_units.Add("-");
            header.Add("Clustersize");
            header_units.Add("(days)");
            header.Add("Grid Purchase");
            header_units.Add("(kWh)");
            header.Add("Feed In");
            header_units.Add("(kWh)");
            header.Add("Battery State-of-Charge");
            header_units.Add("(kWh)");
            header.Add("Battery Charging");
            header_units.Add("(kWh)");
            header.Add("Battery Discharging");
            header_units.Add("(kWh)");
            header.Add("Total PV generation");
            header_units.Add("(kWh)");


            var outputString = new List<List<string>>();
            outputString.Add(header);
            outputString.Add(header_units);

            for (int p = 0; p < numPeriods; p++)
            {
                for (int t = 0; t < horizon; t++)
                {
                    var newLine = new List<string>();
                    newLine.Add(Convert.ToString(p));
                    newLine.Add(Convert.ToString(ehubResults.Clustersize[p][t]));
                    newLine.Add(Convert.ToString(ehubResults.XOperationElecPurchase[p][t]));
                    newLine.Add(Convert.ToString(ehubResults.XOperationFeedIn[p][t]));
                    newLine.Add(Convert.ToString(ehubResults.XOperationBatterySoc[p][t]));
                    newLine.Add(Convert.ToString(ehubResults.XOperationBatteryCharge[p][t]));
                    newLine.Add(Convert.ToString(ehubResults.XOperationBatteryDischarge[p][t]));
                    newLine.Add(Convert.ToString(ehubResults.XOperationPvElectricity[p][t]));
                    outputString.Add(newLine);
                }
            }


            Misc.WriteTextFile(outputPath, "Operation_epsilon_" + Convert.ToString(epsilonCut) + ".csv", outputString);
        }

        // only once
        static void WriteTypicalLoads(string path,
            List<double[]> allTypicalElecDays, List<double[]> allTypicalGhi, List<double[]> allTypicalAmbientTemp,
            List<double[][]> allTypicalSolarLoads)
        {
            int numberOfSolarAreas = allTypicalSolarLoads[0].Length;    // number of solar profiles in period 0. should be the same in all periods.
            int NumPeriods = allTypicalSolarLoads.Count;                // number of periods
            int horizon = allTypicalSolarLoads[0][0].Length;            // number of hours per typical year. should be the same as allTypicalElecDays[0].Length

            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            var header = new List<string>();
            // write units to 2nd row
            var header_units = new List<string>();

            header.Add("Period");
            header_units.Add("(Year)");

            header.Add("Typical Electricity Loads");
            header_units.Add("(kWh)");

            header.Add("Typical Global Horizontal Irradiation");
            header_units.Add("(kWh/m^2)");

            header.Add("Typical Ambient Temperature");
            header_units.Add("(deg C)");

            for (int i = 0; i < numberOfSolarAreas; i++)
            {
                header.Add("Typical Solar Potentials " + i);
                header_units.Add("W/m^2");
            }

            var outputString = new List<List<string>>();
            outputString.Add(header);
            outputString.Add(header_units);

            for (int p = 0; p < NumPeriods; p++)
            {
                for (int t = 0; t < horizon; t++)
                {
                    var newLine = new List<string>();
                    newLine.Add(Convert.ToString(p));
                    newLine.Add(Convert.ToString(allTypicalElecDays[p][t]));
                    newLine.Add(Convert.ToString(allTypicalGhi[p][t]));
                    newLine.Add(Convert.ToString(allTypicalAmbientTemp[p][t]));
                    for (int i = 0; i < numberOfSolarAreas; i++)
                        newLine.Add(Convert.ToString(allTypicalSolarLoads[p][i][t]));
                    outputString.Add(newLine);
                }
            }

            Misc.WriteTextFile(outputPath, "TypicalLoads.csv", outputString);
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
        static void LoadSurfaceAreasInput(string inputFile, out List<double> solarArea, int minusIndex)
        {
            solarArea = new List<double>();

            string[] solarAreaLines = File.ReadAllLines(inputFile);
            for (int i = 1; i < solarAreaLines.Length - minusIndex; i++)
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
            firstLine = firstLine.Where(x => !string.IsNullOrEmpty(x)).ToArray();
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
