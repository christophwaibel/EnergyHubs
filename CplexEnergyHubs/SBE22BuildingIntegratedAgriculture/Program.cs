using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using EhubMisc;
using System.Linq;

using System.Diagnostics;
using System.Threading;


namespace Cisbat23BuildingIntegratedAgriculture
{
    class Program
    {
        static void Main(string[] args)
        {
            #region loading data
            //________________________________________________________________________
            //________________________________________________________________________
            //________________________________________________________________________
            // LOADING DATA
            string buildingId = "B1007";
            string rootFolder = @"\\nas22\arch_ita_schlueter\03-Research\01-Projects\29_FCLGlobal\04_Numerical\BuildingIntegratedAgriculture\";


            // Why would I do that??? that aint gon save computing time
            /*
            // try loading from a folder "buildingId" first, if it doesnt exist it means it needs to be deserialized first from Zhongming's Jsons

            // Note : Directory.GetCurrentDirectory() can also return the current working directory.
            if (!Directory.Exists(rootFolder + buildingId))
            {
                Console.WriteLine("Folder doesn't exist. Loading from Zhongmings Jsons and writing csvs");

                saveBiaJsonSolarAsCsvFiles(rootFolder, buildingId);
            }
            // load from csvs
            */


            // solar potentials
            // several thousands of surfaces
            JObject o1 = JObject.Parse(File.ReadAllText(rootFolder + @"Zhongming Data\outputs\data\solar-radiation\" + buildingId + "_insolation_Whm2.json"));
            //var stuff = o1["srf0"].ToObject<double[]>();
            Dictionary<string, double[]> dictObj = o1.ToObject<Dictionary<string, double[]>>();
            o1 = null;
            List<double[]> solarPotentials = new List<double[]>(dictObj.Values);


            // surface areas
            var inputFile = rootFolder + @"Zhongming Data\outputs\data\solar-radiation\" + buildingId + "_geometry.csv";
            Misc.LoadTimeSeries(inputFile, out var surfaceAreas, 10, 1); // it's actually not a timeseries, but whaevva

            // for this file:
            inputFile = rootFolder + @"Zhongming Data\outputs\data\potentials\agriculture_yield\surface\" + buildingId + "_BIA_metrics_AmaranthRed.csv";

            //  ghg emissions if same amount bought from Malaysia or Indonesia
            // column D ([3]): "ghg_kg_co2_mys"
            // column E ([4]): "ghg_kg_co2_idn"
            // column J ([9]): "ghg_kg_co2_bia" -> co2 per year for operating bia
            Misc.LoadTimeSeries(inputFile, out var ghgFoodMalaysia, 3, 1);
            Misc.LoadTimeSeries(inputFile, out var ghgFoodIndonesia, 4, 1);
            Misc.LoadTimeSeries(inputFile, out var ghgBia, 9, 1);
            var ghgBiaVsSupermarket = ghgBia.Zip(ghgFoodIndonesia, (x, y) => x - y).ToList(); // should be a negative number, coz we should be saving with BIA. otherwise it bad

            // Opex (how much money is earned from BIA) per year
            // column AH: "opex_all_USD_per_year" in USD per year
            Misc.LoadTimeSeries(inputFile, out var opexBia, 33, 1);

            // Capex (how much the whole BIA system costs at this surface). for annualising, assume 20 years lifetime. IR 3% zhongming, but use same as in Ehub
            // column AA: "capex_all_USD" in USD
            Misc.LoadTimeSeries(inputFile, out var capexBia, 26, 1);


            // building loads
            inputFile = rootFolder + @"Zhongming Data\demand_chris\" + buildingId + "_Chris.csv";
            Misc.LoadTimeSeries(inputFile, out var elec, 4, 1);
            Misc.LoadTimeSeries(inputFile, out var dhw, 5, 1);
            Misc.LoadTimeSeries(inputFile, out var clg, 6, 1);


            // ghi, ambientTemp
            inputFile = rootFolder + @"Zhongming Data\inputs\weather\weatherZhongming.csv";
            Misc.LoadTimeSeries(inputFile, out var ghi, 0, 1);
            Misc.LoadTimeSeries(inputFile, out var temp, 1, 1);

            // technology data from csv
            inputFile = rootFolder + "Bia_technology.csv";
            Misc.LoadTechParameters(inputFile, out var technologyParameters);
            technologyParameters.Add("NumberOfBuildingsInEHub", 1);
            #endregion




            #region prepping for ehub
            //________________________________________________________________________
            //________________________________________________________________________
            //________________________________________________________________________
            // CLUSTERING TIME SERIES & SOLVE EHUB
            Console.WriteLine("Clustering and generating typical days...");
            int numberOfSolarAreas = surfaceAreas.Count;
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
                fullProfiles[0][t] = dhw[t];
                fullProfiles[1][t] = clg[t];
                fullProfiles[2][t] = elec[t];
                fullProfiles[3][t] = ghi[t];
                fullProfiles[4][t] = temp[t];
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


            //int[] clustersizePerTimestep = typicalDays.NumberOfDaysPerTimestep;
            //Ehub ehub = new Ehub(typicalDays.DayProfiles[0], typicalDays.DayProfiles[1], typicalDays.DayProfiles[2],
            //    typicalSolarLoads, solarAreas.ToArray(),
            //    typicalDays.DayProfiles[4], technologyParameters,
            //    clustersizePerTimestep);
            //ehub.Solve(3, true);

            //Console.WriteLine();
            //Console.WriteLine("ENERGY HUB SOLVER COMPLETED");






            //________________________________________________________________________
            //________________________________________________________________________
            //________________________________________________________________________
            // RESULTS WRITING

            //    WriteOutput(scenarioString[scenario], path, numberOfSolarAreas, ehub, typicalDays, numBaseLoads);
            #endregion
        }






        private static void saveBiaJsonSolarAsCsvFiles(string rootFolder, string buildingId)
        {
            // solar potentials
            // several thousands of surfaces
            JObject o1 = JObject.Parse(File.ReadAllText(rootFolder + @"Zhongming Data\outputs\data\solar-radiation\" + buildingId + "_insolation_Whm2.json"));
            //var stuff = o1["srf0"].ToObject<double[]>();

            Dictionary<string, double[]> dictObj = o1.ToObject<Dictionary<string, double[]>>();
            o1 = null;

            List<double[]> solarPotentials = new List<double[]>(dictObj.Values);


            string newFolder = rootFolder + buildingId;
            Directory.CreateDirectory(newFolder);
            foreach(var sol in solarPotentials)
            {
                //Misc.WriteTextFile(newFolder, buildingId+"")
            }

        }


    }


}
