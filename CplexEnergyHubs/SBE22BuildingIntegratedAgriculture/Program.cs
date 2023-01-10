using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cisbat23BuildingIntegratedAgriculture
{
    class Program
    {
        static void Main(string[] args)
        {

            string buildingId = "B1007";

            // solar potentials
            // several thousands of surfaces
            JObject o1 = JObject.Parse(File.ReadAllText(@"\\nas22\arch_ita_schlueter\03-Research\01-Projects\29_FCLGlobal\04_Numerical\BuildingIntegratedAgriculture\Zhongming Data\outputs\data\solar-radiation\" + buildingId +"_insolation_Whm2.json"));
            //var stuff = o1["srf0"].ToObject<double[]>();

            Dictionary<string, double[]> dictObj = o1.ToObject<Dictionary<string, double[]>>();
            o1 = null;            
            List<double[]> solarPotentials = new List<double[]>(dictObj.Values);

            // surface areas
            var inputFile = @"\\nas22\arch_ita_schlueter\03-Research\01-Projects\29_FCLGlobal\04_Numerical\BuildingIntegratedAgriculture\Zhongming Data\outputs\data\solar-radiation\" + buildingId + "_geometry.csv";
            EhubMisc.Misc.LoadTimeSeries(inputFile, out var surfaceAreas, 10, 1); // it's actually not a timeseries, but whaevva


            // for this file:
            inputFile = @"\\nas22\arch_ita_schlueter\03-Research\01-Projects\29_FCLGlobal\04_Numerical\BuildingIntegratedAgriculture\Zhongming Data\outputs\data\potentials\agriculture_yield\surface\" + buildingId + "_BIA_metrics_AmaranthRed.csv";

            //  ghg emissions if same amount bought from Malaysia or Indonesia
            // column D ([3]): "ghg_kg_co2_mys"
            // column E ([4]): "ghg_kg_co2_idn"
            EhubMisc.Misc.LoadTimeSeries(inputFile, out var ghgFoodMalaysia, 3, 1);
            EhubMisc.Misc.LoadTimeSeries(inputFile, out var ghgFoodIndonesia, 4, 1);

            // Opex (how much money is earned from BIA) per year
            // column AH: "opex_all_USD_per_year" in USD per year
            EhubMisc.Misc.LoadTimeSeries(inputFile, out var opexBia, 33, 1);

            // Capex (how much the whole BIA system costs at this surface). for annualising, assume 20 years lifetime. IR 3% zhongming, but use same as in Ehub
            // column AA: "capex_all_USD" in USD
            EhubMisc.Misc.LoadTimeSeries(inputFile, out var capexBia, 26, 1);


        }



    }


}
