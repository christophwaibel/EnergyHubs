using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SBE22BuildingIntegratedAgriculture
{
    class Program
    {
        static void Main(string[] args)
        {
            JObject o1 = JObject.Parse(File.ReadAllText(@"\\nas22\arch_ita_schlueter\03-Research\01-Projects\29_FCLGlobal\04_NumericalExperiments\BuildingIntegratedAgriculture\Zhongming Data\outputs\data\solar-radiation\B1004_insolation_Whm2.json"));
            //var stuff = o1["srf0"].ToObject<double[]>();

            Dictionary<string, double[]> dictObj = o1.ToObject<Dictionary<string, double[]>>();
            o1 = null;

        }


  
    }


}
