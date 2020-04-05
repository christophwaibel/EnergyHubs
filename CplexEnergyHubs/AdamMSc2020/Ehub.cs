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
        internal double [] CoolingDemand { get; private set; }
        internal double [] HeatingDemand { get; private set; }
        internal double [] ElectricityDemand { get; private set; }
        internal double [] DHWDemand { get; private set; }
        internal double [][] SolarLoads { get; private set; }
        internal double [] SolarAreas { get; private set; }

        internal double[] CoolingWeights { get; private set; }
        internal double[] HeatingWeights { get; private set; }
        internal double[] ElectricityWeights { get; private set; }
        internal double[] DHWWeights { get; private set; }
        internal double[][] SolarWeights { get; private set; }

        internal int NumberOfSolarAreas { get; private set; }

        internal int Horizon { get; private set; }
        #endregion




        #region inputs cost parameters        
        internal double InterestRate { get; private set; }
        
        internal double CostPV { get; private set; }
        internal double CostBattery { get; private set; }


        internal double AnnuityPV { get; private set; }
        internal double AnnuityBattery { get; private set; }
        
        internal double c_PV { get; private set; }
        internal double c_Battery { get; private set; }

        internal double c_PV_OM { get; private set; }
        internal double c_Battery_OM { get; private set; }

        internal double [] c_Grid { get; private set; }
        internal double [] c_FeedIn { get; private set; }
        #endregion


        #region inputs LCA parameters
        internal double[] lca_GridEmissions { get; private set; }
        internal double lca_PV { get; private set; }
        internal double lca_Battery { get; private set; }
        #endregion


        #region inputs technical parameters
        internal double LifetimePV { get; private set; }
        internal double LifetimeBattery { get; private set; }

        internal double[] AmbientTemperature { get; private set; }

        // constraint coefficients
        internal double[][] a_PVEfficiency { get; private set; }
        internal double b_MaxBattery { get; private set; }      // maximal battery capacity. constraint

        // coefficients
        /// <summary>
        /// Nominal operating cell temperature
        /// </summary>
        internal double pv_NOCT { get; private set; }
        /// <summary>
        /// reference temperature for NOCT
        /// </summary>
        internal double pv_T_aNOCT { get; private set; }
        /// <summary>
        /// Irradiation of NOCT
        /// </summary>
        internal double pv_P_NOCT { get; private set; }
        /// <summary>
        /// temperature coefficient
        /// </summary>
        internal double pv_beta_ref { get; private set; }
        /// <summary>
        /// PV efficiency under NOCT
        /// </summary>
        internal double pv_n_ref { get; private set; }

        // battery
        internal double bat_ch_eff { get; private set; }        // Battery charging efficiency
        internal double bat_disch_eff { get; private set; }     // Battery discharging efficiency
        internal double bat_decay { get; private set; }         // Battery hourly decay
        internal double bat_max_ch { get; private set; }        // Battery max charging rate
        internal double bat_max_disch { get; private set; }     // Battery max discharging rate
        internal double bat_min_state { get; private set; }     // Battery minimum state of charge
        
        
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
            double [] ambientTemperature,
            Dictionary<string, double> technologyParameters)
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
            this.SetParameters(technologyParameters);


        }


        internal void Solve(int epsilonCuts, bool verbose = false)
        {
            double costTolerance = 1;
            double carbonTolerance = 0.01;
            double[] carbonConstraints = new double[epsilonCuts];
            this.Outputs = new EhubOutputs[epsilonCuts + 2];

            // 1. solve for minCarbon, ignoring cost
            EhubOutputs minCarbon = EnergyHub("carbon", null, null, verbose);

            // 2. solve for minCost, using minCarbon value found in 1 (+ small torelance)
            EhubOutputs minCost = EnergyHub("cost", null, null, verbose);

            // 3. solve for minCost, ignoring Carbon (then, solve for minCarbon, using mincost as constraint. check, if it makes a difference in carbon)
            this.Outputs[0] = EnergyHub("cost", minCarbon.carbon + carbonTolerance, null, verbose);
            this.Outputs[epsilonCuts + 1] = EnergyHub("carbon", null, minCost.cost + costTolerance, verbose);
            double carbonInterval = (minCost.carbon - minCarbon.carbon) / (epsilonCuts + 1);

            // 4. make epsilonCuts cuts and solve for each minCost s.t. carbon
            for(int i=0; i<epsilonCuts; i++)
                this.Outputs[i + 1] = EnergyHub("cost", minCarbon.carbon + carbonInterval * (i+1), null, verbose);
            
            // 5. report all values into Outputs
            //  ...already done by this.Outputs
        }


        private void SetParameters(Dictionary<string, double> technologyParameters)
        {
            if (technologyParameters.ContainsKey("InterestRate"))
                this.InterestRate = technologyParameters["InterestRate"];
            else
                this.InterestRate = 0.08;

            // cost
            if (technologyParameters.ContainsKey("CostPV"))
                this.CostPV = technologyParameters["CostPV"];
            else
                this.CostPV = 250.0;
            if (technologyParameters.ContainsKey("CostBattery"))
                this.CostBattery = technologyParameters["CostBattery"];
            else
                this.CostBattery = 600.0;

            // Operation and Maintenance cost
            if (technologyParameters.ContainsKey("c_PV_OM"))
                this.c_PV_OM = technologyParameters["c_PV_OM"];
            else
                this.c_PV_OM = 0.0;
            if (technologyParameters.ContainsKey("c_Battery_OM"))
                this.c_Battery_OM = technologyParameters["c_Battery_OM"];
            else
                this.c_Battery_OM = 0.0;


            // lifetime
            if (technologyParameters.ContainsKey("LifetimePV"))
                this.LifetimePV = technologyParameters["LifetimePV"];
            else
                this.LifetimePV = 20.0;
            
            if (technologyParameters.ContainsKey("LifetimeBattery"))
                this.LifetimeBattery = technologyParameters["LifetimeBattery"];
            else
                this.LifetimeBattery = 20.0;


            // Tech PV
            if (technologyParameters.ContainsKey("pv_NOCT"))
                this.pv_NOCT = technologyParameters["pv_NOCT"];
            else
                this.pv_NOCT = 45.0;

            if (technologyParameters.ContainsKey("pv_T_aNOCT"))
                this.pv_T_aNOCT = technologyParameters["pv_T_aNOCT"];
            else
                this.pv_T_aNOCT = 20.0;

            if (technologyParameters.ContainsKey("pv_P_NOCT"))
                this.pv_P_NOCT = technologyParameters["pv_P_NOCT"];
            else
                this.pv_P_NOCT = 800.0;

            if (technologyParameters.ContainsKey("pv_beta_ref"))
                this.pv_beta_ref = technologyParameters["pv_beta_ref"];
            else
                this.pv_beta_ref = 0.004;

            if (technologyParameters.ContainsKey("pv_n_ref"))
                this.pv_n_ref = technologyParameters["pv_n_ref"];
            else
                this.pv_n_ref = 0.2;


            // Tech Battery
            if (technologyParameters.ContainsKey("b_MaxBattery"))
                this.b_MaxBattery = technologyParameters["b_MaxBattery"];
            else
                this.b_MaxBattery = 800.0; // Tesla car has 80 kWh

            if (technologyParameters.ContainsKey("bat_ch_eff"))
                this.bat_ch_eff = technologyParameters["bat_ch_eff"];
            else
                bat_ch_eff = 0.92;

            if (technologyParameters.ContainsKey("bat_disch_eff"))
                this.bat_disch_eff = technologyParameters["bat_disch_eff"];
            else
                bat_disch_eff = 0.92;

            if (technologyParameters.ContainsKey("bat_decay"))
                this.bat_decay = technologyParameters["bat_decay"];
            else
                this.bat_decay = 0.001;

            if (technologyParameters.ContainsKey("bat_max_ch"))
                this.bat_max_ch = technologyParameters["bat_max_ch"];
            else
                this.bat_max_ch = 0.3;

            if (technologyParameters.ContainsKey("bat_max_disch"))
                this.bat_max_disch = technologyParameters["bat_max_disch"];
            else
                this.bat_max_disch = 0.33;

            if (technologyParameters.ContainsKey("bat_min_state"))
                this.bat_min_state = technologyParameters["bat_min_state"];
            else
                this.bat_min_state = 0.3;


            // LCA
            if (technologyParameters.ContainsKey("lca_PV"))
                this.lca_PV = technologyParameters["lca_PV"];
            else
                this.lca_PV = 0.0;
            if (technologyParameters.ContainsKey("lca_PV"))
                this.lca_PV = technologyParameters["lca_PV"];
            else
                this.lca_Battery = 0.0;



            this.lca_GridEmissions = new double[this.Horizon];
            this.c_FeedIn = new double[this.Horizon];
            this.c_Grid = new double[this.Horizon];

            for (int t = 0; t < this.Horizon; t++)
            {
                this.c_FeedIn[t] = -0.15;
                this.c_Grid[t] = 0.15;
                this.lca_GridEmissions[t] = 0.49;
            }



            this.AnnuityPV = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimePV)))));
            this.AnnuityBattery = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeBattery)))));
            this.c_PV = this.CostPV * this.AnnuityPV;
            this.c_Battery = this.CostBattery * this.AnnuityBattery;

            this.a_PVEfficiency = new double[this.NumberOfSolarAreas][];
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                this.a_PVEfficiency[i] = EhubMisc.TechnologyEfficiencies.CalculateEfficiencyPhotovoltaic(AmbientTemperature, this.SolarLoads[i],
                    this.pv_NOCT, this.pv_T_aNOCT, this.pv_P_NOCT, this.pv_beta_ref, this.pv_n_ref);
        }


        private EhubOutputs EnergyHub(string objective = "cost", double? carbonConstraint = null, double? costConstraint = null, bool verbose = false)
        {
            /// _________________________________________________________________________
            /// ////////////////////////////////////////////////////////////////////////
            // Initialize solver and other
            Cplex cpl = new Cplex();


            /// _________________________________________________________________________
            /// ////////////////////////////////////////////////////////////////////////
            /// Variables
            
            // PV
            INumVar[] x_PV = new INumVar[this.NumberOfSolarAreas];
            ILinearNumExpr[] x_PV_production = new ILinearNumExpr[this.Horizon];  // dummy expression to store total PV electricity production
            ILinearNumExpr[] x_PV_productionScaled = new ILinearNumExpr[this.Horizon];  // dummy expression to store total PV electricity production. scaled with individual weights
            double OM_PV = 0.0; // operation maintanence for PV

            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                x_PV[i] = cpl.NumVar(0, this.SolarAreas[i]);

            INumVar[] y = new INumVar[this.Horizon];    // binary to indicate if PV is used (=1). no selling and purchasing from the grid at the same time allowed
            INumVar[] x_Purchase = new INumVar[this.Horizon];
            INumVar[] x_FeedIn = new INumVar[this.Horizon];

            // Battery
            INumVar x_Battery = cpl.NumVar(0.0, this.b_MaxBattery);     // kWh
            INumVar[] x_BatteryCharge = new INumVar[this.Horizon];      // kW
            INumVar[] x_BatteryDischarge = new INumVar[this.Horizon];   // kW
            INumVar[] x_BatteryStored = new INumVar[this.Horizon];      // kW

            for (int t = 0; t < this.Horizon; t++)
            {
                y[t] = cpl.BoolVar();
                x_Purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_FeedIn[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_PV_production[t] = cpl.LinearNumExpr();
                x_PV_productionScaled[t] = cpl.LinearNumExpr();

                x_BatteryCharge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_BatteryDischarge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_BatteryStored[t] = cpl.NumVar(0.0, System.Double.MaxValue);
            }


            /// _________________________________________________________________________
            /// ////////////////////////////////////////////////////////////////////////
            /// Constraints
            ILinearNumExpr carbonEmissions = cpl.LinearNumExpr();
            for(int t=0; t<this.Horizon; t++)
            {
                // elec demand must be met by PV production, battery and grid, minus feed in
                ILinearNumExpr elecGeneration = cpl.LinearNumExpr();
                ILinearNumExpr elecAdditionalDemand = cpl.LinearNumExpr();
                for (int i = 0; i < this.NumberOfSolarAreas; i++)
                {
                    double pvElec = this.SolarLoads[i][t] * this.SolarWeights[i][t] * 0.001 * a_PVEfficiency[i][t];
                    elecGeneration.AddTerm(pvElec, x_PV[i]);
                    x_PV_production[t].AddTerm(this.SolarLoads[i][t] * 0.001 * a_PVEfficiency[i][t], x_PV[i]);
                    x_PV_productionScaled[t].AddTerm(pvElec, x_PV[i]);
                    OM_PV += pvElec * this.c_PV_OM;
                }
                elecGeneration.AddTerm(1, x_Purchase[t]);
                elecGeneration.AddTerm(1, x_BatteryDischarge[t]);
                elecAdditionalDemand.AddTerm(1, x_FeedIn[t]);
                elecAdditionalDemand.AddTerm(1, x_BatteryCharge[t]);
                cpl.AddGe(cpl.Diff(elecGeneration, elecAdditionalDemand), this.ElectricityDemand[t] * this.ElectricityWeights[t]);

                // pv production must be greater equal feedin
                cpl.AddGe(x_PV_productionScaled[t], x_FeedIn[t]);

                // donnot allow feedin and purchase at the same time. y = 1 means elec is produced
                cpl.AddLe(x_Purchase[t], cpl.Prod(M, y[t]));    
                cpl.AddLe(x_FeedIn[t], cpl.Prod(M, cpl.Diff(1, y[t])));

                // co2 emissions from grid
                carbonEmissions.AddTerm((this.lca_GridEmissions[t] / 1000), x_Purchase[t]);
            }

            // battery model
            for (int t=0; t<this.Horizon-1; t++)
            {
                ILinearNumExpr batteryState = cpl.LinearNumExpr();
                batteryState.AddTerm((1 - this.bat_decay), x_BatteryStored[t]);
                batteryState.AddTerm(this.bat_ch_eff, x_BatteryCharge[t]);
                batteryState.AddTerm(-1 / this.bat_disch_eff, x_BatteryDischarge[t]);
                cpl.AddEq(x_BatteryStored[t + 1], batteryState);
            }
            cpl.AddGe(x_BatteryStored[0], cpl.Prod(x_Battery, this.bat_min_state)); // initial state of battery >= min_state
            cpl.AddEq(x_BatteryStored[0], cpl.Diff(x_BatteryStored[this.Horizon - 1], x_BatteryDischarge[this.Horizon - 1])); // initial state equals the last state minis discharge at last timestep
            cpl.AddEq(x_BatteryDischarge[0], 0);        // no discharge at t=0

            for (int t=0; t<this.Horizon; t++)
            {
                cpl.AddGe(x_BatteryStored[t], cpl.Prod(x_Battery, this.bat_min_state));     // min state of charge
                cpl.AddLe(x_BatteryCharge[t], cpl.Prod(x_Battery, this.bat_max_ch));        // battery charging
                cpl.AddLe(x_BatteryDischarge[t], cpl.Prod(x_Battery, this.bat_max_disch));  // battery discharging
                cpl.AddLe(x_BatteryStored[t], x_Battery);                                   // battery sizing
            }


            /// _________________________________________________________________________
            /// ////////////////////////////////////////////////////////////////////////
            /// embodied carbon emissions of all technologies
            for (int i=0; i<this.NumberOfSolarAreas; i++)
                carbonEmissions.AddTerm(this.lca_PV, x_PV[i]);
            carbonEmissions.AddTerm(this.lca_Battery, x_Battery);

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


            /// _________________________________________________________________________
            /// ////////////////////////////////////////////////////////////////////////
            /// Cost coefficients formulation
            ILinearNumExpr opex = cpl.LinearNumExpr();
            ILinearNumExpr capex = cpl.LinearNumExpr();
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                capex.AddTerm(this.c_PV, x_PV[i]);
            capex.AddTerm(this.c_Battery, x_Battery);

            for (int t = 0; t < this.Horizon; t++)
            {
                opex.AddTerm(this.c_Grid[t], x_Purchase[t]);
                opex.AddTerm(this.c_FeedIn[t], x_FeedIn[t]);

                opex.AddTerm(this.c_Battery_OM, x_BatteryDischarge[t]);    // assuming discharging is causing deterioration
            }


            /// _________________________________________________________________________
            /// ////////////////////////////////////////////////////////////////////////
            /// Objective function
            if (isCostMinimization) cpl.AddMinimize(cpl.Sum(capex, cpl.Sum(OM_PV, opex)));
            else cpl.AddMinimize(carbonEmissions);

            // epsilon constraints for carbon, 
            // or cost constraint in case of carbon minimization (the same reason why carbon minimization needs a cost constraint)
            if (hasCarbonConstraint && isCostMinimization) cpl.AddLe(carbonEmissions, (double)carbonConstraint);
            else if (hasCostConstraint && !isCostMinimization) cpl.AddLe(cpl.Sum(opex, capex), (double)costConstraint);


            /// _________________________________________________________________________
            /// ////////////////////////////////////////////////////////////////////////
            /// Solve
            if (!verbose) cpl.SetOut(null);
            cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.01);

            //if (!this.multithreading)
            //    cpl.SetParam(Cplex.Param.Threads, 1);

            bool success = cpl.Solve();


            /// outputs
            /// 
            EhubOutputs solution = new EhubOutputs();
            if (!success) return solution;
            
            solution.carbon = cpl.GetValue(carbonEmissions);
            solution.OPEX = cpl.GetValue(opex) + OM_PV;
            solution.CAPEX = cpl.GetValue(capex);
            solution.cost = solution.OPEX + solution.CAPEX;

            solution.x_pv = new double[this.NumberOfSolarAreas];
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                solution.x_pv[i] = cpl.GetValue(x_PV[i]);
            solution.x_bat = cpl.GetValue(x_Battery);

            solution.b_pvprod = new double[this.Horizon];
            solution.b_pvprodUnscaled = new double[this.Horizon];
            solution.x_batcharge = new double[this.Horizon];
            solution.x_batdischarge = new double[this.Horizon];
            solution.x_batsoc = new double[this.Horizon];
            solution.x_elecpur = new double[this.Horizon];
            solution.x_feedin = new double[this.Horizon];
            for (int t = 0; t < this.Horizon; t++)
            {
                solution.b_pvprod[t] = cpl.GetValue(x_PV_productionScaled[t]);
                solution.b_pvprodUnscaled[t] = cpl.GetValue(x_PV_production[t]);
                solution.x_batcharge[t] = cpl.GetValue(x_BatteryCharge[t]);
                solution.x_batdischarge[t] = cpl.GetValue(x_BatteryDischarge[t]);
                solution.x_batsoc[t] = cpl.GetValue(x_BatteryStored[t]);
                solution.x_elecpur[t] = cpl.GetValue(x_Purchase[t]);
                solution.x_feedin[t] = cpl.GetValue(x_FeedIn[t]);
            }
            return solution;
        }
    }
}
