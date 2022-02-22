using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace CISBAT21
{
    class Program
    {

        /// <summary>
        /// Main program, use console to switch between modes
        /// 0 = run CISBAT2021 energy hub. You will further be asked, which scenario to run: Singapore, Zurich, current climate or future climate. Results are written into a results folder
        /// 1 = (postprocessing) write annual solar potentials of 4 sensor points for each of the original 193 surfaces into a csv file. That results in 772 actual PV surfaces (193 surfaces are basically split into 4 each). Each column of the csv has: 1st row: SP ID; 2nd row: kWh/m2a ; 3rd row: surface area in m2
        /// 2 = run the same CISBAT21 energy hub, but multiple times, using stochastich solar profiles. Building loads and other inputs remain deterministic. Used for SBE22 Yufei Zhang's paper
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            int scenario; // 0 = run ehub console app, 1 = solar potentials, 2 = run ehub with deterministic load inputs but stochastich solar profiles
            if (args != null && args.Length > 0)
            {
                if (!Int32.TryParse(args[0], out scenario)) scenario = 0; //run cisbat21 ehub by default
            }
            else
            {
                Console.WriteLine("Run ehub (0), write annual solar potentials (1), or run ehub with stochastic solar profiles (2)");
                string eHubOrSolar = Console.ReadLine();
                if (!Int32.TryParse(eHubOrSolar, out scenario)) scenario = 0;
            }

            try
            {
                if (scenario == 0) ehubRun();
                else if (scenario == 1) RunWriteAnnualSolarPotentials();
                else if (scenario == 2) ehubRunStochasticSolar();
            }
            catch (Exception e)
            {
                string[] cat = EhubMisc.Misc.AsciiDrawing("cat");

                foreach (string c in cat)
                    Console.WriteLine(c);

                Console.WriteLine(e);
            }
            Console.ReadKey();
        }


        static void ehubRunStochasticSolar()
        {
            int epsilonCuts = 3;

            Console.WriteLine("___________________________________________________________________ \n ");
            Console.WriteLine("SBE22 EnergyHub for Yufei Zhang, with stochastic solar profiles. \nCode based on CISBAT 21 paper (Waibel, Hsieh, Schlüter)");
            Console.WriteLine("___________________________________________________________________ \n");

            // get current directory. There should be sub-folders containing input data, deterministic and stochastic
            Console.WriteLine(@"Please enter the path of your inputs folder in the following format: 'c:\inputs\'");
            Console.WriteLine("Or just hit ENTER and it will use the current file directory \n");
            string path = Console.ReadLine();
            if (path.Length == 0)
                path = System.AppDomain.CurrentDomain.BaseDirectory;
            if (!path.EndsWith(@"\"))
                path = path + @"\";

            string pathStochastic = path + @"input_stochastic\";
            string pathDeterministic = path + @"input_deterministic\";

            if (!Directory.Exists(pathStochastic) || !Directory.Exists(pathDeterministic))
            {
                Console.WriteLine("*********************\nWARNING: {0} and / or {1} folder(s) missing!!! Hit any key to quit program", pathStochastic, pathDeterministic);
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Cheers, using path: {0}\n", path);
            Console.WriteLine("Make sure your subfolders '\\input_deterministic'  and '\\input_stochastic' contain all necessary input files...");
            Console.WriteLine("'\\input_deterministic' contains all building loads, technology parameters and available surface area per sensor point. You'll get those files from me. Hardcoded to 'Risch_2020'.");
            Console.WriteLine("'\\input_stochastic' will need to contain all the stochastic solar potentials in a specific naming convention. I'll provide you with some example files.");
            Console.WriteLine();
            Console.WriteLine("The program will run the energy hub for 5 epsilon cuts for each of the uncertain scenarios that it finds in the \"input_stochastic\" folder. \n");
            Console.WriteLine("___________________________________________________________________ \n");
            Console.WriteLine("\n Hit any key to start...");
            Console.ReadKey();

            // load deterministic inputs
            // load demand
            LoadBuildingInput(pathDeterministic + "Risch_2020_demand.csv",
                out var heating, out var dhw, out var cooling, out var electricity);

            // load peak loads per building, filter for 'Bxxx_Qh_40_kWh', 'Bxxx_Qh_60_kWh' and aggregate dhw and sh
            LoadPeakLoads(pathDeterministic + "Risch_2020_PeakLoads.csv",
                out var numBuildings, out var peakHeatingLoads, out var peakCoolingLoads);

            // load GHI
            LoadTimeSeries(pathDeterministic + "Risch_2020_GHI.csv", out var ghi);

            // load dry bulb
            LoadTimeSeries(pathDeterministic + "Risch_2020_DryBulb.csv", out var dryBulb);

            //Building Tech
            LoadTechParameters(pathDeterministic + "Risch_2020_technology.csv", out var technologyParameters);
            technologyParameters.Add("NumberOfBuildingsInEHub", Convert.ToDouble(numBuildings));
            for (int i = 0; i < numBuildings; i++)
            {
                technologyParameters.Add("Peak_Htg_" + Convert.ToString(i), peakHeatingLoads[i]);
                technologyParameters.Add("Peak_Clg_" + Convert.ToString(i), peakCoolingLoads[i]);
            }

            //load surface areas
            LoadSurfaceAreasInput(pathDeterministic + "SurfaceAreas.csv", out var solarAreas);





            /// data preparation, clustering and typical days
            int numberOfSolarAreas = solarAreas.Count;
            int numBaseLoads = 5;                               // heating, cooling, electricity, ghi, tamb
            int numLoads = numBaseLoads + numberOfSolarAreas;   // heating, cooling, electricity, ghi, tamb, solar. however, solar will include several profiles.
            const int hoursPerYear = 8760;
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
                fullProfiles[4][t] = dryBulb[t];
                useForClustering[0] = true;
                useForClustering[1] = true;
                useForClustering[2] = true;
                useForClustering[3] = true;
                useForClustering[4] = false;
            }









            // load stochastic inputs and run ehub for each
            string[] fileEntires = Directory.GetFiles(pathStochastic);
            int scenarios = fileEntires.Length;
            Console.WriteLine("Found {0} stochastic solar scenarios in {1}\n", scenarios, pathStochastic);
            for (int s=0; s<scenarios; s++) 
            {
                LoadSolarPotentialsInput(fileEntires[s], out var solarPotentials);
                for (int u = 0; u < numberOfSolarAreas; u++)
                {
                    useForClustering[u + numBaseLoads] = false;
                    peakDays[u + numBaseLoads] = false;
                    correctionLoad[u + numBaseLoads] = true;
                    loadTypes[u + numBaseLoads] = "solar";
                    for (int t = 0; t < hoursPerYear; t++)
                        fullProfiles[u + numBaseLoads][t] = solarPotentials[u][t];
                }

                // TO DO: load in GHI time series, add it to full profiles (right after heating, cooling, elec), and use it for clustering. exclude other solar profiles from clustering, but they need to be reshaped too
                EhubMisc.HorizonReduction.TypicalDays typicalDays = EhubMisc.HorizonReduction.GenerateTypicalDays(fullProfiles, loadTypes,
                    12, peakDays, useForClustering, correctionLoad, true);




                /// Running Energy Hub
                Console.WriteLine("Solving MILP optimization model...");
                double[][] typicalSolarLoads = new double[numberOfSolarAreas][];

                // solar profiles negative or very small numbers. rounding floating numbers thing?
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


                int[] clustersizePerTimestep = typicalDays.NumberOfDaysPerTimestep;
                Ehub ehub = new Ehub(typicalDays.DayProfiles[0], typicalDays.DayProfiles[1], typicalDays.DayProfiles[2],
                    typicalSolarLoads, solarAreas.ToArray(),
                    typicalDays.DayProfiles[4], technologyParameters,
                    clustersizePerTimestep);
                ehub.Solve(epsilonCuts, false);

                WriteOutput("result_scenario_"+s.ToString(), path, numberOfSolarAreas, ehub, typicalDays, numBaseLoads);
                Console.WriteLine("\nScenario {0} done", s);
            }





            Console.WriteLine("ALL SCENARIOS DONE. HIT ANY KEY TO EXIT");
            Console.ReadKey();
        }


        static void RunWriteAnnualSolarPotentials()
        {
            Console.WriteLine("--------Writing annual solar potentials-----------");
            Console.Write("Enter your path now and confirm by hitting the Enter-key: ");
            string path = Console.ReadLine();
            if (path.Length == 0)
                path = @"C:\Users\christoph\Documents\GitHub\EnergyHubs\CplexEnergyHubs\CISBAT21\data\";
            if (!path.EndsWith(@"\"))
                path = path + @"\";
            Console.WriteLine("Cheers, using path {0}", path);
            Console.WriteLine();
            Console.WriteLine("Which Case? 0) Risch 2020; 1) Risch 2050; 2) Singapore 2020; 3) Singapore 2050? Enter an integer 0-3:");
            string scenarioSelect = Console.ReadLine();
            int scenario;
            if (!Int32.TryParse(scenarioSelect, out scenario)) scenario = 0;
            var scenarioString = new string[4] { "Risch_2020", "Risch_2050_RCP8-5", "Singapore_2020", "Singapore_2050_RCP8-5" };
            Console.WriteLine("Cheers, using scenario {0}", scenarioString[scenario]);

            // read in solar data
            string fileName = path + scenarioString[scenario];
            LoadSolarPotentialsAndAreasInput(path + "SurfaceAreas.csv",
                new string[4] {fileName+ "_solar_SP0.csv", fileName + "_solar_SP1.csv",
                    fileName + "_solar_SP4.csv", fileName + "_solar_SP5.csv"},
                out var irradiance, out var solarAreas);

            WriteSolarProfiles(scenarioString[scenario],path, irradiance, solarAreas.ToArray());
            Console.WriteLine("Done. Hit any key to close");
        }


        static void ehubRun()
        {
            Console.WriteLine("___________________________________________________________________");
            Console.WriteLine("CISBAT 21 EnergyHubs with Demand Response to check BIPV feasibility");
            Console.WriteLine("___________________________________________________________________");
            Console.WriteLine("");
            Console.WriteLine(@"Please enter the path of your inputs folder in the following format: 'c:\inputs\'");
            Console.WriteLine("The folder should contain ...");
            Console.WriteLine();
            Console.Write("Enter your path now and confirm by hitting the Enter-key: ");
            string path = Console.ReadLine();
            if (path.Length == 0)
                path = @"C:\Users\christoph\Documents\GitHub\EnergyHubs\CplexEnergyHubs\CISBAT21\data\";
            if (!path.EndsWith(@"\"))
                path = path + @"\";
            Console.WriteLine("Cheers, using path {0}", path);
            Console.WriteLine();
            Console.WriteLine("Which Case? 0) Risch 2020; 1) Risch 2050; 2) Singapore 2020; 3) Singapore 2050? Enter an integer 0-3:");
            string scenarioSelect = Console.ReadLine();
            int scenario;
            if (!Int32.TryParse(scenarioSelect, out scenario)) scenario = 0;
            var scenarioString = new string[4] { "Risch_2020", "Risch_2050_RCP8-5", "Singapore_2020", "Singapore_2050_RCP8-5" };
            Console.WriteLine("Cheers, using scenario {0}", scenarioString[scenario]);

            // read in solar data
            // SurfaceAreas.csv containts 193 areas, but we have 4 SPs per area (_SP0, _SP1, _SP4, _SP5). Thus, we basically split each area into 4 sub-surfaces and assign one SP to each. As a result we have 772 PV surfaces
            string fileName = path + scenarioString[scenario];
            LoadSolarPotentialsAndAreasInput(path + "SurfaceAreas.csv",
                new string[4] {fileName+ "_solar_SP0.csv", fileName + "_solar_SP1.csv",
                    fileName + "_solar_SP4.csv", fileName + "_solar_SP5.csv"},
                out var irradiance, out var solarAreas);

            // load demand
            LoadBuildingInput(path + scenarioString[scenario] + "_demand.csv", 
                out var heating, out var dhw, out var cooling, out var electricity);

            // load peak loads per building, filter for 'Bxxx_Qh_40_kWh', 'Bxxx_Qh_60_kWh' and aggregate dhw and sh
            LoadPeakLoads(path + scenarioString[scenario] + "_PeakLoads.csv",
                out var numBuildings, out var peakHeatingLoads, out var peakCoolingLoads);

            // load GHI
            LoadTimeSeries(path + scenarioString[scenario] + "_GHI.csv", out var ghi);

            // load dry bulb
            LoadTimeSeries(path + scenarioString[scenario] + "_DryBulb.csv", out var dryBulb);

            //Building Tech
            LoadTechParameters(path + scenarioString[scenario] + "_technology.csv", out var technologyParameters);
            technologyParameters.Add("NumberOfBuildingsInEHub", Convert.ToDouble(numBuildings));
            for (int i = 0; i < numBuildings; i++)
            {
                technologyParameters.Add("Peak_Htg_" + Convert.ToString(i), peakHeatingLoads[i]);
                technologyParameters.Add("Peak_Clg_" + Convert.ToString(i), peakCoolingLoads[i]);
            }

            Console.WriteLine();
            Console.WriteLine("Loading Complete...");
            Console.WriteLine();

            /// data preparation, clustering and typical days
            Console.WriteLine("Clustering and generating typical days...");
            int numberOfSolarAreas = solarAreas.Count;
            int numBaseLoads = 5;                               // heating, cooling, electricity, ghi, tamb
            int numLoads = numBaseLoads + numberOfSolarAreas;   // heating, cooling, electricity, ghi, tamb, solar. however, solar will include several profiles.
            const int hoursPerYear = 8760;
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
                fullProfiles[4][t] = dryBulb[t];
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
                    fullProfiles[u + numBaseLoads][t] = irradiance[u][t];
            }

            // TO DO: load in GHI time series, add it to full profiles (right after heating, cooling, elec), and use it for clustering. exclude other solar profiles from clustering, but they need to be reshaped too
            EhubMisc.HorizonReduction.TypicalDays typicalDays = EhubMisc.HorizonReduction.GenerateTypicalDays(fullProfiles, loadTypes, 
                12, peakDays, useForClustering, correctionLoad, true);

            /// Running Energy Hub
            Console.WriteLine("Solving MILP optimization model...");
            double[][] typicalSolarLoads = new double[numberOfSolarAreas][];

            // solar profiles negative or very small numbers. rounding floating numbers thing?
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
            for (int t=0; t<typicalDays.DayProfiles[0].Length; t++)
                for(int i=0; i<3; i++)
                    if (typicalDays.DayProfiles[i][t] < 0.001) typicalDays.DayProfiles[i][t] = 0.0;


            int[] clustersizePerTimestep = typicalDays.NumberOfDaysPerTimestep;
            Ehub ehub = new Ehub(typicalDays.DayProfiles[0], typicalDays.DayProfiles[1], typicalDays.DayProfiles[2],
                typicalSolarLoads, solarAreas.ToArray(),
                typicalDays.DayProfiles[4], technologyParameters,
                clustersizePerTimestep);
            ehub.Solve(3,true);

            Console.WriteLine();
            Console.WriteLine("ENERGY HUB SOLVER COMPLETED");
            WriteOutput(scenarioString[scenario], path, numberOfSolarAreas, ehub, typicalDays, numBaseLoads);



        }


        static void WriteSolarProfiles(string scenario, string path, double [][] solarPotentials, double [] solarAreas)
        {
            // i need kWh/m2a per sensor point (from full 8760, coz typical profiles are scaled thus "unphysical". 
            // and sized area per scenario

            //just write a vector with annual kWh/m2a per SP
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            List<string> header = new List<string>();
            List<string> firstLine = new List<string>();
            List<string> secondLine = new List<string>();
            int counter = 0;
            foreach (var sensorPoint in solarPotentials)
            {
                header.Add("SP" + Convert.ToString(counter));
                double sum = sensorPoint.Sum()/1000.0; // in kWh
                firstLine.Add(Convert.ToString(sum));
                secondLine.Add(Convert.ToString(solarAreas[counter]));
                counter++;
            }

            List<List<string>> outputString = new List<List<string>>();
            outputString.Add(header);
            outputString.Add(firstLine);
            outputString.Add(secondLine);
            using var sw = new StreamWriter(outputPath + scenario + "_annualSolar.csv");
            foreach (List<string> line in outputString)
            {
                foreach (string cell in line)
                    sw.Write(cell + ";");
                sw.Write(Environment.NewLine);
            }
            sw.Close();
        }


        static void WriteOutput(string scenario, string path, int numberOfSolarAreas, Ehub ehub, EhubMisc.HorizonReduction.TypicalDays typicalDays, int numBaseLoads)
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
            header.Add("x_DistrictHeatingNetwork");
            header_units.Add("m");
            header.Add("x_coolingTower");
            header_units.Add("kWh");
            header.Add("x_Battery");
            header_units.Add("kWh");
            header.Add("x_TES");
            header_units.Add("kWh");
            header.Add("x_clgTES");
            header_units.Add("kWh");
            header.Add("x_CHP");
            header_units.Add("kW");
            header.Add("x_Boiler");
            header_units.Add("kW");
            header.Add("x_BiomassBoiler");
            header_units.Add("kW");
            header.Add("x_ASHP");
            header_units.Add("kW");
            header.Add("x_ElecChiller");
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
            header.Add("x_clgTES_charge");
            header_units.Add("kWh");
            header.Add("x_clgTES_discharge");
            header_units.Add("kWh");
            header.Add("x_clgTES_soc");
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
            header.Add("x_DR_elec_pos");
            header_units.Add("kWh");
            header.Add("x_DR_elec_neg");
            header_units.Add("kWh");
            header.Add("x_DR_heat_pos");
            header_units.Add("kWh");
            header.Add("x_DR_heat_neg");
            header_units.Add("kWh");
            header.Add("x_DR_cool_pos");
            header_units.Add("kWh");
            header.Add("x_DR_cool_neg");
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
            for (int i = 0; i < numberOfSolarAreas; i++)
            {
                header.Add("TypicalPotentialsSP_" + i);
                header_units.Add("kWh/m^2");
            }

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
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dh));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtower));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes));
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
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_charge[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_discharge[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_soc[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_e[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_h[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_dump[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_boi_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bmboi_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_hp_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_ac_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_elecpur[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_feedin[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_elec_pos[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_elec_neg[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_heat_pos[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_heat_neg[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_cool_pos[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_cool_neg[0]));
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
                    for (int i = 0; i < numberOfSolarAreas; i++)
                        firstLine.Add(Convert.ToString(typicalDays.DayProfiles[numBaseLoads+i][0]));

                        outputString.Add(firstLine);

                    for (int t = 1; t < ehub.Outputs[e].x_elecpur.Length; t++)
                    {
                        List<string> newLine = new List<string>();
                        for (int skip = 0; skip < 15; skip++)
                            newLine.Add("");
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_charge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_discharge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_soc[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_charge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_discharge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_soc[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_charge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_discharge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_soc[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_e[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_h[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_dump[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_boi_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bmboi_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_hp_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_ac_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_elecpur[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_feedin[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_elec_pos[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_elec_neg[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_heat_pos[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_heat_neg[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_cool_pos[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_cool_neg[t]));
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
                        for (int i = 0; i < numberOfSolarAreas; i++)
                            newLine.Add(Convert.ToString(typicalDays.DayProfiles[numBaseLoads + i][t]));

                        outputString.Add(newLine);
                    }

                }
                using var sw = new StreamWriter(outputPath + scenario + "_result_epsilon_" + e + ".csv");
                foreach (List<string> line in outputString)
                {
                    foreach (string cell in line)
                        sw.Write(cell + ";");
                    sw.Write(Environment.NewLine);
                }
                sw.Close();

            }
        }


        /// <summary>
        /// one surface area per sensor point
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
                double area = Convert.ToDouble(line[^1]);  // ^1 get's me the last index of an array. That's the 'usefulearea' column in the input csv
                solarArea.Add(area);
            }
        }


        /// <summary>
        /// one sensor point per profile
        /// </summary>
        static void LoadSolarPotentialsInput(string inputFile, out double [][] solarPotentials)
        {
            int horizon = 8760;


            string[] linesSp = File.ReadAllLines(inputFile);
            string[] firstLine = linesSp[0].Split(new char[2] { ',', ';' });
            solarPotentials = new double[firstLine.Length - 1][];
            for (int i = 0; i < solarPotentials.Length; i++) 
                solarPotentials[i] = new double[horizon];
                
            for(int t=1; t<linesSp.Length; t++) // first line is header, so skip
            {
                string[] currentHour = linesSp[t].Split(new char[2] { ',', ';' });
                for (int i = 0; i < solarPotentials.Length; i++)
                    solarPotentials[i][t-1] = Convert.ToDouble(currentHour[i]);
            }
        }


        /// <summary>
        /// it takes 4 sensor points per surface patch 
        /// </summary>
        /// <param name="inputFileArea"></param>
        /// <param name="inputFilesPotentials"></param>
        /// <param name="solarPotentials"></param>
        /// <param name="solarArea"></param>
        static void LoadSolarPotentialsAndAreasInput(string inputFileArea, string[] inputFilesPotentials, out double[][] solarPotentials, out List<double> solarArea)
        {
            //string[] inputFilesPotentials,
            // load solar potentials on building facades
            // solar_Risch_2020_SP0.csv
            // solar_Risch_2020_SP1.csv
            // solar_Risch_2020_SP4.csv
            // solar_Risch_2020_SP5.csv

            // load solar surface areas. contains 193 rows plus header. 
            //      each row corresponds to one row in solar_xxxx_20xx_SPx.csv
            //      get 1/4 of that area for each SP 0, 1, 4, 5
            //      construct an array with 193*4 entries to represent each sensor point
            // SurfaceAreas.csv

            // construct one matrix with solarPotentials[surface_index][time_step]
            solarArea = new List<double>();

            string[] solarAreaLines = File.ReadAllLines(inputFileArea);
            for (int i = 1; i < solarAreaLines.Length; i++)
            {
                string[] line = solarAreaLines[i].Split(new char[2] { ',', ';' });
                // because I have 4 sensor points per surface area
                double area = Convert.ToDouble(line[^1]) / 4;  // ^1 get's me the last index of an array
                solarArea.Add(area);
                solarArea.Add(area);
                solarArea.Add(area);
                solarArea.Add(area);
            }

            int horizon = 8760;
            solarPotentials = new double[solarArea.Count][];
            for(int i=0; i<solarArea.Count; i++)
                solarPotentials[i] = new double[horizon];

            string sp0 = inputFilesPotentials[0];
            string sp1 = inputFilesPotentials[1];
            string sp4 = inputFilesPotentials[2];
            string sp5 = inputFilesPotentials[3];

            string[] linesSp0 = File.ReadAllLines(sp0);
            string[] linesSp1 = File.ReadAllLines(sp1);
            string[] linesSp4 = File.ReadAllLines(sp4);
            string[] linesSp5 = File.ReadAllLines(sp5);

            int numberOfProfilesPerSp = linesSp0[0].Split(new char[2] { ',', ';' }).Length - 1;
            for (int t = 1; t < linesSp0.Length; t++) // first line is header, so skip
            {
                int counter = 0;
                string[] lineSp0 = linesSp0[t].Split(new char[2] { ',', ';' });
                string[] lineSp1 = linesSp1[t].Split(new char[2] { ',', ';' });
                string[] lineSp4 = linesSp4[t].Split(new char[2] { ',', ';' });
                string[] lineSp5 = linesSp5[t].Split(new char[2] { ',', ';' });
                for (int i = 0; i < numberOfProfilesPerSp; i++)
                {
                    solarPotentials[counter][t - 1] = Convert.ToDouble(lineSp0[i]);
                    solarPotentials[counter+1][t - 1] = Convert.ToDouble(lineSp1[i]);
                    solarPotentials[counter+2][t - 1] = Convert.ToDouble(lineSp4[i]);
                    solarPotentials[counter+3][t - 1] = Convert.ToDouble(lineSp5[i]);
                    counter += 4;
                }
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
            for (int i=1; i<lines.Length; i++)
            {
                var line = lines[i].Split(new char[2] { ',', ';' });
                peakHeatingLoads.Add(Convert.ToDouble(line[3])); // sh_and_dhw at [3]
                peakCoolingLoads.Add(Convert.ToDouble(line[4])); 
            }

            numBuildings = peakHeatingLoads.Count;
        }



        static void LoadTimeSeries(string inputFile, out List<double> timeSeries)
        {
            timeSeries = new List<double>();
            var lines = File.ReadAllLines(inputFile);
            foreach (var line in lines)
            {
                var lineSplit  = line.Split(new char[2] {',', ';'});
                timeSeries.Add(Convert.ToDouble(lineSplit[0]));
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


    }
}
