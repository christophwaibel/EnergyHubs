using EhubMisc;
using ILOG.Concert;
using ILOG.CPLEX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cisbat23BuildingIntegratedAgriculture
{
    internal class EHubBiA
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
        internal double LifetimeBia { get; private set; }
        internal double LifetimePV { get; private set; }
        internal double LifetimeBattery { get; private set; }
        internal double LifetimeTES { get; private set; }
        internal double LifetimeASHP { get; private set; }
        internal double LifetimeCHP { get; private set; }
        internal double LifetimeBoiler { get; private set; }
        internal double LifetimeBiomassBoiler { get; private set; }
        internal double LifetimeElecChiller { get; private set; }
        internal double LifetimeDistrictHeating { get; private set; }
        internal double LifetimeHeatExchanger { get; private set; }
        internal double LifetimeCoolingTower { get; private set; }

        // Coefficients Demand Respons
        internal double a_DrElec { get; private set; }
        internal double a_DrCool { get; private set; }
        internal double a_DrHeat { get; private set; }

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

        // Coefficients natural gas and biomass boilers
        internal double a_boi_eff { get; private set; }
        internal double a_bmboi_eff { get; private set; }
        internal double b_maxbiomassperyear { get; private set; }  // kWh biomass per year

        // Coefficients CHP
        internal double c_chp_eff_el { get; private set; }      // electric efficiency. so 1 kWh of gas results in 0.3 kWh of elec
        internal double c_chp_htp { get; private set; }         // heat to power ratio (e.g. htp = 1.73, then 1.73 kW of heat is produced for 1 kW of elec)
        internal double c_chp_heatdump { get; private set; }    // heat dump allowed = 1

        // Coefficients Electric Chiller
        internal double c_ElecChiller_eff_clg { get; private set; }
        internal double c_ElecChiller_eff_htg { get; private set; }


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

        // Minimal Capacities
        internal double minCapBattery { get; private set; }
        internal double minCapTES { get; private set; }
        internal double minCapBoiler { get; private set; }
        internal double minCapBioBoiler { get; private set; }
        internal double minCapCHP { get; private set; }
        internal double minCapElecChiller { get; private set; }
        internal double minCapASHP { get; private set; }

        #endregion


        #region inputs LCA parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// LCA
        /// ////////////////////////////////////////////////////////////////////////
        internal double lca_GridElectricity { get; private set; }
        internal double lca_NaturalGas { get; private set; }
        internal double lca_Biomass { get; private set; }

        // annualized LCA of technologies
        internal double lca_Bia { get; private set; }
        internal double lca_PV { get; private set; }
        internal double lca_Battery { get; private set; }
        internal double lca_TES { get; private set; }
        internal double lca_ASHP { get; private set; }
        internal double lca_CHP { get; private set; }
        internal double lca_Boiler { get; private set; }
        internal double lca_BiomassBoiler { get; private set; }
        internal double lca_ElecChiller { get; private set; }
        internal double lca_DistrictHeating { get; private set; }
        internal double lca_HeatExchanger { get; private set; }
        internal double lca_CoolingTower { get; private set; }


        // total (non-annualized) LCA of technologies 
        internal double LcaTotal_Bia { get; private set; }
        internal double LcaTotal_PV { get; private set; }
        internal double LcaTotal_Battery { get; private set; }
        internal double LcaTotal_TES { get; private set; }
        internal double LcaTotal_ASHP { get; private set; }
        internal double LcaTotal_CHP { get; private set; }
        internal double LcaTotal_Boiler { get; private set; }
        internal double LcaTotal_BiomassBoiler { get; private set; }
        internal double LcaTotal_ElecChiller { get; private set; }
        internal double LcaTotal_DistrictHeating { get; private set; }
        internal double LcaTotal_HeatExchanger { get; private set; }
        internal double LcaTotal_CoolingTower { get; private set; }

        // levelized LCA of building construction
        internal double lca_Building { get; private set; }
        #endregion


        #region inputs cost parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// Cost Parameters
        /// ////////////////////////////////////////////////////////////////////////
        internal double InterestRate { get; private set; }
        internal double c_NaturalGas { get; private set; }
        internal double c_Biomass { get; private set; }

        // Linear Investment Cost
        internal double CostPV { get; private set; }
        internal double CostBattery { get; private set; }
        internal double CostTES { get; private set; }
        internal double CostBoiler { get; private set; }
        internal double CostBiomassBoiler { get; private set; }
        internal double CostCHPElectric { get; private set; } // cost per kW of electric power
        internal double CostElecChiller { get; private set; }
        internal double CostASHP { get; private set; }
        internal double CostDistrictHeating { get; private set; }
        internal double CostHeatExchanger { get; private set; }
        internal double CostCoolingTower { get; private set; }

        // Fix Cost
        internal double [] FixCostBia { get; private set; } // index for each surface
        internal double FixCostPV { get; private set; }
        internal double FixCostBattery { get; private set; }
        internal double FixCostTES { get; private set; }
        internal double FixCostBoiler { get; private set; }
        internal double FixCostBiomassBoiler { get; private set; }
        internal double FixCostCHP { get; private set; }
        internal double FixCostElecChiller { get; private set; }
        internal double FixCostASHP { get; private set; }
        internal double FixCostDistrictHeating { get; private set; }
        internal double FixCostHeatExchanger { get; private set; }
        internal double FixCostCoolingTower { get; private set; }

        // Annuity
        internal double[] AnnuityBia { get; private set; }
        internal double AnnuityPV { get; private set; }
        internal double AnnuityBattery { get; private set; }
        internal double AnnuityTES { get; private set; }
        internal double AnnuityBoiler { get; private set; }
        internal double AnnuityBiomassBoiler { get; private set; }
        internal double AnnuityCHP { get; private set; }
        internal double AnnuityElecChiller { get; private set; }
        internal double AnnuityASHP { get; private set; }
        internal double AnnuityDistrictHeating { get; private set; }
        internal double AnnuityHeatExchanger { get; private set; }
        internal double AnnuityCoolingTower { get; private set; }

        // annualized investment cost
        internal double [] c_Bia { get; private set; }
        internal double c_PV { get; private set; }
        internal double c_Battery { get; private set; }
        internal double c_TES { get; private set; }
        internal double c_Boiler { get; private set; }
        internal double c_BiomassBoiler { get; private set; }
        internal double c_CHP { get; private set; }
        internal double c_ElecChiller { get; private set; }
        internal double c_ASHP { get; private set; }
        internal double c_DistrictHeating { get; private set; }
        internal double c_HeatExchanger { get; private set; }
        internal double c_CoolingTower { get; private set; }

        // annualized fix cost
        internal double [] c_fix_Bia { get; private set; }
        internal double c_fix_PV { get; private set; }
        internal double c_fix_Battery { get; private set; }
        internal double c_fix_TES { get; private set; }
        internal double c_fix_Boiler { get; private set; }
        internal double c_fix_BiomassBoiler { get; private set; }
        internal double c_fix_CHP { get; private set; }
        internal double c_fix_ElecChiller { get; private set; }
        internal double c_fix_ASHP { get; private set; }
        internal double c_fix_DistrictHeating { get; private set; }
        internal double c_fix_HeatExchanger { get; private set; }
        internal double c_fix_CoolingTower { get; private set; }

        // operation and maintenance cost
        internal double c_Bia_OM { get; private set; }
        internal double c_PV_OM { get; private set; }
        internal double c_Battery_OM { get; private set; }
        internal double c_TES_OM { get; private set; }
        internal double c_Boiler_OM { get; private set; }
        internal double c_BiomassBoiler_OM { get; private set; }
        internal double c_CHP_OM { get; private set; }
        internal double c_ElecChiller_OM { get; private set; }
        internal double c_ASHP_OM { get; private set; }

        // time resolved operation cost
        internal double[] c_Grid { get; private set; }
        internal double[] c_FeedIn { get; private set; }
        #endregion


        #region District Heating and Cooling
        internal int NumberOfBuildingsInDistrict { get; private set; } // loads are aggregated. but if this number >1, then dh costs apply (HX and DH pipes)
        internal double[] PeakHeatingLoadsPerBuilding { get; private set; } // in kW. length of this array corresponds to number of buildings in the district
        internal double[] PeakCoolingLoadsPerBuilding { get; private set; }
        internal double NetworkLengthTotal { get; private set; } // in m
        #endregion


        #region MILP stuff
        /// ////////////////////////////////////////////////////////////////////////
        /// MILP
        /// ////////////////////////////////////////////////////////////////////////
        private const double M = 99999;   // Big M method
        #endregion



        // To Do: fix DR formulation
        // -a d_t <= x_t 
        // a d_t >= x_t
        // sum_t^24 x_t = 0

        // add Bia... y_pv + y_bia <= 1
        // add veggi demand
        // add veggi related cost and emissions



        /// <summary>
        /// always hourly! I.e. it assumes the demand arrays are of length days x 24
        /// </summary>
        /// <param name="heatingDemand"></param>
        /// <param name="coolingDemand"></param>
        /// <param name="electricityDemand"></param>
        /// <param name="irradiance"></param>
        /// <param name="solarTechSurfaceAreas"></param>
        /// <param name="ambientTemperature"></param>
        /// <param name="technologyParameters"></param>
        /// <param name="clustersizePerTimestep">how many days a typical day represents</param>
        /// <param name="BiaCapexIn">BIA (Building Integrated Agriculture) total Capex per surface index []</param>
        /// <param name="BiaOpexIn">Yearly BIA Opex per surface index [], i.e., the money earnt for selling all the food to the supermarket</param>
        /// <param name="BiaGhgIn">Yearly BIA Ghg per surface index [], if this amount of food was purchased from Indonesia or Malaysia (in kg CO2 eq). For abating (i.e., if BIA selected), substract this value in the energy hub</param>
        internal EHubBiA(double[] heatingDemand, double[] coolingDemand, double[] electricityDemand,
            double[][] irradiance, double[] solarTechSurfaceAreas,
            double[] ambientTemperature, Dictionary<string, double> technologyParameters,
            int[] clustersizePerTimestep,
            double [] BiaCapexIn, double [] BiaOpexIn, double [] BiaGhgIn)
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
            double costTolerance = 100.0;
            double carbonTolerance = 0.1;
            this.Outputs = new EhubOutputs[epsilonCuts + 2];

            // 1. solve for minCarbon, ignoring cost. solve again, but mincost, with minCarbon constraint
            EhubOutputs minCarbon = EnergyHub("carbon", null, null, verbose);

            // 2. solve for minCost, 
            EhubOutputs minCost = EnergyHub("cost", null, null, verbose);

            // 3. solve for minCost, ignoring Carbon (then, solve for minCarbon, using mincost as constraint. check, if it makes a difference in carbon)
            this.Outputs[0] = EnergyHub("cost", minCarbon.carbon + carbonTolerance, null, verbose);
            //this.Outputs[epsilonCuts + 1] = minCost; 
            this.Outputs[epsilonCuts + 1] = EnergyHub("carbon", null, minCost.cost + costTolerance, verbose);
            double carbonInterval = (minCost.carbon - minCarbon.carbon) / (epsilonCuts + 1);

            // 4. make epsilonCuts cuts and solve for each minCost s.t. carbon
            for (int i = 0; i < epsilonCuts; i++)
                this.Outputs[i + 1] = EnergyHub("cost", minCarbon.carbon + carbonInterval * (i + 1), null, verbose);

            // 5. report all values into Outputs
            //  ...already done by this.Outputs
        }


        private void SetParameters(Dictionary<string, double> technologyParameters)
        {
            /// ////////////////////////////////////////////////////////////////////////
            /// Technical Parameters
            /// ////////////////////////////////////////////////////////////////////////

            // Demand Response
            if (technologyParameters.ContainsKey("DemandResponseElec"))
                this.a_DrElec = technologyParameters["DemandResponseElec"];
            else
                this.a_DrElec = 0.1;
            if (technologyParameters.ContainsKey("DemandResponseCool"))
                this.a_DrCool = technologyParameters["DemandResponseCool"];
            else
                this.a_DrCool = 0.1;
            if (technologyParameters.ContainsKey("DemandResponseHeat"))
                this.a_DrHeat = technologyParameters["DemandResponseHeat"];
            else
                this.a_DrHeat = 0.1;

            // floor area
            double _floorarea;
            if (technologyParameters.ContainsKey("TotalFloorArea"))
                _floorarea = technologyParameters["TotalFloorArea"];
            else
                _floorarea = 1000.0;

            // Electric Chiller
            if (technologyParameters.ContainsKey("c_ElecChiller_eff_clg"))
                this.c_ElecChiller_eff_clg = technologyParameters["c_ElecChiller_eff_clg"];
            else
                this.c_ElecChiller_eff_clg = 4.9;
            if (technologyParameters.ContainsKey("c_ElecChiller_eff_htg"))
                this.c_ElecChiller_eff_htg = technologyParameters["c_ElecChiller_eff_htg"];
            else
                this.c_ElecChiller_eff_htg = 5.8;

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

            // Biomass Boiler
            if (technologyParameters.ContainsKey("a_bmboi_eff"))
                this.a_bmboi_eff = technologyParameters["a_bmboi_eff"];
            else
                this.a_bmboi_eff = 0.9;
            if (technologyParameters.ContainsKey("b_MaxBiomassAvailable"))
                this.b_maxbiomassperyear = technologyParameters["b_MaxBiomassAvailable"];
            else
                this.b_maxbiomassperyear = 10000.0;

            // CHP
            if (technologyParameters.ContainsKey("c_chp_eff"))
                this.c_chp_eff_el = technologyParameters["c_chp_eff"];
            else
                this.c_chp_eff_el = 0.3;
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
                this.b_MaxBattery = technologyParameters["b_MaxBattery"] * _floorarea;
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
                this.b_MaxTES = technologyParameters["b_MaxTES"] * _floorarea;
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
            /// Minimal Capacities
            /// ////////////////////////////////////////////////////////////////////////
            if (technologyParameters.ContainsKey("minCapBattery"))
                this.minCapBattery = technologyParameters["minCapBattery"];
            else
                this.minCapBattery = 10;
            if (technologyParameters.ContainsKey("minCapTES"))
                this.minCapTES = technologyParameters["minCapTES"];
            else
                this.minCapTES = 10;
            if (technologyParameters.ContainsKey("minCapBoiler"))
                this.minCapBoiler = technologyParameters["minCapBoiler"];
            else
                this.minCapBoiler = 10;
            if (technologyParameters.ContainsKey("minCapBioBoiler"))
                this.minCapBioBoiler = technologyParameters["minCapBioBoiler"];
            else
                this.minCapBioBoiler = 10;
            if (technologyParameters.ContainsKey("minCapCHP"))
                this.minCapCHP = technologyParameters["minCapCHP"];
            else
                this.minCapCHP = 10;
            if (technologyParameters.ContainsKey("minCapElecChiller"))
                this.minCapElecChiller = technologyParameters["minCapElecChiller"];
            else
                this.minCapElecChiller = 10;
            if (technologyParameters.ContainsKey("minCapASHP"))
                this.minCapASHP = technologyParameters["minCapASHP"];
            else
                this.minCapASHP = 10;


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

            // Total LCA of technologies
            if (technologyParameters.ContainsKey("lca_PV"))
                this.LcaTotal_PV = technologyParameters["lca_PV"];
            else
                this.LcaTotal_PV = 0.0;
            if (technologyParameters.ContainsKey("lca_Battery"))
                this.LcaTotal_Battery = technologyParameters["lca_Battery"];
            else
                this.LcaTotal_Battery = 0.0;
            if (technologyParameters.ContainsKey("lca_TES"))
                this.LcaTotal_TES = technologyParameters["lca_TES"];
            else
                this.LcaTotal_TES = 0.0;
            if (technologyParameters.ContainsKey("lca_ASHP"))
                this.LcaTotal_ASHP = technologyParameters["lca_ASHP"];
            else
                this.LcaTotal_ASHP = 0.0;
            if (technologyParameters.ContainsKey("lca_CHP"))
                this.LcaTotal_CHP = technologyParameters["lca_CHP"];
            else
                this.LcaTotal_CHP = 0.0;
            if (technologyParameters.ContainsKey("lca_Boiler"))
                this.LcaTotal_Boiler = technologyParameters["lca_Boiler"];
            else
                this.LcaTotal_Boiler = 0.0;
            if (technologyParameters.ContainsKey("lca_BiomassBoiler"))
                this.LcaTotal_BiomassBoiler = technologyParameters["lca_BiomassBoiler"];
            else
                this.LcaTotal_BiomassBoiler = 0.0;
            if (technologyParameters.ContainsKey("lca_ElecChiller"))
                this.LcaTotal_ElecChiller = technologyParameters["lca_ElecChiller"];
            else
                this.LcaTotal_ElecChiller = 0.0;
            if (technologyParameters.ContainsKey("lca_DistrictHeating"))
                this.LcaTotal_DistrictHeating = technologyParameters["lca_DistrictHeating"];
            else
                this.LcaTotal_DistrictHeating = 0.0;
            if (technologyParameters.ContainsKey("lca_HeatExchanger"))
                this.LcaTotal_HeatExchanger = technologyParameters["lca_HeatExchanger"];
            else
                this.LcaTotal_HeatExchanger = 0.0;
            if (technologyParameters.ContainsKey("lca_CoolingTower"))
                this.LcaTotal_CoolingTower = technologyParameters["lca_CoolingTower"];
            else
                this.LcaTotal_CoolingTower = 0.0;

            // levelized lca of building construction
            if (technologyParameters.ContainsKey("lca_Building"))
                this.lca_Building = technologyParameters["lca_Building"];
            else
                this.lca_Building = 0.0;


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
            if (technologyParameters.ContainsKey("c_Biomass"))
                this.c_Biomass = technologyParameters["c_Biomass"];
            else
                this.c_Biomass = 0.2;

            double _gridOffPeak, _gridPeak, _feedIn;
            if (technologyParameters.ContainsKey("c_Grid_OffPeak"))
                _gridOffPeak = technologyParameters["c_Grid_OffPeak"];
            else
                _gridOffPeak = 0.1;
            if (technologyParameters.ContainsKey("c_Grid"))
                _gridPeak = technologyParameters["c_Grid"];
            else
                _gridPeak = 0.2;
            if (technologyParameters.ContainsKey("c_FeedIn"))
                _feedIn = technologyParameters["c_FeedIn"];
            else
                _feedIn = -0.15;

            this.c_FeedIn = new double[this.Horizon];
            this.c_Grid = new double[this.Horizon];
            for (int t = 0; t < this.Horizon; t += 24)  // default values from Wu et al 2017. he didn't have off-peak grid 
            {
                for (int u = t; u < t + 24; u++)
                {
                    this.c_FeedIn[u] = _feedIn;
                    if (u > t + 7 && u < t + 18)
                        this.c_Grid[u] = _gridPeak;
                    else
                        this.c_Grid[u] = _gridOffPeak;
                }
            }



            // Linear Investment Cost
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
            if (technologyParameters.ContainsKey("CostBiomassBoiler"))
                this.CostBiomassBoiler = technologyParameters["CostBiomassBoiler"];
            else
                this.CostBiomassBoiler = 300.0;
            if (technologyParameters.ContainsKey("CostCHP"))
                this.CostCHPElectric = technologyParameters["CostCHP"];
            else
                this.CostCHPElectric = 1500.0;
            if (technologyParameters.ContainsKey("CostElecChiller"))
                this.CostElecChiller = technologyParameters["CostElecChiller"];
            else
                this.CostElecChiller = 360.0;
            if (technologyParameters.ContainsKey("CostASHP"))
                this.CostASHP = technologyParameters["CostASHP"];
            else
                this.CostASHP = 1000.0;
            if (technologyParameters.ContainsKey("CostDistrictHeating"))
                this.CostDistrictHeating = technologyParameters["CostDistrictHeating"];
            else
                this.CostDistrictHeating = 200.0;
            if (technologyParameters.ContainsKey("CostHeatExchanger"))
                this.CostHeatExchanger = technologyParameters["CostHeatExchanger"];
            else
                this.CostHeatExchanger = 200.0;
            if (technologyParameters.ContainsKey("CostCoolingTower"))
                this.CostCoolingTower = technologyParameters["CostCoolingTower"];
            else
                this.CostCoolingTower = 200.0;

            // Fix Investment Cost
            if (technologyParameters.ContainsKey("FixCostPV"))
                this.FixCostPV = technologyParameters["FixCostPV"];
            else
                this.FixCostPV = 250.0;
            if (technologyParameters.ContainsKey("FixCostBattery"))
                this.FixCostBattery = technologyParameters["FixCostBattery"];
            else
                this.FixCostBattery = 250.0;
            if (technologyParameters.ContainsKey("FixCostTES"))
                this.FixCostTES = technologyParameters["FixCostTES"];
            else
                this.FixCostTES = 250.0;
            if (technologyParameters.ContainsKey("FixCostBoiler"))
                this.FixCostBoiler = technologyParameters["FixCostBoiler"];
            else
                this.FixCostBoiler = 250.0;
            if (technologyParameters.ContainsKey("FixCostBiomassBoiler"))
                this.FixCostBiomassBoiler = technologyParameters["FixCostBiomassBoiler"];
            else
                this.FixCostBiomassBoiler = 250.0;
            if (technologyParameters.ContainsKey("FixCostCHP"))
                this.FixCostCHP = technologyParameters["FixCostCHP"];
            else
                this.FixCostCHP = 250.0;
            if (technologyParameters.ContainsKey("FixCostElecChiller"))
                this.FixCostElecChiller = technologyParameters["FixCostElecChiller"];
            else
                this.FixCostElecChiller = 250.0;
            if (technologyParameters.ContainsKey("FixCostASHP"))
                this.FixCostASHP = technologyParameters["FixCostASHP"];
            else
                this.FixCostASHP = 250.0;
            if (technologyParameters.ContainsKey("FixCostDistrictHeating"))
                this.FixCostDistrictHeating = technologyParameters["FixCostDistrictHeating"];
            else
                this.FixCostDistrictHeating = 250.0;
            if (technologyParameters.ContainsKey("FixCostHeatExchanger"))
                this.FixCostHeatExchanger = technologyParameters["FixCostHeatExchanger"];
            else
                this.FixCostHeatExchanger = 250.0;
            if (technologyParameters.ContainsKey("FixCostCoolingTower"))
                this.FixCostCoolingTower = technologyParameters["FixCostCoolingTower"];
            else
                this.FixCostCoolingTower = 250.0;

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
            if (technologyParameters.ContainsKey("c_BiomassBoiler_OM"))
                this.c_BiomassBoiler_OM = technologyParameters["c_BiomassBoiler_OM"];
            else
                this.c_BiomassBoiler_OM = 0.01;
            if (technologyParameters.ContainsKey("c_CHP_OM"))
                this.c_CHP_OM = technologyParameters["c_CHP_OM"];
            else
                this.c_CHP_OM = 0.021;    // Waibel et al 2017
            if (technologyParameters.ContainsKey("c_ElecChiller_OM"))
                this.c_ElecChiller_OM = technologyParameters["c_ElecChiller_OM"];
            else
                this.c_ElecChiller_OM = 0.1;
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
            if (technologyParameters.ContainsKey("LifetimeBiomassBoiler"))
                this.LifetimeBiomassBoiler = technologyParameters["LifetimeBiomassBoiler"];
            else
                this.LifetimeBiomassBoiler = 30.0;
            if (technologyParameters.ContainsKey("LifetimeElecChiller"))
                this.LifetimeElecChiller = technologyParameters["LifetimeElecChiller"];
            else
                this.LifetimeElecChiller = 20.0;
            if (technologyParameters.ContainsKey("LifetimeDistrictHeating"))
                this.LifetimeDistrictHeating = technologyParameters["LifetimeDistrictHeating"];
            else
                this.LifetimeDistrictHeating = 50.0;
            if (technologyParameters.ContainsKey("LifetimeHeatExchanger"))
                this.LifetimeHeatExchanger = technologyParameters["LifetimeHeatExchanger"];
            else
                this.LifetimeHeatExchanger = 30.0;
            if (technologyParameters.ContainsKey("LifetimeCoolingTower"))
                this.LifetimeCoolingTower = technologyParameters["LifetimeCoolingTower"];
            else
                this.LifetimeCoolingTower = 50.0;

            // Annuity
            this.AnnuityPV = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimePV)))));
            this.AnnuityBattery = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeBattery)))));
            this.AnnuityTES = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeTES)))));
            this.AnnuityASHP = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeASHP)))));
            this.AnnuityCHP = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeCHP)))));
            this.AnnuityBoiler = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeBoiler)))));
            this.AnnuityBiomassBoiler = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeBiomassBoiler)))));
            this.AnnuityElecChiller = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeElecChiller)))));
            this.AnnuityDistrictHeating = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeDistrictHeating)))));
            this.AnnuityHeatExchanger = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeHeatExchanger)))));
            this.AnnuityCoolingTower = this.InterestRate / (1 - (1 / (Math.Pow((1 + this.InterestRate), (this.LifetimeCoolingTower)))));

            // Levelized cost
            this.c_PV = this.CostPV * this.AnnuityPV;
            this.c_Battery = this.CostBattery * this.AnnuityBattery;
            this.c_TES = this.CostTES * this.AnnuityTES;
            this.c_ASHP = this.CostASHP * this.AnnuityASHP;
            this.c_CHP = this.CostCHPElectric * this.AnnuityCHP;
            this.c_Boiler = this.CostBoiler * this.AnnuityBoiler;
            this.c_BiomassBoiler = this.CostBiomassBoiler * this.AnnuityBiomassBoiler;
            this.c_ElecChiller = this.CostElecChiller * this.AnnuityElecChiller;
            this.c_DistrictHeating = this.CostDistrictHeating * this.AnnuityDistrictHeating;
            this.c_HeatExchanger = this.CostHeatExchanger * this.AnnuityHeatExchanger;
            this.c_CoolingTower = this.CostCoolingTower * this.AnnuityCoolingTower;

            // levelized fix cost
            this.c_fix_PV = this.FixCostPV * this.AnnuityPV;
            this.c_fix_Battery = this.FixCostBattery * this.AnnuityBattery;
            this.c_fix_TES = this.FixCostTES * this.AnnuityTES;
            this.c_fix_Boiler = this.FixCostBoiler * this.AnnuityBoiler;
            this.c_fix_BiomassBoiler = this.FixCostBiomassBoiler * this.AnnuityBiomassBoiler;
            this.c_fix_CHP = this.FixCostCHP * this.AnnuityCHP;
            this.c_fix_ElecChiller = this.FixCostElecChiller * this.AnnuityElecChiller;
            this.c_fix_ASHP = this.FixCostASHP * this.AnnuityASHP;
            this.c_fix_DistrictHeating = this.FixCostDistrictHeating * this.AnnuityDistrictHeating;
            this.c_fix_HeatExchanger = this.FixCostHeatExchanger * this.AnnuityHeatExchanger;
            this.c_fix_CoolingTower = this.FixCostCoolingTower * this.AnnuityCoolingTower;

            // PV efficiency
            this.a_PV_Efficiency = new double[this.NumberOfSolarAreas][];
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                this.a_PV_Efficiency[i] = EhubMisc.TechnologyEfficiencies.CalculateEfficiencyPhotovoltaic(AmbientTemperature, this.SolarLoads[i],
                    this.pv_NOCT, this.pv_T_aNOCT, this.pv_P_NOCT, this.pv_beta_ref, this.pv_n_ref);

            this.a_ASHP_Efficiency = EhubMisc.TechnologyEfficiencies.CalculateCOPHeatPump(this.AmbientTemperature, this.hp_supplyTemp, this.hp_pi1, this.hp_pi2, this.hp_pi3, this.hp_pi4);


            // District Heating
            this.NumberOfBuildingsInDistrict = technologyParameters.ContainsKey("NumberOfBuildingsInEHub") ? Convert.ToInt32(technologyParameters["NumberOfBuildingsInEHub"]) : 1;
            if (this.NumberOfBuildingsInDistrict > 1)
            {
                this.NetworkLengthTotal = technologyParameters.ContainsKey("GridLengthDistrictHeating") ? technologyParameters["GridLengthDistrictHeating"] : 1000.0;

                //get peak loads per building
                this.PeakHeatingLoadsPerBuilding = new double[this.NumberOfBuildingsInDistrict];
                this.PeakCoolingLoadsPerBuilding = new double[this.NumberOfBuildingsInDistrict];
                for (int i = 0; i < this.NumberOfBuildingsInDistrict; i++)
                {
                    string heatingLoad = "Peak_Htg_" + Convert.ToString(i + 1);
                    if (technologyParameters.ContainsKey(heatingLoad))
                        this.PeakHeatingLoadsPerBuilding[i] = technologyParameters[heatingLoad];
                    else
                        this.PeakHeatingLoadsPerBuilding[i] = 1000.0;

                    string coolingLoad = "Peak_Clg_" + Convert.ToString(i + 1);
                    if (technologyParameters.ContainsKey(coolingLoad))
                        this.PeakCoolingLoadsPerBuilding[i] = technologyParameters[coolingLoad];
                    else
                        this.PeakCoolingLoadsPerBuilding[i] = 1000.0;
                }
            }
            else
            {
                this.NetworkLengthTotal = 0.0;
                this.PeakHeatingLoadsPerBuilding = new double[1] { 0.0 };
                this.PeakCoolingLoadsPerBuilding = new double[1] { 0.0 };
                this.c_HeatExchanger = 0.0;
                this.c_DistrictHeating = 0.0;
                this.c_fix_DistrictHeating = 0.0;
                this.c_fix_HeatExchanger = 0.0;
            }


            // levelized LCA of technologies
            this.lca_ElecChiller = this.LcaTotal_ElecChiller / this.LifetimeElecChiller;
            this.lca_ASHP = this.LcaTotal_ASHP / this.LifetimeASHP;
            this.lca_Battery = this.LcaTotal_Battery / this.LifetimeBattery;
            this.lca_Boiler = this.LcaTotal_Boiler / this.LifetimeBoiler;
            this.lca_BiomassBoiler = this.LcaTotal_BiomassBoiler / this.LifetimeBiomassBoiler;
            this.lca_CHP = this.LcaTotal_CHP / this.LifetimeCHP;
            this.lca_DistrictHeating = this.LcaTotal_DistrictHeating / this.LifetimeDistrictHeating;
            this.lca_PV = this.LcaTotal_PV / this.LifetimePV;
            this.lca_TES = this.LcaTotal_TES / this.LifetimeTES;
            this.lca_HeatExchanger = this.LcaTotal_HeatExchanger / this.LifetimeHeatExchanger;
            this.lca_CoolingTower = this.LcaTotal_CoolingTower / this.LifetimeCoolingTower;
        }


        private EhubOutputs EnergyHub(string objective = "cost", double? carbonConstraint = null, double? costConstraint = null, bool verbose = false)
        {
            Cplex cpl = new Cplex();
            EhubOutputs solution = new EhubOutputs();
            solution.x_hx_dh = new double[this.NumberOfBuildingsInDistrict];
            solution.x_hx_clg_dh = new double[this.NumberOfBuildingsInDistrict];

            /// ////////////////////////////////////////////////////////////////////////
            /// District Heating
            /// ////////////////////////////////////////////////////////////////////////
            double LevCostDH = (this.NetworkLengthTotal * this.c_DistrictHeating + this.c_fix_DistrictHeating + this.c_fix_HeatExchanger) * 2; // *2 because heating and cooling networks
            double[] LevCostHX = new double[this.NumberOfBuildingsInDistrict];
            double[] LevCostHXClg = new double[this.NumberOfBuildingsInDistrict];
            double TotHXsizing = 0.0;
            double TotHXsizingClg = 0.0;
            double TotLevCostDH = 0.0;
            for (int i = 0; i < this.NumberOfBuildingsInDistrict; i++)
            {
                TotHXsizing += this.PeakHeatingLoadsPerBuilding[i];
                LevCostHX[i] = this.c_HeatExchanger * this.PeakHeatingLoadsPerBuilding[i];
                TotLevCostDH += LevCostHX[i];
                solution.x_hx_dh[i] = this.PeakHeatingLoadsPerBuilding[i];

                TotHXsizingClg += this.PeakCoolingLoadsPerBuilding[i];
                LevCostHXClg[i] = this.c_HeatExchanger * this.PeakCoolingLoadsPerBuilding[i];
                TotLevCostDH += LevCostHXClg[i];
                solution.x_hx_clg_dh[i] = this.PeakCoolingLoadsPerBuilding[i];
            }
            TotLevCostDH += LevCostDH; // add this to total investment cost. ignore operation cost

            solution.x_dh = this.NetworkLengthTotal;

            // cooling tower:
            solution.x_clgtower = (this.CoolingDemand.Max() / this.c_ElecChiller_eff_clg) * this.c_ElecChiller_eff_htg;




            /// ////////////////////////////////////////////////////////////////////////
            /// Variables
            /// ////////////////////////////////////////////////////////////////////////

            // Demand Response
            INumVar[] x_DrElecPos = new INumVar[this.Horizon];  // postive shifting variable elec (generating more)
            INumVar[] x_DrElecNeg = new INumVar[this.Horizon];  // negative shifting variable elec (consuming more)
            INumVar[] y_DrElecPos = new INumVar[this.Horizon];  // booleans for positive and negative shifting
            INumVar[] y_DrElecNeg = new INumVar[this.Horizon];
            INumVar[] x_DrHeatPos = new INumVar[this.Horizon];  // generating more
            INumVar[] x_DrHeatNeg = new INumVar[this.Horizon];  // consuming more
            INumVar[] y_DrHeatPos = new INumVar[this.Horizon];
            INumVar[] y_DrHeatNeg = new INumVar[this.Horizon];
            INumVar[] x_DrCoolPos = new INumVar[this.Horizon];  // generating more
            INumVar[] x_DrCoolNeg = new INumVar[this.Horizon];  // consuming more
            INumVar[] y_DrCoolPos = new INumVar[this.Horizon];
            INumVar[] y_DrCoolNeg = new INumVar[this.Horizon];

            // district heating dummys. needed for adding to cost and carbon
            INumVar dh_dummy = cpl.BoolVar();
            cpl.AddEq(1, dh_dummy);

            // cooling tower dummy. needed for adding to cost and carbon
            INumVar clgtower_dummy = cpl.BoolVar();
            cpl.AddEq(1, clgtower_dummy);

            // building lca dummy
            INumVar lcabuilding_dummy = cpl.BoolVar();
            cpl.AddEq(1, lcabuilding_dummy);

            // grid
            INumVar[] x_Purchase = new INumVar[this.Horizon];
            INumVar[] x_FeedIn = new INumVar[this.Horizon];

            // PV
            INumVar[] x_PV = new INumVar[this.NumberOfSolarAreas];
            ILinearNumExpr[] x_PV_production = new ILinearNumExpr[Horizon];
            double OM_PV = 0.0; // operation maintanence for PV
            INumVar[] y_PV = new INumVar[this.NumberOfSolarAreas];
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
            {
                x_PV[i] = cpl.NumVar(0, this.SolarAreas[i]);
                y_PV[i] = cpl.BoolVar();
            }
            INumVar[] y_FeedIn = new INumVar[this.Horizon];    // binary to indicate Feed-In (=1). no selling and purchasing from the grid at the same time allowed


            // AirCon
            INumVar x_ElecChiller = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_ElecChiller_op = new INumVar[this.Horizon];
            INumVar y_ElecChiller = cpl.BoolVar();

            // Boiler
            INumVar x_Boiler = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_Boiler_op = new INumVar[this.Horizon];
            INumVar y_Boiler = cpl.BoolVar();

            // Biomass Boiler
            INumVar x_BiomassBoiler = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_BiomassBoiler_op = new INumVar[this.Horizon];
            INumVar y_BiomassBoiler = cpl.BoolVar();

            // CHP
            INumVar x_CHP = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_CHP_op_e = new INumVar[this.Horizon];
            INumVar[] x_CHP_op_th = new INumVar[this.Horizon];
            INumVar[] x_CHP_op_dump = new INumVar[this.Horizon];
            INumVar y_CHP = cpl.BoolVar();

            // ASHP
            INumVar x_ASHP = cpl.NumVar(0.0, System.Double.MaxValue);
            INumVar[] x_ASHP_op = new INumVar[this.Horizon];
            INumVar y_ASHP = cpl.BoolVar();

            // Battery
            INumVar x_Battery = cpl.NumVar(0.0, this.b_MaxBattery);     // kWh
            INumVar[] x_Battery_charge = new INumVar[this.Horizon];     // kW
            INumVar[] x_Battery_discharge = new INumVar[this.Horizon];  // kW
            INumVar[] x_Battery_soc = new INumVar[this.Horizon];        // kWh
            INumVar y_Battery = cpl.BoolVar();

            // TES
            INumVar x_TES = cpl.NumVar(0.0, this.b_MaxTES);             // kWh
            INumVar[] x_TES_charge = new INumVar[this.Horizon];         // kW
            INumVar[] x_TES_discharge = new INumVar[this.Horizon];      // kW
            INumVar[] x_TES_soc = new INumVar[this.Horizon];            // kWh
            INumVar[] y_TES_op = new INumVar[this.Horizon];
            INumVar y_TES = cpl.BoolVar();

            // cool storage. using same parameters as heat storage for now, as in https://doi.org/10.1016/j.energy.2019.02.021
            INumVar x_clgTES = cpl.NumVar(0.0, this.b_MaxTES);             // kWh
            INumVar[] x_clgTES_charge = new INumVar[this.Horizon];         // kW
            INumVar[] x_clgTES_discharge = new INumVar[this.Horizon];      // kW
            INumVar[] x_clgTES_soc = new INumVar[this.Horizon];            // kWh
            INumVar[] y_clgTES_op = new INumVar[this.Horizon];
            INumVar y_clgTES = cpl.BoolVar();

            for (int t = 0; t < this.Horizon; t++)
            {
                x_DrElecNeg[t] = cpl.NumVar(0, this.ElectricityDemand[t] * a_DrElec);
                x_DrElecPos[t] = cpl.NumVar(0, this.ElectricityDemand[t] * a_DrElec);
                y_DrElecNeg[t] = cpl.BoolVar();
                y_DrElecPos[t] = cpl.BoolVar();
                x_DrHeatNeg[t] = cpl.NumVar(0, this.HeatingDemand[t] * a_DrHeat);
                x_DrHeatPos[t] = cpl.NumVar(0, this.HeatingDemand[t] * a_DrHeat);
                y_DrHeatNeg[t] = cpl.BoolVar();
                y_DrHeatPos[t] = cpl.BoolVar();
                x_DrCoolNeg[t] = cpl.NumVar(0, this.CoolingDemand[t] * a_DrCool);
                x_DrCoolPos[t] = cpl.NumVar(0, this.CoolingDemand[t] * a_DrCool);
                y_DrCoolNeg[t] = cpl.BoolVar();
                y_DrCoolPos[t] = cpl.BoolVar();

                y_FeedIn[t] = cpl.BoolVar();
                x_Purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_FeedIn[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_PV_production[t] = cpl.LinearNumExpr();

                x_CHP_op_e[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_CHP_op_th[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_CHP_op_dump[t] = cpl.NumVar(0.0, System.Double.MaxValue);

                x_ElecChiller_op[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_Boiler_op[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_BiomassBoiler_op[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_ASHP_op[t] = cpl.NumVar(0.0, System.Double.MaxValue);

                x_Battery_charge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_Battery_discharge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_Battery_soc[t] = cpl.NumVar(0.0, System.Double.MaxValue);

                x_TES_charge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_TES_discharge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_TES_soc[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                y_TES_op[t] = cpl.BoolVar();

                x_clgTES_charge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_clgTES_discharge[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                x_clgTES_soc[t] = cpl.NumVar(0.0, System.Double.MaxValue);
                y_clgTES_op[t] = cpl.BoolVar();
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// Constraints
            /// ////////////////////////////////////////////////////////////////////////
            /// 

            // meeting demands
            ILinearNumExpr carbonEmissions = cpl.LinearNumExpr();
            ILinearNumExpr biomassConsumptionTotal = cpl.LinearNumExpr();
            for (int t = 0; t < this.Horizon; t++)
            {
                ILinearNumExpr elecGeneration = cpl.LinearNumExpr();
                ILinearNumExpr elecAdditionalDemand = cpl.LinearNumExpr();
                ILinearNumExpr thermalGeneration = cpl.LinearNumExpr();
                ILinearNumExpr thermalAdditionalDemand = cpl.LinearNumExpr();
                ILinearNumExpr coolingGeneration = cpl.LinearNumExpr();
                ILinearNumExpr coolingAdditionalDemand = cpl.LinearNumExpr();

                /// ////////////////////////////////////////////////////////////////////////
                /// Cooling
                coolingGeneration.AddTerm(1, x_ElecChiller_op[t]);
                coolingGeneration.AddTerm(1, x_clgTES_discharge[t]);
                coolingGeneration.AddTerm(1, x_DrCoolPos[t]);
                elecAdditionalDemand.AddTerm(1 / this.c_ElecChiller_eff_clg, x_ElecChiller_op[t]);
                coolingAdditionalDemand.AddTerm(1, x_clgTES_charge[t]);
                coolingAdditionalDemand.AddTerm(1, x_DrCoolNeg[t]);

                /// ////////////////////////////////////////////////////////////////////////
                /// Heating
                thermalGeneration.AddTerm(1, x_Boiler_op[t]);
                thermalGeneration.AddTerm(1, x_BiomassBoiler_op[t]);
                thermalGeneration.AddTerm(1, x_CHP_op_th[t]);
                thermalGeneration.AddTerm(1, x_ASHP_op[t]);
                thermalGeneration.AddTerm(1, x_TES_discharge[t]);
                thermalGeneration.AddTerm(1, x_DrHeatPos[t]);
                elecAdditionalDemand.AddTerm(1 / this.a_ASHP_Efficiency[t], x_ASHP_op[t]);
                thermalAdditionalDemand.AddTerm(1, x_TES_charge[t]);
                thermalAdditionalDemand.AddTerm(1, x_CHP_op_dump[t]);
                thermalAdditionalDemand.AddTerm(1, x_DrHeatNeg[t]);

                /// ////////////////////////////////////////////////////////////////////////
                /// Electricity
                // elec demand must be met by PV production, battery and grid, minus feed in
                for (int i = 0; i < this.NumberOfSolarAreas; i++)
                {
                    double pvElec = this.SolarLoads[i][t] * 0.001 * this.a_PV_Efficiency[i][t];
                    elecGeneration.AddTerm(pvElec, x_PV[i]);
                    x_PV_production[t].AddTerm(pvElec, x_PV[i]);
                    OM_PV += pvElec * this.c_PV_OM;
                }
                elecGeneration.AddTerm(1, x_Purchase[t]);
                elecGeneration.AddTerm(1, x_Battery_discharge[t]);
                elecGeneration.AddTerm(1, x_CHP_op_e[t]);
                elecGeneration.AddTerm(1, x_DrElecPos[t]);
                elecAdditionalDemand.AddTerm(1, x_FeedIn[t]);
                elecAdditionalDemand.AddTerm(1, x_Battery_charge[t]);
                elecAdditionalDemand.AddTerm(1, x_DrElecNeg[t]);


                /// ////////////////////////////////////////////////////////////////////////
                /// Demand Response Constraints
                cpl.Le(cpl.Sum(y_DrElecNeg[t], y_DrElecPos[t]), 1);         // only allow either positive or negative demand shifting
                cpl.AddLe(x_DrElecPos[t], cpl.Prod(M, y_DrElecPos[t]));     // toggle boolean on if positive demand response is activated
                cpl.AddLe(x_DrElecNeg[t], cpl.Prod(M, y_DrElecNeg[t]));     // toggle boolean on if negative demand response is activated
                cpl.Le(cpl.Sum(y_DrHeatNeg[t], y_DrHeatPos[t]), 1);
                cpl.AddLe(x_DrHeatPos[t], cpl.Prod(M, y_DrHeatPos[t]));
                cpl.AddLe(x_DrHeatNeg[t], cpl.Prod(M, y_DrHeatNeg[t]));
                cpl.Le(cpl.Sum(y_DrCoolNeg[t], y_DrCoolPos[t]), 1);
                cpl.AddLe(x_DrCoolPos[t], cpl.Prod(M, y_DrCoolPos[t]));
                cpl.AddLe(x_DrCoolNeg[t], cpl.Prod(M, y_DrCoolNeg[t]));


                /// ////////////////////////////////////////////////////////////////////////
                /// PV Technical Constraints
                // pv production must be greater equal feedin
                cpl.AddGe(x_PV_production[t], x_FeedIn[t]);
                // donnot allow feedin and purchase at the same time. y = 1 means elec is produced
                cpl.AddLe(x_Purchase[t], cpl.Prod(M, y_FeedIn[t]));
                cpl.AddLe(x_FeedIn[t], cpl.Prod(M, cpl.Diff(1, y_FeedIn[t])));


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
                /// Biomass availability per year
                biomassConsumptionTotal.AddTerm(this.ClustersizePerTimestep[t] / this.a_bmboi_eff, x_BiomassBoiler_op[t]);


                /// ////////////////////////////////////////////////////////////////////////
                /// Sizing
                cpl.AddLe(x_ElecChiller_op[t], x_ElecChiller);
                cpl.AddLe(x_CHP_op_e[t], x_CHP);
                cpl.AddLe(x_Boiler_op[t], x_Boiler);
                cpl.AddLe(x_BiomassBoiler_op[t], x_BiomassBoiler);
                cpl.AddLe(x_ASHP_op[t], x_ASHP);


                /// ////////////////////////////////////////////////////////////////////////
                /// Emissions
                carbonEmissions.AddTerm(this.ClustersizePerTimestep[t] * this.lca_GridElectricity, x_Purchase[t]);     // data needs to be kgCO2eq./kWh
                carbonEmissions.AddTerm(this.ClustersizePerTimestep[t] * this.lca_NaturalGas * 1 / this.a_boi_eff, x_Boiler_op[t]);
                carbonEmissions.AddTerm(this.ClustersizePerTimestep[t] * this.lca_NaturalGas * 1 / this.c_chp_eff_el, x_CHP_op_e[t]);
                carbonEmissions.AddTerm(this.ClustersizePerTimestep[t] * this.lca_Biomass * 1 / this.a_bmboi_eff, x_BiomassBoiler_op[t]);


                /// ////////////////////////////////////////////////////////////////////////
                /// Energy Balance
                cpl.AddEq(cpl.Diff(coolingGeneration, coolingAdditionalDemand), this.CoolingDemand[t]);
                cpl.AddEq(cpl.Diff(thermalGeneration, thermalAdditionalDemand), this.HeatingDemand[t]);
                cpl.AddGe(cpl.Diff(elecGeneration, elecAdditionalDemand), this.ElectricityDemand[t]);
            }


            // ////////////////////////////////////////////////////////////////////////
            /// Demand Response Model
            ILinearNumExpr DrElecPos = cpl.LinearNumExpr();
            ILinearNumExpr DrElecNeg = cpl.LinearNumExpr();
            ILinearNumExpr DrHeatPos = cpl.LinearNumExpr();
            ILinearNumExpr DrHeatNeg = cpl.LinearNumExpr();
            ILinearNumExpr DrCoolPos = cpl.LinearNumExpr();
            ILinearNumExpr DrCoolNeg = cpl.LinearNumExpr();
            for (int t = 0; t < this.Horizon; t++)
            {
                DrElecPos.AddTerm(1, x_DrElecPos[t]);
                DrElecNeg.AddTerm(1, x_DrElecNeg[t]);
                DrHeatPos.AddTerm(1, x_DrHeatPos[t]);
                DrHeatNeg.AddTerm(1, x_DrHeatNeg[t]);
                DrCoolPos.AddTerm(1, x_DrCoolPos[t]);
                DrCoolNeg.AddTerm(1, x_DrCoolNeg[t]);

                if ((t + 1) % 24 == 0)
                {
                    cpl.AddEq(DrElecPos, DrElecNeg);
                    DrElecNeg = cpl.LinearNumExpr();
                    DrElecPos = cpl.LinearNumExpr();

                    cpl.AddEq(DrHeatPos, DrHeatNeg);
                    DrHeatNeg = cpl.LinearNumExpr();
                    DrHeatPos = cpl.LinearNumExpr();

                    cpl.AddEq(DrCoolPos, DrCoolNeg);
                    DrCoolNeg = cpl.LinearNumExpr();
                    DrCoolPos = cpl.LinearNumExpr();
                }
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// Total Biomass consumption per year
            cpl.AddLe(biomassConsumptionTotal, this.b_maxbiomassperyear);


            /// ////////////////////////////////////////////////////////////////////////
            /// battery model
            for (int t = 0; t < this.Horizon; t++)
            {
                ILinearNumExpr batteryState = cpl.LinearNumExpr();
                batteryState.AddTerm((1 - this.bat_decay), x_Battery_soc[t]);
                batteryState.AddTerm(this.bat_ch_eff, x_Battery_charge[t]);
                batteryState.AddTerm(-1 / this.bat_disch_eff, x_Battery_discharge[t]);
                if (t == this.Horizon - 1)
                    cpl.AddEq(x_Battery_soc[0], batteryState);
                else
                    cpl.AddEq(x_Battery_soc[t + 1], batteryState);

                if ((t + 1) % 24 == 0)
                {
                    if (t != this.Horizon - 1)
                        cpl.AddEq(x_Battery_soc[t + 1], x_Battery_soc[t + 1 - 24]);
                    cpl.AddEq(x_Battery_discharge[t], 0);
                    cpl.AddEq(x_Battery_charge[t], 0);
                }
            }
            cpl.AddGe(x_Battery_soc[0], cpl.Prod(x_Battery, this.bat_min_state));                 // initial state of battery >= min_state

            for (int t = 0; t < this.Horizon; t++)
            {
                cpl.AddGe(x_Battery_soc[t], cpl.Prod(x_Battery, this.bat_min_state));     // min state of charge
                cpl.AddLe(x_Battery_charge[t], cpl.Prod(x_Battery, this.bat_max_ch));        // battery charging
                cpl.AddLe(x_Battery_discharge[t], cpl.Prod(x_Battery, this.bat_max_disch));  // battery discharging
                cpl.AddLe(x_Battery_soc[t], x_Battery);                                   // battery sizing
            }

            /// ////////////////////////////////////////////////////////////////////////
            /// TES  model
            for (int t = 0; t < this.Horizon; t++)
            {
                ILinearNumExpr tesState = cpl.LinearNumExpr();
                tesState.AddTerm((1 - this.tes_decay), x_TES_soc[t]);
                tesState.AddTerm(this.tes_ch_eff, x_TES_charge[t]);
                tesState.AddTerm(-1 / this.tes_disch_eff, x_TES_discharge[t]);
                if (t == this.Horizon - 1)
                    cpl.AddEq(x_TES_soc[0], tesState);
                else
                    cpl.AddEq(x_TES_soc[t + 1], tesState);

                if ((t + 1) % 24 == 0)
                {
                    if (t != this.Horizon - 1)
                        cpl.AddEq(x_TES_soc[t + 1], x_TES_soc[t + 1 - 24]);
                    cpl.AddEq(x_TES_discharge[t], 0);
                    cpl.AddEq(x_TES_charge[t], 0);
                }
            }

            for (int t = 0; t < this.Horizon; t++)
            {
                cpl.AddLe(x_TES_charge[t], cpl.Prod(x_TES, this.tes_max_ch));
                cpl.AddLe(x_TES_discharge[t], cpl.Prod(x_TES, this.tes_max_disch));
                cpl.AddLe(x_TES_soc[t], x_TES);

                // donnot allow charge and discharge at the same time. y = 1 means charging
                cpl.AddLe(x_TES_charge[t], cpl.Prod(M, y_TES_op[t]));
                cpl.AddLe(x_TES_discharge[t], cpl.Prod(M, cpl.Diff(1, y_TES_op[t])));
            }


            // ////////////////////////////////////////////////////////////////////////
            /// cool storage model
            for (int t = 0; t < this.Horizon; t++)
            {
                ILinearNumExpr clgTesState = cpl.LinearNumExpr();
                clgTesState.AddTerm((1 - this.tes_decay), x_clgTES_soc[t]);
                clgTesState.AddTerm(this.tes_ch_eff, x_clgTES_charge[t]);
                clgTesState.AddTerm(-1 / this.tes_disch_eff, x_clgTES_discharge[t]);
                if (t == this.Horizon - 1)
                    cpl.AddEq(x_clgTES_soc[0], clgTesState);
                else
                    cpl.AddEq(x_clgTES_soc[t + 1], clgTesState);

                if ((t + 1) % 24 == 0)
                {
                    if (t != this.Horizon - 1)
                        cpl.AddEq(x_clgTES_soc[t + 1], x_clgTES_soc[t + 1 - 24]);
                    cpl.AddEq(x_clgTES_discharge[t], 0);
                    cpl.AddEq(x_clgTES_charge[t], 0);
                }
            }

            for (int t = 0; t < this.Horizon; t++)
            {
                cpl.AddLe(x_clgTES_charge[t], cpl.Prod(x_clgTES, this.tes_max_ch));
                cpl.AddLe(x_clgTES_discharge[t], cpl.Prod(x_clgTES, this.tes_max_disch));
                cpl.AddLe(x_clgTES_soc[t], x_clgTES);

                // donnot allow charge and discharge at the same time. y = 1 means charging
                cpl.AddLe(x_clgTES_charge[t], cpl.Prod(M, y_clgTES_op[t]));
                cpl.AddLe(x_clgTES_discharge[t], cpl.Prod(M, cpl.Diff(1, y_clgTES_op[t])));
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// Binary selection variables
            /// ////////////////////////////////////////////////////////////////////////
            cpl.AddLe(x_Battery, cpl.Prod(M, y_Battery));
            cpl.AddGe(x_Battery, cpl.Prod(this.minCapBattery, y_Battery));
            cpl.AddLe(x_TES, cpl.Prod(M, y_TES));
            cpl.AddGe(x_TES, cpl.Prod(this.minCapTES, y_TES));
            cpl.AddLe(x_clgTES, cpl.Prod(M, y_clgTES));
            cpl.AddGe(x_clgTES, cpl.Prod(this.minCapTES, y_clgTES));
            cpl.AddLe(x_Boiler, cpl.Prod(M, y_Boiler));
            cpl.AddGe(x_Boiler, cpl.Prod(this.minCapBoiler, y_Boiler));
            cpl.AddLe(x_BiomassBoiler, cpl.Prod(M, y_BiomassBoiler));
            cpl.AddGe(x_BiomassBoiler, cpl.Prod(this.minCapBioBoiler, y_BiomassBoiler));
            cpl.AddLe(x_CHP, cpl.Prod(M, y_CHP));
            cpl.AddGe(x_CHP, cpl.Prod(this.minCapCHP, y_CHP));
            cpl.AddLe(x_ElecChiller, cpl.Prod(M, y_ElecChiller));
            cpl.AddGe(x_ElecChiller, cpl.Prod(this.minCapElecChiller, y_ElecChiller));
            cpl.AddLe(x_ASHP, cpl.Prod(M, y_ASHP));
            cpl.AddGe(x_ASHP, cpl.Prod(this.minCapASHP, y_ASHP));
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
            {
                cpl.AddLe(x_PV[i], cpl.Prod(M, y_PV[i]));
                cpl.AddGe(x_PV[i], cpl.Prod(0.0, y_PV[i]));
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// embodied carbon emissions of all technologies
            /// ////////////////////////////////////////////////////////////////////////
            for (int i = 0; i < this.NumberOfSolarAreas; i++)
                carbonEmissions.AddTerm(this.lca_PV, x_PV[i]);
            carbonEmissions.AddTerm(this.lca_Battery, x_Battery);
            carbonEmissions.AddTerm(this.lca_ElecChiller, x_ElecChiller);
            carbonEmissions.AddTerm(this.lca_ASHP, x_ASHP);
            carbonEmissions.AddTerm(this.lca_Boiler, x_Boiler);
            carbonEmissions.AddTerm(this.lca_BiomassBoiler, x_BiomassBoiler);
            carbonEmissions.AddTerm(this.lca_CHP, x_CHP);
            carbonEmissions.AddTerm(this.lca_TES, x_TES);
            carbonEmissions.AddTerm(this.lca_TES, x_clgTES);
            carbonEmissions.AddTerm(this.lca_HeatExchanger * TotHXsizing, dh_dummy);
            carbonEmissions.AddTerm(this.lca_HeatExchanger * TotHXsizingClg, dh_dummy);
            carbonEmissions.AddTerm(this.lca_DistrictHeating * this.NetworkLengthTotal * 2, dh_dummy); // *2 because I have separate cooling and heating network
            carbonEmissions.AddTerm(this.lca_CoolingTower * solution.x_clgtower, clgtower_dummy);

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
            {
                capex.AddTerm(this.c_PV, x_PV[i]);
                capex.AddTerm(this.c_fix_PV, y_PV[i]);
            }

            capex.AddTerm(this.c_Battery, x_Battery);
            capex.AddTerm(this.c_fix_Battery, y_Battery);
            capex.AddTerm(this.c_ElecChiller, x_ElecChiller);
            capex.AddTerm(this.c_fix_ElecChiller, y_ElecChiller);
            capex.AddTerm(this.c_ASHP, x_ASHP);
            capex.AddTerm(this.c_fix_ASHP, y_ASHP);
            capex.AddTerm(this.c_Boiler, x_Boiler);
            capex.AddTerm(this.c_fix_Boiler, y_Boiler);
            capex.AddTerm(this.c_BiomassBoiler, x_BiomassBoiler);
            capex.AddTerm(this.c_fix_BiomassBoiler, y_BiomassBoiler);
            capex.AddTerm(this.c_CHP, x_CHP);
            capex.AddTerm(this.c_fix_CHP, y_CHP);
            capex.AddTerm(this.c_TES, x_TES);
            capex.AddTerm(this.c_fix_TES, y_TES);
            capex.AddTerm(this.c_TES, x_clgTES);
            capex.AddTerm(this.c_fix_TES, y_clgTES);
            capex.AddTerm(TotLevCostDH, dh_dummy);
            capex.AddTerm(this.c_CoolingTower * solution.x_clgtower, clgtower_dummy);
            capex.AddTerm(this.c_fix_CoolingTower, clgtower_dummy);

            for (int t = 0; t < this.Horizon; t++)
            {
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_NaturalGas / this.c_chp_eff_el, x_CHP_op_e[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_NaturalGas / this.a_boi_eff, x_Boiler_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_Biomass / this.a_bmboi_eff, x_BiomassBoiler_op[t]);

                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_Grid[t], x_Purchase[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_FeedIn[t], x_FeedIn[t]);

                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_Battery_OM, x_Battery_discharge[t]);    // assuming discharging is causing deterioration
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_Boiler_OM, x_Boiler_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_BiomassBoiler_OM, x_BiomassBoiler_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_ElecChiller_OM, x_ElecChiller_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_CHP_OM, x_CHP_op_e[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_ASHP_OM, x_ASHP_op[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_TES_OM, x_TES_discharge[t]);
                opex.AddTerm(this.ClustersizePerTimestep[t] * this.c_TES_OM, x_clgTES_discharge[t]);
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// Objective function
            /// ////////////////////////////////////////////////////////////////////////
            if (isCostMinimization) cpl.AddMinimize(cpl.Sum(capex, cpl.Sum(OM_PV, opex)));
            else cpl.AddMinimize(carbonEmissions);

            // epsilon constraints for carbon, 
            // or cost constraint in case of carbon minimization (the same reason why carbon minimization needs a cost constraint)
            if (hasCarbonConstraint && isCostMinimization) cpl.AddLe(carbonEmissions, (double)carbonConstraint);
            else if (hasCostConstraint && !isCostMinimization) cpl.AddLe(cpl.Sum(capex, cpl.Sum(OM_PV, opex)), (double)costConstraint);


            /// ////////////////////////////////////////////////////////////////////////
            /// Solve
            /// ////////////////////////////////////////////////////////////////////////
            if (!verbose) cpl.SetOut(null);
            cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.005);
            cpl.SetParam(Cplex.IntParam.MIPDisplay, 4);
            //if (!this.multithreading)
            //    cpl.SetParam(Cplex.Param.Threads, 1);

            try
            {
                bool success = cpl.Solve();
                if (!success)
                {
                    solution.infeasible = true;
                    return solution;
                }
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
                solution.x_clgtes = cpl.GetValue(x_clgTES);
                solution.x_chp = cpl.GetValue(x_CHP);
                solution.x_boi = cpl.GetValue(x_Boiler);
                solution.x_bmboi = cpl.GetValue(x_BiomassBoiler);
                solution.x_hp = cpl.GetValue(x_ASHP);
                solution.x_ac = cpl.GetValue(x_ElecChiller);

                solution.x_dr_elec_neg = new double[this.Horizon];
                solution.x_dr_elec_pos = new double[this.Horizon];
                solution.x_dr_heat_neg = new double[this.Horizon];
                solution.x_dr_heat_pos = new double[this.Horizon];
                solution.x_dr_cool_neg = new double[this.Horizon];
                solution.x_dr_cool_pos = new double[this.Horizon];

                solution.b_pvprod = new double[this.Horizon];
                solution.x_bat_charge = new double[this.Horizon];
                solution.x_bat_discharge = new double[this.Horizon];
                solution.x_bat_soc = new double[this.Horizon];
                solution.x_elecpur = new double[this.Horizon];
                solution.x_feedin = new double[this.Horizon];
                solution.x_boi_op = new double[this.Horizon];
                solution.x_bmboi_op = new double[this.Horizon];
                solution.x_ac_op = new double[this.Horizon];
                solution.x_hp_op = new double[this.Horizon];
                solution.x_chp_op_e = new double[this.Horizon];
                solution.x_chp_op_h = new double[this.Horizon];
                solution.x_chp_dump = new double[this.Horizon];
                solution.x_tes_charge = new double[this.Horizon];
                solution.x_tes_discharge = new double[this.Horizon];
                solution.x_tes_soc = new double[this.Horizon];
                solution.x_clgtes_charge = new double[this.Horizon];
                solution.x_clgtes_discharge = new double[this.Horizon];
                solution.x_clgtes_soc = new double[this.Horizon];
                solution.clustersize = new int[this.Horizon];
                for (int t = 0; t < this.Horizon; t++)
                {
                    solution.x_dr_elec_neg[t] = cpl.GetValue(x_DrElecNeg[t]);
                    solution.x_dr_elec_pos[t] = cpl.GetValue(x_DrElecPos[t]);
                    solution.x_dr_heat_neg[t] = cpl.GetValue(x_DrHeatNeg[t]);
                    solution.x_dr_heat_pos[t] = cpl.GetValue(x_DrHeatPos[t]);
                    solution.x_dr_cool_neg[t] = cpl.GetValue(x_DrCoolNeg[t]);
                    solution.x_dr_cool_pos[t] = cpl.GetValue(x_DrCoolPos[t]);

                    solution.b_pvprod[t] = cpl.GetValue(x_PV_production[t]);
                    solution.x_bat_charge[t] = cpl.GetValue(x_Battery_charge[t]);
                    solution.x_bat_discharge[t] = cpl.GetValue(x_Battery_discharge[t]);
                    solution.x_bat_soc[t] = cpl.GetValue(x_Battery_soc[t]);
                    solution.x_elecpur[t] = cpl.GetValue(x_Purchase[t]);
                    solution.x_feedin[t] = cpl.GetValue(x_FeedIn[t]);
                    solution.x_boi_op[t] = cpl.GetValue(x_Boiler_op[t]);
                    solution.x_bmboi_op[t] = cpl.GetValue(x_BiomassBoiler_op[t]);
                    solution.x_ac_op[t] = cpl.GetValue(x_ElecChiller_op[t]);
                    solution.x_hp_op[t] = cpl.GetValue(x_ASHP_op[t]);
                    solution.x_chp_op_e[t] = cpl.GetValue(x_CHP_op_e[t]);
                    solution.x_chp_op_h[t] = cpl.GetValue(x_CHP_op_th[t]);
                    solution.x_chp_dump[t] = cpl.GetValue(x_CHP_op_dump[t]);
                    solution.x_tes_charge[t] = cpl.GetValue(x_TES_charge[t]);
                    solution.x_tes_discharge[t] = cpl.GetValue(x_TES_discharge[t]);
                    solution.x_tes_soc[t] = cpl.GetValue(x_TES_soc[t]);
                    solution.x_clgtes_charge[t] = cpl.GetValue(x_clgTES_charge[t]);
                    solution.x_clgtes_discharge[t] = cpl.GetValue(x_clgTES_discharge[t]);
                    solution.x_clgtes_soc[t] = cpl.GetValue(x_clgTES_soc[t]);

                    solution.clustersize[t] = this.ClustersizePerTimestep[t];
                }

                solution.cost_dh = TotLevCostDH;
                solution.x_hx_dh = new double[this.NumberOfBuildingsInDistrict];
                for (int i = 0; i < this.NumberOfBuildingsInDistrict; i++)
                {
                    solution.x_hx_dh[i] = this.PeakHeatingLoadsPerBuilding[i];
                    solution.x_dh = this.NetworkLengthTotal;
                }

                solution.biomassConsumed = cpl.GetValue(biomassConsumptionTotal);
                return solution;
            }
            catch (ILOG.Concert.Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
                solution.infeasible = true;
                return solution;
            }
        }
    }
}
