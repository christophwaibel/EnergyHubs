using System;
using System.Collections.Generic;
using System.IO;

namespace CISBAT21
{
    class Program
    {
        static void Main(string[] args)
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
            string[] scenarioString = new string[4] { "Risch_2020", "Risch_2050_8-5", "Singapore_2020", "Singapore_2050_RCP8-5" };
            Console.WriteLine("Cheers, using scenario {0}", scenarioString[scenario]);

            double[][] irradiance;
            List<double> solarAreas;
            string fileName = path + scenarioString[scenario];
            LoadSolarInput(path + "SurfaceAreas.csv",
                new string[4] {fileName+ "_solar_SP0.csv", fileName + "_solar_SP1.csv",
                    fileName + "_solar_SP4.csv", fileName + "_solar_SP5.csv"},
                out irradiance, out solarAreas);





            // load hourly demand: 
            // Risch_2020_demand.csv

            // load peak loads per building, filter for 'Bxxx_Qh_40_kWh', 'Bxxx_Qh_60_kWh' and aggregate dhw and sh
            // Risch_2020_PeakLoads.csv

            // load GHI
            // Risch_2020_GHI.csv

            // load dry bulb
            // Risch_2020_DryBulb.csv









            //var irradiance = new double[2][];
            var dict = new Dictionary<string, double>() { };

            try
            {
                Ehub ehub = new Ehub(new double[2] { 1, 1 }, new double[2] { 1, 1 },
                    new double[2] { 1, 1 }, irradiance, new double[2] { 1, 1 },
                    new double[2] { 1, 1 }, dict, new int[1] { 1 });
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

            // construct one matrix with solar[surface_index][time_step] -> solar

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
    }
}
