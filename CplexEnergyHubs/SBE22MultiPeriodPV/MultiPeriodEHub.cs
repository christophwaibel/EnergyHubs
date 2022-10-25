using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        internal List<int[]> ClusterSizePerTimestep { get; private set; }

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
        private const double M = 99999999;   // Big M method
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
            this.ClusterSizePerTimestep = clustersizePerTimestep;
            this.NumPeriods = ElectricityDemand.Count;
            this.Horizon = ElectricityDemand[0].Length; // this assumes each year has the same number of typical days
            this.YearsPerPeriod = yearsPerPeriod;

            /// read in these parameters as struct parameters
            this.AmbientTemperature = ambientTemperature;
            this.SetParameters(technologyParameters);
        }

        private void SetParameters(List<Dictionary<string, double>> technologyParameters)
        {
            // looping through all periods
            for (int p = 0; p < technologyParameters.Count; p++)
            {
                //_____________________________________________________________________________________
                //_____________________________________________________________________________________
                /// Technical Parameters

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


                //_____________________________________________________________________________________
                //_____________________________________________________________________________________
                /// Minimal Capacities

                if (technologyParameters[p].ContainsKey("minCapBattery"))
                    this.minCapBattery.Add(technologyParameters[p]["minCapBattery"]);
                else
                    this.minCapBattery.Add(10);


                //_____________________________________________________________________________________
                //_____________________________________________________________________________________
                /// Cost

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


                // Fix Investment Cost
                // TO DO: only 1 fix cost per building, not per surface. coz 1 building may have 100 surfaces- doesnt make sense to have such high fix cost for each little patch
                //if (technologyParameters[p].ContainsKey("FixCostPV_mono"))
                //    this.FixCostPvMono.Add(technologyParameters[p]["FixCostPV_mono"]);
                //else
                //    this.FixCostPvMono.Add(250.0);
                //if (technologyParameters[p].ContainsKey("FixCostPV_cdte"))
                //    this.FixCostPVCdte.Add(technologyParameters[p]["FixCostPV_cdte"]);
                //else
                //    this.FixCostPVCdte.Add(250.0);
                this.FixCostPVCdte.Add(0.0);
                this.FixCostPvMono.Add(0.0);

              
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
                // will be done in the energy hub obejctive function later, because there is also salvage (?)


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




                //_____________________________________________________________________________________
                //_____________________________________________________________________________________
                /// LCA

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



                //_____________________________________________________________________________________
                //_____________________________________________________________________________________
                // annual embodied LCA of technologies
                this.LcaAnnualBattery.Add(this.LcaTotalBattery[p] / this.LifetimeBattery[p]);
                this.LcaAnnualPvMono.Add(this.LcaTotalPvMono[p] / this.LifetimePvMono[p]);
                this.LcaAnnualPvCdte.Add(this.LcaTotalPvCdte[p] / this.LifetimePvCdte[p]);
            }

        }


        internal void Solve(int epsilonCuts, bool verbose = false)
        {
            //// prototyping only elec
            //MultiPeriodEhubOutput minCost = EHubSimple(verbose);
            //Outputs = new MultiPeriodEhubOutput[1];
            //Outputs[0] = minCost;



            double costTolerance = 100.0;
            double carbonTolerance = 1.0;
            Outputs = new MultiPeriodEhubOutput[epsilonCuts + 2];

            //// prototyping PV
            //MultiPeriodEhubOutput minCost = EnergyHub("cost", null, null, verbose);
            //Outputs = new MultiPeriodEhubOutput[1];
            //Outputs[0] = minCost;


            // 1. solve for minCarbon, ignoring cost. solve again, but mincost, with minCarbon constraint
            MultiPeriodEhubOutput minCarbon = EnergyHub("carbon", null, null, verbose);

            // 2. solve for minCost, 
            MultiPeriodEhubOutput minCost = EnergyHub("cost", null, null, verbose);

            // 3. 0 = carbon minimal solution (minCost as objective to avoid crazy cost)
            Outputs[0] = EnergyHub("cost", minCarbon.Carbon + carbonTolerance, null, verbose);
            Outputs[epsilonCuts + 1] = EnergyHub("carbon", null, minCost.Cost + costTolerance, verbose);
            double carbonInterval = (minCost.Carbon - minCarbon.Carbon) / (epsilonCuts + 1);

            // 4. make epsilonCuts cuts and solve for each minCost s.t. carbon
            for (int i = 0; i < epsilonCuts; i++)
                Outputs[i + 1] = EnergyHub("cost", minCarbon.Carbon + carbonInterval * (i + 1), null, verbose);
        }


        //electricity only 
        private MultiPeriodEhubOutput EnergyHub(string objective = "cost", double? carbonConstraint = null,
            double? costConstraint = null, bool verbose = false)
        {
            var solution = new MultiPeriodEhubOutput();
            Cplex cpl = new Cplex();
            var constraints = new List<IConstraint>();


            // hardcoding 3 investment periods: 2020, 2030, 2040
            // that means, we need 3 separate variables for each tech, because each tech per period will have different efficiencies, embodied emissions and cost parameters
            // also 3 separate arrays (incl set of constraints & expressions) for demands, irradiance, ghi, tamb, and conversion matrices

            // however, if I have 5 years intervals, I have to work with arrays. Can't have them manually anymore, would be too messy


            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            // Declaring and initializing variables and terms

            // CAPACITIES
            // Battery per period
            // PV mono and cdte per period and surface
            INumVar[] xNewBattery = new INumVar[NumPeriods]; // ignore binary for min battery capacity, I am aggregating over whole district anyway
            INumVar[][] xNewPvMono = new INumVar[NumPeriods][];
            INumVar[][] yNewPvMono = new INumVar[NumPeriods][];
            INumVar[][] xNewPvCdte = new INumVar[NumPeriods][];
            INumVar[][] yNewPvCdte = new INumVar[NumPeriods][];
            for (int p = 0; p < NumPeriods; p++)
            {
                // also check for total battery, not just newly installed
                xNewBattery[p] = cpl.NumVar(0, this.b_MaxBattery[p]);
                // pv
                xNewPvMono[p] = new INumVar[NumberOfSolarAreas];
                xNewPvCdte[p] = new INumVar[NumberOfSolarAreas];
                yNewPvMono[p] = new INumVar[NumberOfSolarAreas];
                yNewPvCdte[p] = new INumVar[NumberOfSolarAreas];
                for (int i = 0; i < NumberOfSolarAreas; i++)
                {
                    // for each period, same surface area
                    // later, special constraint to ensure total pv mono + cdte <= surfaceArea
                    xNewPvMono[p][i] = cpl.NumVar(0, SolarAreas[i]); 
                    xNewPvCdte[p][i] = cpl.NumVar(0, SolarAreas[i]);
                    yNewPvMono[p][i] = cpl.BoolVar();
                    yNewPvCdte[p][i] = cpl.BoolVar();
                }
            }

            // Total Capacities: over all periods
            INumVar[] totalCapacityBattery = new INumVar[NumPeriods];
            INumVar[][] totalCapacityPvMono = new INumVar[NumPeriods][]; // period, surface
            INumVar[][] totalCapacityPvCdte = new INumVar[NumPeriods][];
            for (int p = 0; p < NumPeriods; p++)
            {
                totalCapacityBattery[p] = cpl.NumVar(0.0, this.b_MaxBattery[p]);

                // I have to sum up in one totalCapPV to check for max space usage.
                // But I can't use totalCapPV for yield calculation, coz I'll have different efficiencies per period
                totalCapacityPvMono[p] = new INumVar[NumberOfSolarAreas];
                totalCapacityPvCdte[p] = new INumVar[NumberOfSolarAreas];
                for (int i = 0; i < NumberOfSolarAreas; i++)
                {
                    totalCapacityPvMono[p][i] = cpl.NumVar(0, SolarAreas[i]);
                    totalCapacityPvCdte[p][i] = cpl.NumVar(0, SolarAreas[i]);
                }
            }

            // OPERATION
            ILinearNumExpr[][] totalPvElectricity = new ILinearNumExpr[NumPeriods][];
            INumVar[][] xPvElectricity = new INumVar[NumPeriods][];
            INumVar[][] xOperationGridPurchase = new INumVar[NumPeriods][];
            INumVar[][] xOperationFeedIn = new INumVar[NumPeriods][];
            INumVar[][] yOperationFeedIn = new INumVar[NumPeriods][];
            INumVar[][] xOperationBatteryCharge = new INumVar[NumPeriods][];
            INumVar[][] xOperationBatteryDischarge = new INumVar[NumPeriods][];
            INumVar[][] xOperationBatteryStateOfCharge = new INumVar[NumPeriods][];
            for (int p = 0; p < NumPeriods; p++)
            {
                totalPvElectricity[p] = new ILinearNumExpr[Horizon];
                xPvElectricity[p] = new INumVar[Horizon];
                xOperationGridPurchase[p] = new INumVar[Horizon];
                xOperationFeedIn[p] = new INumVar[Horizon];
                yOperationFeedIn[p] = new INumVar[Horizon];
                xOperationBatteryCharge[p] = new INumVar[Horizon];
                xOperationBatteryDischarge[p] = new INumVar[Horizon];
                xOperationBatteryStateOfCharge[p] = new INumVar[Horizon];
                for (int t = 0; t < Horizon; t++)
                {
                    totalPvElectricity[p][t] = cpl.LinearNumExpr();
                    xOperationGridPurchase[p][t] = cpl.NumVar(0, double.MaxValue);
                    xOperationFeedIn[p][t] = cpl.NumVar(0, double.MaxValue);
                    yOperationFeedIn[p][t] = cpl.BoolVar();
                    xPvElectricity[p][t] = cpl.NumVar(0, double.MaxValue);
                    xOperationBatteryCharge[p][t] = cpl.NumVar(0, double.MaxValue);
                    xOperationBatteryDischarge[p][t] = cpl.NumVar(0, double.MaxValue);
                    xOperationBatteryStateOfCharge[p][t] = cpl.NumVar(0, double.MaxValue);
                }
            }




            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            // Constraints

            // Lifetime constraint
            // at each period, total PVs cant be larger than available surface
            // at each period, total batteries cant be larger than available space (based on floor area)
            for (int p = 0; p < NumPeriods; p++)
            {
                // battery max cap
                ILinearNumExpr sumExistingAndNewBattery = cpl.LinearNumExpr();
                for (int pp = (int) Math.Max(0, p - Math.Floor(LifetimeBattery[p] / YearsPerPeriod) + 1); pp <= p; pp++)
                    sumExistingAndNewBattery.AddTerm(1, xNewBattery[pp]);
                constraints.Add(cpl.AddEq(totalCapacityBattery[p], sumExistingAndNewBattery));

                // PV max cap
                for (int i = 0; i < NumberOfSolarAreas; i++)
                {
                    ILinearNumExpr sumExistingAndNewMono = cpl.LinearNumExpr();
                    ILinearNumExpr sumExistingAndNewCdte = cpl.LinearNumExpr();
                    for (int pp = (int)Math.Max(0, p - Math.Floor(LifetimePvMono[p] / YearsPerPeriod) + 1); pp <= p; pp++)
                        sumExistingAndNewMono.AddTerm(1, xNewPvMono[pp][i]);
                    for (int pp = (int)Math.Max(0, p - Math.Floor(LifetimePvCdte[p] / YearsPerPeriod) + 1); pp <= p; pp++)
                        sumExistingAndNewCdte.AddTerm(1, xNewPvCdte[pp][i]);
                    constraints.Add(cpl.AddEq(totalCapacityPvMono[p][i], sumExistingAndNewMono));
                    constraints.Add(cpl.AddEq(totalCapacityPvCdte[p][i], sumExistingAndNewCdte));
                }
            }
            // because mono and cdte are competing for same surface
            for (int p = 0; p < NumPeriods; p++)
                for (int i = 0; i < NumberOfSolarAreas; i++)
                    constraints.Add(cpl.AddGe(SolarAreas[i], cpl.Sum(totalCapacityPvMono[p][i], totalCapacityPvCdte[p][i])));


            // Energy Balance: meeting demands
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
                    elecGeneration.AddTerm(1, xOperationBatteryDischarge[p][t]);
                    elecAdditionalDemand.AddTerm(1, xOperationBatteryCharge[p][t]);
                    elecAdditionalDemand.AddTerm(1, xOperationFeedIn[p][t]);


                    /// PV Technical Constraints
                    // getting total pv generation. need it for OM cost
                    constraints.Add(cpl.AddEq(totalPvElectricity[p][t], xPvElectricity[p][t]));
                    // pv production must be greater equal feedin
                    constraints.Add(cpl.AddGe(totalPvElectricity[p][t], xOperationFeedIn[p][t]));
                    // donnot allow feedin and purchase at the same time. y = 1 means elec is produced
                    constraints.Add(cpl.AddLe(xOperationGridPurchase[p][t], cpl.Prod(M, yOperationFeedIn[p][t])));
                    constraints.Add(cpl.AddLe(xOperationFeedIn[p][t], cpl.Prod(M, cpl.Diff(1, yOperationFeedIn[p][t]))));

                    /// Energy Balance
                    constraints.Add(cpl.AddGe(cpl.Diff(elecGeneration, elecAdditionalDemand), this.ElectricityDemand[p][t]));
                }
            }



            /// Battery model
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int t = 0; t < this.Horizon; t++)
                {
                    ILinearNumExpr batteryState = cpl.LinearNumExpr();
                    batteryState.AddTerm((1 - this.bat_decay[p]), xOperationBatteryStateOfCharge[p][t]);
                    batteryState.AddTerm(this.bat_ch_eff[p], xOperationBatteryCharge[p][t]);
                    batteryState.AddTerm(-1 / this.bat_disch_eff[p], xOperationBatteryDischarge[p][t]);
                    if (t == this.Horizon - 1)
                        cpl.AddEq(xOperationBatteryStateOfCharge[p][0], batteryState);
                    else
                        cpl.AddEq(xOperationBatteryStateOfCharge[p][t + 1], batteryState);

                    if ((t + 1) % 24 == 0)
                    {
                        if (t != this.Horizon - 1)
                            cpl.AddEq(xOperationBatteryStateOfCharge[p][t + 1], xOperationBatteryStateOfCharge[p][t + 1 - 24]);
                        cpl.AddEq(xOperationBatteryDischarge[p][t], 0);
                        cpl.AddEq(xOperationBatteryCharge[p][t], 0);
                    }
                }
                cpl.AddGe(xOperationBatteryStateOfCharge[p][0], cpl.Prod(totalCapacityBattery[p], this.bat_min_state[p]));

                for (int t = 0; t < this.Horizon; t++)
                {
                    cpl.AddGe(xOperationBatteryStateOfCharge[p][t], cpl.Prod(totalCapacityBattery[p], this.bat_min_state[p]));     // min state of charge
                    cpl.AddLe(xOperationBatteryCharge[p][t], cpl.Prod(totalCapacityBattery[p], this.bat_max_ch[p]));        // battery charging
                    cpl.AddLe(xOperationBatteryDischarge[p][t], cpl.Prod(totalCapacityBattery[p], this.bat_max_disch[p]));  // battery discharging
                    cpl.AddLe(xOperationBatteryStateOfCharge[p][t], totalCapacityBattery[p]);                                   // battery sizing
                }
            }



            // Binary selection variables
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int i = 0; i < this.NumberOfSolarAreas; i++)
                {
                    cpl.AddLe(xNewPvMono[p][i], cpl.Prod(M, yNewPvMono[p][i]));
                    cpl.AddLe(xNewPvCdte[p][i], cpl.Prod(M, yNewPvCdte[p][i]));
                }
            }


            // Cost coefficients formulation
            ILinearNumExpr carbonEmissions = cpl.LinearNumExpr();
            ILinearNumExpr opex = cpl.LinearNumExpr();
            ILinearNumExpr capex = cpl.LinearNumExpr();
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int i = 0; i < NumberOfSolarAreas; i++)
                {
                    capex.AddTerm(LinearCostPvMono[p] / Math.Pow(1+InterestRate[p], p * YearsPerPeriod), xNewPvMono[p][i]);
                    //capex.AddTerm(FixCostPvMono[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), yNewPvMono[p][i]);
                    capex.AddTerm(LinearCostPvCdte[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xNewPvCdte[p][i]);
                    //capex.AddTerm(FixCostPVCdte[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), yNewPvCdte[p][i]);

                    carbonEmissions.AddTerm(this.LcaAnnualPvMono[p], xNewPvMono[p][i]);
                    carbonEmissions.AddTerm(this.LcaAnnualPvCdte[p], xNewPvCdte[p][i]);
                }
                capex.AddTerm(LinearCostBattery[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xNewBattery[p]);
                carbonEmissions.AddTerm(LcaAnnualBattery[p], xNewBattery[p]);

                for (int t = 0; t < Horizon; t++)
                {
                    opex.AddTerm((ClusterSizePerTimestep[p][t] * OperationCostGrid[p][t] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xOperationGridPurchase[p][t]);
                    opex.AddTerm((ClusterSizePerTimestep[p][t] * OperationRevenueFeedIn[p][t] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xOperationFeedIn[p][t]);
                    opex.AddTerm((ClusterSizePerTimestep[p][t] * OmCostPV[p] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xPvElectricity[p][t]);
                    opex.AddTerm((ClusterSizePerTimestep[p][t] * OmCostBattery[p] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xOperationBatteryDischarge[p][t]); //OM because battery deterioration
                    carbonEmissions.AddTerm(ClusterSizePerTimestep[p][t] * this.LcaGridElectricity[p], xOperationGridPurchase[p][t]);     // data needs to be kgCO2eq./kWh
                }
            }


            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            // TO DO: Salvage
            // only account for cost until end of horizon -> annualized cost for PV and battery -> whatever lives beyond: subtract from capex
            // https://iea-etsap.org/docs/Documentation_for_the_TIMES_Model-PartII.pdf page 173, found in Mango Mavromatidis and Petkov 2021

            // battery, pv mono, pv cdte
            for (int p = 0; p < NumPeriods; p++)
            {
                if (LifetimeBattery[p] > YearsPerPeriod * (NumPeriods - p))
                {
                    double overlife = LifetimeBattery[p] - (YearsPerPeriod * (NumPeriods - p));
                    var deductible = cpl.LinearNumExpr();
                    deductible.AddTerm((LinearCostBattery[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod)) * (overlife / LifetimeBattery[p]) * -1, xNewBattery[p]);
                    capex.Add(deductible);
                }

                if (LifetimePvMono[p] > YearsPerPeriod * (NumPeriods - p))
                {
                    for (int i = 0; i < NumberOfSolarAreas; i++)
                    {
                        double overlife = LifetimePvMono[p] - (YearsPerPeriod * (NumPeriods - p));
                        var deductible = cpl.LinearNumExpr();
                        deductible.AddTerm((LinearCostPvMono[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod)) * (overlife / LifetimePvMono[p]) * -1, xNewPvMono[p][i]);
                        capex.Add(deductible);
                    }
                }

                if (LifetimePvCdte[p] > YearsPerPeriod * (NumPeriods - p))
                {
                    for (int i = 0; i < NumberOfSolarAreas; i++)
                    {
                        double overlife = LifetimePvCdte[p] - (YearsPerPeriod * (NumPeriods - p));
                        var deductible = cpl.LinearNumExpr();
                        deductible.AddTerm((LinearCostPvCdte[p] / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod)) * (overlife / LifetimePvCdte[p]) * -1, xNewPvCdte[p][i]);
                        capex.Add(deductible);
                    }
                }
            }



            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            bool isCostMinimization = false;
            if (string.Equals(objective, "cost"))
                isCostMinimization = true;

            bool hasCarbonConstraint = false;
            bool hasCostConstraint = false;
            if (!carbonConstraint.IsNullOrDefault())
                hasCarbonConstraint = true;
            if (!costConstraint.IsNullOrDefault())
                hasCostConstraint = true;

            /// Objective function
            if (isCostMinimization) cpl.AddMinimize(cpl.Sum(capex, opex));
            else cpl.AddMinimize(carbonEmissions);
            // TO DO: CO2
            //constraint smaler eq co2 target

            // epsilon constraints for carbon, 
            // or cost constraint in case of carbon minimization (the same reason why carbon minimization needs a cost constraint)
            if (hasCarbonConstraint && isCostMinimization) cpl.AddLe(carbonEmissions, (double)carbonConstraint);
            else if (hasCostConstraint && !isCostMinimization) cpl.AddLe(cpl.Sum(capex, opex), (double)costConstraint);


            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            //_________________________________________________________________________________________
            /// Solve
            if (!verbose) cpl.SetOut(null);
            cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.005);
            cpl.SetParam(Cplex.IntParam.MIPDisplay, 4);
            

            try
            {
                bool success = cpl.Solve();
                //var conflict = cpl.GetConflict(constraints.ToArray());
                //foreach (var con in conflict) 
                //{
                //    Console.WriteLine(con.ToString());
                //}
                if (!success)
                {
                    solution.infeasible = true;
                    return solution;
                }

                /// Outputs
                solution.Opex = cpl.GetValue(opex);
                solution.Capex = cpl.GetValue(capex);
                solution.Cost = solution.Opex + solution.Capex;
                solution.Carbon = cpl.GetValue(carbonEmissions);
                // TO DO: CO2

                solution.XTotalPvMono = new double[NumPeriods][];
                solution.XTotalPvCdte = new double[NumPeriods][];
                solution.XNewPvMono = new double[NumPeriods][];
                solution.XNewPvCdte = new double[NumPeriods][];
                solution.XNewBattery = new double[NumPeriods];
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
                    solution.XNewBattery[p] = cpl.GetValue(xNewBattery[p]); 
                }


                solution.XOperationPvElectricity = new double[NumPeriods][];
                solution.XOperationElecPurchase = new double[NumPeriods][];
                solution.XOperationFeedIn = new double[NumPeriods][];
                solution.XOperationBatterySoc = new double[NumPeriods][];
                solution.XOperationBatteryCharge = new double[NumPeriods][];
                solution.XOperationBatteryDischarge = new double[NumPeriods][];

                solution.Clustersize = new int[NumPeriods][];
                for (int p = 0; p < NumPeriods; p++)
                {
                    solution.XOperationPvElectricity[p] = new double[Horizon];
                    solution.XOperationElecPurchase[p] = new double[Horizon];
                    solution.XOperationFeedIn[p] = new double[Horizon];
                    solution.XOperationBatterySoc[p] = new double[Horizon];
                    solution.XOperationBatteryCharge[p] = new double[Horizon];
                    solution.XOperationBatteryDischarge[p] = new double[Horizon];
                    solution.Clustersize[p] = new int[Horizon];
                    for (int t = 0; t < this.Horizon; t++)
                    {
                        solution.XOperationPvElectricity[p][t] = cpl.GetValue(xPvElectricity[p][t]);
                        solution.XOperationElecPurchase[p][t] = cpl.GetValue(xOperationGridPurchase[p][t]);
                        solution.XOperationFeedIn[p][t] = cpl.GetValue(xOperationFeedIn[p][t]);
                        solution.XOperationBatterySoc[p][t] = cpl.GetValue(xOperationBatteryStateOfCharge[p][t]);
                        solution.XOperationBatteryCharge[p][t] = cpl.GetValue(xOperationBatteryCharge[p][t]);
                        solution.XOperationBatteryDischarge[p][t] = cpl.GetValue(xOperationBatteryDischarge[p][t]);

                        solution.Clustersize[p][t] = ClusterSizePerTimestep[p][t];
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


        // just for debugging...
        private MultiPeriodEhubOutput EHubSimple(bool verbose = false)
        {
            var solution = new MultiPeriodEhubOutput();
            Cplex cpl = new Cplex();
            var constraints = new List<IConstraint>();


            // declare variables
            INumVar[][] xPvMonoNew = new INumVar[NumPeriods][]; //how much new PV sized at that period and on that surface
            INumVar[][] xOperationGridPurchase = new INumVar[NumPeriods][];
            INumVar[][] xOperationFeedIn = new INumVar[NumPeriods][];
            INumVar[][] yFeedIn = new INumVar[NumPeriods][];

            // declare terms
            ILinearNumExpr[][] totalPvElectricity = new ILinearNumExpr[NumPeriods][];

            // Init variables & terms
            for (int p = 0; p < NumPeriods; p++)
            {
                xPvMonoNew[p] = new INumVar[NumberOfSolarAreas];
                for (int s=0; s<NumberOfSolarAreas; s++)
                    xPvMonoNew[p][s] = cpl.NumVar(0.0, SolarAreas[s]);

                xOperationGridPurchase[p] = new INumVar[Horizon];
                xOperationFeedIn[p] = new INumVar[Horizon];
                yFeedIn[p] = new INumVar[Horizon];

                totalPvElectricity[p] = new ILinearNumExpr[Horizon];
                for (int t = 0; t < Horizon; t++)
                {
                    totalPvElectricity[p][t] = cpl.LinearNumExpr();
                    xOperationGridPurchase[p][t] = cpl.NumVar(0, double.MaxValue);
                    xOperationFeedIn[p][t] = cpl.NumVar(0, double.MaxValue);
                    yFeedIn[p][t] = cpl.BoolVar();
                }
            }


            /// constraints

            // meeting demands

            // total pv at each period cannot exceed total available area
            ILinearNumExpr[][] sumPvAreas = new ILinearNumExpr[NumPeriods][];
            for (int p = 0; p < NumPeriods; p++)
            {
                sumPvAreas[p] = new ILinearNumExpr[NumberOfSolarAreas];
                for (int s = 0; s < NumberOfSolarAreas; s++)
                    sumPvAreas[p][s] = cpl.LinearNumExpr();
            }

            for (int p = 0; p < NumPeriods; p++)
            {
                // summing all PvMono of all periods together
                // PV over all periods cant be bigger than the surface
                // TO DO: lifetime ending
                for (int s = 0; s < NumberOfSolarAreas; s++) 
                { 
                    for (int pp = 0; pp <= p; pp++)
                        sumPvAreas[p][s].AddTerm(1, xPvMonoNew[pp][s]);
                    constraints.Add(cpl.AddLe(sumPvAreas[p][s], SolarAreas[s]));
                }


                for (int t = 0; t < this.Horizon; t++)
                {
                    ILinearNumExpr elecOutgoing = cpl.LinearNumExpr();
                    elecOutgoing.AddTerm(1, xOperationFeedIn[p][t]);


                    ILinearNumExpr elecGeneration = cpl.LinearNumExpr();
                    elecGeneration.AddTerm(1, xOperationGridPurchase[p][t]);
                    for (int s = 0; s < NumberOfSolarAreas; s++)
                    {
                        // need to go through all PVs sized from all periods. use efficiency from respective period, but solar load from current period
                        for (int pp = 0; pp <= p; pp++)
                        {
                            double pvElec = this.SolarLoads[p][s][t] * 0.001 * this.PvEfficiencyMono[pp][s][t];
                            totalPvElectricity[p][t].AddTerm(xPvMonoNew[pp][s], pvElec);
                            elecGeneration.AddTerm(xPvMonoNew[pp][s], pvElec);
                        }
                    }
                    

                    // pv production must be greater equal feedin
                    constraints.Add(cpl.AddGe(totalPvElectricity[p][t], xOperationFeedIn[p][t]));
                    // donnot allow feedin and purchase at the same time. y = 1 means elec is produced. NOTE: Infeasible before because big M too small
                    cpl.AddLe(xOperationGridPurchase[p][t], cpl.Prod(M, yFeedIn[p][t]));
                    cpl.AddLe(xOperationFeedIn[p][t], cpl.Prod(M, cpl.Diff(1, yFeedIn[p][t])));

                    /// Energy Balance
                    constraints.Add(cpl.AddGe(cpl.Diff(elecGeneration, elecOutgoing), this.ElectricityDemand[p][t]));
                    //constraints.Add(cpl.AddGe(xOperationGridPurchase[p][t], this.ElectricityDemand[p][t]));
                }
            }





            /// ////////////////////////////////////////////////////////////////////////
            /// Cost coefficients formulation
            /// ////////////////////////////////////////////////////////////////////////
            ILinearNumExpr opex = cpl.LinearNumExpr();
            ILinearNumExpr capex = cpl.LinearNumExpr();
            for (int p = 0; p < NumPeriods; p++)
            {
                for (int t = 0; t < Horizon; t++)
                {
                    opex.AddTerm((ClusterSizePerTimestep[p][t] * OperationCostGrid[p][t] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod), xOperationGridPurchase[p][t]);
                    opex.AddTerm(((ClusterSizePerTimestep[p][t] * OperationRevenueFeedIn[p][t] * YearsPerPeriod) / Math.Pow(1 + InterestRate[p], p * YearsPerPeriod)), xOperationFeedIn[p][t]); //revenue already negative value. dont need *-1
                }

                //for (int s = 0; s < NumberOfSolarAreas; s++)
                //    capex.AddTerm(xPvMono[p][s], this.LinearCostPvMono[p]);
            }



            /// ////////////////////////////////////////////////////////////////////////
            /// Objective function
            /// ////////////////////////////////////////////////////////////////////////
            cpl.AddMinimize(cpl.Sum(opex, capex));


            /// ////////////////////////////////////////////////////////////////////////
            /// Solve
            /// ////////////////////////////////////////////////////////////////////////
            if (!verbose) cpl.SetOut(null);
            cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.005);
            cpl.SetParam(Cplex.IntParam.MIPDisplay, 4);


            try
            {
                bool success = cpl.Solve();
                //Cplex.ConflictStatus[] conflict = cpl.GetConflict(constraints.ToArray());
                //foreach(var cons in constraints)
                //{
                //    Console.WriteLine(cons.ToString());
                //}
                //foreach(var conf in conflict)
                //{
                //    Console.WriteLine(conf.ToString());
                //}

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

                solution.Clustersize = new int[NumPeriods][];
                solution.XOperationElecPurchase = new double[NumPeriods][];
                solution.XOperationFeedIn = new double[NumPeriods][];
                solution.XOperationBatterySoc = new double[NumPeriods][];
                solution.XOperationBatteryCharge = new double[NumPeriods][];
                solution.XOperationBatteryDischarge = new double[NumPeriods][];
                solution.XOperationPvElectricity = new double[NumPeriods][];

                solution.XNewPvMono = new double[NumPeriods][];

                for (int p = 0; p < NumPeriods; p++)
                {
                    solution.Clustersize[p] = new int[Horizon];
                    solution.XOperationElecPurchase[p] = new double[Horizon];
                    solution.XOperationFeedIn[p] = new double[Horizon];
                    solution.XOperationBatterySoc[p] = new double[Horizon];
                    solution.XOperationBatteryCharge[p] = new double[Horizon];
                    solution.XOperationBatteryDischarge[p] = new double[Horizon];
                    solution.XOperationPvElectricity[p] = new double[Horizon];

                    for (int t = 0; t < this.Horizon; t++)
                    {
                        solution.Clustersize[p][t] = ClusterSizePerTimestep[p][t];
                        solution.XOperationElecPurchase[p][t] = cpl.GetValue(xOperationGridPurchase[p][t]);
                        solution.XOperationFeedIn[p][t] = cpl.GetValue(xOperationFeedIn[p][t]);
                        solution.XOperationBatterySoc[p][t] = 0;
                        solution.XOperationBatteryCharge[p][t] = 0;
                        solution.XOperationBatteryDischarge[p][t] = 0;
                        solution.XOperationPvElectricity[p][t] = cpl.GetValue(totalPvElectricity[p][t]);
                    }

                    solution.XNewPvMono[p] = new double[NumberOfSolarAreas];
                    for (int s = 0; s < NumberOfSolarAreas; s++)
                    {
                        solution.XNewPvMono[p][s] = cpl.GetValue(xPvMonoNew[p][s]);
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
