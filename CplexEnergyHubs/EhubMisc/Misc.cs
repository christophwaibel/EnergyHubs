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



        /// <summary>
        /// Loading all files in a directory and saving as one. Each row in a file is transposed into one column, then each file goes into one row
        /// </summary>
        /// <param name="inputDirectory"></param>
        /// <param name="contentInRowsPerFile"></param>
        public static void LoadAllFilesInDirectory(string inputDirectory, out List<string> contentInRowsPerFile)
        {
            contentInRowsPerFile = new List<string>();
            string[] files = Directory.GetFiles(inputDirectory);
            

            //Console.WriteLine("Files in " + dir + ":");
            // For each sample in this folder
            foreach (string file in files)
            {
                string filePath = file;
                string[] lines = File.ReadAllLines(filePath);

                // transpose rows into one column
                string oneRow = file + ";";
                foreach(var line in lines)
                    oneRow += line+";";
                contentInRowsPerFile.Add(oneRow);               
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
        /// Returns 2 values: Solar Fraction and Solar Self-Consumption
        /// Solar Fraction (also Solar Self-Sufficiency, or Autarky): The amounft of energy demand that is covered by solar energy. Also: Solar Fraction, Autarky.
        /// Solar Self-Consumption: The amount of solar energy consumed on-site.
        /// </summary>
        /// <param name="clusterSize">time series of</param>
        /// <param name="typicalGridPurchase"></param>
        /// <param name="typicalPvProduction"></param>
        /// <param name="typicalFeedIn"></param>
        /// <param name="totalElecDemand">Total electricity demand of the system, including for heat pumps, batteries, etc...</param>
        public static double [] CalcSolarAutonomy(double [] clusterSize, double [] typicalGridPurchase, double [] typicalPvProduction, double [] typicalFeedIn, double [] totalElecDemand)
        {
            int horizon = clusterSize.Length;
            double totalWeightedGridPurchase = 0.0;
            double totalWeightedPvProduction = 0.0;
            double totalWeightedFeedIn = 0.0;
            double totalWeightedElecDemand = 0.0;

            double solarSelfSuffnumerator = 0.0;
            double solarSelfSuffdenominator = 0.0;
            for (int t=0; t<horizon; t++)
            {
                double weightedGridPurchase = clusterSize[t] * typicalGridPurchase[t];
                double weightedPvProduction = clusterSize[t] * typicalPvProduction[t];
                double weightedFeedIn = clusterSize[t] * typicalFeedIn[t];
                double PvMinusFeedIn = weightedPvProduction - weightedFeedIn;
                double GridPlusPvMinusFeedIn = weightedGridPurchase + PvMinusFeedIn;

                totalWeightedGridPurchase += weightedGridPurchase;
                totalWeightedPvProduction += weightedPvProduction;
                totalWeightedFeedIn += weightedFeedIn;
                totalWeightedElecDemand += (totalElecDemand[t] * clusterSize[t]);

                solarSelfSuffnumerator += PvMinusFeedIn;
                solarSelfSuffdenominator += GridPlusPvMinusFeedIn;
            }

            double solarFraction = (totalWeightedPvProduction - totalWeightedFeedIn) / totalWeightedElecDemand;
            double solarSelfConsumption = ((totalWeightedPvProduction - totalWeightedFeedIn) / totalWeightedPvProduction);
            return new double[2] { solarFraction, solarSelfConsumption };
        }
    }
}
