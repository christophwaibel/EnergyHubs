using System;
using System.Collections.Generic;
using System.Text;
using ILOG.Concert;
using ILOG.CPLEX;
using EhubMisc;

namespace SBE22MultiPeriodPV
{
    internal class MultiPeriodPV
    {
        internal MultiPeriodPV(double[] heatingDemand, double[] coolingDemand, double[] electricityDemand,
            double[][] irradiance, double[] solarTechSurfaceAreas,
            double[] ambientTemperature, Dictionary<string, double> technologyParameters,
            int[] clustersizePerTimestep)
        {

        }


        internal void Solve(int epsilonCuts, bool verbose = false)
        {


        }

        private EhubOutputs EnergyHub(string objective = "cost", double? carbonConstraint = null, double? costConstraint = null, bool verbose = false)
        {
            Cplex cpl = new Cplex();

            // hardcoding 3 investment periods: 2020, 2030, 2040
            // that means, we need 3 separate variables for each tech, because each tech per period will have different efficiencies, embodied emissions and cost parameters
            // also 3 separate arrays (incl set of constraints & expressions) for demands, irradiance, ghi, tamb, and conversion matrices

            // however, if I have 5 years intervals, I have to work with arrays. Can't have them manually anymore, would be too messy

            const int yearHorizon = 2050 - 2020;
            const int intervals = 5;
            const int periods = yearHorizon / intervals;
            const int numPVs = 4;
            const int hours = 24;
            const int days = 15; // e.g., typical days


            // adapt to 4 different PV technologies!
            double[] lifetimePV = new double[numPVs];
            for (int i = 0; i < numPVs; i++)
                lifetimePV[i] = 20; // should be 25 actually..

            INumVar[] x_PV = new INumVar[numPVs];
            INumVar[][] x_newPV = new INumVar[numPVs][];
            for (int i = 0; i < numPVs; i++)
                x_newPV[i] = new INumVar[periods];

            double[] maxCapPV = new double[numPVs];
            for (int i = 0; i < x_PV.Length; i++)
                x_PV[i] = cpl.NumVar(0, maxCapPV[i]);


            // sqm of Monocristalline PV installed at period p. Needs constraint later to check max surface area, accounting for lifetime
            INumVar[][] x_PV_mono = new INumVar[periods][];
            for (int p = 0; p < periods; p++)
                x_PV_mono[p] = new INumVar[numPVs];

            // same for cdte


            // all other tech also needs an index for each period
            INumVar[] x_CHP = new INumVar[periods];
            for (int p = 0; p < periods; p++)
                x_CHP[p] = cpl.NumVar(0, double.MaxValue);


            // indices: period, day, hour
            INumVar[][][] x_CHP_op = new INumVar[periods][][];
            INumVar[][][] x_ASHP_op = new INumVar[periods][][];
            INumVar[][][] x_Boiler_op = new INumVar[periods][][];
            INumVar[][][] x_BioBoiler_op = new INumVar[periods][][];


            for (int p=0; p<periods; p++)
            {
                x_CHP_op[p] = new INumVar[days][];
                for(int d=0; d<days; d++)
                {
                    x_CHP_op[p][d] = new INumVar[hours];
                    x_ASHP_op[p][d] = new INumVar[hours];
                    // ...
                    for (int h=0; h<hours; h++)
                    {
                        x_CHP_op[p][d][h] = cpl.NumVar(0, double.MaxValue);
                        x_ASHP_op[p][d][h] = cpl.NumVar(0, double.MaxValue);
                        // ...
                    }
                }
            }







            // Lifetime constraint
            ILinearNumExpr[][] totalCapPV = new ILinearNumExpr[numPVs][];
            for (int i = 0; i < numPVs; i++)
            {
                // I have to sum up in one totalCapPV to check for max space usage. But I can't use totalCapPV for yield calculation, coz I'll have differente efficiencies per period
                totalCapPV[i] = new ILinearNumExpr[periods]; 
                for (int p = 0; p < periods; p++)
                    totalCapPV[i][p] = cpl.LinearNumExpr();
            }

            for (int i = 0; i < numPVs; i++)
                for (int p = 0; p < periods; p++)
                    for(int pp=(int)Math.Max(0,p-lifetimePV[p]+1); pp<periods; pp++)
                        totalCapPV[i][p].AddTerm(1,x_newPV[i][p]);
            for (int i = 0; i < numPVs; i++)
                for(int p=0; p<periods; p++)
                    cpl.AddGe(maxCapPV[i], totalCapPV[i][p]);


            return new EhubOutputs();
        }





    }
}
