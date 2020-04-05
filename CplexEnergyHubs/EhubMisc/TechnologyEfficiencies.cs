using System;
using System.Collections.Generic;
using System.Text;

namespace EhubMisc
{
    public static class TechnologyEfficiencies
    {
        /// <summary>
        /// Calculate time resolved PV efficiency
        /// </summary>
        /// <remarks>Source: Garcia-Domingo et al. (2014), found in Mavromatidis et al (2015).</remarks>
        /// <param name="Tamb"></param>
        /// <param name="I"></param>
        /// <param name="NOCT"></param>
        /// <param name="T_aNOCT"></param>
        /// <param name="P_NOCT"></param>
        /// <param name="pv_beta_ref"></param>
        /// <param name="n_ref"></param>
        /// <returns></returns>
        public static double[] CalculateEfficiencyPhotovoltaic(double[] Tamb, double[] I,
    double NOCT, double T_aNOCT, double P_NOCT, double beta_ref, double n_ref)
        {
            int horizon = Tamb.Length;
            double[] nPV = new double[horizon];

            for (int t = 0; t < horizon; t++)
            {
                double Tcell = Tamb[t] + ((NOCT - T_aNOCT) / P_NOCT) * I[t];
                nPV[t] = n_ref * (1 - beta_ref * (Tcell - 25));
            }
            return nPV;
        }


        /// <summary>
        /// Calculate time resolved efficiency of solar thermal collectors
        /// </summary>
        /// <remarks>Source: Omu, Hsieh, Orehounig 2016, eqt. (2)</remarks>
        /// <param name="Tamb"></param>
        /// <param name="I"></param>
        /// <param name="Tin"></param>
        /// <param name="Frta"></param>
        /// <param name="FrUl"></param>
        /// <returns></returns>
        public static double[] CalculateEfficiencySolarThermal(double[] Tamb, double[] I, double Tin, double Frta, double FrUl)
        {
            int horizon = Tamb.Length;
            double[] nST = new double[horizon];
            for (int t = 0; t < horizon; t++)
            {
                nST[t] = Frta - ((FrUl * (Tin - Tamb[t])) / I[t]);
                if (nST[t] < 0) nST[t] = 0;
            }
            return nST;
        }


        /// <summary>
        /// calculate time resolved COP of Air-source heat pump
        /// </summary>
        /// <remarks>Source: Ashouri et al 2013</remarks>
        /// <param name="Tamb"></param>
        /// <param name="Tsup"></param>
        /// <returns></returns>
        public static double[] CalculateCOPHeatPump(double[] Tamb, double Tsup, double pi1, double pi2, double pi3, double pi4)
        {
            int horizon = Tamb.Length;
            double[] COP_HP = new double[horizon];

            for (int t = 0; t < horizon; t++)
            {
                COP_HP[t] = pi1 * Math.Exp(pi2 * (Tsup - Tamb[t])) + pi3 * Math.Exp(pi4 * (Tsup - Tamb[t]));
            }
            return COP_HP;
        }


        /// <summary>
        /// Calculating timeresolved COP of an AirCon depending on ambient temperature.
        /// </summary>
        /// <remarks>Source: 
        /// mode = "Choi": Choi, Lee, Kim 2005, foud in Gracik et al 2015
        /// mode = "Ryu": Ryu, Lee, Kim (2013). Optimum placement of top discharge outdoor unit installed near a wall. Eqt. (14)</remarks>
        /// <param name="Tamb"></param>
        /// <returns></returns>
        public static double[] CalculateCOPAirCon(double[] Tamb, string mode = "Choi")
        {
            int horizon = Tamb.Length;
            double[] COP_AC = new double[horizon];
            switch (mode)
            {
                default:
                case "Choi":
                    for (int t = 0; t < horizon; t++)
                        COP_AC[t] = (638.95 - 4.238 * Tamb[t]) / (100 + 3.534 * Tamb[t]); //Choi, Lee, Kim 2005, foud in Gracik et al 2015
                    break;
                case "Ryu":
                    for (int t = 0; t < horizon; t++)
                        COP_AC[t] = 12 - 0.35 * Tamb[t] + 0.0034 * Math.Pow(Tamb[t], 2); //Ryu et al 203
                    break;
            }

            return COP_AC;
        }
    }
}
