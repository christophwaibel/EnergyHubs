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

        #region inputs demand and typical days
        public double [] CoolingDemand { get; private set; }
        public double [] HeatingDemand { get; private set; }
        public double [] ElectricityDemand { get; private set; }
        public double [] DHWDemand { get; private set; }
        public double [][] SolarLoads { get; private set; }
        public double [] SolarAreas { get; private set; }

        public double[] CoolingWeights { get; private set; }
        public double[] HeatingWeights { get; private set; }
        public double[] ElectricityWeights { get; private set; }
        public double[] DHWWeights { get; private set; }
        public double[][] SolarWeights { get; private set; }

        public int NumberOfSolarAreas { get; private set; }

        public int Horizon { get; private set; }
        #endregion


        #region inputs cost parameters
        public double c_PV { get; private set; }
        public double [] c_Grid { get; private set; }
        public double [] c_FeedIn { get; private set; }

        #endregion


        #region LCA parameters
        public double[] lca_GridEmissions { get; private set; }
        public double lca_PVEmissions { get; private set; }

        #endregion


        #region inputs technology parameters (no cost and carbon)
        public double[] AmbientTemperature { get; private set; }
        public double[][] a_PVEfficiency { get; private set; }

        #endregion


        #region MILP stuff
        private const double M = 9999999;   // Big M method

        #endregion


        /// <summary>
        /// always hourly! I.e. it assumes the demand arrays are of length days x 24
        /// </summary>
        /// <param name="heatingDemand"></param>
        /// <param name="coolingDemand"></param>
        /// <param name="electricityDemand"></param>
        /// <param name="dhwDemand"></param>
        /// <param name="irradiance"></param>
        /// <param name="solarTechSurfaceAreas"></param>
        /// <param name="weightsOfLoads">If typical days are used, these weights are used to account for how many days a typical day represents</param>
        internal Ehub(double [] heatingDemand, double[] coolingDemand, double[] electricityDemand, double [] dhwDemand,
            double[][] irradiance, double [] solarTechSurfaceAreas,
            double [] weightsOfHeatingLoads, double [] weightsOfCoolingLoads, double [] weightsOfElectricityLoads, double [] weightsOfDHWLoads, 
            double [][] weightsOfSolarLoads,
            double [] ambientTemperature)
        {
            this.CoolingDemand = coolingDemand;
            this.HeatingDemand = heatingDemand;
            this.ElectricityDemand = electricityDemand;
            this.DHWDemand = dhwDemand;
            this.SolarLoads = irradiance;
            this.SolarAreas = solarTechSurfaceAreas;

            this.CoolingWeights = weightsOfCoolingLoads;
            this.HeatingWeights = weightsOfHeatingLoads;
            this.ElectricityWeights = weightsOfElectricityLoads;
            this.DHWWeights = weightsOfDHWLoads;
            this.SolarWeights = weightsOfSolarLoads;

            this.NumberOfSolarAreas = solarTechSurfaceAreas.Length;

            this.Horizon = coolingDemand.Length;


            /// read in these parameters as struct parameters
            /// 
            this.AmbientTemperature = ambientTemperature;

            this.c_PV = 100.0;
            this.c_FeedIn = new double[this.Horizon];
            this.c_Grid = new double[this.Horizon];
            this.lca_GridEmissions = new double[this.Horizon];
            this.lca_PVEmissions = 0.0;

            for (int t=0; t<this.Horizon; t++)
            {
                this.c_FeedIn[t] = -0.15;
                this.c_Grid[t] = 0.15;
                this.lca_GridEmissions[t] = 0.49;
            }

            double pv_NOCT = 20.0;
            double pv_T_aNOCT = 20.0;
            double pv_P_NOCT = 20.0;
            double pv_beta_ref = 1.0;
            double pv_n_ref = 0.15;

            this.a_PVEfficiency = new double[this.NumberOfSolarAreas][];
            for(int i=0; i<this.NumberOfSolarAreas; i++)
                this.a_PVEfficiency[i] = EhubMisc.TechnologyEfficiencies.CalculateEfficiencyPhotovoltaic(AmbientTemperature, this.SolarLoads[i],
                    pv_NOCT, pv_T_aNOCT, pv_P_NOCT, pv_beta_ref, pv_n_ref);


        }


        internal void Solve(int epsilonCuts)
        {
            this.Outputs = new EhubOutputs[epsilonCuts];

            // 1. solve for minCarbon, ignoring cost
            // 2. solve for minCost, using minCarbon value found in 1 (+ small torelance)
            // 3. solve for minCost, ignoring Carbon (then, solve for minCarbon, using mincost as constraint. check, if it makes a difference in carbon)
            // 4. make epsilonCuts (-2) cuts and solve for each minCost s.t. carbon
            // 5. report all values into Outputs

            this.Outputs[0] = EnergyHub("cost");

        }


        private EhubOutputs EnergyHub(string objective = "cost", double? carbonConstraint = null, double? costConstraint = null)
        {
            Cplex cpl = new Cplex();


            /// Variables
            /// 
            INumVar[] x_pv = new INumVar[this.NumberOfSolarAreas];
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                x_pv[i] = cpl.NumVar(0, this.SolarAreas[i]);

            INumVar[] y = new INumVar[this.Horizon];    // binary to indicate if PV is used (=1). no selling and purchasing from the grid at the same time allowed
            INumVar[] x_purchase = new INumVar[this.Horizon];
            INumVar[] x_feedin = new INumVar[this.Horizon];
            for(int t=0; t<this.Horizon; t++)
            {
                y[t] = cpl.BoolVar();
                x_purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_feedin[t] = cpl.NumVar(0, System.Double.MaxValue);
            }
            // xpurchase and other elec gen must satisfy weightsElec[t] * elecLoads[t]


            /// Constraints
            /// 
            ILinearNumExpr carbonEmissions = cpl.LinearNumExpr();
            for(int t=0; t<this.Horizon; t++)
            {
                // elec demand must be met by PV production and grid, minus feed in
                ILinearNumExpr pv_production = cpl.LinearNumExpr();
                for(int i=0; i<this.NumberOfSolarAreas; i++)
                    pv_production.AddTerm(this.SolarLoads[i][t] * this.SolarWeights[i][t] * 0.001 * a_PVEfficiency[i][t], x_pv[i]);
                cpl.AddGe(cpl.Diff(cpl.Sum(pv_production, x_purchase[t]), x_feedin[t]), this.ElectricityDemand[t] * this.ElectricityWeights[t]);

                // pv production must be greater equal feedin
                cpl.AddGe(pv_production, x_feedin[t]);
                cpl.AddLe(x_purchase[t], cpl.Prod(M, y[t]));    // y = 1 means elec is produced
                cpl.AddLe(x_feedin[t], cpl.Prod(M, cpl.Diff(1, y[t])));

                carbonEmissions.AddTerm((this.lca_GridEmissions[t] / 1000), x_purchase[t]);
            }


            /// embodied carbon emissions of all technologies
            /// 
            for(int i=0; i<this.NumberOfSolarAreas; i++)
                carbonEmissions.AddTerm(this.lca_PVEmissions, x_pv[i]);


            /// checking for objectives and cost/carbon constraints
            /// 
            bool isCostMinimization = false;
            if (string.Equals(objective, "cost"))
                isCostMinimization = true;

            bool hasCarbonConstraint = false;
            bool hasCostConstraint = false;
            if (!carbonConstraint.IsNullOrDefault())
                hasCarbonConstraint = true;
            if (!costConstraint.IsNullOrDefault())
                hasCostConstraint = true;


            /// Cost coefficients formulation
            /// 
            ILinearNumExpr opex = cpl.LinearNumExpr();
            ILinearNumExpr capex = cpl.LinearNumExpr();
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                capex.AddTerm(this.c_PV, x_pv[i]);
            for (int t = 0; t < this.Horizon; t++)
            {
                opex.AddTerm(this.c_Grid[t], x_purchase[t]);
                opex.AddTerm(this.c_FeedIn[t], x_feedin[t]);
            }




            /// Objective function
            /// 
            if (isCostMinimization) cpl.AddMinimize(cpl.Sum(opex, capex));
            else cpl.AddMinimize(carbonEmissions);

            // epsilon constraints for carbon, 
            // or cost constraint in case of carbon minimization (the same reason why carbon minimization needs a cost constraint)
            if (hasCarbonConstraint && isCostMinimization) cpl.AddLe(carbonEmissions, (double)carbonConstraint);
            else if (hasCostConstraint && !isCostMinimization) cpl.AddLe(cpl.Sum(opex, capex), (double)costConstraint);


            /// Solve
            /// 
            //cpl.SetOut(null);
            cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.01);

            //if (!this.multithreading)
            //    cpl.SetParam(Cplex.Param.Threads, 1);

            bool success = cpl.Solve();


            /// outputs
            /// 
            EhubOutputs solution = new EhubOutputs();
            return solution;
        }
    }
}
