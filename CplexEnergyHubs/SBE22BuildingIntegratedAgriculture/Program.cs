using EhubMisc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


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
            //Misc.LoadTimeSeries(inputFile, out var ghgFoodIndonesia, 4, 1);  // coefficient, read from technologies.csv
            Misc.LoadTimeSeries(inputFile, out var ghgBia, 9, 1);
            Misc.LoadTimeSeries(inputFile, out var yieldBia, 1, 1); // yield in kg per surface
            //var ghgBiaVsSupermarket = ghgBia.Zip(ghgFoodIndonesia, (x, y) => x - y).ToList(); // should be a negative number, coz we should be saving with BIA. otherwise it bad

            // Opex (how much money is earned from BIA) per year
            // column AH: "opex_all_USD_per_year" in USD per year
            //Misc.LoadTimeSeries(inputFile, out var opexBia, 33, 1);
            Misc.LoadTimeSeries(inputFile, out var opexSeeds, 28, 1);
            Misc.LoadTimeSeries(inputFile, out var opexFertilizer, 30, 1);
            var opexBia = opexSeeds.Zip(opexFertilizer, (x, y) => x + y);   // cost per year

            // Capex (how much the whole BIA system costs at this surface). for annualising, assume 20 years lifetime. IR 3% zhongming, but use same as in Ehub
            // column AA: "capex_all_USD" in USD
            Misc.LoadTimeSeries(inputFile, out var capexBia, 26, 1);  // total cost for whole lifetime. lifetime 10 years?


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


            int[] clustersizePerTimestep = typicalDays.NumberOfDaysPerTimestep;
            #endregion



            EHubBiA ehub = new EHubBiA(typicalDays.DayProfiles[0], typicalDays.DayProfiles[1], typicalDays.DayProfiles[2],
                typicalSolarLoads, surfaceAreas.ToArray(),
                typicalDays.DayProfiles[4], technologyParameters,
                clustersizePerTimestep,capexBia.ToArray(),opexBia.ToArray(),ghgBia.ToArray(), yieldBia.ToArray());
            ehub.Solve(3, true);

            Console.WriteLine();
            Console.WriteLine("ENERGY HUB SOLVER COMPLETED");




            //________________________________________________________________________
            //________________________________________________________________________
            //________________________________________________________________________
            // RESULTS WRITING

            WriteOutput("ehub-bia", rootFolder, numberOfSolarAreas, ehub, typicalDays, numBaseLoads);
            WriteBIAOutput("ehub-bia", rootFolder, numberOfSolarAreas, ehub);
            Console.ReadKey();
        }


        static void WriteBIAOutput(string scenario, string path, int numberOfSolarAreas, EHubBiA ehub)
        {
            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            // write a csv for each epsilon cut, name it "_e1", "_e2", .. etc
            List<string> header = new List<string>();
            // write units to 2nd row
            List<string> header_units = new List<string>();
            header.Add("veggie demand");
            header_units.Add("cal");
            header.Add("x_supermarket");
            header_units.Add("kg"); 
            header.Add("y_BIA_i");
            header_units.Add("yes or no");
            header.Add("x_PV_i");
            header_units.Add("m2");
            header.Add("x_BIAsold_i");
            header_units.Add("kg");
            header.Add("b_BIA_totalProduction");
            header_units.Add("kg red amaranth");
            header.Add("ann. fix cost bia");
            header_units.Add("SGD/year");

            for (int e = 0; e < ehub.Outputs.Length; e++)
            {
                List<List<string>> outputString = new List<List<string>>();
                if (ehub.Outputs[e].infeasible)
                {
                    outputString.Add(new List<string> { "Infeasible" });
                }
                else
                {
                    outputString.Add(header);
                    outputString.Add(header_units);

                    List<string> firstLine = new List<string>();
                    firstLine.Add(Convert.ToString(ehub.totalDemandFood));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_supermarket));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].y_bia[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_pv[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bia_sold[0]));
                    firstLine.Add(Convert.ToString(ehub.b_bia[0]));
                    firstLine.Add(Convert.ToString(ehub.c_Bia_fix[0]));

                    outputString.Add(firstLine);

                   
                        
                    for (int i = 1; i < numberOfSolarAreas; i++)
                    {
                        List<string> newLine = new List<string>();
                        for (int skip = 0; skip < 2; skip++)
                            newLine.Add("");
                       
                        newLine.Add(Convert.ToString(ehub.Outputs[e].y_bia[i]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_pv[i]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bia_sold[i]));
                        newLine.Add(Convert.ToString(ehub.b_bia[i]));
                        newLine.Add(Convert.ToString(ehub.c_Bia_fix[i]));

                        outputString.Add(newLine);
                    }
                    
                    

                }
                using var sw = new StreamWriter(outputPath + scenario + "_result_bia_epsilon_" + e + ".csv");
                foreach (List<string> line in outputString)
                {
                    foreach (string cell in line)
                        sw.Write(cell + ";");
                    sw.Write(Environment.NewLine);
                }
                sw.Close();

            }
        }



        static void WriteOutput(string scenario, string path, int numberOfSolarAreas, EHubBiA ehub, EhubMisc.HorizonReduction.TypicalDays typicalDays, int numBaseLoads)
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
                    Console.WriteLine("Writing solution {0}", e.ToString());
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
                        firstLine.Add(Convert.ToString(typicalDays.DayProfiles[numBaseLoads + i][0]));

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
