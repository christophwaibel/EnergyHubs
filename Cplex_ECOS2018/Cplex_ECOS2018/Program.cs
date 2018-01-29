using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using ILOG.CPLEX;
using ILOG.Concert;





// TO DO:

// 1. FINISH MULTI ENERGY HUB. cooling loads to electricity loads with AC COP 3, and sizing according to peak cooling load.

// 1. allow carbon benefit when pv feed-in
// 2. make three options: a) no LCA, b) with LCA, c) with LCA but carbon benefit
// 3. make runs for Alice' scenarios: A), D).... because A) has worse CO2



namespace Cplex_ECOS2018
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine(@"///////////////////////////////////////");
            Console.WriteLine("Morris sampling for ECOS 2018 energy hubs A, B and C...");
            Console.WriteLine(@"80 morris samples...");
            Console.WriteLine(@"///////////////////////////////////////");
            Console.WriteLine();
            Console.WriteLine(@"Please enter an existing path for writing results, and where the inputs are. E.g.: C:\Chris\Ecos2018\");
            string basepath = Console.ReadLine();
            Console.WriteLine(@"Please enter the number of epsilon cuts for the pareto front. 7 is a good number.");
            int epsilonsteps = Convert.ToInt16(Console.ReadLine());
            Console.WriteLine();

            //int epsilonsteps = 7;   // including min co2 and min cost
            int morrissamples = 400;
            //string path = @"C:\Users\Christoph\Desktop\Ecosinputs\";
            //string pathout_A = @"C:\Users\Christoph\Desktop\EcosResults\A\";
            //string pathout_B = @"C:\Users\Christoph\Desktop\EcosResults\B\";
            //string pathout_C = @"C:\Users\Christoph\Desktop\EcosResults\C\";
            string path = basepath + @"inputs\";
            string pathout_A = basepath + @"Results_A\";
            string pathout_B = basepath + @"Results_B\";
            string pathout_C = basepath + @"Results_C\";

            String[][][] Morris_outputsA = new String[morrissamples][][];
            String[][][] Morris_outputsB = new String[morrissamples][][];
            String[][][] Morris_outputsC = new String[morrissamples][][];

            for (int m = 0; m < morrissamples; m++)
            {
                Ehub ehub_A = new Ehub(path, m, morrissamples);
                ehub_A.Solve_MultiObejctive(epsilonsteps, 0);
                ehub_A.writeOutputCSV(pathout_A);

                Ehub ehub_B = new Ehub(path, m, morrissamples);
                ehub_B.Solve_MultiObejctive(epsilonsteps, 1);
                ehub_B.writeOutputCSV(pathout_B);

                Ehub ehub_C = new Ehub(path, m, morrissamples);
                ehub_C.Solve_MultiObejctive(epsilonsteps, 2);
                ehub_C.writeOutputCSV(pathout_C);

                // only write outputs for the morris
                Morris_outputsA[m] = new String[epsilonsteps][];
                Morris_outputsB[m] = new String[epsilonsteps][];
                Morris_outputsC[m] = new String[epsilonsteps][];
                for (int e = 0; e < epsilonsteps; e++)
                {
                    Morris_outputsA[m][e] = new String[10 + 60 + 2];     // Total, OPEX, CAPEX, CO2, hp, boi, chp, tes, bat, ac, 60 times pv, totalgridpurchase, totalfeedin);
                    Morris_outputsA[m][e][0] = Convert.ToString(ehub_A.outputs[e].cost);
                    Morris_outputsA[m][e][1] = Convert.ToString(ehub_A.outputs[e].OPEX);
                    Morris_outputsA[m][e][2] = Convert.ToString(ehub_A.outputs[e].CAPEX);
                    Morris_outputsA[m][e][3] = Convert.ToString(ehub_A.outputs[e].carbon);
                    Morris_outputsA[m][e][4] = Convert.ToString(ehub_A.outputs[e].x_hp);
                    Morris_outputsA[m][e][5] = Convert.ToString(ehub_A.outputs[e].x_boi);
                    Morris_outputsA[m][e][6] = Convert.ToString(ehub_A.outputs[e].x_chp);
                    Morris_outputsA[m][e][7] = Convert.ToString(ehub_A.outputs[e].x_tes);
                    Morris_outputsA[m][e][8] = Convert.ToString(ehub_A.outputs[e].x_bat);
                    Morris_outputsA[m][e][9] = Convert.ToString(ehub_A.outputs[e].x_ac);
                    Morris_outputsB[m][e] = new String[10 + 60 + 2];     // Total, OPEX, CAPEX, CO2, hp, boi, chp, tes, bat, ac, 60 times pv, totalgridpurchase, totalfeedin);
                    Morris_outputsB[m][e][0] = Convert.ToString(ehub_B.outputs[e].cost);
                    Morris_outputsB[m][e][1] = Convert.ToString(ehub_B.outputs[e].OPEX);
                    Morris_outputsB[m][e][2] = Convert.ToString(ehub_B.outputs[e].CAPEX);
                    Morris_outputsB[m][e][3] = Convert.ToString(ehub_B.outputs[e].carbon);
                    Morris_outputsB[m][e][4] = Convert.ToString(ehub_B.outputs[e].x_hp);
                    Morris_outputsB[m][e][5] = Convert.ToString(ehub_B.outputs[e].x_boi);
                    Morris_outputsB[m][e][6] = Convert.ToString(ehub_B.outputs[e].x_chp);
                    Morris_outputsB[m][e][7] = Convert.ToString(ehub_B.outputs[e].x_tes);
                    Morris_outputsB[m][e][8] = Convert.ToString(ehub_B.outputs[e].x_bat);
                    Morris_outputsB[m][e][9] = Convert.ToString(ehub_B.outputs[e].x_ac);
                    Morris_outputsC[m][e] = new String[10 + 60 + 2];     // Total, OPEX, CAPEX, CO2, hp, boi, chp, tes, bat, ac, 60 times pv, totalgridpurchase, totalfeedin);
                    Morris_outputsC[m][e][0] = Convert.ToString(ehub_C.outputs[e].cost);
                    Morris_outputsC[m][e][1] = Convert.ToString(ehub_C.outputs[e].OPEX);
                    Morris_outputsC[m][e][2] = Convert.ToString(ehub_C.outputs[e].CAPEX);
                    Morris_outputsC[m][e][3] = Convert.ToString(ehub_C.outputs[e].carbon);
                    Morris_outputsC[m][e][4] = Convert.ToString(ehub_C.outputs[e].x_hp);
                    Morris_outputsC[m][e][5] = Convert.ToString(ehub_C.outputs[e].x_boi);
                    Morris_outputsC[m][e][6] = Convert.ToString(ehub_C.outputs[e].x_chp);
                    Morris_outputsC[m][e][7] = Convert.ToString(ehub_C.outputs[e].x_tes);
                    Morris_outputsC[m][e][8] = Convert.ToString(ehub_C.outputs[e].x_bat);
                    Morris_outputsC[m][e][9] = Convert.ToString(ehub_C.outputs[e].x_ac);
                    for (int p = 0; p < ehub_A.outputs[e].x_pv.Length; p++)
                    {
                        Morris_outputsA[m][e][p + 10] = Convert.ToString(ehub_A.outputs[e].x_pv[p]);
                        Morris_outputsB[m][e][p + 10] = Convert.ToString(ehub_B.outputs[e].x_pv[p]);
                        Morris_outputsC[m][e][p + 10] = Convert.ToString(ehub_C.outputs[e].x_pv[p]);
                    }
                    Morris_outputsA[m][e][70] = Convert.ToString(ehub_A.outputs[e].x_elecpur.Sum());
                    Morris_outputsB[m][e][70] = Convert.ToString(ehub_B.outputs[e].x_elecpur.Sum());
                    Morris_outputsC[m][e][70] = Convert.ToString(ehub_C.outputs[e].x_elecpur.Sum());
                    Morris_outputsA[m][e][71] = Convert.ToString(ehub_A.outputs[e].x_feedin.Sum());
                    Morris_outputsB[m][e][71] = Convert.ToString(ehub_B.outputs[e].x_feedin.Sum());
                    Morris_outputsC[m][e][71] = Convert.ToString(ehub_C.outputs[e].x_feedin.Sum());
                }
                Console.WriteLine("morris run: {0}", m);
            }


            // Write first line for outputs: Total, OPEX, CAPEX, CO2, hp, boi, chp, tes, bat, ac, 60 times pv

            // write seperate text files for each epsilon constraint
            // per text file, each column is one output.
            for (int e = 0; e < epsilonsteps; e++)
            {
                string fileName = pathout_A + "EhubA_" + e + ".txt";
                using (FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write("Total;OPEX;CAPEX;Carbon;HP;Boiler;CHP;TES;Battery;AC;");
                    for (int p = 0; p < 60; p++)
                    {
                        sw.Write("PV_" + Convert.ToString(p) + ";");
                    }
                    sw.Write("TotPur;TotFeedin;\r\n");

                    for (int m = 0; m < morrissamples; m++)
                    {
                        for (int t = 0; t < 72; t++)
                        {
                            sw.Write(Morris_outputsA[m][e][t] + ";");
                        }
                        sw.Write("\r\n");
                    }
                }

                fileName = pathout_B + "EhubB_" + e + ".txt";
                using (FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write("Total;OPEX;CAPEX;Carbon;HP;Boiler;CHP;TES;Battery;AC;");
                    for (int p = 0; p < 60; p++)
                    {
                        sw.Write("PV_" + Convert.ToString(p) + ";");
                    }
                    sw.Write("TotPur;TotFeedin;\r\n");

                    for (int m = 0; m < morrissamples; m++)
                    {
                        for (int t = 0; t < 72; t++)
                        {
                            sw.Write(Morris_outputsB[m][e][t] + ";");
                        }
                        sw.Write("\r\n");
                    }
                }

                fileName = pathout_C + "EhubC_" + e + ".txt";
                using (FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write("Total;OPEX;CAPEX;Carbon;HP;Boiler;CHP;TES;Battery;AC;");
                    for (int p = 0; p < 60; p++)
                    {
                        sw.Write("PV_" + Convert.ToString(p) + ";");
                    }
                    sw.Write("TotPur;TotFeedin;\r\n");

                    for (int m = 0; m < morrissamples; m++)
                    {
                        for (int t = 0; t < 72; t++)
                        {
                            sw.Write(Morris_outputsC[m][e][t] + ";");
                        }
                        sw.Write("\r\n");
                    }
                }
            }


        }


    }


}
