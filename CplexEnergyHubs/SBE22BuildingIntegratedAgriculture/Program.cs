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
            JObject o1 = JObject.Parse(File.ReadAllText(@"C:\Users\chwaibel\Desktop\BIA\outputs\data\solar-radiation\B1004_insolation_Whm2.json"));
        }


  
    }


}
