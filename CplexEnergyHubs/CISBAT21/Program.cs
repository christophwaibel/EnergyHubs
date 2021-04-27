using System;
using System.Collections.Generic;

namespace CISBAT21
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //    Ehub ehub = new Ehub(heatingDemand, coolingDemand, electricityDemand, irradiance, 
            //        solarTechSurfaceAreas, ambientTemperature, technologyParameters, clustersizePerTimestep);


            // load hourly demand: 
            // Risch_2020_demand.csv

            // load peak loads per building, filter for 'Bxxx_Qh_40_kWh', 'Bxxx_Qh_60_kWh' and aggregate dhw and sh
            // Risch_2020_PeakLoads.csv

            // load GHI
            // Risch_2020_GHI.csv

            // load dry bulb
            // Risch_2020_DryBulb.csv

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

            // construct one matrix with solar[surface_index][time_step]

            var irradiance = new double[2][];
            var dict = new Dictionary<string, double>() { };

            try
            {
                Ehub ehub = new Ehub(new double[2] { 1, 1 }, new double[2] { 1, 1 }, new double[2] { 1, 1 }, irradiance, new double[2] { 1, 1 }, new double[2] { 1, 1 }, dict, new int[1] { 1 });
            }
            catch(Exception e)
            {
                string[] cat = EhubMisc.Misc.AsciiDrawing("cat");

                foreach (string c in cat)
                    Console.WriteLine(c);

                Console.WriteLine(e);

            }
            Console.ReadKey();

            
        }
    }
}
