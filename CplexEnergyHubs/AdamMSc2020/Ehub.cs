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
        internal EhubOutputs[] Outputs;



        #region inputs demand and typical days
        /// ////////////////////////////////////////////////////////////////////////
        /// Demand (might be typical days) and scaling factors (a.k.a. weights)
        /// ////////////////////////////////////////////////////////////////////////
        internal double[] CoolingDemand { get; private set; }
        internal double[] HeatingDemand { get; private set; }
        internal double[] ElectricityDemand { get; private set; }
        internal double[][] SolarLoads { get; private set; }
        internal double[] SolarAreas { get; private set; }

        internal int[] ClustersizePerTimestep { get; private set; }
        
        internal int NumberOfSolarAreas { get; private set; }

        internal int Horizon { get; private set; }
        #endregion



        #region inputs technical parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// Technical Parameters
        /// ////////////////////////////////////////////////////////////////////////
        internal double[] AmbientTemperature { get; private set; }

        // Lifetime
        internal double LifetimePV { get; private set; }
        internal double LifetimeBattery { get; private set; }
        internal double LifetimeTES { get; private set; }
        internal double LifetimeASHP { get; private set; }
        internal double LifetimeCHP { get; private set; }
        internal double LifetimeBoiler { get; private set; }
        internal double LifetimeAirCon { get; private set; }
        
        // Coefficients PV
        internal double pv_NOCT { get; private set; }
        internal double pv_T_aNOCT { get; private set; }
        internal double pv_P_NOCT { get; private set; }
        internal double pv_beta_ref { get; private set; }
        internal double pv_n_ref { get; private set; }
        internal double[][] a_PV_Efficiency { get; private set; }

        // Coefficients ASHP
        internal double hp_pi1 { get; private set; }
        internal double hp_pi2 { get; private set; }
        internal double hp_pi3 { get; private set; }
        internal double hp_pi4 { get; private set; }
        internal double hp_supplyTemp { get; private set; }
        internal double[] a_ASHP_Efficiency { get; private set; }

        // Coefficients natural gas boiler
        internal double a_boi_eff { get; private set; }

        // Coefficients CHP
        internal double c_chp_eff { get; private set; }
        internal double c_chp_htp { get; private set; }         // heat to power ratio (for 1 kW of heat, 1.73 kW of electricity is produced)
        internal double c_chp_heatdump { get; private set; }    // heat dump allowed = 1

        // Coefficients AirCon
        internal double[] a_AirCon_Efficiency { get; private set; }

        // Coefficients Battery
        internal double bat_ch_eff { get; private set; }        // Battery charging efficiency
        internal double bat_disch_eff { get; private set; }     // Battery discharging efficiency
        internal double bat_decay { get; private set; }         // Battery hourly decay
        internal double bat_max_ch { get; private set; }        // Battery max charging rate
        internal double bat_max_disch { get; private set; }     // Battery max discharging rate
        internal double bat_min_state { get; private set; }     // Battery minimum state of charge
        internal double b_MaxBattery { get; private set; }      // maximal battery capacity. constraint    

        // Coefficients Thermal Energy Storage
        internal double tes_ch_eff { get; private set; }
        internal double tes_disch_eff { get; private set; }
        internal double tes_decay { get; private set; }
        internal double tes_max_ch { get; private set; }
        internal double tes_max_disch { get; private set; }
        internal double b_MaxTES { get; private set; }

        #endregion



        #region inputs LCA parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// LCA
        /// ////////////////////////////////////////////////////////////////////////
        internal double lca_GridElectricity { get; private set; }
        internal double lca_NaturalGas { get; private set; }
        internal double lca_PV { get; private set; }
        internal double lca_Battery { get; private set; }
        internal double lca_TES { get; private set; }
        internal double lca_ASHP { get; private set; }
        internal double lca_CHP { get; private set; }
        internal double lca_Boiler { get; private set; }
        internal double lca_AirCon { get; private set; }
        #endregion



        #region inputs cost parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// Cost Parameters
        /// ////////////////////////////////////////////////////////////////////////
        internal double InterestRate { get; private set; }
        internal double c_NaturalGas { get; private set; }

        // Investment Cost
        internal double CostPV { get; private set; }
        internal double CostBattery { get; private set; }
        internal double CostTES { get; private set; }
        internal double CostBoiler { get; private set; }
        internal double CostCHP { get; private set; }
        internal double CostAirCon { get; private set; }
        internal double CostASHP { get; private set; }

        // Annuity
        internal double AnnuityPV { get; private set; }
        internal double AnnuityBattery { get; private set; }
        internal double AnnuityTES { get; private set; }
        internal double AnnuityBoiler { get; private set; }
        internal double AnnuityCHP { get; private set; }
        internal double AnnuityAirCon { get; private set; }
        internal double AnnuityASHP { get; private set; }

        // levelized investment cost
        internal double c_PV { get; private set; }
        internal double c_Battery { get; private set; }
        internal double c_TES { get; private set; }
        internal double c_Boiler { get; private set; }
        internal double c_CHP { get; private set; }
        internal double c_AirCon { get; private set; }
        internal double c_ASHP { get; private set; }

        // operation and maintenance cost
        internal double c_PV_OM { get; private set; }
        internal double c_Battery_OM { get; private set; }
        internal double c_TES_OM { get; private set; }
        internal double c_Boiler_OM { get; private set; }
        internal double c_CHP_OM { get; private set; }
        internal double c_AirCon_OM { get; private set; }
        internal double c_ASHP_OM { get; private set; }

        // time resolved operation cost
        internal double[] c_Grid { get; private set; }
        internal double[] c_FeedIn { get; private set; }
        #endregion



        #region MILP stuff
        /// ////////////////////////////////////////////////////////////////////////
        /// MILP
        /// ////////////////////////////////////////////////////////////////////////
        private const double M = 9999999;   // Big M method
        #endregion



        /// <summary>
        /// always hourly! I.e. it assumes the demand arrays are of length days x 24
        /// </summary>
        /// <param name="heatingDemand"></param>
        /// <param name="coolingDemand"></param>
        /// <param name="electricityDemand"></param>
        /// <param name="irradiance"></param>
        /// <param name="solarTechSurfaceAreas"></param>
        /// <param name="weightsOfLoads">If typical days are used, these weights are used to account for how many days a typical day represents</param>
        internal Ehub(double [] heatingDemand, double[] coolingDemand, double[] electricityDemand,
            double[][] irradiance, double [] solarTechSurfaceAreas,
            double [] ambientTemperature, Dictionary<string, double> technologyParameters,
            int [] clustersizePerTimestep)
        {
            this.CoolingDemand = coolingDemand;
            this.HeatingDemand = heatingDemand;
            this.ElectricityDemand = electricityDemand;
            this.SolarLoads = irradiance;
            this.SolarAreas = solarTechSurfaceAreas;

            this.NumberOfSolarAreas = solarTechSurfaceAreas.Length;
            this.ClustersizePerTimestep = clustersizePerTimestep;

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
            /// ////////////////////////////////////////////////////////////////////////
            /// Technical Parameters
            /// ////////////////////////////////////////////////////////////////////////

            // PV
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

            // ASHP
            if (technologyParameters.ContainsKey("hp_pi1"))
                this.hp_pi1 = technologyParameters["hp_pi1"];
            else
                this.hp_pi1 = 13.39;
            if (technologyParameters.ContainsKey("hp_pi2"))
                this.hp_pi2 = technologyParameters["hp_pi2"];
            else
                this.hp_pi2 = -0.047;
            if (technologyParameters.ContainsKey("hp_pi3"))
                this.hp_pi3 = technologyParameters["hp_pi3"];
            else
                this.hp_pi3 = 1.109;
            if (technologyParameters.ContainsKey("hp_pi4"))
                this.hp_pi4 = technologyParameters["hp_pi4"];
            else
                this.hp_pi4 = 0.012;
            if (technologyParameters.ContainsKey("hp_supplyTemp"))
                this.hp_supplyTemp = technologyParameters["hp_supplyTemp"];
            else
                this.hp_supplyTemp = 65.0;

            // Naural Gas Boiler
            if (technologyParameters.ContainsKey("a_boi_eff"))
                this.a_boi_eff = technologyParameters["a_boi_eff"];
            else
                this.a_boi_eff = 0.94;

            // CHP
            if (technologyParameters.ContainsKey("c_chp_eff"))
                this.c_chp_eff = technologyParameters["c_chp_eff"];
            else
                this.c_chp_eff = 0.3;
            if (technologyParameters.ContainsKey("c_chp_htp"))
                this.c_chp_htp = technologyParameters["c_chp_htp"];
            else
                this.c_chp_htp = 1.73;
            if (technologyParameters.ContainsKey("c_chp_heatdump"))
                this.c_chp_heatdump = technologyParameters["c_chp_heatdump"];
            else
                this.c_chp_heatdump = 1;

            // Battery
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

            // TES
            if (technologyParameters.ContainsKey("b_MaxTES"))
                this.b_MaxTES = technologyParameters["b_MaxTES"];
            else
                this.b_MaxTES = 1400.0;
            if (technologyParameters.ContainsKey("tes_ch_eff"))
                this.tes_ch_eff = technologyParameters["tes_ch_eff"];
            else
                this.tes_ch_eff = 0.9;
            if (technologyParameters.ContainsKey("tes_disch_eff"))
                this.tes_disch_eff = technologyParameters["tes_disch_eff"];
            else
                this.tes_disch_eff = 0.9;
            if (technologyParameters.ContainsKey("tes_decay"))
                this.tes_decay = technologyParameters["tes_decay"];
            else
                this.tes_decay = 0.001;
            if (technologyParameters.ContainsKey("tes_max_ch"))
                this.tes_max_ch = technologyParameters["tes_max_ch"];
            else
                this.tes_max_ch = 0.25;
            if (technologyParameters.ContainsKey("tes_max_disch"))
                this.tes_max_disch = technologyParameters["tes_max_disch"];
            else
                this.tes_max_disch = 0.25;



            /// ////////////////////////////////////////////////////////////////////////
            /// LCA
            /// ////////////////////////////////////////////////////////////////////////
            if (technologyParameters.ContainsKey("lca_GridElectricity"))
                this.lca_GridElectricity = technologyParameters["lca_GridElectricity"];
            else
                this.lca_GridElectricity = 0.14840; // from Wu et al. 2017
            if (technologyParameters.ContainsKey("lca_NaturalGas"))
                this.lca_NaturalGas = technologyParameters["lca_NaturalGas"];
            else
                this.lca_NaturalGas = 0.237;        // from Waibel 2019 co-simu paper
            if (technologyParameters.ContainsKey("lca_PV"))
                this.lca_PV = technologyParameters["lca_PV"];
            else
                this.lca_PV = 0.0;
            if (technologyParameters.ContainsKey("lca_Battery"))
                this.lca_Battery = technologyParameters["lca_Battery"];
            else
                this.lca_Battery = 0.0;
            if (technologyParameters.ContainsKey("lca_TES"))
                this.lca_TES = technologyParameters["lca_TES"];
            else
                this.lca_TES = 0.0;
            if (technologyParameters.ContainsKey("lca_ASHP"))
                this.lca_ASHP = technologyParameters["lca_ASHP"];
            else
                this.lca_ASHP = 0.0;
            if (technologyParameters.ContainsKey("lca_CHP"))
                this.lca_CHP = technologyParameters["lca_CHP"];
            else
                this.lca_CHP = 0.0;
            if (technologyParameters.ContainsKey("lca_Boiler"))
                this.lca_Boiler = technologyParameters["lca_Boiler"];
            else
                this.lca_Boiler = 0.0;
            if (technologyParameters.ContainsKey("lca_AirCon"))
                this.lca_AirCon = technologyParameters["lca_AirCon"];
            else
                this.lca_AirCon = 0.0;



            /// ////////////////////////////////////////////////////////////////////////
            /// Cost
            /// ////////////////////////////////////////////////////////////////////////
            if (technologyParameters.ContainsKey("InterestRate"))
                this.InterestRate = technologyParameters["InterestRate"];
            else
                this.InterestRate = 0.08;
            if (technologyParameters.ContainsKey("c_NaturalGas"))
                this.c_NaturalGas = technologyParameters["c_NaturalGas"];
            else
                this.c_NaturalGas = 0.09;


            this.c_FeedIn = new double[this.Horizon];
            this.c_Grid = new double[this.Horizon];

            for (int t = 0; t < this.Horizon; t++)  // Wu et al 2017
            {
                this.c_FeedIn[t] = -0.15;
                this.c_Grid[t] = 0.2;            
            }

            // Investment Cost
            if (technologyParameters.ContainsKey("CostPV"))
                this.CostPV = technologyParameters["CostPV"];
            else
                this.CostPV = 250.0;
            if (technologyParameters.ContainsKey("CostBattery"))
                this.CostBattery = technologyParameters["CostBattery"];
            else
                this.CostBattery = 600.0;
            if (technologyParameters.ContainsKey("CostTES"))
                this.CostTES = technologyParameters["CostTES"];
            else
                this.CostTES = 100.0;
            if (technologyParameters.ContainsKey("CostBoiler"))
                this.CostBoiler = technologyParameters["CostBoiler"];
            else
                this.CostBoiler = 200.0;
            if (technologyParameters.ContainsKey("CostCHP"))
                this.CostCHP = technologyParameters["CostCHP"];
            else
                this.CostCHP = 1500.0;
            if (technologyParameters.ContainsKey("CostAirCon"))
                this.CostAirCon = technologyParameters["CostAirCon"];
            else
                this.CostAirCon = 360.0;
            if (technologyParameters.ContainsKey("CostASHP"))
                this.CostASHP = technologyParameters["CostASHP"];
            else
                this.CostASHP = 1000.0;

            // Operation and Maintenance cost
            if (technologyParameters.ContainsKey("c_PV_OM"))
                this.c_PV_OM = technologyParameters["c_PV_OM"];
            else
                this.c_PV_OM = 0.0;
            if (technologyParameters.ContainsKey("c_Battery_OM"))
                this.c_Battery_OM = technologyParameters["c_Battery_OM"];
            else
                this.c_Battery_OM = 0.0;
            if (technologyParameters.ContainsKey("c_TES_OM"))
                this.c_TES_OM = technologyParameters["c_TES_OM"];
            else
                this.c_TES_OM = 0.0;
            if (technologyParameters.ContainsKey("c_Boiler_OM"))
                this.c_Boiler_OM = technologyParameters["c_Boiler_OM"];
            else
                this.c_Boiler_OM = 0.01;    // Waibel et al 2017
            if (technologyParameters.ContainsKey("c_CHP_OM"))
                this.c_CHP_OM = technologyParameters["c_CHP_OM"];
            else
                this.c_CHP_OM = 0.021;    // Waibel et al 2017
            if (technologyParameters.ContainsKey("c_AirCon_OM"))
                this.c_AirCon_OM = technologyParameters["c_AirCon_OM"];
            else
                this.c_AirCon_OM = 0.1;
            if (technologyParameters.ContainsKey("c_ASHP_OM"))
                this.c_ASHP_OM = technologyParameters["c_ASHP_OM"];
            else
                this.c_ASHP_OM = 0.1;    // Waibel et al 2017



            // lifetime
            if (technologyParameters.ContainsKey("LifetimePV"))
                this.LifetimePV = technologyParameters["LifetimePV"];
            else
                this.LifetimePV = 20.0;
            if (technologyParameters.ContainsKey("LifetimeBattery"))
                this.LifetimeBattery = technologyParameters["LifetimeBattery"];
            else
                this.LifetimeBattery = 20.0;
            if (technologyParameters.ContainsKey("LifetimeTES"))
                this.LifetimeTES = technologyParameters["LifetimeTES"];
            else
                this.LifetimeTES = 17.0;
            if (technologyParameters.ContainsKey("LifetimeASHP"))
                this.LifetimeASHP = technologyParameters["LifetimeASHP"];
            else
                this.LifetimeASHP = 20.0;
            if (technologyParameters.ContainsKey("LifeetimeCHP"))
                this.LifetimeCHP = technologyParameters["LifetimeCHP"];
            else
                this.LifetimeCHP = 20.0;
            if (technologyParameters.ContainsKey("LifetimeBoiler"))
                this.LifetimeBoiler = technologyParameters["LifetimeBoiler"];
            else
                this.LifetimeBoiler = 30.0;
            if (technologyParameters.ContainsKey("LifetimeAirCon"))
                this.LifetimeAirCon = technologyParameters["LifetimeAirCon"];
            else
                this.LifetimeAirCon = 20.0;

            // Annuity
            this.AnnuityPV = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimePV)))));
            this.AnnuityBattery = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeBattery)))));
            this.AnnuityTES = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeTES)))));
            this.AnnuityASHP = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeASHP)))));
            this.AnnuityCHP = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeCHP)))));
            this.AnnuityBoiler = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeBoiler)))));
            this.AnnuityAirCon = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeAirCon)))));

            // Levelized cost
            this.c_PV = this.CostPV * this.AnnuityPV;
            this.c_Battery = this.CostBattery * this.AnnuityBattery;
            this.c_TES = this.CostTES * this.AnnuityTES;
            this.c_ASHP = this.CostASHP * this.AnnuityASHP;
            this.c_CHP = this.CostCHP * this.AnnuityCHP;
            this.c_Boiler = this.CostBoiler * this.AnnuityBoiler;
            this.c_AirCon = this.CostAirCon * this.AnnuityAirCon;


            // PV efficiency
            this.a_PV_Efficiency = new double[this.NumberOfSolarAreas][];
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                this.a_PV_Efficiency[i] = EhubMisc.TechnologyEfficiencies.CalculateEfficiencyPhotovoltaic(AmbientTemperature, this.SolarLoads[i],
                    this.pv_NOCT, this.pv_T_aNOCT, this.pv_P_NOCT, this.pv_beta_ref, this.pv_n_ref);

            this.a_ASHP_Efficiency = EhubMisc.TechnologyEfficiencies.CalculateCOPHeatPump(this.AmbientTemperature, this.hp_supplyTemp, this.hp_pi1, this.hp_pi2, this.hp_pi3, this.hp_pi4);
            this.a_AirCon_Efficiency = EhubMisc.TechnologyEfficiencies.CalculateCOPAirCon(this.AmbientTemperature);
        }


        private EhubOutputs EnergyHub(string objective = "cost", double? carbonConstraint = null, double? costConstraint = null, bool verbose = false)
        {
            Cplex cpl = new Cplex();


            /// ////////////////////////////////////////////////////////////////////////
            /// Variables
            /// ////////////////////////////////////////////////////////////////////////

            // grid
            INumVar[] x_Purchase = new INumVar[this.Horizon];
            INumVar[] x_FeedIn = new INumVar[this.Horizon];

            // PV
            INumVar[] x_PV = new INumVar[this.NumberOfSolarAreas];
            ILinearNumExpr[] x_PV_production = new ILinearNumExpr[Horizon];  
            double OM_PV = 0.0; // operation maintanence for PV
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                x_PV[i] = cpl.NumVar(0, this.SolarAreas[i]);
            INumVar[] y_PV = new INumVar[this.Horizon];    // binary to indicate if PV is used (=1). no selling and purchasing from the grid at the same time allowed
            
            // AirCon
            INumVar x_AirCon = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_AirCon_op = new INumVar[this.Horizon];

            // Boiler
            INumVar x_Boiler = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_Boiler_op = new INumVar[this.Horizon];

            // CHP
            INumVar x_CHP = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_CHP_op_e = new INumVar[this.Horizon];
            INumVar[] x_CHP_op_th = new INumVar[this.Horizon];
            INumVar[] x_CHP_op_dump = new INumVar[this.Horizon];

            // ASHP
            INumVar x_ASHP = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_ASHP_op = new INumVar[this.Horizon];

            // Battery
            INumVar x_Battery = cpl.NumVar(0.0, this.b_MaxBattery);     // kWh
            INumVar[] x_Battery_charge = new INumVar[this.Horizon];     // kW
            INumVar[] x_Battery_discharge = new INumVar[this.Horizon];  // kW
            INumVar[] x_Battery_soc = new INumVar[this.Horizon];        // kWh

            // TES
            INumVar x_TES = cpl.NumVar(0.0, this.b_MaxTES);             // kWh
            INumVar[] x_TES_charge = new INumVar[this.Horizon];         // kW
            INumVar[] x_TES_discharge = new INumVar[this.Horizon];      // kW
            INumVar[] x_TES_soc = new INumVar[this.Horizon];            // kWh
            INumVar[] y_TES = new INumVar[this.Horizon];

            for (int t = 0; t < this.Horizon; t++)
            {
                y_PV[t] = cpl.BoolVar();
                x_Purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_FeedIn[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_PV_production[t] = cpl.LinearNumExpr();

                x_CHP_op_e[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_CHP_op_th[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_CHP_op_dump[t] = cpl.NumVar(0.0, System.Double.MaxValue);

                x_AirCon_op[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_Boiler_op[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_ASHP_op[t] = cpl.NumVar(0.0, System.Double.MaxValue);

                x_Battery_charge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_Battery_discharge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_Battery_soc[t] = cpl.NumVar(0.0, System.Double.MaxValue);

                x_TES_charge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_TES_discharge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_TES_soc[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                y_TES[t] = cpl.BoolVar();
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// Constraints
            /// ////////////////////////////////////////////////////////////////////////
            /// 

            // meeting demands
            ILinearNumExpr carbonEmissions = cpl.LinearNumExpr();
            for(int t=0; t<this.Horizon; t++)
            {
                ILinearNumExpr elecGeneration = cpl.LinearNumExpr();
                ILinearNumExpr elecAdditionalDemand = cpl.LinearNumExpr();
                ILinearNumExpr thermalGeneration = cpl.LinearNumExpr();
                ILinearNumExpr thermalAdditionalDemand = cpl.LinearNumExpr();

                /// ////////////////////////////////////////////////////////////////////////
                /// Cooling
                elecAdditionalDemand.AddTerm(1 / this.a_AirCon_Efficiency[t], x_AirCon_op[t]);


                /// ////////////////////////////////////////////////////////////////////////
                /// Heating
                thermalGeneration.AddTerm(1, x_Boiler_op[t]);
                thermalGeneration.AddTerm(1, x_CHP_op_th[t]);
                thermalGeneration.AddTerm(1, x_ASHP_op[t]);
                thermalGeneration.AddTerm(1, x_TES_discharge[t]);
                elecAdditionalDemand.AddTerm(1 / this.a_ASHP_Efficiency[t], x_ASHP_op[t]);
                thermalAdditionalDemand.AddTerm(1, x_TES_charge[t]);
                thermalAdditionalDemand.AddTerm(1, x_CHP_op_dump[t]);


                /// ////////////////////////////////////////////////////////////////////////
                /// Electricity
                // elec demand must be met by PV production, battery and grid, minus feed in
                for (int i = 0; i < this.NumberOfSolarAreas; i++)
                {
                    double pvElec = this.SolarLoads[i][t]  * 0.001 * this.a_PV_Efficiency[i][t];
                    elecGeneration.AddTerm(pvElec, x_PV[i]);
                    x_PV_production[t].AddTerm(pvElec, x_PV[i]);
                    OM_PV += pvElec * this.c_PV_OM;
                }
                elecGeneration.AddTerm(1, x_Purchase[t]);
                elecGeneration.AddTerm(1, x_Battery_discharge[t]);
                elecGeneration.AddTerm(this.c_chp_htp, x_CHP_op_e[t]);
                elecAdditionalDemand.AddTerm(1, x_FeedIn[t]);
                elecAdditionalDemand.AddTerm(1, x_Battery_charge[t]);


                /// ////////////////////////////////////////////////////////////////////////
                /// PV Technical Constraints
                // pv production must be greater equal feedin
                cpl.AddGe(x_PV_production[t], x_FeedIn[t]);
                // donnot allow feedin and purchase at the same time. y = 1 means elec is produced
                cpl.AddLe(x_Purchase[t], cpl.Prod(M, y_PV[t]));    
                cpl.AddLe(x_FeedIn[t], cpl.Prod(M, cpl.Diff(1, y_PV[t])));


                /// ////////////////////////////////////////////////////////////////////////
                /// CHP Technical Constraints
                // heat recovery and heat dump from CHP is equal to electricity generation by CHP times heat to power ratio
                ILinearNumExpr chpheatrecov = cpl.LinearNumExpr();
                ILinearNumExpr chpheatfromelec = cpl.LinearNumExpr();
                chpheatrecov.AddTerm(1, x_CHP_op_th[t]);
                chpheatrecov.AddTerm(1, x_CHP_op_dump[t]);
                chpheatfromelec.AddTerm(this.c_chp_htp, x_CHP_op_e[t]);
                cpl.AddEq(chpheatrecov, chpheatfromelec);
                // Limiting the amount of heat that chps can dump
                cpl.AddLe(x_CHP_op_dump[t], cpl.Prod(this.c_chp_heatdump, x_CHP_op_th[t]));


                /// ////////////////////////////////////////////////////////////////////////
                /// Sizing
                cpl.AddLe(x_AirCon_op[t], x_AirCon);
                cpl.AddLe(x_CHP_op_e[t], x_CHP);
                cpl.AddLe(x_Boiler_op[t], x_Boiler);
                cpl.AddLe(x_ASHP_op[t], x_ASHP);


                /// ////////////////////////////////////////////////////////////////////////
                /// Emissions
                carbonEmissions.AddTerm(this.ClustersizePerTimestep[t] * this.lca_GridElectricity, x_Purchase[t]);     // data needs to be kgCO2eq./kWh
                carbonEmissions.AddTerm(this.ClustersizePerTimestep[t] * this.lca_NaturalGas * 1 / this.a_boi_eff, x_Boiler_op[t]);
                carbonEmissions.AddTerm(this.ClustersizePerTimestep[t] * this.lca_NaturalGas * 1 / this.c_chp_eff, x_CHP_op_e[t]);


                /// ////////////////////////////////////////////////////////////////////////
                /// Energy Balance
                cpl.AddEq(x_AirCon_op[t], this.CoolingDemand[t] );
                cpl.AddEq(cpl.Diff(thermalGeneration, thermalAdditionalDemand), this.HeatingDemand[t] );
                cpl.AddGe(cpl.Diff(elecGeneration, elecAdditionalDemand), this.ElectricityDemand[t] );
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// battery model
            for (int t=0; t<this.Horizon-1; t++)
            {
                ILinearNumExpr batteryState = cpl.LinearNumExpr();
                batteryState.AddTerm((1 - this.bat_decay), x_Battery_soc[t]);
                batteryState.AddTerm(this.bat_ch_eff, x_Battery_charge[t]);
                batteryState.AddTerm(-1 / this.bat_disch_eff, x_Battery_discharge[t]);
                cpl.AddEq(x_Battery_soc[t + 1], batteryState);
            }
            cpl.AddGe(x_Battery_soc[0], cpl.Prod(x_Battery, this.bat_min_state)); // initial state of battery >= min_state
            cpl.AddEq(x_Battery_soc[0], cpl.Diff(x_Battery_soc[this.Horizon - 1], x_Battery_discharge[this.Horizon - 1])); // initial state equals the last state minis discharge at last timestep
            cpl.AddEq(x_Battery_discharge[0], 0);        // no discharge at t=0

            for (int t=0; t<this.Horizon; t++)
            {
                cpl.AddGe(x_Battery_soc[t], cpl.Prod(x_Battery, this.bat_min_state));     // min state of charge
                cpl.AddLe(x_Battery_charge[t], cpl.Prod(x_Battery, this.bat_max_ch));        // battery charging
                cpl.AddLe(x_Battery_discharge[t], cpl.Prod(x_Battery, this.bat_max_disch));  // battery discharging
                cpl.AddLe(x_Battery_soc[t], x_Battery);                                   // battery sizing
            }

            /// ////////////////////////////////////////////////////////////////////////
            /// TES model
            for (int t = 0; t < this.Horizon - 1; t++)
            {
                ILinearNumExpr tesState = cpl.LinearNumExpr();
                tesState.AddTerm((1 - this.tes_decay), x_TES_soc[t]);
                tesState.AddTerm(this.tes_ch_eff, x_TES_charge[t]);
                tesState.AddTerm(-1 / this.tes_disch_eff, x_TES_discharge[t]);
                cpl.AddEq(x_TES_soc[t + 1], tesState);
            }
            cpl.AddEq(x_TES_soc[0], x_TES_soc[this.Horizon - 1]);
            cpl.AddEq(x_TES_discharge[0], 0);
            for (int t = 0; t < this.Horizon; t++)
            {
                cpl.AddLe(x_TES_charge[t], cpl.Prod(x_TES, this.tes_max_ch));
                cpl.AddLe(x_TES_discharge[t], cpl.Prod(x_TES, this.tes_max_disch));
                cpl.AddLe(x_TES_soc[t], x_TES);

                // donnot allow charge and discharge at the same time. y = 1 means charging
                cpl.AddLe(x_TES_charge[t], cpl.Prod(M, y_TES[t]));
                cpl.AddLe(x_TES_discharge[t], cpl.Prod(M, cpl.Diff(1, y_TES[t])));
            }



            /// ////////////////////////////////////////////////////////////////////////
            /// embodied carbon emissions of all technologies
            /// ////////////////////////////////////////////////////////////////////////
            for (int i=0; i<this.NumberOfSolarAreas; i++)
                carbonEmissions.AddTerm(this.lca_PV, x_PV[i]);
            carbonEmissions.AddTerm(this.lca_Battery, x_Battery);
            carbonEmissions.AddTerm(this.lca_AirCon, x_AirCon);
            carbonEmissions.AddTerm(this.lca_ASHP, x_ASHP);
            carbonEmissions.AddTerm(this.lca_Boiler, x_Boiler);
            carbonEmissions.AddTerm(this.lca_CHP, x_CHP);
            carbonEmissions.AddTerm(this.lca_TES, x_TES);

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



            /// ////////////////////////////////////////////////////////////////////////
            /// Cost coefficients formulation
            /// ////////////////////////////////////////////////////////////////////////
            ILinearNumExpr opex = cpl.LinearNumExpr();
            ILinearNumExpr capex = cpl.LinearNumExpr();
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                capex.AddTerm(this.c_PV, x_PV[i]);
            capex.AddTerm(this.c_Battery, x_Battery);
            capex.AddTerm(this.c_AirCon, x_AirCon);
            capex.AddTerm(this.c_ASHP, x_ASHP);
            capex.AddTerm(this.c_Boiler, x_Boiler);
            capex.AddTerm(this.c_CHP, x_CHP);
            capex.AddTerm(this.c_TES, x_TES);

            for (int t = 0; t < this.Horizon; t++)
            {
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_NaturalGas / this.c_chp_eff, x_CHP_op_e[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_NaturalGas / this.a_boi_eff, x_Boiler_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_Grid[t], x_Purchase[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_FeedIn[t], x_FeedIn[t]);

                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_Battery_OM, x_Battery_discharge[t]);    // assuming discharging is causing deterioration
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_Boiler_OM, x_Boiler_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_AirCon_OM, x_AirCon_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_CHP_OM, x_CHP_op_e[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_ASHP_OM, x_ASHP_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_TES_OM, x_TES_discharge[t]);
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// Objective function
            /// ////////////////////////////////////////////////////////////////////////
            if (isCostMinimization) cpl.AddMinimize(cpl.Sum(capex, cpl.Sum(OM_PV, opex)));
            else cpl.AddMinimize(carbonEmissions);

            // epsilon constraints for carbon, 
            // or cost constraint in case of carbon minimization (the same reason why carbon minimization needs a cost constraint)
            if (hasCarbonConstraint && isCostMinimization) cpl.AddLe(carbonEmissions, (double)carbonConstraint);
            else if (hasCostConstraint && !isCostMinimization) cpl.AddLe(cpl.Sum(opex, capex), (double)costConstraint);


            /// ////////////////////////////////////////////////////////////////////////
            /// Solve
            /// ////////////////////////////////////////////////////////////////////////
            if (!verbose) cpl.SetOut(null);
            cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.01);

            //if (!this.multithreading)
            //    cpl.SetParam(Cplex.Param.Threads, 1);
            EhubOutputs solution = new EhubOutputs();
            try
            {
                bool success = cpl.Solve();
                if (!success) return solution;
                /// ////////////////////////////////////////////////////////////////////////
                /// Outputs
                /// ////////////////////////////////////////////////////////////////////////

                solution.carbon = cpl.GetValue(carbonEmissions);
                solution.OPEX = cpl.GetValue(opex) + OM_PV;
                solution.CAPEX = cpl.GetValue(capex);
                solution.cost = solution.OPEX + solution.CAPEX;

                solution.x_pv = new double[this.NumberOfSolarAreas];
                for (int i = 0; i < this.NumberOfSolarAreas; i++)
                    solution.x_pv[i] = cpl.GetValue(x_PV[i]);
                solution.x_bat = cpl.GetValue(x_Battery);
                solution.x_tes = cpl.GetValue(x_TES);
                solution.x_chp = cpl.GetValue(x_CHP);
                solution.x_boi = cpl.GetValue(x_Boiler);
                solution.x_hp = cpl.GetValue(x_ASHP);
                solution.x_ac = cpl.GetValue(x_AirCon);

                solution.b_pvprod = new double[this.Horizon];
                solution.x_bat_charge = new double[this.Horizon];
                solution.x_bat_discharge = new double[this.Horizon];
                solution.x_bat_soc = new double[this.Horizon];
                solution.x_elecpur = new double[this.Horizon];
                solution.x_feedin = new double[this.Horizon];
                solution.x_boi_op = new double[this.Horizon];
                solution.x_ac_op = new double[this.Horizon];
                solution.x_hp_op = new double[this.Horizon];
                solution.x_chp_op_e = new double[this.Horizon];
                solution.x_chp_op_h = new double[this.Horizon];
                solution.x_chp_dump = new double[this.Horizon];
                solution.x_tes_charge = new double[this.Horizon];
                solution.x_tes_discharge = new double[this.Horizon];
                solution.x_tes_soc = new double[this.Horizon];
                for (int t = 0; t < this.Horizon; t++)
                {
                    solution.b_pvprod[t] = cpl.GetValue(x_PV_production[t]);
                    solution.x_bat_charge[t] = cpl.GetValue(x_Battery_charge[t]);
                    solution.x_bat_discharge[t] = cpl.GetValue(x_Battery_discharge[t]);
                    solution.x_bat_soc[t] = cpl.GetValue(x_Battery_soc[t]);
                    solution.x_elecpur[t] = cpl.GetValue(x_Purchase[t]);
                    solution.x_feedin[t] = cpl.GetValue(x_FeedIn[t]);
                    solution.x_boi_op[t] = cpl.GetValue(x_Boiler_op[t]);
                    solution.x_ac_op[t] = cpl.GetValue(x_AirCon_op[t]);
                    solution.x_hp_op[t] = cpl.GetValue(x_ASHP_op[t]);
                    solution.x_chp_op_e[t] = cpl.GetValue(x_CHP_op_e[t]);
                    solution.x_chp_op_h[t] = cpl.GetValue(x_CHP_op_th[t]);
                    solution.x_chp_dump[t] = cpl.GetValue(x_CHP_op_dump[t]);
                    solution.x_tes_charge[t] = cpl.GetValue(x_TES_charge[t]);
                    solution.x_tes_discharge[t] = cpl.GetValue(x_TES_discharge[t]);
                    solution.x_tes_soc[t] = cpl.GetValue(x_TES_soc[t]);
                }

                return solution;
            }
            catch(ILOG.Concert.Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
                return solution;
            }
        }
    }
}
