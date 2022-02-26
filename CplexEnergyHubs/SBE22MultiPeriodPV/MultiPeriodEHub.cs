using System;
using System.Collections.Generic;
using System.Text;
using ILOG.Concert;
using ILOG.CPLEX;
using EhubMisc;

namespace SBE22MultiPeriodPV
{
    internal class MultiPeriodEHub
    {
        internal MultiPeriodEhubOutput[] Outputs;

        internal int NumPeriods;
        internal int YearsPerPeriod;
        


        #region inputs demand and typical days
        /// ////////////////////////////////////////////////////////////////////////
        /// Demand (might be typical days) and scaling factors (a.k.a. weights)
        /// ////////////////////////////////////////////////////////////////////////
        internal List<double[]> CoolingDemand { get; private set; }
        internal List<double[]> HeatingDemand { get; private set; }
        internal List<double[]> ElectricityDemand { get; private set; }
        internal List<double[][]> SolarLoads { get; private set; }
        internal double[] SolarAreas { get; private set; }

        internal List<int[]> ClustersizePerTimestep { get; private set; }

        internal int NumberOfSolarAreas { get; private set; }

        internal int Horizon { get; private set; }
        #endregion


        #region inputs technical parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// Technical Parameters
        /// ////////////////////////////////////////////////////////////////////////
        internal List<double[]> AmbientTemperature { get; } = new List<double[]>();

        // Lifetime
        internal List<double> LifetimePvMono { get; } = new List<double>();
        internal List<double> LifetimePvCdte { get; } = new List<double>();
        internal List<double> LifetimeBattery { get; } = new List<double>();
        internal List<double> LifetimeTES { get; } = new List<double>();
        internal List<double> LifetimeASHP { get; } = new List<double>();
        internal List<double> LifetimeCHP { get; } = new List<double>();
        internal List<double> LifetimeBoiler { get; } = new List<double>();
        internal List<double> LifetimeBiomassBoiler { get; } = new List<double>();
        internal List<double> LifetimeElecChiller { get; } = new List<double>();
        internal List<double> LifetimeDistrictHeating { get; } = new List<double>();
        internal List<double> LifetimeHeatExchanger { get; } = new List<double>();
        internal List<double> LifetimeCoolingTower { get; } = new List<double>();

        // Coefficients PV
        internal double PvNoct { get; private set; }
        internal double PvTempAmbNoct { get; private set; }
        internal double PvPNoct { get; private set; }
        internal double PvBetaRef { get; private set; }
        internal List<double> PvEtaRefMono { get; } = new List<double>();
        internal List<double> PvEtaRefCdte { get; } = new List<double>();
        internal List<double[][]> PvEfficiencyMono { get; } = new List<double[][]>();
        internal List<double[][]> PvEfficiencyCdte { get; } = new List<double[][]>();

        // Coefficients ASHP
        internal double HpPi1 { get; private set; }
        internal double HpPi2 { get; private set; }
        internal double HpPi3 { get; private set; }
        internal double HpPi4 { get; private set; }
        internal List<double> HpSupplyTemp { get; } = new List<double>();
        internal List<double[]> AshpEfficiency { get; } = new List<double[]>();

        // Coefficients natural gas and biomass boilers
        internal List<double> BoilerEfficiency { get; } = new List<double>();
        internal List<double> BiomassBoilerEfficiency { get; } = new List<double>();
        internal List<double> MaxBiomassPerYear { get; } = new List<double>();  // kWh biomass per year

        // Coefficients CHP
        internal List<double> c_chp_eff_el { get; } = new List<double>();      // electric efficiency. so 1 kWh of gas results in 0.3 kWh of elec
        internal List<double> c_chp_htp { get; } = new List<double>();         // heat to power ratio (e.g. htp = 1.73, then 1.73 kW of heat is produced for 1 kW of elec)
        internal List<double> c_chp_heatdump { get; } = new List<double>();    // heat dump allowed = 1

        // Coefficients Electric Chiller
        internal List<double> c_ElecChiller_eff_clg { get; } = new List<double>();
        internal List<double> c_ElecChiller_eff_htg { get; } = new List<double>();


        // Coefficients Battery
        internal List<double> bat_ch_eff { get; } = new List<double>();        // Battery charging efficiency
        internal List<double> bat_disch_eff { get; } = new List<double>();     // Battery discharging efficiency
        internal List<double> bat_decay { get; } = new List<double>();         // Battery hourly decay
        internal List<double> bat_max_ch { get; } = new List<double>();        // Battery max charging rate
        internal List<double> bat_max_disch { get; } = new List<double>();     // Battery max discharging rate
        internal List<double> bat_min_state { get; } = new List<double>();     // Battery minimum state of charge
        internal List<double> b_MaxBattery { get; } = new List<double>();      // maximal battery capacity. constraint    

        // Coefficients Thermal Energy Storage
        internal List<double> tes_ch_eff { get; } = new List<double>();
        internal List<double> tes_disch_eff { get; } = new List<double>();
        internal List<double> tes_decay { get; } = new List<double>();
        internal List<double> tes_max_ch { get; } = new List<double>();
        internal List<double> tes_max_disch { get; } = new List<double>();
        internal List<double> b_MaxTES { get; } = new List<double>();

        // Minimal Capacities
        internal List<double> minCapBattery { get; } = new List<double>();
        internal List<double> minCapTES { get; } = new List<double>();
        internal List<double> minCapBoiler { get; } = new List<double>();
        internal List<double> minCapBioBoiler { get; } = new List<double>();
        internal List<double> minCapCHP { get; } = new List<double>();
        internal List<double> minCapElecChiller { get; } = new List<double>();
        internal List<double> minCapASHP { get; } = new List<double>();

        #endregion


        #region inputs LCA parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// LCA
        /// ////////////////////////////////////////////////////////////////////////
        internal List<double> LcaGridElectricity { get; } = new List<double>();
        internal List<double> LcaNaturalGas { get; } = new List<double>();
        internal List<double> LcaBiomass { get; } = new List<double>();

        // annual LCA of technologies
        internal List<double> LcaAnnualPvMono { get; } = new List<double>();
        internal List<double> LcaAnnualPvCdte { get; } = new List<double>();
        internal List<double> LcaAnnualBattery { get; } = new List<double>();
        internal List<double> LcaAnnualTes { get; } = new List<double>();
        internal List<double> LcaAnnualAshp { get; } = new List<double>();
        internal List<double> LcaAnnualChp { get; } = new List<double>();
        internal List<double> LcaAnnualBoiler { get; } = new List<double>();
        internal List<double> LcaAnnualBiomassBoiler { get; } = new List<double>();
        internal List<double> LcaAnnualElecChiller { get; } = new List<double>();
        internal List<double> LcaAnnualDistrictHeating { get; } = new List<double>();
        internal List<double> LcaAnnualHeatExchanger { get; } = new List<double>();
        internal List<double> LcaAnnualCoolingTower { get; } = new List<double>();


        // total (non-annualized) LCA of technologies 
        internal List<double> LcaTotalPvMono { get; } = new List<double>();
        internal List<double> LcaTotalPvCdte { get; } = new List<double>();
        internal List<double> LcaTotalBattery { get; } = new List<double>();
        internal List<double> LcaTotalTes { get; } = new List<double>();
        internal List<double> LcaTotalAshp { get; } = new List<double>();
        internal List<double> LcaTotalChp { get; } = new List<double>();
        internal List<double> LcaTotalBoiler { get; } = new List<double>();
        internal List<double> LcaTotalBiomassBoiler { get; } = new List<double>();
        internal List<double> LcaTotalElecChiller { get; } = new List<double>();
        internal List<double> LcaTotalDistrictHeating { get; } = new List<double>();
        internal List<double> LcaTotalHeatExchanger { get; } = new List<double>();
        internal List<double> LcaTotalCoolingTower { get; } = new List<double>();
        #endregion

        #region inputs cost parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// Cost Parameters
        /// ////////////////////////////////////////////////////////////////////////
        internal List<double> InterestRate { get; } = new List<double>();


        // Linear Investment Cost
        internal List<double> LinearCostPvMono { get; } = new List<double>();
        internal List<double> LinearCostPvCdte { get; } = new List<double>();
        internal List<double> LinearCostBattery { get; } = new List<double>();
        internal List<double> LinearCostTes { get; } = new List<double>();
        internal List<double> LinearCostBoiler { get; } = new List<double>();
        internal List<double> LinearCostBiomassBoiler { get; } = new List<double>();
        internal List<double> LinearCostChpElectric { get; } = new List<double>();// cost per kW of electric power
        internal List<double> LinearCostElecChiller { get; } = new List<double>();
        internal List<double> LinearCostAshp { get; } = new List<double>();
        internal List<double> LinearCostDistrictHeating { get; } = new List<double>();
        internal List<double> LinearCostHeatExchanger { get; } = new List<double>();
        internal List<double> LinearCostCoolingTower { get; } = new List<double>();

        // Fix Cost
        internal List<double> FixCostPvMono { get; } = new List<double>();
        internal List<double> FixCostPVCdte { get; } = new List<double>();
        internal List<double> FixCostBattery { get; } = new List<double>();
        internal List<double> FixCostTES { get; } = new List<double>();
        internal List<double> FixCostBoiler { get; } = new List<double>();
        internal List<double> FixCostBiomassBoiler { get; } = new List<double>();
        internal List<double> FixCostCHP { get; } = new List<double>();
        internal List<double> FixCostElecChiller { get; } = new List<double>();
        internal List<double> FixCostASHP { get; } = new List<double>();
        internal List<double> FixCostDistrictHeating { get; } = new List<double>();
        internal List<double> FixCostHeatExchanger { get; } = new List<double>();
        internal List<double> FixCostCoolingTower { get; } = new List<double>();


        // operation and maintenance cost
        internal List<double> OmCostPV { get; } = new List<double>();
        internal List<double> OmCostBattery { get; } = new List<double>();
        internal List<double> OmCostTes { get; } = new List<double>();
        internal List<double> OmCostBoiler { get; } = new List<double>();
        internal List<double> OmCostBiomassBoiler { get; } = new List<double>();
        internal List<double> OmCostChp { get; } = new List<double>();
        internal List<double> OmCostElecChiller { get; } = new List<double>();
        internal List<double> OmCostAshp { get; } = new List<double>();


        // (time resolved) operation cost
        internal List<double[]> OperationCostGrid { get; } = new List<double[]>();
        internal List<double[]> OperationRevenueFeedIn { get; } = new List<double[]>();
        internal List<double> OperationCostNaturalGas { get; } = new List<double>();
        internal List<double> OperationCostBiomass { get; } = new List<double>();
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



        internal MultiPeriodEHub(List<double[]> heatingDemand, List<double[]> coolingDemand,
            List<double[]> electricityDemand,
            List<double[][]> irradiance, double[] solarTechSurfaceAreas,
            List<double[]> ambientTemperature, List<Dictionary<string, double>> technologyParameters,
            List<int[]> clustersizePerTimestep, int yearsPerPeriod)
        {
            this.CoolingDemand = coolingDemand;
            this.HeatingDemand = heatingDemand;
            this.ElectricityDemand = electricityDemand;
            this.SolarLoads = irradiance;
            this.SolarAreas = solarTechSurfaceAreas;

            this.NumberOfSolarAreas = solarTechSurfaceAreas.Length;
            this.ClustersizePerTimestep = clustersizePerTimestep;
            this.NumPeriods = heatingDemand.Count;
            this.Horizon = heatingDemand[0].Length; // this assumes each year has the same number of typical days
            this.YearsPerPeriod = yearsPerPeriod;

            /// read in these parameters as struct parameters
            this.AmbientTemperature = ambientTemperature;
            this.SetParameters(technologyParameters);
        }

        private void SetParameters(List<Dictionary<string, double>> technologyParameters)
        {
            this.NumberOfBuildingsInDistrict = technologyParameters[0].ContainsKey("NumberOfBuildingsInEHub")
                ? Convert.ToInt32(technologyParameters[0]["NumberOfBuildingsInEHub"])
                : 1;
            this.PeakHeatingLoadsPerBuilding = new double[this.NumberOfBuildingsInDistrict];
            this.PeakCoolingLoadsPerBuilding = new double[this.NumberOfBuildingsInDistrict];

            for (int p = 0; p < technologyParameters.Count; p++)
            {
                /// ////////////////////////////////////////////////////////////////////////
                /// Technical Parameters
                /// ////////////////////////////////////////////////////////////////////////

                // floor area
                double _floorarea;
                if (technologyParameters[p].ContainsKey("TotalFloorArea"))
                    _floorarea = technologyParameters[p]["TotalFloorArea"];
                else
                    _floorarea = 1000.0;

                // Electric Chiller
                this.c_ElecChiller_eff_clg.Add(4.9);
                this.c_ElecChiller_eff_htg.Add(5.8);

                // PV
                this.PvNoct = 45.0;
                this.PvTempAmbNoct = 20.0;
                this.PvPNoct = 800.0;
                this.PvBetaRef = 0.004;
                if (technologyParameters[p].ContainsKey("pv_n_ref_mono"))
                    this.PvEtaRefMono.Add(technologyParameters[p]["pv_n_ref_mono"]);
                else
                    this.PvEtaRefMono.Add(0.2);
                if (technologyParameters[p].ContainsKey("pv_n_ref_cdte"))
                    this.PvEtaRefCdte.Add(technologyParameters[p]["pv_n_ref_cdte"]);
                else
                    this.PvEtaRefCdte.Add(0.2);

                // ASHP
                this.HpPi1 = 13.39;
                this.HpPi2 = -0.047;
                this.HpPi3 = 1.109;
                this.HpPi4 = 0.012;
                if (technologyParameters[p].ContainsKey("hp_supplyTemp"))
                    this.HpSupplyTemp.Add(technologyParameters[p]["hp_supplyTemp"]);
                else
                    this.HpSupplyTemp.Add(65.0);

                // Naural Gas Boiler
                if (technologyParameters[p].ContainsKey("a_boi_eff"))
                    this.BoilerEfficiency.Add(technologyParameters[p]["a_boi_eff"]);
                else
                    this.BoilerEfficiency.Add(0.94);

                // Biomass Boiler
                if (technologyParameters[p].ContainsKey("a_bmboi_eff"))
                    this.BiomassBoilerEfficiency.Add(technologyParameters[p]["a_bmboi_eff"]);
                else
                    this.BiomassBoilerEfficiency.Add(0.9);
                if (technologyParameters[p].ContainsKey("b_MaxBiomassAvailable"))
                    this.MaxBiomassPerYear.Add(technologyParameters[p]["b_MaxBiomassAvailable"]);
                else
                    this.MaxBiomassPerYear.Add(10000.0);

                // CHP
                if (technologyParameters[p].ContainsKey("c_chp_eff"))
                    this.c_chp_eff_el.Add(technologyParameters[p]["c_chp_eff"]);
                else
                    this.c_chp_eff_el.Add(0.3);
                if (technologyParameters[p].ContainsKey("c_chp_htp"))
                    this.c_chp_htp.Add(technologyParameters[p]["c_chp_htp"]);
                else
                    this.c_chp_htp.Add(1.73);
                if (technologyParameters[p].ContainsKey("c_chp_heatdump"))
                    this.c_chp_heatdump.Add(technologyParameters[p]["c_chp_heatdump"]);
                else
                    this.c_chp_heatdump.Add(1);

                // Battery
                if (technologyParameters[p].ContainsKey("b_MaxBattery"))
                    this.b_MaxBattery.Add(technologyParameters[p]["b_MaxBattery"] * _floorarea);
                else
                    this.b_MaxBattery.Add(800.0); // Tesla car has 80 kWh
                if (technologyParameters[p].ContainsKey("bat_ch_eff"))
                    this.bat_ch_eff.Add(technologyParameters[p]["bat_ch_eff"]);
                else
                    bat_ch_eff.Add(0.92);
                if (technologyParameters[p].ContainsKey("bat_disch_eff"))
                    this.bat_disch_eff.Add(technologyParameters[p]["bat_disch_eff"]);
                else
                    bat_disch_eff.Add(0.92);
                if (technologyParameters[p].ContainsKey("bat_decay"))
                    this.bat_decay.Add(technologyParameters[p]["bat_decay"]);
                else
                    this.bat_decay.Add(0.001);
                if (technologyParameters[p].ContainsKey("bat_max_ch"))
                    this.bat_max_ch.Add(technologyParameters[p]["bat_max_ch"]);
                else
                    this.bat_max_ch.Add(0.3);
                if (technologyParameters[p].ContainsKey("bat_max_disch"))
                    this.bat_max_disch.Add(technologyParameters[p]["bat_max_disch"]);
                else
                    this.bat_max_disch.Add(0.33);
                if (technologyParameters[p].ContainsKey("bat_min_state"))
                    this.bat_min_state.Add(technologyParameters[p]["bat_min_state"]);
                else
                    this.bat_min_state.Add(0.3);

                // TES
                if (technologyParameters[p].ContainsKey("b_MaxTES"))
                    this.b_MaxTES.Add(technologyParameters[p]["b_MaxTES"] * _floorarea);
                else
                    this.b_MaxTES.Add(1400.0);
                if (technologyParameters[p].ContainsKey("tes_ch_eff"))
                    this.tes_ch_eff.Add(technologyParameters[p]["tes_ch_eff"]);
                else
                    this.tes_ch_eff.Add(0.9);
                if (technologyParameters[p].ContainsKey("tes_disch_eff"))
                    this.tes_disch_eff.Add(technologyParameters[p]["tes_disch_eff"]);
                else
                    this.tes_disch_eff.Add(0.9);
                if (technologyParameters[p].ContainsKey("tes_decay"))
                    this.tes_decay.Add(technologyParameters[p]["tes_decay"]);
                else
                    this.tes_decay.Add(0.001);
                if (technologyParameters[p].ContainsKey("tes_max_ch"))
                    this.tes_max_ch.Add(technologyParameters[p]["tes_max_ch"]);
                else
                    this.tes_max_ch.Add(0.25);
                if (technologyParameters[p].ContainsKey("tes_max_disch"))
                    this.tes_max_disch.Add(technologyParameters[p]["tes_max_disch"]);
                else
                    this.tes_max_disch.Add(0.25);


                /// ////////////////////////////////////////////////////////////////////////
                /// Minimal Capacities
                /// ////////////////////////////////////////////////////////////////////////
                if (technologyParameters[p].ContainsKey("minCapBattery"))
                    this.minCapBattery.Add(technologyParameters[p]["minCapBattery"]);
                else
                    this.minCapBattery.Add(10);
                if (technologyParameters[p].ContainsKey("minCapTES"))
                    this.minCapTES.Add(technologyParameters[p]["minCapTES"]);
                else
                    this.minCapTES.Add(10);
                if (technologyParameters[p].ContainsKey("minCapBoiler"))
                    this.minCapBoiler.Add(technologyParameters[p]["minCapBoiler"]);
                else
                    this.minCapBoiler.Add(10);
                if (technologyParameters[p].ContainsKey("minCapBioBoiler"))
                    this.minCapBioBoiler.Add(technologyParameters[p]["minCapBioBoiler"]);
                else
                    this.minCapBioBoiler.Add(10);
                if (technologyParameters[p].ContainsKey("minCapCHP"))
                    this.minCapCHP.Add(technologyParameters[p]["minCapCHP"]);
                else
                    this.minCapCHP.Add(10);
                if (technologyParameters[p].ContainsKey("minCapElecChiller"))
                    this.minCapElecChiller.Add(technologyParameters[p]["minCapElecChiller"]);
                else
                    this.minCapElecChiller.Add(10);
                if (technologyParameters[p].ContainsKey("minCapASHP"))
                    this.minCapASHP.Add(technologyParameters[p]["minCapASHP"]);
                else
                    this.minCapASHP.Add(10);


                /// ////////////////////////////////////////////////////////////////////////
                /// LCA
                /// ////////////////////////////////////////////////////////////////////////
                if (technologyParameters[p].ContainsKey("lca_GridElectricity"))
                    this.LcaGridElectricity.Add(technologyParameters[p]["lca_GridElectricity"]);
                else
                    this.LcaGridElectricity.Add(0.14840); // from Wu et al. 2017
                if (technologyParameters[p].ContainsKey("lca_NaturalGas"))
                    this.LcaNaturalGas.Add(technologyParameters[p]["lca_NaturalGas"]);
                else
                    this.LcaNaturalGas.Add(0.237); // from Waibel 2019 co-simu paper
                if (technologyParameters[p].ContainsKey("lca_Biomass"))
                    this.LcaBiomass.Add(technologyParameters[p]["lca_Biomass"]);
                else
                    this.LcaBiomass.Add(0.237);

                // Total LCA of technologies
                if (technologyParameters[p].ContainsKey("lca_PV_mono"))
                    this.LcaTotalPvMono.Add(technologyParameters[p]["lca_PV_mono"]);
                else
                    this.LcaTotalPvMono.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_PV_cdte"))
                    this.LcaTotalPvCdte.Add(technologyParameters[p]["lca_PV_cdte"]);
                else
                    this.LcaTotalPvCdte.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_Battery"))
                    this.LcaTotalBattery.Add(technologyParameters[p]["lca_Battery"]);
                else
                    this.LcaTotalBattery.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_TES"))
                    this.LcaTotalTes.Add(technologyParameters[p]["lca_TES"]);
                else
                    this.LcaTotalTes.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_ASHP"))
                    this.LcaTotalAshp.Add(technologyParameters[p]["lca_ASHP"]);
                else
                    this.LcaTotalAshp.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_CHP"))
                    this.LcaTotalChp.Add(technologyParameters[p]["lca_CHP"]);
                else
                    this.LcaTotalChp.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_Boiler"))
                    this.LcaTotalBoiler.Add(technologyParameters[p]["lca_Boiler"]);
                else
                    this.LcaTotalBoiler.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_BiomassBoiler"))
                    this.LcaTotalBiomassBoiler.Add(technologyParameters[p]["lca_BiomassBoiler"]);
                else
                    this.LcaTotalBiomassBoiler.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_ElecChiller"))
                    this.LcaTotalElecChiller.Add(technologyParameters[p]["lca_ElecChiller"]);
                else
                    this.LcaTotalElecChiller.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_DistrictHeating"))
                    this.LcaTotalDistrictHeating.Add(technologyParameters[p]["lca_DistrictHeating"]);
                else
                    this.LcaTotalDistrictHeating.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_HeatExchanger"))
                    this.LcaTotalHeatExchanger.Add(technologyParameters[p]["lca_HeatExchanger"]);
                else
                    this.LcaTotalHeatExchanger.Add(0.0);
                if (technologyParameters[p].ContainsKey("lca_CoolingTower"))
                    this.LcaTotalCoolingTower.Add(technologyParameters[p]["lca_CoolingTower"]);
                else
                    this.LcaTotalCoolingTower.Add(0.0);



                /// ////////////////////////////////////////////////////////////////////////
                /// Cost
                /// ////////////////////////////////////////////////////////////////////////
                if (technologyParameters[p].ContainsKey("InterestRate"))
                    this.InterestRate.Add(technologyParameters[p]["InterestRate"]);
                else
                    this.InterestRate.Add(0.08);
                if (technologyParameters[p].ContainsKey("c_NaturalGas"))
                    this.OperationCostNaturalGas.Add(technologyParameters[p]["c_NaturalGas"]);
                else
                    this.OperationCostNaturalGas.Add(0.09);
                if (technologyParameters[p].ContainsKey("c_Biomass"))
                    this.OperationCostBiomass.Add(technologyParameters[p]["c_Biomass"]);
                else
                    this.OperationCostBiomass.Add(0.2);

                double _gridOffPeak, _gridPeak, _feedIn;
                //if (technologyParameters[p].ContainsKey("c_Grid_OffPeak"))
                //    _gridOffPeak = technologyParameters[p]["c_Grid_OffPeak"];
                //else
                //    _gridOffPeak = 0.1;
                if (technologyParameters[p].ContainsKey("c_Grid"))
                    _gridPeak = technologyParameters[p]["c_Grid"];
                else
                    _gridPeak = 0.2;
                if (technologyParameters[p].ContainsKey("c_FeedIn"))
                    _feedIn = technologyParameters[p]["c_FeedIn"];
                else
                    _feedIn = -0.15;

                this.OperationRevenueFeedIn.Add(new double[this.Horizon]);
                this.OperationCostGrid.Add(new double[this.Horizon]);
                for (int t = 0;
                    t < this.Horizon;
                    t += 24) // default values from Wu et al 2017. he didn't have off-peak grid 
                {
                    for (int u = t; u < t + 24; u++)
                    {
                        this.OperationRevenueFeedIn[p][u] = _feedIn;
                        this.OperationCostGrid[p][u] = _gridPeak; // mavromatidis also doesnt have offpeak. otherwise, grid is too cheap in comparison with PV fix cost...?
                        //if (u > t + 7 && u < t + 18)
                        //    this.OperationCostGrid[p][u] = _gridPeak;
                        //else
                        //    this.OperationCostGrid[p][u] = _gridOffPeak;
                    }
                }


                // Linear Investment Cost
                if (technologyParameters[p].ContainsKey("CostPV_mono"))
                    this.LinearCostPvMono.Add(technologyParameters[p]["CostPV_mono"]);
                else
                    this.LinearCostPvMono.Add(250.0);
                if (technologyParameters[p].ContainsKey("CostPV_cdte"))
                    this.LinearCostPvCdte.Add(technologyParameters[p]["CostPV_cdte"]);
                else
                    this.LinearCostPvCdte.Add(250.0);
                if (technologyParameters[p].ContainsKey("CostBattery"))
                    this.LinearCostBattery.Add(technologyParameters[p]["CostBattery"]);
                else
                    this.LinearCostBattery.Add(600.0);
                if (technologyParameters[p].ContainsKey("CostTES"))
                    this.LinearCostTes.Add(technologyParameters[p]["CostTES"]);
                else
                    this.LinearCostTes.Add(100.0);
                if (technologyParameters[p].ContainsKey("CostBoiler"))
                    this.LinearCostBoiler.Add(technologyParameters[p]["CostBoiler"]);
                else
                    this.LinearCostBoiler.Add(200.0);
                if (technologyParameters[p].ContainsKey("CostBiomassBoiler"))
                    this.LinearCostBiomassBoiler.Add(technologyParameters[p]["CostBiomassBoiler"]);
                else
                    this.LinearCostBiomassBoiler.Add(300.0);
                if (technologyParameters[p].ContainsKey("CostCHP"))
                    this.LinearCostChpElectric.Add(technologyParameters[p]["CostCHP"]);
                else
                    this.LinearCostChpElectric.Add(1500.0);
                if (technologyParameters[p].ContainsKey("CostElecChiller"))
                    this.LinearCostElecChiller.Add(technologyParameters[p]["CostElecChiller"]);
                else
                    this.LinearCostElecChiller.Add(360.0);
                if (technologyParameters[p].ContainsKey("CostASHP"))
                    this.LinearCostAshp.Add(technologyParameters[p]["CostASHP"]);
                else
                    this.LinearCostAshp.Add(1000.0);
                if (technologyParameters[p].ContainsKey("CostDistrictHeating"))
                    this.LinearCostDistrictHeating.Add(technologyParameters[p]["CostDistrictHeating"]);
                else
                    this.LinearCostDistrictHeating.Add(200.0);
                if (technologyParameters[p].ContainsKey("CostHeatExchanger"))
                    this.LinearCostHeatExchanger.Add(technologyParameters[p]["CostHeatExchanger"]);
                else
                    this.LinearCostHeatExchanger.Add(200.0);
                if (technologyParameters[p].ContainsKey("CostCoolingTower"))
                    this.LinearCostCoolingTower.Add(technologyParameters[p]["CostCoolingTower"]);
                else
                    this.LinearCostCoolingTower.Add(200.0);

                // Fix Investment Cost
                // too high for PV?
                //if (technologyParameters[p].ContainsKey("FixCostPV_mono"))
                //    this.FixCostPvMono.Add(technologyParameters[p]["FixCostPV_mono"]);
                //else
                //    this.FixCostPvMono.Add(250.0);
                //if (technologyParameters[p].ContainsKey("FixCostPV_cdte"))
                //    this.FixCostPVCdte.Add(technologyParameters[p]["FixCostPV_cdte"]);
                //else
                //    this.FixCostPVCdte.Add(250.0);
                this.FixCostPvMono.Add(900);
                this.FixCostPVCdte.Add(900);
                if (technologyParameters[p].ContainsKey("FixCostBattery"))
                    this.FixCostBattery.Add(technologyParameters[p]["FixCostBattery"]);
                else
                    this.FixCostBattery.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostTES"))
                    this.FixCostTES.Add(technologyParameters[p]["FixCostTES"]);
                else
                    this.FixCostTES.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostBoiler"))
                    this.FixCostBoiler.Add(technologyParameters[p]["FixCostBoiler"]);
                else
                    this.FixCostBoiler.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostBiomassBoiler"))
                    this.FixCostBiomassBoiler.Add(technologyParameters[p]["FixCostBiomassBoiler"]);
                else
                    this.FixCostBiomassBoiler.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostCHP"))
                    this.FixCostCHP.Add(technologyParameters[p]["FixCostCHP"]);
                else
                    this.FixCostCHP.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostElecChiller"))
                    this.FixCostElecChiller.Add(technologyParameters[p]["FixCostElecChiller"]);
                else
                    this.FixCostElecChiller.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostASHP"))
                    this.FixCostASHP.Add(technologyParameters[p]["FixCostASHP"]);
                else
                    this.FixCostASHP.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostDistrictHeating"))
                    this.FixCostDistrictHeating.Add(technologyParameters[p]["FixCostDistrictHeating"]);
                else
                    this.FixCostDistrictHeating.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostHeatExchanger"))
                    this.FixCostHeatExchanger.Add(technologyParameters[p]["FixCostHeatExchanger"]);
                else
                    this.FixCostHeatExchanger.Add(250.0);
                if (technologyParameters[p].ContainsKey("FixCostCoolingTower"))
                    this.FixCostCoolingTower.Add(technologyParameters[p]["FixCostCoolingTower"]);
                else
                    this.FixCostCoolingTower.Add(250.0);

                // Operation and Maintenance cost
                //if (technologyParameters[p].ContainsKey("c_PV_OM"))
                //    this.OmCostPV.Add(technologyParameters[p]["c_PV_OM"]);
                //else
                //    this.OmCostPV.Add(0.0);
                this.OmCostPV.Add(0.0);

                if (technologyParameters[p].ContainsKey("c_Battery_OM"))
                    this.OmCostBattery.Add(technologyParameters[p]["c_Battery_OM"]);
                else
                    this.OmCostBattery.Add(0.0);
                if (technologyParameters[p].ContainsKey("c_TES_OM"))
                    this.OmCostTes.Add(technologyParameters[p]["c_TES_OM"]);
                else
                    this.OmCostTes.Add(0.0);
                if (technologyParameters[p].ContainsKey("c_Boiler_OM"))
                    this.OmCostBoiler.Add(technologyParameters[p]["c_Boiler_OM"]);
                else
                    this.OmCostBoiler.Add(0.01); // Waibel et al 2017
                if (technologyParameters[p].ContainsKey("c_BiomassBoiler_OM"))
                    this.OmCostBiomassBoiler.Add(technologyParameters[p]["c_BiomassBoiler_OM"]);
                else
                    this.OmCostBiomassBoiler.Add(0.01);
                if (technologyParameters[p].ContainsKey("c_CHP_OM"))
                    this.OmCostChp.Add(technologyParameters[p]["c_CHP_OM"]);
                else
                    this.OmCostChp.Add(0.021); // Waibel et al 2017
                if (technologyParameters[p].ContainsKey("c_ElecChiller_OM"))
                    this.OmCostElecChiller.Add(technologyParameters[p]["c_ElecChiller_OM"]);
                else
                    this.OmCostElecChiller.Add(0.1);
                if (technologyParameters[p].ContainsKey("c_ASHP_OM"))
                    this.OmCostAshp.Add(technologyParameters[p]["c_ASHP_OM"]);
                else
                    this.OmCostAshp.Add(0.1); // Waibel et al 2017

                // lifetime
                if (technologyParameters[p].ContainsKey("LifetimePV_mono"))
                    this.LifetimePvMono.Add(technologyParameters[p]["LifetimePV_mono"]);
                else
                    this.LifetimePvMono.Add(20.0);
                if (technologyParameters[p].ContainsKey("LifetimePV_cdte"))
                    this.LifetimePvCdte.Add(technologyParameters[p]["LifetimePV_cdte"]);
                else
                    this.LifetimePvCdte.Add(20.0);
                if (technologyParameters[p].ContainsKey("LifetimeBattery"))
                    this.LifetimeBattery.Add(technologyParameters[p]["LifetimeBattery"]);
                else
                    this.LifetimeBattery.Add(20.0);
                if (technologyParameters[p].ContainsKey("LifetimeTES"))
                    this.LifetimeTES.Add(technologyParameters[p]["LifetimeTES"]);
                else
                    this.LifetimeTES.Add(17.0);
                if (technologyParameters[p].ContainsKey("LifetimeASHP"))
                    this.LifetimeASHP.Add(technologyParameters[p]["LifetimeASHP"]);
                else
                    this.LifetimeASHP.Add(20.0);
                if (technologyParameters[p].ContainsKey("LifeetimeCHP"))
                    this.LifetimeCHP.Add(technologyParameters[p]["LifetimeCHP"]);
                else
                    this.LifetimeCHP.Add(20.0);
                if (technologyParameters[p].ContainsKey("LifetimeBoiler"))
                    this.LifetimeBoiler.Add(technologyParameters[p]["LifetimeBoiler"]);
                else
                    this.LifetimeBoiler.Add(30.0);
                if (technologyParameters[p].ContainsKey("LifetimeBiomassBoiler"))
                    this.LifetimeBiomassBoiler.Add(technologyParameters[p]["LifetimeBiomassBoiler"]);
                else
                    this.LifetimeBiomassBoiler.Add(30.0);
                if (technologyParameters[p].ContainsKey("LifetimeElecChiller"))
                    this.LifetimeElecChiller.Add(technologyParameters[p]["LifetimeElecChiller"]);
                else
                    this.LifetimeElecChiller.Add(20.0);
                if (technologyParameters[p].ContainsKey("LifetimeDistrictHeating"))
                    this.LifetimeDistrictHeating.Add(technologyParameters[p]["LifetimeDistrictHeating"]);
                else
                    this.LifetimeDistrictHeating.Add(50.0);
                if (technologyParameters[p].ContainsKey("LifetimeHeatExchanger"))
                    this.LifetimeHeatExchanger.Add(technologyParameters[p]["LifetimeHeatExchanger"]);
                else
                    this.LifetimeHeatExchanger.Add(30.0);
                if (technologyParameters[p].ContainsKey("LifetimeCoolingTower"))
                    this.LifetimeCoolingTower.Add(technologyParameters[p]["LifetimeCoolingTower"]);
                else
                    this.LifetimeCoolingTower.Add(50.0);


                // CALCULATE NET PRESENT VALUE FOR FUTURE PERIODS
                // will be done in the energy hub obejctive function later


                // PV efficiency
                this.PvEfficiencyMono.Add(new double[this.NumberOfSolarAreas][]);
                this.PvEfficiencyCdte.Add(new double[this.NumberOfSolarAreas][]);
                for (int i = 0; i < this.NumberOfSolarAreas; i++)
                {
                    this.PvEfficiencyMono[p][i] = TechnologyEfficiencies.CalculateEfficiencyPhotovoltaic(
                        AmbientTemperature[p], this.SolarLoads[p][i],
                        this.PvNoct, this.PvTempAmbNoct, this.PvPNoct, this.PvBetaRef, this.PvEtaRefMono[p]);
                    this.PvEfficiencyCdte[p][i] = TechnologyEfficiencies.CalculateEfficiencyPhotovoltaic(
                        AmbientTemperature[p], this.SolarLoads[p][i],
                        this.PvNoct, this.PvTempAmbNoct, this.PvPNoct, this.PvBetaRef, this.PvEtaRefCdte[p]);
                }

                this.AshpEfficiency.Add(TechnologyEfficiencies.CalculateCOPHeatPump(this.AmbientTemperature[p],
                    this.HpSupplyTemp[p], this.HpPi1, this.HpPi2, this.HpPi3, this.HpPi4));


                // annual embodied LCA of technologies
                this.LcaAnnualElecChiller.Add(this.LcaTotalElecChiller[p] / this.LifetimeElecChiller[p]);
                this.LcaAnnualAshp.Add(this.LcaTotalAshp[p] / this.LifetimeASHP[p]);
                this.LcaAnnualBattery.Add(this.LcaTotalBattery[p] / this.LifetimeBattery[p]);
                this.LcaAnnualBoiler.Add(this.LcaTotalBoiler[p] / this.LifetimeBoiler[p]);
                this.LcaAnnualBiomassBoiler.Add(this.LcaTotalBiomassBoiler[p] / this.LifetimeBiomassBoiler[p]);
                this.LcaAnnualChp.Add(this.LcaTotalChp[p] / this.LifetimeCHP[p]);
                this.LcaAnnualDistrictHeating.Add(this.LcaTotalDistrictHeating[p] / this.LifetimeDistrictHeating[p]);
                this.LcaAnnualPvMono.Add(this.LcaTotalPvMono[p] / this.LifetimePvMono[p]);
                this.LcaAnnualPvCdte.Add(this.LcaTotalPvCdte[p] / this.LifetimePvCdte[p]);
                this.LcaAnnualTes.Add(this.LcaTotalTes[p] / this.LifetimeTES[p]);
                this.LcaAnnualHeatExchanger.Add(this.LcaTotalHeatExchanger[p] / this.LifetimeHeatExchanger[p]);
                this.LcaAnnualCoolingTower.Add(this.LcaTotalCoolingTower[p] / this.LifetimeCoolingTower[p]);


                //get peak loads per building. need to get the max out of all periods - for simplicity no multi-period DH sizing
                for (int i = 0; i < this.NumberOfBuildingsInDistrict; i++)
                {
                    string heatingLoad = "Peak_Htg_" + Convert.ToString(i + 1);
                    double maxHtgLoad, maxClgLoad;
                    if (technologyParameters[p].ContainsKey(heatingLoad))
                        maxHtgLoad = technologyParameters[p][heatingLoad];
                    else
                        maxHtgLoad = 1000.0;

                    string coolingLoad = "Peak_Clg_" + Convert.ToString(i + 1);
                    if (technologyParameters[p].ContainsKey(coolingLoad))
                        maxClgLoad = technologyParameters[p][coolingLoad];
                    else
                        maxClgLoad = 1000.0;

                    if (p == 0)
                    {
                        this.PeakHeatingLoadsPerBuilding[i] = maxHtgLoad;
                        this.PeakCoolingLoadsPerBuilding[i] = maxClgLoad;
                    }
                    else
                    {
                        this.PeakHeatingLoadsPerBuilding[i] = maxHtgLoad > this.PeakHeatingLoadsPerBuilding[i]
                            ? maxHtgLoad
                            : this.PeakHeatingLoadsPerBuilding[i];
                        this.PeakCoolingLoadsPerBuilding[i] = maxClgLoad > this.PeakCoolingLoadsPerBuilding[i]
                            ? maxClgLoad
                            : this.PeakCoolingLoadsPerBuilding[i];
                    }
                }
            }


            // District Heating. only for period 0
            if (this.NumberOfBuildingsInDistrict > 1)
            {
                this.NetworkLengthTotal = technologyParameters[0].ContainsKey("GridLengthDistrictHeating")
                    ? technologyParameters[0]["GridLengthDistrictHeating"]
                    : 1000.0;
            }
            else
            {
                this.NetworkLengthTotal = 0.0;
                this.PeakHeatingLoadsPerBuilding = new double[1] { 0.0 };
                this.PeakCoolingLoadsPerBuilding = new double[1] { 0.0 };
            }
        }


        internal void Solve(int epsilonCuts, bool verbose = false)
        {
            double costTolerance = 100.0;
            double carbonTolerance = 0.1;
            Outputs = new MultiPeriodEhubOutput[epsilonCuts + 2];

            // prototyping
            MultiPeriodEhubOutput minCost = EnergyHub("cost", null, null, verbose);
            Outputs[0] = minCost;


            //// 1. solve for minCarbon, ignoring cost. solve again, but mincost, with minCarbon constraint
            //MultiPeriodEhubOutput minCarbon = EnergyHub("carbon", null, null, verbose);

            //// 2. solve for minCost, 
            //MultiPeriodEhubOutput minCost = EnergyHub("cost", null, null, verbose);

            //// 3. 0 = carbon minimal solution (minCost as objective to avoid crazy cost)
            //Outputs[0] = EnergyHub("cost", minCarbon.Carbon + carbonTolerance, null, verbose);
            //Outputs[epsilonCuts + 1] = EnergyHub("carbon", null, minCost.Cost + costTolerance, verbose);
            //double carbonInterval = (minCost.Carbon - minCarbon.Carbon) / (epsilonCuts + 1);

            //// 4. make epsilonCuts cuts and solve for each minCost s.t. carbon
            //for (int i = 0; i < epsilonCuts; i++)
            //    Outputs[i + 1] = EnergyHub("cost", minCarbon.Carbon + carbonInterval * (i + 1), null, verbose);
        }

        private MultiPeriodEhubOutput EnergyHub(string objective = "cost", double? carbonConstraint = null,
            double? costConstraint = null, bool verbose = false)
        {
            var solution = new MultiPeriodEhubOutput();
            Cplex cpl = new Cplex();

            // hardcoding 3 investment periods: 2020, 2030, 2040
            // that means, we need 3 separate variables for each tech, because each tech per period will have different efficiencies, embodied emissions and cost parameters
            // also 3 separate arrays (incl set of constraints & expressions) for demands, irradiance, ghi, tamb, and conversion matrices

            // however, if I have 5 years intervals, I have to work with arrays. Can't have them manually anymore, would be too messy


            // PV mono and cdte, period, surface
            INumVar[][] xNewPvMono = new INumVar[NumPeriods][];
            INumVar[][] yNewPvMono = new INumVar[NumPeriods][];
            INumVar[][] xNewPvCdte = new INumVar[NumPeriods][];
            INumVar[][] yNewPvCdte = new INumVar[NumPeriods][];
            for (int p = 0; p < NumPeriods; p++)
            {
                xNewPvMono[p] = new INumVar[NumberOfSolarAreas];
                xNewPvCdte[p] = new INumVar[NumberOfSolarAreas];
                yNewPvMono[p] = new INumVar[NumberOfSolarAreas];
                yNewPvCdte[p] = new INumVar[NumberOfSolarAreas];
                for (int i = 0; i < NumberOfSolarAreas; i++)
                {
                    xNewPvMono[p][i] = cpl.NumVar(0, SolarAreas[i]); // for each period, same surface area. later, special constraint to ensure total pv mono + cdte <= surfaceArea
                    xNewPvCdte[p][i] = cpl.NumVar(0, SolarAreas[i]);
                    yNewPvMono[p][i] = cpl.BoolVar();
                    yNewPvCdte[p][i] = cpl.BoolVar();
                }
            }



            ILinearNumExpr[][] totalPvElectricity = new ILinearNumExpr[NumPeriods][];
            INumVar[][] xPvElectricity = new INumVar[NumPeriods][];
            INumVar[][] xOperationGridPurchase = new INumVar[NumPeriods][];
            INumVar[][] xOperationFeedIn = new INumVar[NumPeriods][];
            INumVar[][] yOperationFeedIn = new INumVar[NumPeriods][];
            for (int p = 0; p < NumPeriods; p++)
            {
                totalPvElectricity[p] = new ILinearNumExpr[Horizon];
                xPvElectricity[p] = new INumVar[Horizon];
                xOperationGridPurchase[p] = new INumVar[Horizon];
                xOperationFeedIn[p] = new INumVar[Horizon];
                yOperationFeedIn[p] = new INumVar[Horizon];
                for (int t = 0; t < Horizon; t++)
                {
                    totalPvElectricity[p][t] = cpl.LinearNumExpr();
                    xOperationGridPurchase[p][t] = cpl.NumVar(0, double.MaxValue);
                    xOperationFeedIn[p][t] = cpl.NumVar(0, double.MaxValue);
                    yOperationFeedIn[p][t] = cpl.BoolVar();
                    xPvElectricity[p][t] = cpl.NumVar(0, double.MaxValue);
                }
            }


   

            INumVar[][] totalCapacityPvMono = new INumVar[NumPeriods][]; // period, surface
            INumVar[][] totalCapacityPvCdte = new INumVar[NumPeriods][];
            for (int p = 0; p < NumPeriods; p++)
            {
                // I have to sum up in one totalCapPV to check for max space usage. But I can't use totalCapPV for yield calculation, coz I'll have different efficiencies per period
                totalCapacityPvMono[p] = new INumVar[NumberOfSolarAreas];
                totalCapacityPvCdte[p] = new INumVar[NumberOfSolarAreas];
                for (int i = 0; i < NumberOfSolarAreas; i++)
                {
                    totalCapacityPvMono[p][i] = cpl.NumVar(0, SolarAreas[i]);
                    totalCapacityPvCdte[p][i] = cpl.NumVar(0, SolarAreas[i]);
                }
            }

            // Lifetime constraint
            // at each period, total pv cant be larger than available surface
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int i = 0; i < NumberOfSolarAreas; i++)
                {
                    ILinearNumExpr sumNewMono = cpl.LinearNumExpr();
                    ILinearNumExpr sumNewCdte = cpl.LinearNumExpr();
                    for (int pp = (int)Math.Max(0, p - Math.Floor(LifetimePvMono[p] / YearsPerPeriod) + 1); pp <= p; pp++)
                        sumNewMono.AddTerm(1, xNewPvMono[pp][i]);
                    for (int pp = (int)Math.Max(0, p - Math.Floor(LifetimePvCdte[p] / YearsPerPeriod) + 1); pp <= p; pp++)
                        sumNewCdte.AddTerm(1, xNewPvCdte[pp][i]);
                    cpl.AddEq(totalCapacityPvMono[p][i], sumNewMono);
                    cpl.AddEq(totalCapacityPvCdte[p][i], sumNewCdte);
                }
            }

            for (int p = 0; p < NumPeriods; p++)
                for (int i = 0; i < NumberOfSolarAreas; i++)
                    cpl.AddGe(SolarAreas[i], cpl.Sum(totalCapacityPvMono[p][i], totalCapacityPvCdte[p][i]));

            // meeting demands
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int t = 0; t < this.Horizon; t++)
                {
                    ILinearNumExpr elecGeneration = cpl.LinearNumExpr();
                    ILinearNumExpr elecAdditionalDemand = cpl.LinearNumExpr();

                    /// Electricity
                    // elec demand must be met by PV production, battery and grid, minus feed in
                    for (int i = 0; i < NumberOfSolarAreas; i++)
                    {
                        double pvElecMonoGenPerSqm = SolarLoads[p][i][t] * 0.001 * this.PvEfficiencyMono[p][i][t];
                        double pvElecCdteGenPerSqm = SolarLoads[p][i][t] * 0.001 * this.PvEfficiencyCdte[p][i][t];
                        elecGeneration.AddTerm(pvElecMonoGenPerSqm, totalCapacityPvMono[p][i]);
                        elecGeneration.AddTerm(pvElecCdteGenPerSqm, totalCapacityPvCdte[p][i]);
                        totalPvElectricity[p][t].AddTerm(pvElecMonoGenPerSqm, totalCapacityPvMono[p][i]);
                        totalPvElectricity[p][t].AddTerm(pvElecCdteGenPerSqm, totalCapacityPvCdte[p][i]);
                    }

                    elecGeneration.AddTerm(1, xOperationGridPurchase[p][t]);
                    elecAdditionalDemand.AddTerm(1, xOperationFeedIn[p][t]);



                    /// PV Technical Constraints
                    // getting total pv generation. need it for OM cost
                    cpl.AddEq(totalPvElectricity[p][t], xPvElectricity[p][t]);
                    // pv production must be greater equal feedin
                    cpl.AddGe(totalPvElectricity[p][t], xOperationFeedIn[p][t]);
                    // donnot allow feedin and purchase at the same time. y = 1 means elec is produced
                    cpl.AddLe(xOperationGridPurchase[p][t], cpl.Prod(M, yOperationFeedIn[p][t]));
                    cpl.AddLe(xOperationFeedIn[p][t], cpl.Prod(M, cpl.Diff(1, yOperationFeedIn[p][t])));

                    

                    /// Energy Balance
                    cpl.AddGe(cpl.Diff(elecGeneration, elecAdditionalDemand), this.ElectricityDemand[p][t]);
                }
            }





            /// ////////////////////////////////////////////////////////////////////////
            /// Binary selection variables
            /// ////////////////////////////////////////////////////////////////////////
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int i = 0; i < this.NumberOfSolarAreas; i++)
                {
                    cpl.AddLe(xNewPvMono[p][i], cpl.Prod(M, yNewPvMono[p][i]));
                    cpl.AddLe(xNewPvCdte[p][i], cpl.Prod(M, yNewPvCdte[p][i]));
                }
            }


            /// ////////////////////////////////////////////////////////////////////////
            /// Cost coefficients formulation
            /// ////////////////////////////////////////////////////////////////////////
            ILinearNumExpr opex = cpl.LinearNumExpr();
            ILinearNumExpr capex = cpl.LinearNumExpr();
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int i = 0; i < NumberOfSolarAreas; i++)
                {
                    capex.AddTerm(LinearCostPvMono[p] / Math.Pow(1+InterestRate[p], p * YearsPerPeriod), xNewPvMono[p][i]);
                    capex.AddTerm(FixCostPvMono[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), yNewPvMono[p][i]);
                    capex.AddTerm(LinearCostPvCdte[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xNewPvCdte[p][i]);
                    capex.AddTerm(FixCostPVCdte[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), yNewPvCdte[p][i]);
                }

                for (int t = 0; t < Horizon; t++)
                {
                    opex.AddTerm((ClustersizePerTimestep[p][t] * OperationCostGrid[p][t] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xOperationGridPurchase[p][t]);
                    opex.AddTerm((ClustersizePerTimestep[p][t] * OperationRevenueFeedIn[p][t] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xOperationFeedIn[p][t]);
                    opex.AddTerm((ClustersizePerTimestep[p][t] * OmCostPV[p] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xPvElectricity[p][t]);
                }
            }

            // salvage?




            /// ////////////////////////////////////////////////////////////////////////
            /// Objective function
            /// ////////////////////////////////////////////////////////////////////////
            cpl.AddMinimize(cpl.Sum(capex, opex));

          


            /// ////////////////////////////////////////////////////////////////////////
            /// Solve
            /// ////////////////////////////////////////////////////////////////////////
            if (!verbose) cpl.SetOut(null);
            cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.005);
            cpl.SetParam(Cplex.IntParam.MIPDisplay, 4);
            

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

                solution.Opex = cpl.GetValue(opex);
                solution.Capex = cpl.GetValue(capex);
                solution.Cost = solution.Opex + solution.Capex;

                solution.XTotalPvMono = new double[NumPeriods][];
                solution.XTotalPvCdte = new double[NumPeriods][];
                solution.XNewPvMono = new double[NumPeriods][];
                solution.XNewPvCdte = new double[NumPeriods][];
                for (int p = 0; p < NumPeriods; p++)
                {
                    solution.XTotalPvMono[p] = new double[NumberOfSolarAreas];
                    solution.XTotalPvCdte[p] = new double[NumberOfSolarAreas];
                    solution.XNewPvMono[p] = new double[NumberOfSolarAreas];
                    solution.XNewPvCdte[p] = new double[NumberOfSolarAreas];
                    for (int i = 0; i < this.NumberOfSolarAreas; i++)
                    {
                        solution.XTotalPvMono[p][i] = cpl.GetValue(totalCapacityPvMono[p][i]);
                        solution.XTotalPvCdte[p][i] = cpl.GetValue(totalCapacityPvCdte[p][i]);
                        solution.XNewPvMono[p][i] = cpl.GetValue(xNewPvMono[p][i]);
                        solution.XNewPvCdte[p][i] = cpl.GetValue(xNewPvCdte[p][i]);
                    }
                }


                solution.XOperationPvElectricity = new double[NumPeriods][];
                solution.XOperationElecPurchase = new double[NumPeriods][];
                solution.XOperationFeedIn = new double[NumPeriods][];
                solution.Clustersize = new int[NumPeriods][];
                for (int p = 0; p < NumPeriods; p++)
                {
                    solution.XOperationPvElectricity[p] = new double[Horizon];
                    solution.XOperationElecPurchase[p] = new double[Horizon];
                    solution.XOperationFeedIn[p] = new double[Horizon];
                    solution.Clustersize[p] = new int[Horizon];
                    for (int t = 0; t < this.Horizon; t++)
                    {
                        solution.XOperationPvElectricity[p][t] = cpl.GetValue(xPvElectricity[p][t]);
                        solution.XOperationElecPurchase[p][t] = cpl.GetValue(xOperationGridPurchase[p][t]);
                        solution.XOperationFeedIn[p][t] = cpl.GetValue(xOperationFeedIn[p][t]);
                        solution.Clustersize[p][t] = ClustersizePerTimestep[p][t];
                    }
                }


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
