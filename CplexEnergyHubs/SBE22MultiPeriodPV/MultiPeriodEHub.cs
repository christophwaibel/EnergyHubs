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


        // Coefficients PV
        internal double PvNoct { get; private set; }
        internal double PvTempAmbNoct { get; private set; }
        internal double PvPNoct { get; private set; }
        internal double PvBetaRef { get; private set; }
        internal List<double> PvEtaRefMono { get; } = new List<double>();
        internal List<double> PvEtaRefCdte { get; } = new List<double>();
        internal List<double[][]> PvEfficiencyMono { get; } = new List<double[][]>();
        internal List<double[][]> PvEfficiencyCdte { get; } = new List<double[][]>();


        // Coefficients Battery
        internal List<double> bat_ch_eff { get; } = new List<double>();        // Battery charging efficiency
        internal List<double> bat_disch_eff { get; } = new List<double>();     // Battery discharging efficiency
        internal List<double> bat_decay { get; } = new List<double>();         // Battery hourly decay
        internal List<double> bat_max_ch { get; } = new List<double>();        // Battery max charging rate
        internal List<double> bat_max_disch { get; } = new List<double>();     // Battery max discharging rate
        internal List<double> bat_min_state { get; } = new List<double>();     // Battery minimum state of charge
        internal List<double> b_MaxBattery { get; } = new List<double>();      // maximal battery capacity. constraint    

        // Minimal Capacities
        internal List<double> minCapBattery { get; } = new List<double>();
        #endregion


        #region inputs LCA parameters
        /// ////////////////////////////////////////////////////////////////////////
        /// LCA
        /// ////////////////////////////////////////////////////////////////////////
        internal List<double> LcaGridElectricity { get; } = new List<double>();

        // annual LCA of technologies
        internal List<double> LcaAnnualPvMono { get; } = new List<double>();
        internal List<double> LcaAnnualPvCdte { get; } = new List<double>();
        internal List<double> LcaAnnualBattery { get; } = new List<double>();


        // total (non-annualized) LCA of technologies 
        internal List<double> LcaTotalPvMono { get; } = new List<double>();
        internal List<double> LcaTotalPvCdte { get; } = new List<double>();
        internal List<double> LcaTotalBattery { get; } = new List<double>();
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

        // Fix Cost
        internal List<double> FixCostPvMono { get; } = new List<double>();
        internal List<double> FixCostPVCdte { get; } = new List<double>();
        internal List<double> FixCostBattery { get; } = new List<double>();


        // operation and maintenance cost
        internal List<double> OmCostPV { get; } = new List<double>();
        internal List<double> OmCostBattery { get; } = new List<double>();


        // (time resolved) operation cost
        internal List<double[]> OperationCostGrid { get; } = new List<double[]>();
        internal List<double[]> OperationRevenueFeedIn { get; } = new List<double[]>();
        #endregion


        #region MILP stuff
        /// ////////////////////////////////////////////////////////////////////////
        /// MILP
        /// ////////////////////////////////////////////////////////////////////////
        private const double M = 99999;   // Big M method
        #endregion



        internal MultiPeriodEHub(List<double[]> electricityDemand,
            List<double[][]> irradiance, double[] solarTechSurfaceAreas,
            List<double[]> ambientTemperature, List<Dictionary<string, double>> technologyParameters,
            List<int[]> clustersizePerTimestep, int yearsPerPeriod)
        {
            this.ElectricityDemand = electricityDemand;
            this.SolarLoads = irradiance;
            this.SolarAreas = solarTechSurfaceAreas;

            this.NumberOfSolarAreas = solarTechSurfaceAreas.Length;
            this.ClustersizePerTimestep = clustersizePerTimestep;
            this.NumPeriods = ElectricityDemand.Count;
            this.Horizon = ElectricityDemand[0].Length; // this assumes each year has the same number of typical days
            this.YearsPerPeriod = yearsPerPeriod;

            /// read in these parameters as struct parameters
            this.AmbientTemperature = ambientTemperature;
            this.SetParameters(technologyParameters);
        }

        private void SetParameters(List<Dictionary<string, double>> technologyParameters)
        {
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

              
                /// ////////////////////////////////////////////////////////////////////////
                /// Minimal Capacities
                /// ////////////////////////////////////////////////////////////////////////
                if (technologyParameters[p].ContainsKey("minCapBattery"))
                    this.minCapBattery.Add(technologyParameters[p]["minCapBattery"]);
                else
                    this.minCapBattery.Add(10);
                


                /// ////////////////////////////////////////////////////////////////////////
                /// LCA
                /// ////////////////////////////////////////////////////////////////////////
                if (technologyParameters[p].ContainsKey("lca_GridElectricity"))
                    this.LcaGridElectricity.Add(technologyParameters[p]["lca_GridElectricity"]);
                else
                    this.LcaGridElectricity.Add(0.14840); // from Wu et al. 2017
               
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
                



                /// ////////////////////////////////////////////////////////////////////////
                /// Cost
                /// ////////////////////////////////////////////////////////////////////////
                if (technologyParameters[p].ContainsKey("InterestRate"))
                    this.InterestRate.Add(technologyParameters[p]["InterestRate"]);
                else
                    this.InterestRate.Add(0.08);
                
                double _gridOffPeak, _gridPeak, _feedIn;
                if (technologyParameters[p].ContainsKey("c_Grid_OffPeak"))
                    _gridOffPeak = technologyParameters[p]["c_Grid_OffPeak"];
                else
                    _gridOffPeak = 0.1;
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
                        //this.OperationCostGrid[p][u] = _gridPeak; // mavromatidis also doesnt have offpeak. otherwise, grid is too cheap in comparison with PV fix cost...?
                        if (u > t + 7 && u < t + 18)
                            this.OperationCostGrid[p][u] = _gridPeak;
                        else
                            this.OperationCostGrid[p][u] = _gridOffPeak;
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
                

                //// Fix Investment Cost
                //// too high for PV?
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


                // annual embodied LCA of technologies
                this.LcaAnnualBattery.Add(this.LcaTotalBattery[p] / this.LifetimeBattery[p]);
                this.LcaAnnualPvMono.Add(this.LcaTotalPvMono[p] / this.LifetimePvMono[p]);
                this.LcaAnnualPvCdte.Add(this.LcaTotalPvCdte[p] / this.LifetimePvCdte[p]);
                
            }

        }


        internal void Solve(int epsilonCuts, bool verbose = false)
        {
            double costTolerance = 100.0;
            double carbonTolerance = 0.1;
            //Outputs = new MultiPeriodEhubOutput[epsilonCuts + 2];


            // prototyping
            MultiPeriodEhubOutput minCost = EHubSimple(verbose);
            Outputs = new MultiPeriodEhubOutput[1];
            Outputs[0] = minCost;

            //// prototyping PV
            //MultiPeriodEhubOutput minCost = EnergyHub("cost", null, null, verbose);
            //Outputs[0] = minCost;


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


        private MultiPeriodEhubOutput EHubSimple(bool verbose = false)
        {
            var solution = new MultiPeriodEhubOutput();
            Cplex cpl = new Cplex();
            ILinearNumExpr[][] totalPvElectricity = new ILinearNumExpr[NumPeriods][];
            INumVar[][] xOperationGridPurchase = new INumVar[NumPeriods][];
           
            for (int p = 0; p < NumPeriods; p++)
            {
                totalPvElectricity[p] = new ILinearNumExpr[Horizon];
                xOperationGridPurchase[p] = new INumVar[Horizon];
                for (int t = 0; t < Horizon; t++)
                {
                    totalPvElectricity[p][t] = cpl.LinearNumExpr();
                    xOperationGridPurchase[p][t] = cpl.NumVar(0, double.MaxValue);
                }
            }



            // meeting demands
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int t = 0; t < this.Horizon; t++)
                {
                    ILinearNumExpr elecGeneration = cpl.LinearNumExpr();
                    elecGeneration.AddTerm(1, xOperationGridPurchase[p][t]);
  
                    /// Energy Balance
                    cpl.AddGe(elecGeneration, this.ElectricityDemand[p][t]);
                }
            }



            /// ////////////////////////////////////////////////////////////////////////
            /// Cost coefficients formulation
            /// ////////////////////////////////////////////////////////////////////////
            ILinearNumExpr opex = cpl.LinearNumExpr();
            for (int p = 0; p < NumPeriods; p++)
                for (int t = 0; t < Horizon; t++)
                    opex.AddTerm((ClustersizePerTimestep[p][t] * OperationCostGrid[p][t] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xOperationGridPurchase[p][t]);
 


            /// ////////////////////////////////////////////////////////////////////////
            /// Objective function
            /// ////////////////////////////////////////////////////////////////////////
            cpl.AddMinimize(opex);


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
                solution.Capex = 0;
                solution.Cost = 0;

                solution.XOperationElecPurchase = new double[NumPeriods][];
                solution.Clustersize = new int[NumPeriods][];
                for (int p = 0; p < NumPeriods; p++)
                {
                    solution.XOperationElecPurchase[p] = new double[Horizon];
                    solution.Clustersize[p] = new int[Horizon];
                    for (int t = 0; t < this.Horizon; t++)
                    {
                        solution.XOperationElecPurchase[p][t] = cpl.GetValue(xOperationGridPurchase[p][t]);
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
