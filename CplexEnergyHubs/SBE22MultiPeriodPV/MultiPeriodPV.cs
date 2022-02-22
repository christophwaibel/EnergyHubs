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

            // adapt to 4 different PV technologies!
            double[] lifetimePV = new double[numPVs];
            for (int i = 0; i < numPVs; i++)
                lifetimePV[i] = 20; // should be 25 actually..

            INumVar[] x_PV = new INumVar[numPVs];
            INumVar[][] x_newPV = new INumVar[numPVs][];
            for(int i=0; i<numPVs; i++)
                x_newPV[i] = new INumVar[periods];

            double[] maxCapPV = new double[numPVs];
            for(int i=0; i<x_PV.Length; i++)
                x_PV[i] = cpl.NumVar(0, maxCapPV[i]);



            // Lifetime constraint
            ILinearNumExpr[][] totalCapPV = new ILinearNumExpr[numPVs][];
            for (int i = 0; i < numPVs; i++)
            {
                totalCapPV[i] = new ILinearNumExpr[periods]; // I have to sum up in one totalCapPV to check for max space usage. But I can't use totalCapPV for yield calculation, coz I'll have differente efficiencies per period
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
