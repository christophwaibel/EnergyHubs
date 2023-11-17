using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EhubMisc
{
    public static class Misc
    {
        /// <summary>
        /// Art by Susie Oviatt, from: https://www.asciiart.eu/animals
        /// </summary>
        /// <param name="graphic">dog or cat</param>
        /// <returns></returns>
        public static string[] AsciiDrawing(string graphic)
        {
            string[] dog = new string[28];
            dog[0] = @"                          ..,,,,,,,,,..";
            dog[1] = @"                     .,;%%%%%%%%%%%%%%%%%%%%;,.";
            dog[2] = @"                   %%%%%%%%%%%%%%%%%%%%////%%%%%%, .,;%%;,";
            dog[3] = @"            .,;%/,%%%%%/////%%%%%%%%%%%%%%////%%%%,%%//%%%, ";
            dog[4] = @"        .,;%%%%/,%%%///%%%%%%%%%%%%%%%%%%%%%%%%%%%%,////%%%%;, ";
            dog[5] = @"     .,%%%%%%//,%%%%%%%%%%%%%%%%@@%a%%%%%%%%%%%%%%%%,%%/%%%%%%%;, ";
            dog[6] = @"   .,%//%%%%//,%%%%///////%%%%%%%@@@%%%%%%///////%%%%,%%//%%%%%%%%, ";
            dog[7] = @" ,%%%%%///%%//,%%//%%%%%///%%%%%@@@%%%%%////%%%%%%%%%,/%%%%%%%%%%%%% ";
            dog[8] = @".%%%%%%%%%////,%%%%%%%//%///%%%%@@@@%%%////%%/////%%%,/;%%%%%%%%/%%% ";
            dog[9] = @"%/%%%%%%%/////,%%%%///%%////%%%@@@@@%%%///%%/%%%%%//%,////%%%%//%%%' ";
            dog[10] = @"%//%%%%%//////,%/%a`  'a%///%%%@@@@@@%%////a`  'a%%%%,//%///%/%%%%% ";
            dog[11] = @"%///%%%%%%///,%%%%@@aa@@%//%%%@@@@S@@@%%///@@aa@@%%%%%,/%////%%%%% ";
            dog[12] = @"%%//%%%%%%%//,%%%%%///////%%%@S@@@@SS@@@%%/////%%%%%%%,%////%%%%%' ";
            dog[13] = @"%%//%%%%%%%//,%%%%/////%%@%@SS@@@@@@@S@@@@%%%%/////%%%,////%%%%%' ";
            dog[14] = @"`%/%%%%//%%//,%%%///%%%%@@@S@@@@@@@@@@@@@@@S%%%%////%%,///%%%%%' ";
            dog[15] = @"  %%%%//%%%%/,%%%%%%%%@@@@@@@@@@@@@@@@@@@@@SS@%%%%%%%%,//%%%%%' ";
            dog[16] = @"  `%%%//%%%%/,%%%%@%@@@@@@@@@@@@@@@@@@@@@@@@@S@@%%%%%,/////%%' ";
            dog[17] = @"   `%%%//%%%/,%%%@@@SS@@SSs@@@@@@@@@@@@@sSS@@@@@@%%%,//%%//%' ";
            dog[18] = @"    `%%%%%%/  %% S@@SS@@@@@Ss` .,,.    'sS@@@S@@@@%'  ///%/%' ";
            dog[19] = @"      `%%%/    % SS@@@@SSS@@S.         .S@@SSS@@@@'    //%%'";
            dog[20] = @"                /`S@@@@@@SSSSSs,     ,sSSSSS@@@@@' ";
            dog[21] = @"              %%//`@@@@@@@@@@@@@Ss,sS@@@@@@@@@@@'/ ";
            dog[22] = @"            %%%%@@00`@@@@@@@@@@@@@'@@@@@@@@@@@'//%% ";
            dog[23] = @"        %%%%%% a %@@@@000aaaaaaaaa00a00aaaaaaa00 %@%%%%%";
            dog[24] = @"        %%%%%% a %%@@@@@@@@@@000000000000000000@@@%@@%%%@%%%";
            dog[25] = @"       %%%%%% a %%@@@%@@@@@@@@@@@00000000000000@@@@@@@@@%@@%%@%%";
            dog[26] = @"        %%% aa %@@@@@@@@@@@@@@0000000000000000000000@@@@@@@@%@@@%%%%";
            dog[27] = @"          %%@@@@@@@@@@@@@@@00000000000000000000000000000@@@@@@@@@%%%%%";


            string[] cat = new string[28];
            cat[0] = @"                   ,";
            cat[1] = @"                   \`-._           __";
            cat[2] = @"                    \\  `-..____,.'  `.";
            cat[3] = @"                     :`.         /    \`.";
            cat[4] = @"                     :  )       :      : \";
            cat[5] = @"                      ;'        '   ;  |  :";
            cat[6] = @"                      )..      .. .:.`.;  :";
            cat[7] = @"                     /::...  .:::...   ` ;";
            cat[8] = @"                     ; _ '    __        /:\";
            cat[9] = @"                     `:o>   /\o_>      ;:. `.";
            cat[10] = @"                    `-`.__ ;   __..--- /:.   \";
            cat[11] = @"                    === \_/   ;=====_.':.     ;";
            cat[12] = @"                     ,/'`--'...`--....        ;";
            cat[13] = @"                          ;                    ;";
            cat[14] = @"                        .'                      ;";
            cat[15] = @"                      .'                        ;";
            cat[16] = @"                    .'     ..     ,      .       ;";
            cat[17] = @"                   :       ::..  /      ;::.     |";
            cat[18] = @"                  /      `.;::.  |       ;:..    ;";
            cat[19] = @"                 :         |:.   :       ;:.    ;";
            cat[20] = @"                 :         ::     ;:..   |.    ;";
            cat[21] = @"                  :       :;      :::....|     |";
            cat[22] = @"                  /\     ,/ \      ;:::::;     ;";
            cat[23] = @"                .:. \:..|    :     ; '.--|     ;";
            cat[24] = @"               ::.  :''  `-.,,;     ;'   ;     ;";
            cat[25] = @"            .-'. _.'\      / `;      \,__:      \";
            cat[26] = @"            `---'    `----'   ;      /    \,.,,,/";
            cat[27] = @"                               `----`              ";

            if (string.Equals(graphic, "dog")) return dog;
            else return cat;
        }

        public static bool IsNullOrDefault<T>(this Nullable<T> value) where T : struct
        {
            return default(T).Equals(value.GetValueOrDefault());
        }


        public static double Distance2Pts(double[] x1, double[] x2, string distanceMeasure = "SqEuclidean")
        {
            int n = x1.Length;
            if (x2.Length < n) n = x2.Length;

            double distance = 0.0;
            for (int i = 0; i < n; i++)
                distance += Math.Pow(x1[i] - x2[i], 2);
            if (string.Equals(distanceMeasure, "Euclidean"))
                distance = Math.Sqrt(distance);

            return distance;
        }


        /// <summary>
        /// Loads a timeseries of a csv file. Separator ',' and ';' and uses the first value per row
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="timeSeries"></param>
        public static void LoadTimeSeries(string inputFile, out List<double> timeSeries, int valuePosition = 0, int skipLine = 0)
        {
            timeSeries = new List<double>();
            var lines = File.ReadAllLines(inputFile);
            int counter = 0;
            foreach (var line in lines)
            {
                if (skipLine > 0 && counter < skipLine)
                {
                    counter++;
                    continue;
                }
                var lineSplit = line.Split(new char[2] { ',', ';' });
                timeSeries.Add(Convert.ToDouble(lineSplit[valuePosition]));
            }
        }



        public static void WriteTextFile(string outputPath, string fileName, List<List<string>> outputString)
        {
            using (var sw = new StreamWriter(outputPath + fileName))
            {
                foreach (List<string> line in outputString)
                {
                    foreach (string cell in line)
                        sw.Write(cell + ";");
                    sw.Write(Environment.NewLine);
                }
                sw.Close();
            }
        }


        public static void LoadTechParameters(string inputFile, out Dictionary<string, double> technologyParameters)
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



        /// <summary>
        /// Returns 2 values: Solar Fraction and Solar Self-Sufficiency
        /// </summary>
        /// <param name="clusterSize">time series of</param>
        /// <param name="typicalElecDemand"></param>
        /// <param name="typicalGridPurchase"></param>
        /// <param name="typicalPvProduction"></param>
        /// <param name="typicalFeedIn"></param>
        public static double [] CalcSolarAutonomy(double [] clusterSize, double [] typicalElecDemand, double [] typicalGridPurchase, double [] typicalPvProduction, double [] typicalFeedIn)
        {
            int horizon = clusterSize.Length;

            double totalElecDemand = 0.0;
            double totalGridPurchase = 0.0;
            double totalPvProduction = 0.0;
            double totalFeedIn = 0.0;

            double solarSelfSuffnumerator = 0.0;
            double solarSelfSuffdenominator = 0.0;
            for (int i=0; i<horizon; i++)
            {
                double weightedElecDemand = clusterSize[i] * typicalElecDemand[i];
                double weightedGridPurchase = clusterSize[i] * typicalGridPurchase[i];
                double weightedPvProduction = clusterSize[i] * typicalPvProduction[i];
                double weightedFeedIn = clusterSize[i] * typicalFeedIn[i];
                double PvMinusFeedIn = weightedPvProduction - weightedFeedIn;
                double GridPlusPvMinusFeedIn = weightedGridPurchase + PvMinusFeedIn;

                totalElecDemand += weightedElecDemand;
                totalGridPurchase += weightedGridPurchase;
                totalPvProduction += weightedPvProduction;
                totalFeedIn += weightedFeedIn;

                solarSelfSuffnumerator += PvMinusFeedIn;
                solarSelfSuffdenominator += GridPlusPvMinusFeedIn;
            }


            double solarFraction = (totalPvProduction - totalFeedIn) / totalElecDemand;
            double solarSelfSufficiency = solarSelfSuffnumerator / solarSelfSuffdenominator;
            return new double[2] { solarFraction, solarSelfSufficiency };
        }
    }
}
