using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CISBAT21
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                ehubRun();
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
                path = @"C:\Users\Christoph\Documents\GitHub\EnergyHubs\CplexEnergyHubs\CISBAT21\data\";
            if (!path.EndsWith(@"\"))
                path = path + @"\";
            Console.WriteLine("Cheers, using path {0}", path);
            Console.WriteLine();
            Console.WriteLine("Which Case? 1) Risch 2020; 2) Risch 2050; 3) Singapore 2020; 4) Singapore 2050? Enter an integer 1-4:");
            string scenarioSelect = Console.ReadLine();
            int scenario;
            if (!Int32.TryParse(scenarioSelect, out scenario)) scenario = 0;
            var scenarioString = new string[4] { "Risch_2020", "Risch_2050_8-5", "Singapore_2020", "Singapore_2050_RCP8-5" };
            Console.WriteLine("Cheers, using scenario {0}", scenarioString[scenario]);

            // read in solar data
            string fileName = path + scenarioString[scenario];
            LoadSolarInput(path + "SurfaceAreas.csv",
                new string[4] {fileName+ "_solar_SP0.csv", fileName + "_solar_SP1.csv",
                    fileName + "_solar_SP4.csv", fileName + "_solar_SP5.csv"},
                out var irradiance, out var solarAreas);

            // load demand
            LoadBuildingInput(path + scenarioString[scenario] + "_demand.csv", 
                out var heating, out var dhw, out var cooling, out var electricity);

            // load peak loads per building, filter for 'Bxxx_Qh_40_kWh', 'Bxxx_Qh_60_kWh' and aggregate dhw and sh
            LoadPeakLoads(path + scenarioString[scenario] + "_PeakLoads.csv",
                out var numBuildings, out var peakHeatingLoads);

            // load GHI
            LoadTimeSeries(path + scenarioString[scenario] + "_GHI.csv", out var ghi);

            // load dry bulb
            LoadTimeSeries(path + scenarioString[scenario] + "_DryBulb.csv", out var dryBulb);

            //Building Tech
            LoadTechParameters(path + scenarioString[scenario] + "_technology.csv", out var technologyParameters);
            technologyParameters.Add("NumberOfBuildingsInEHub", Convert.ToDouble(numBuildings));
            for (int i = 0; i < numBuildings; i++)
                technologyParameters.Add("Peak_B_" + Convert.ToString(i), peakHeatingLoads[i]);

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
                12, peakDays, useForClustering, correctionLoad, false);

            /// Running Energy Hub
            Console.WriteLine("Solving MILP optimization model...");
            double[][] typicalSolarLoads = new double[numberOfSolarAreas][];

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! solar profiles negative numbers in [213]
            int reducedLength = 213;
            for (int u = 0; u < reducedLength; u++) // numberOfSolarAreas
                typicalSolarLoads[u] = typicalDays.DayProfiles[numBaseLoads + u];
            var solarAreasDummy = new double[reducedLength];
            for (int i = 0; i < solarAreasDummy.Length; i++)
                solarAreasDummy[i] = solarAreas[i];

            int[] clustersizePerTimestep = typicalDays.NumberOfDaysPerTimestep;
            Ehub ehub = new Ehub(typicalDays.DayProfiles[0], typicalDays.DayProfiles[1], typicalDays.DayProfiles[2],
                typicalSolarLoads, solarAreasDummy,
                typicalDays.DayProfiles[4], technologyParameters,
                clustersizePerTimestep);
            ehub.Solve(3,true);

            Console.WriteLine();
            Console.WriteLine("ENERGY HUB SOLVER COMPLETED");




        }



        static void LoadSolarInput(string inputFileArea, string[] inputFilesPotentials, out double[][] solarPotentials, out List<double> solarArea)
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
            out int numBuildings, out List<double> peakHeatingLoads)
        {
            peakHeatingLoads = new List<double>();
            var lines = File.ReadAllLines(inputFile);
            for (int i=1; i<lines.Length; i++)
            {
                var line = lines[i].Split(new char[2] { ',', ';' });
                peakHeatingLoads.Add(Convert.ToDouble(line[1])); //assume, there is sh+dhw at [5]. Change later when ShanShan updated it
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


    }
}
