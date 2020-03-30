using System;
using System.Collections.Generic;
using System.Text;

using ILOG.CPLEX;
using ILOG.Concert;

using EhubMisc;

namespace AdamMSc2020
{
    internal class Ehub
    {
        /// <summary>
        /// Optimal solution
        /// </summary>
        internal EhubOutputs [] Outputs;


        internal Ehub(double [] coolingDemand, double [] heatingDemand, double [] electricityDemand,
            double[][] irradiance, double [] solarTechSurfaceAreas)
        {
            // use profiles in clustering functions, return typical profiles 
            

        }


        internal void Solve(int epsilonCuts)
        {
            this.Outputs = new EhubOutputs[epsilonCuts];

            // 1. solve for minCarbon, ignoring cost
            // 2. solve for minCost, using minCarbon value found in 1 (+ small torelance)
            // 3. solve for minCost, ignoring Carbon (then, solve for minCarbon, using mincost as constraint. check, if it makes a difference in carbon)
            // 4. make epsilonCuts (-2) cuts and solve for each minCost s.t. carbon
            // 5. report all values into Outputs



        }


        private EhubOutputs EnergyHub(string objective = "cost", double? carbonConstraint = null, double? costConstraint = null)
        {
            Cplex cpl = new Cplex();


            EhubOutputs solution = new EhubOutputs();
            return solution;
        }
    }
}
