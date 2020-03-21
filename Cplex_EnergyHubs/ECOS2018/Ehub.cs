using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using ILOG.CPLEX;
using ILOG.Concert;

namespace Cplex_ECOS2018
{
    /// <summary>
    /// Outputs. new struct per epsilon cut
    /// </summary>
    internal struct Ehub_outputs
    {

        internal double carbon;             // annual carbon.
        internal double cost;               // cost. levelized.
        internal double OPEX;               // annual operation cost.
        internal double CAPEX;              // capital cost. levelized.

        // Technology sizing
        internal double[] x_pv;             // pv sizing [m2]
        internal double x_bat;              // battery 
        internal double x_hp;               // heat pump. assume it reaches peak heat temperatures as simplification.
        internal double x_tes;              // thermal storage
        internal double x_chp;              // combined heat and power
        internal double x_ac;               // air condition
        internal double x_boi;              // gas boiler

        // Operation. Time resolved.
        internal double[] x_elecpur;        // purchase from grid
        internal double[] x_feedin;         // feedin
        internal double[] x_batdischarge;   // battery discharge
        internal double[] x_batcharge;      // battery charge
        internal double[] x_batsoc;         // battery state of charge
        internal double[] x_tesdischarge;   // thermal energy storage (tes) discharge
        internal double[] x_tescharge;      // tes charge
        internal double[] x_tessoc;         // tes state of charge
        internal double[] x_hp_op;          // heat pump operation
        internal double[] x_boi_op;         // boiler operation
        internal double[] x_chp_op_e;       // chp operation electricity
        internal double[] x_chp_op_h;       // chp operation heat
        internal double[] x_chp_dump;       // chp heat dumped

        internal double[] b_pvprod;     // total pv production
        internal double[] b_pvprod_Roof;// pv production roof
        internal double[] b_pvprod_E;   // pv prod East
        internal double[] b_pvprod_S_a; // pv prod South A
        internal double[] b_pvprod_S_b; // pv prod South B
        internal double[] b_pvprod_W_a; // pv prod West A
        internal double[] b_pvprod_W_b; // pv prod West B

    }



    /// <summary>
    /// Energyhub class
    /// </summary>
    internal class Ehub
    {
        #region global variables

        internal Ehub_outputs[] outputs;




        ///////////////////////////////////////////////////////////////
        // Constants. Mainly technology properties
        ///////////////////////////////////////////////////////////////

        // Technology properties (efficiency)
        // PV data
        internal const double pv_NCOT = 45;             // Nominal operating cell temperatuer
        internal const double pv_T_aNCOT = 20;          // reference Temperature for NCOT
        internal const double pv_P_NCOT = 800;          // irradiation of NCOT
        internal const double pv_beta_ref = 0.004;      // temperature coefficient
        internal const double pv_n_ref = 0.2;           // PV efficiency under NCOT     //ISE freiburg has 22,6 CIGS. 

        // Air source heat pump data
        internal const double hp_pi1 = 13.39;           // coefficient 1
        internal const double hp_pi2 = -0.047;          // coefficient 2
        internal const double hp_pi3 = 1.109;           // coefficient 3
        internal const double hp_pi4 = 0.012;           // coefficient 4
        internal const double hp_dist = 65.0;           // supply temperature

        // Boiler data
        internal const double c_boi_eff = 0.94;         // efficiency (natural gas to heat)

        // Combined Heat and Power (CHP) data
        internal const double c_chp_eff = 0.3;          // efficiency (natural gas to electricity)
        internal const double c_chp_htp = 1.73;         // heat to power ratio (for 1kW of heat, 1.73 kW of electricity)
        internal const double c_chp_minload = 0.5;      // minimum part load
        internal const double c_chp_heatdump = 1;       // heat dump allowed (1 = 100%)

        // Air conditioning
        internal const double ac_cop = 3;                   // η=3

        // Battery data
        internal const double bat_ch_eff = 0.92;            // Battery charging efficiency
        internal const double bat_disch_eff = 0.92;         // Battery discharging efficiency
        internal const double bat_decay = 0.001;            // Battery hourly decay
        internal const double bat_max_ch = 0.3;             // Battery max charging rate
        internal const double bat_max_disch = 0.33;         // Battery max discharging rate
        internal const double bat_min_state = 0.3;          // Battery minimum state of charge

        // Thermal Energy Storage tank data
        internal const double tes_ch_eff = 0.9;             // Thermal energy storage (TES) charging efficiency
        internal const double tes_disch_eff = 0.9;          // TES discharging efficiency
        internal const double tes_decay = 0.001;            // TES heat loss
        internal const double tes_max_ch = 0.25;            // TES max charging rate
        internal const double tes_max_disch = 0.25;         // TES max discharging rate








        //// Life cycle data [kgCO2-eq].
        //internal const double lca_pv_flat = (2320.0 / 8) / LifePV; // per m2. KBOB 2012. 8m2 assumed by george and magnolis
        //internal const double lca_pv_facade = (2140.0 / 8) / LifePV; // per m2. KBOB 2012
        //internal const double lca_battery = 157.14 / LifeBat;         // per kWh installed. Hiremath et al. (2015)
        //internal const double lca_therm = 0.0;              // per kWh installed
        //internal const double lca_hp = 1050 / 8.0;                 // per kW installed (Luft-Wasser Wärmepumpe)
        //internal const double lca_chp = 0.0;                // per kW installed
        //internal const double lca_ac = 0.0;                 // per kW installed
        //internal const double lca_boiler = 1.1;             // per kW installed (Wärmeerzeuger)

        // put this to zero, if I wanna excluce embodied carbon.
        internal const double lca_pv_flat = 0.0; // per m2. KBOB 2012. 8m2 assumed by george and magnolis
        internal const double lca_pv_facade = 0.0; // per m2. KBOB 2012
        internal const double lca_battery = 0.0;         // per kWh installed. Hiremath et al. (2015)
        internal const double lca_therm = 0.0;              // per kWh installed
        internal const double lca_hp = 0.0;                 // per kW installed
        internal const double lca_chp = 0.0;                // per kW installed
        internal const double lca_ac = 0.0;                 // per kW installed
        internal const double lca_boiler = 0.0;             // per kW installed


        internal const double lca_gas = 0.228;                // natural gas from grid, per kWh


        // Economic data
        internal const double intrate = 0.08;

        internal const double CostPV = 250;             // Cost PV per m2. assume cheap production of CIGS
        internal const double CostHP = 1000;            // Cost HP per kW installed
        internal const double CostBoi = 200;            // Cost Boiler per kW installed
        internal const double CostCHP = 1500;           // Cost CHP per kW installed
        internal const double CostAC = 360;             // AirCon capital cost per kW
        internal const double CostBat = 600;            // Battery capital cost per kWh
        internal const double CostTES = 100;            // TES capital cost per kWh

        internal const double LifePV = 20;              // PV lifetime
        internal const double LifeHP = 20;              // HP lifetime 
        internal const double LifeBoi = 30;             // Boiler lifetime
        internal const double LifeCHP = 20;             // CHP lifetime
        internal const double LifeBat = 20;             // Battery lifetime
        internal const double LifeTES = 17;             // TES lifetime
        internal const double LifeAC = 20;              // AirCon lifetime

        internal static double annuityPV = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifePV)))));
        internal static double annuityHP = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeHP)))));
        internal static double annuityBoi = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeBoi)))));
        internal static double annuityCHP = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeCHP)))));
        internal static double annuityAC = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeAC)))));
        internal static double annuityBat = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeBat)))));
        internal static double annuityTES = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeTES)))));

        internal static double c_pv = CostPV * annuityPV;       // levelized cost PV
        internal static double c_hp = CostHP * annuityHP;       // levelized cost HP
        internal static double c_boi = CostBoi * annuityBoi;    // levelized cost boiler
        internal static double c_chp = CostCHP * annuityCHP;    // levelized cost CHP
        internal static double c_ac = CostAC * annuityAC;       // levelized cost AirCon
        internal static double c_bat = CostBat * annuityBat;    // levelized cost battery
        internal static double c_tes = CostTES * annuityTES;    // levelized cost TES

        internal static double c_pv_om = 0.0;                   // operating maintenance cost per kWh
        internal static double c_hp_om = 0.1;
        internal static double c_boi_om = 0.01;
        internal static double c_chp_om = 0.021;

        internal static double c_gas = 0.09;                    // natural gas per kWh

        internal const double M = 9999999;  // big M method



        ///////////////////////////////////////////////////////////////
        // Inputs
        ///////////////////////////////////////////////////////////////
        private double[] b_heat_typ;           // heating demand [kWh]
        private double[] b_cool_typ;           // cooling demand [kWh]
        private double[] b_elec_typ;           // electricity demand [kWh]
        private double[] c_grid_typ;           // dynamic grid electricity cost [CHF/kWh]
        private double[] c_feedin_typ;         // dynamic feedin tarif [CHF/kWh]
        private double[] a_carbon_typ;         // dynamic carbon emission factor [g-CO2/kWh eq.]

        private double[][] a_solar_typ;        // solar potentials. [W/m2]. array: [60][horizon]
        private double[] b_solar_area;         // available areas for pv [m2]

        private double[][] c_solar_eff;        // pv efficiency, time and pv patch dependant 
        private double[] c_hp_eff;             // hp efficiency, time dependant 

        private int[] c_num_of_days;      // number of days, that each typical day represents. should sum to 365. length of horizon. 24x same day.
        private int[] loc_days;         // day of the year, that each typical day represents. needed for sorting.





        ///////////////////////////////////////////////////////////////
        // Properties
        ///////////////////////////////////////////////////////////////
        internal int horizon;       //timesteps, hourly. 10 days are 240 h
        internal int morris_length;
        internal int morris;

        #endregion



        /// <summary>
        /// Create new ECOS 2018 energy hub object.
        /// </summary>
        /// <param name="path">Path to inputs.</param>
        /// <param name="morris">sample number. index to the stochastic profiles</param>
        internal Ehub(string path, int morris, int morris_length)
        {
            // ===================================================================
            // Pre-processing. Loading profiles etc.
            // ===================================================================
            this.morris_length = morris_length;
            this.morris = morris;

            //load stochastic profiles
            this.ReadInput(path, morris,
                out this.b_elec_typ, out this.b_heat_typ, out this.b_cool_typ,
                out this.c_grid_typ, out this.c_feedin_typ, out this.a_carbon_typ,
                out this.a_solar_typ, out this.b_solar_area, out this.c_solar_eff,
                out this.c_hp_eff,
                out this.c_num_of_days, out this.loc_days);

            // horizon
            this.horizon = b_elec_typ.Length;


            //sort according to day of the year, so I can assume seasonal storage
            this.SortInputsSeasons(this.loc_days,
                ref this.c_num_of_days,
                ref this.b_elec_typ, ref this.b_heat_typ, ref this.b_cool_typ,
                ref this.c_grid_typ, ref this.c_feedin_typ, ref this.a_carbon_typ,
                ref this.a_solar_typ, ref this.c_solar_eff, ref this.c_hp_eff);


        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="epsilonsteps">number of epsilon steps for multi objective. spaced equally between min cost and min carbon. includes min cost and min carbon.</param>
        /// <param name="ehubmode">0: electricity only. 1: electricity but with battery. 2: multi energy.</param>
        internal void Solve_MultiObejctive(int epsilonsteps, int ehubmode)
        {
            this.outputs = new Ehub_outputs[epsilonsteps];
            //this.outputs.min_carbon = new double[epsilonsteps];
            //this.outputs.min_cost = new double[epsilonsteps];
            //this.outputs.pv_sizing = new double[epsilonsteps][];
            double mincarb;
            double maxcarb;
            Ehub_outputs outputtemp = new Ehub_outputs();
            double tmpcarb = double.MaxValue;
            switch (ehubmode)
            {

                // ===================================================================
                // elec demand only. grid or PV only.
                // ===================================================================
                case 0:
                    // 1. Minimize carbon. Carbon lower bound of pareto front. store carbon emission. rerun with cost included. 
                    outputtemp = this.ehubmodel_elec(true);
                    tmpcarb = outputtemp.carbon;
                    this.outputs[0] = this.ehubmodel_elec(false, tmpcarb);

                    // 2. Minimize cost. Cost lower bound of pareto front.
                    this.outputs[epsilonsteps - 1] = this.ehubmodel_elec(false);

                    // 3. Add epsilon steps in between min cost and min carbon
                    mincarb = this.outputs[0].carbon;
                    maxcarb = this.outputs[epsilonsteps - 1].carbon;
                    for (int e = 1; e < epsilonsteps - 1; e++)
                    {
                        double carboncut = ((maxcarb - mincarb) / epsilonsteps) * e + mincarb;
                        this.outputs[e] = this.ehubmodel_elec(false, carboncut);
                    }
                    break;


                // ===================================================================
                // elec demand only. grid, PV and battery
                // ===================================================================
                case 1:
                    // 1. Minimize carbon. Carbon lower bound of pareto front. store carbon emission. rerun with cost included. 
                    outputtemp = this.ehubmodel_elecbat(true);
                    tmpcarb = outputtemp.carbon;
                    this.outputs[0] = this.ehubmodel_elecbat(false, tmpcarb);

                    // 2. Minimize cost. Cost lower bound of pareto front.
                    this.outputs[epsilonsteps - 1] = this.ehubmodel_elecbat(false);

                    // 3. Add epsilon steps in between min cost and min carbon
                    mincarb = this.outputs[0].carbon;
                    maxcarb = this.outputs[epsilonsteps - 1].carbon;
                    for (int e = 1; e < epsilonsteps - 1; e++)
                    {
                        double carboncut = ((maxcarb - mincarb) / epsilonsteps) * e + mincarb;
                        this.outputs[e] = this.ehubmodel_elecbat(false, carboncut);
                    }
                    break;


                // ===================================================================
                // multi energy (heat, cool, elec). grid, PV, battery, thermal storage, hp, chp.
                // ===================================================================
                case 2:
                    // 1. Minimize carbon. Carbon lower bound of pareto front. store carbon emission. rerun with cost included. 
                    outputtemp = this.ehubmodel_multi(true);
                    tmpcarb = outputtemp.carbon;
                    this.outputs[0] = this.ehubmodel_multi(false, tmpcarb);

                    // 2. Minimize cost. Cost lower bound of pareto front.
                    this.outputs[epsilonsteps - 1] = this.ehubmodel_multi(false);

                    // 3. Add epsilon steps in between min cost and min carbon
                    mincarb = this.outputs[0].carbon;
                    maxcarb = this.outputs[epsilonsteps - 1].carbon;
                    for (int e = 1; e < epsilonsteps - 1; e++)
                    {
                        double carboncut = ((maxcarb - mincarb) / epsilonsteps) * e + mincarb;
                        this.outputs[e] = this.ehubmodel_multi(false, carboncut);
                    }
                    break;
            }
        }


        private Ehub_outputs ehubmodel_elec(bool bln_mincarbon, double? carbonconstraint = null)
        {
            // Solver
            Cplex cpl = new Cplex();


            Ehub_outputs outs = new Ehub_outputs();

            int pvspots = this.b_solar_area.Length;   // number of different PV patches



            double b_co2_target = double.MaxValue;
            bool bln_co2target = false;


            if (!carbonconstraint.IsNullOrDefault())
            {
                b_co2_target = Convert.ToDouble(carbonconstraint);
                bln_co2target = true;
            }




            //variables
            INumVar[] x_pv = new INumVar[pvspots];
            for (int i = 0; i < pvspots; i++)
            {
                x_pv[i] = cpl.NumVar(0, this.b_solar_area[i]);
            }

            INumVar[] y = new INumVar[this.horizon];            //binary control variable. if pv used, it is 1. to avoid selling and purchasing from and to the grid at the same time. from portia
            INumVar[] x_purchase = new INumVar[this.horizon];
            INumVar[] x_feedin = new INumVar[this.horizon];
            for (int t = 0; t < this.horizon; t++)
            {
                y[t] = cpl.BoolVar();
                x_purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_feedin[t] = cpl.NumVar(0, System.Double.MaxValue);
            }




            //problem
            ILinearNumExpr OPEX = cpl.LinearNumExpr();
            ILinearNumExpr CAPEX = cpl.LinearNumExpr();
            for (int i = 0; i < pvspots; i++)
            {
                CAPEX.AddTerm(c_pv, x_pv[i]);
            }
            for (int t = 0; t < this.horizon; t++)
            {
                OPEX.AddTerm(this.c_grid_typ[t] * this.c_num_of_days[t], x_purchase[t]);
                OPEX.AddTerm(-1.0 * this.c_feedin_typ[t] * this.c_num_of_days[t], x_feedin[t]);
                //OPEX.AddTerm(this.c_grid_typ[t], x_purchase[t]);
                //OPEX.AddTerm(-1.0 * this.c_feedin_typ[t], x_feedin[t]);
            }
            if (!bln_mincarbon) cpl.AddMinimize(cpl.Sum(OPEX, CAPEX));



            //constraints
            ILinearNumExpr a_co2 = cpl.LinearNumExpr();
            for (int t = 0; t < this.horizon; t++)
            {
                //demand must be met by pv and grid, minuse electricty sold to the grid
                ILinearNumExpr pvproduction = cpl.LinearNumExpr();
                for (int i = 0; i < pvspots; i++)
                {
                    pvproduction.AddTerm(this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t], x_pv[i]);     // in kW
                }
                cpl.AddGe(cpl.Diff(cpl.Sum(pvproduction, x_purchase[t]), x_feedin[t]), this.b_elec_typ[t]);

                //pv production must be greater or equal feedin
                cpl.AddGe(pvproduction, x_feedin[t]);

                cpl.AddLe(x_purchase[t], cpl.Prod(M, y[t]));        // y = 1 means electricity is being purchased
                cpl.AddLe(x_feedin[t], cpl.Prod(M, cpl.Diff(1, y[t])));

                a_co2.AddTerm((this.a_carbon_typ[t] / 1000) * this.c_num_of_days[t], x_purchase[t]);
                //a_co2.AddTerm((this.a_carbon_typ[t] / 1000) , x_purchase[t]);
            }

            for (int i = 0; i < pvspots - 6; i++)
                a_co2.AddTerm(lca_pv_facade, x_pv[i]);
            for (int i = pvspots - 6; i < pvspots; i++)
                a_co2.AddTerm(lca_pv_flat, x_pv[i]);

            if (bln_co2target && !bln_mincarbon) cpl.AddLe(a_co2, b_co2_target);
            if (bln_mincarbon) cpl.AddMinimize(a_co2);


            //solve            
            cpl.SetOut(null);
            cpl.Solve();
            //Console.WriteLine("mip gap: {0}", cpl.GetMIPRelativeGap());

            //for (int i = 0; i < pvspots; i++)
            //    Console.WriteLine("pv spot {0}, area installed: {1}", i, cpl.GetValue(x_pv[i]));


            //if (bln_mincarbon)
            //{
            //    Console.WriteLine("cost: " + cpl.GetValue(cpl.Sum(OPEX, CAPEX)));
            //}
            //else
            //    Console.WriteLine("cost: " + cpl.ObjValue);

            //Console.WriteLine("OPEX: {0}... CAPEX: {1}", cpl.GetValue(OPEX), cpl.GetValue(CAPEX));

            //Console.WriteLine("total co2 emissions: {0}", cpl.GetValue(a_co2));
            //Console.WriteLine("tot pur: {0}, tot sold: {1}", cpl.GetValue(cpl.Sum(x_purchase)), cpl.GetValue(cpl.Sum(x_feedin)));
            //Console.ReadKey();

            if (bln_mincarbon)
            {
                outs.cost = cpl.GetValue(cpl.Sum(OPEX, CAPEX));
                outs.carbon = cpl.ObjValue;
            }
            else
            {
                outs.cost = cpl.ObjValue;
                outs.carbon = cpl.GetValue(a_co2);
            }

            outs.x_elecpur = new double[this.horizon];
            outs.x_feedin = new double[this.horizon];
            outs.b_pvprod = new double[this.horizon];
            outs.b_pvprod_S_a = new double[this.horizon];
            outs.b_pvprod_S_b = new double[this.horizon];
            outs.b_pvprod_W_a = new double[this.horizon];
            outs.b_pvprod_W_b = new double[this.horizon];
            outs.b_pvprod_E = new double[this.horizon];
            outs.b_pvprod_Roof = new double[this.horizon];
            outs.x_pv = new double[this.b_solar_area.Length];
            for (int t = 0; t < this.horizon; t++)
            {
                outs.x_elecpur[t] = cpl.GetValue(x_purchase[t]);
                outs.x_feedin[t] = cpl.GetValue(x_feedin[t]);
                outs.b_pvprod[t] = 0;
                for (int i = 0; i < this.b_solar_area.Length; i++)
                {
                    double pvprodnow = this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t] * cpl.GetValue(x_pv[i]);
                    outs.b_pvprod[t] += pvprodnow;
                    if (i >= 0 && i <= 17)
                        outs.b_pvprod_S_a[t] += pvprodnow;
                    else if (i >= 18 && i <= 22)
                        outs.b_pvprod_S_b[t] += pvprodnow;
                    else if (i >= 23 && i <= 32)
                        outs.b_pvprod_W_a[t] += pvprodnow;
                    else if (i >= 33 && i <= 38)
                        outs.b_pvprod_W_b[t] += pvprodnow;
                    else if (i >= 39 && i <= 53)
                        outs.b_pvprod_E[t] += pvprodnow;
                    else if (i >= 54 && i <= 59)
                        outs.b_pvprod_Roof[t] += pvprodnow;
                }
            }
            for (int i = 0; i < this.b_solar_area.Length; i++)
                outs.x_pv[i] = cpl.GetValue(x_pv[i]);

            outs.OPEX = cpl.GetValue(OPEX);
            outs.CAPEX = cpl.GetValue(CAPEX);


            return outs;
        }


        private Ehub_outputs ehubmodel_elecbat(bool bln_mincarbon, double? carbonconstraint = null)
        {
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // Initialize solver and other
            Cplex cpl = new Cplex();


            Ehub_outputs outs = new Ehub_outputs();

            int pvspots = this.b_solar_area.Length;   // number of different PV patches



            double b_co2_target = double.MaxValue;
            bool bln_co2target = false;


            if (!carbonconstraint.IsNullOrDefault())
            {
                b_co2_target = Convert.ToDouble(carbonconstraint);
                bln_co2target = true;
            }
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // Variables
            // PV
            INumVar[] x_pv = new INumVar[pvspots];
            for (int i = 0; i < pvspots; i++)
            {
                x_pv[i] = cpl.NumVar(0, this.b_solar_area[i]);
            }
            INumVar[] y_pv = new INumVar[this.horizon];            //binary control variable. if pv used, it is 1. to avoid selling and purchasing from and to the grid at the same time. from portia
            INumVar[] x_purchase = new INumVar[this.horizon];
            INumVar[] x_feedin = new INumVar[this.horizon];

            // Battery
            INumVar x_bat = cpl.NumVar(0.0, 500.0);   // 5 floors times 100 kWh per floor. 80 kWh has a Tesla car
            INumVar[] x_batch = new INumVar[this.horizon];  // charge battery [kW]
            INumVar[] x_batdis = new INumVar[this.horizon]; // discharge battery [kW]
            INumVar[] x_batstor = new INumVar[this.horizon]; // stored electricity [kW]

            for (int t = 0; t < this.horizon; t++)
            {
                y_pv[t] = cpl.BoolVar();
                x_purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_feedin[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_batch[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_batdis[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_batstor[t] = cpl.NumVar(0, System.Double.MaxValue);
            }


            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////













            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // constraints
            ILinearNumExpr a_co2 = cpl.LinearNumExpr();
            for (int t = 0; t < this.horizon; t++)
            {
                //demand must be met by pv, grid and battery, minus electricty sold to the grid and for charging the battery 
                ILinearNumExpr pvproduction = cpl.LinearNumExpr();
                ILinearNumExpr elecgeneration = cpl.LinearNumExpr();
                ILinearNumExpr elecadditionaldemand = cpl.LinearNumExpr();
                for (int i = 0; i < pvspots; i++)
                {
                    elecgeneration.AddTerm(this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t], x_pv[i]);
                    pvproduction.AddTerm(this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t], x_pv[i]);     // in kW
                }
                elecgeneration.AddTerm(1, x_purchase[t]);
                elecgeneration.AddTerm(1, x_batdis[t]);
                elecadditionaldemand.AddTerm(1, x_feedin[t]);
                elecadditionaldemand.AddTerm(1, x_batch[t]);
                cpl.AddGe(cpl.Diff(elecgeneration, elecadditionaldemand), this.b_elec_typ[t]);

                //pv production must be greater or equal feedin
                cpl.AddGe(pvproduction, x_feedin[t]);

                // donnot allow feedin and purchase at the same time. y = 1 means electricity is being purchased
                cpl.AddLe(x_purchase[t], cpl.Prod(M, y_pv[t]));
                cpl.AddLe(x_feedin[t], cpl.Prod(M, cpl.Diff(1, y_pv[t])));

                // co2 emissions by purchase from grid
                a_co2.AddTerm((this.a_carbon_typ[t] / 1000) * this.c_num_of_days[t], x_purchase[t]);
            }
            for (int i = 0; i < pvspots - 6; i++)
                a_co2.AddTerm(lca_pv_facade, x_pv[i]);
            for (int i = pvspots - 6; i < pvspots; i++)
                a_co2.AddTerm(lca_pv_flat, x_pv[i]);



            // Battery model
            // losses battery
            for (int t = 0; t < this.horizon - 1; t++)
            {
                ILinearNumExpr batstate = cpl.LinearNumExpr();
                batstate.AddTerm((1 - bat_decay), x_batstor[t]);
                batstate.AddTerm(bat_ch_eff, x_batch[t]);
                batstate.AddTerm(-1 / bat_disch_eff, x_batdis[t]);
                cpl.AddEq(x_batstor[t + 1], batstate);
            }
            cpl.AddGe(x_batstor[0], cpl.Prod(x_bat, bat_min_state));    // initial state of battery >= min_state
            cpl.AddEq(x_batstor[0], cpl.Diff(x_batstor[this.horizon - 1], x_batdis[this.horizon - 1])); //initial state also = state(end of year)
            cpl.AddEq(x_batdis[0], 0);                                  // initial discharging of battery

            for (int t = 0; t < this.horizon; t++)
            {
                cpl.AddGe(x_batstor[t], cpl.Prod(x_bat, bat_min_state));    // min state of charge
                cpl.AddLe(x_batch[t], cpl.Prod(x_bat, bat_max_ch));         // battery charging
                cpl.AddLe(x_batdis[t], cpl.Prod(x_bat, bat_max_disch));     // battery discharging
                cpl.AddLe(x_batstor[t], x_bat);                             // battery sizing
            }

            a_co2.AddTerm(lca_battery, x_bat);


            // co2 constraint
            if (bln_co2target && !bln_mincarbon) cpl.AddLe(a_co2, b_co2_target);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // problem
            ILinearNumExpr OPEX = cpl.LinearNumExpr();
            ILinearNumExpr CAPEX = cpl.LinearNumExpr();
            for (int i = 0; i < pvspots; i++)
            {
                CAPEX.AddTerm(c_pv, x_pv[i]);
            }
            CAPEX.AddTerm(c_bat, x_bat);
            for (int t = 0; t < this.horizon; t++)
            {
                OPEX.AddTerm(this.c_grid_typ[t] * this.c_num_of_days[t], x_purchase[t]);
                OPEX.AddTerm(-1.0 * this.c_feedin_typ[t] * this.c_num_of_days[t], x_feedin[t]);
            }
            // cost minimization
            if (!bln_mincarbon) cpl.AddMinimize(cpl.Sum(OPEX, CAPEX));
            // co2 minimization
            if (bln_mincarbon) cpl.AddMinimize(a_co2);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // solve and CPlex settings
            cpl.SetOut(null);
            cpl.Solve();
            //Console.WriteLine("mip gap: {0}", cpl.GetMIPRelativeGap());
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // Outputs
            //for (int i = 0; i < pvspots; i++)
            //    Console.WriteLine("pv spot {0}, area installed: {1}", i, cpl.GetValue(x_pv[i]));


            //if (bln_mincarbon)
            //{
            //    Console.WriteLine("cost: " + cpl.GetValue(cpl.Sum(OPEX, CAPEX)));
            //}
            //else
            //    Console.WriteLine("cost: " + cpl.ObjValue);

            //Console.WriteLine("OPEX: {0}... CAPEX: {1}", cpl.GetValue(OPEX), cpl.GetValue(CAPEX));

            //Console.WriteLine("total co2 emissions: {0}", cpl.GetValue(a_co2));
            //Console.WriteLine("tot pur: {0}, tot sold: {1}", cpl.GetValue(cpl.Sum(x_purchase)), cpl.GetValue(cpl.Sum(x_feedin)));
            //Console.ReadKey();

            if (bln_mincarbon)
            {
                outs.cost = cpl.GetValue(cpl.Sum(OPEX, CAPEX));
                outs.carbon = cpl.ObjValue;
            }
            else
            {
                outs.cost = cpl.ObjValue;
                outs.carbon = cpl.GetValue(a_co2);
            }

            outs.x_elecpur = new double[this.horizon];
            outs.x_feedin = new double[this.horizon];
            outs.b_pvprod = new double[this.horizon];
            outs.b_pvprod_S_a = new double[this.horizon];
            outs.b_pvprod_S_b = new double[this.horizon];
            outs.b_pvprod_W_a = new double[this.horizon];
            outs.b_pvprod_W_b = new double[this.horizon];
            outs.b_pvprod_E = new double[this.horizon];
            outs.b_pvprod_Roof = new double[this.horizon];

            outs.x_batcharge = new double[this.horizon];
            outs.x_batdischarge = new double[this.horizon];
            outs.x_batsoc = new double[this.horizon];
            outs.x_pv = new double[this.b_solar_area.Length];
            for (int t = 0; t < this.horizon; t++)
            {
                outs.x_elecpur[t] = cpl.GetValue(x_purchase[t]);
                outs.x_feedin[t] = cpl.GetValue(x_feedin[t]);
                outs.x_batcharge[t] = cpl.GetValue(x_batch[t]);
                outs.x_batdischarge[t] = cpl.GetValue(x_batdis[t]);
                outs.x_batsoc[t] = cpl.GetValue(x_batstor[t]);
                outs.b_pvprod[t] = 0;
                outs.b_pvprod_S_a[t] = 0;
                outs.b_pvprod_S_b[t] = 0;
                outs.b_pvprod_W_a[t] = 0;
                outs.b_pvprod_W_b[t] = 0;
                outs.b_pvprod_E[t] = 0;
                outs.b_pvprod_Roof[t] = 0;
                for (int i = 0; i < this.b_solar_area.Length; i++)
                {
                    double pvprodnow = this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t] * cpl.GetValue(x_pv[i]);
                    outs.b_pvprod[t] += pvprodnow;
                    if (i >= 0 && i <= 17)
                        outs.b_pvprod_S_a[t] += pvprodnow;
                    else if (i >= 18 && i <= 22)
                        outs.b_pvprod_S_b[t] += pvprodnow;
                    else if (i >= 23 && i <= 32)
                        outs.b_pvprod_W_a[t] += pvprodnow;
                    else if (i >= 33 && i <= 38)
                        outs.b_pvprod_W_b[t] += pvprodnow;
                    else if (i >= 39 && i <= 53)
                        outs.b_pvprod_E[t] += pvprodnow;
                    else if (i >= 54 && i <= 59)
                        outs.b_pvprod_Roof[t] += pvprodnow;
                }
            }
            for (int i = 0; i < this.b_solar_area.Length; i++)
                outs.x_pv[i] = cpl.GetValue(x_pv[i]);

            outs.x_bat = cpl.GetValue(x_bat);

            outs.OPEX = cpl.GetValue(OPEX);
            outs.CAPEX = cpl.GetValue(CAPEX);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////




            return outs;
        }



        private Ehub_outputs ehubmodel_multi(bool bln_mincarbon, double? carbonconstraint = null)
        {

            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // add cooling demand to electricity demand. sizing AC according to peak cooling
            double peakcool = 0.0;
            double[] b_ElecCool = new double[this.horizon];
            for (int t = 0; t < this.horizon; t++)
            {
                b_ElecCool[t] = this.b_elec_typ[t] + (this.b_cool_typ[t] / ac_cop);
                if (this.b_cool_typ[t] > peakcool) peakcool = this.b_cool_typ[t];
            }

            double x_AC = peakcool;  // AC sizing
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // Initialize solver and other
            Cplex cpl = new Cplex();


            Ehub_outputs outs = new Ehub_outputs();

            int pvspots = this.b_solar_area.Length;   // number of different PV patches



            double b_co2_target = double.MaxValue;
            bool bln_co2target = false;


            if (!carbonconstraint.IsNullOrDefault())
            {
                b_co2_target = Convert.ToDouble(carbonconstraint);
                bln_co2target = true;
            }
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // VARIABLES
            // PV
            INumVar[] x_pv = new INumVar[pvspots];                          // m2 installed per patch (pvspots)
            for (int i = 0; i < pvspots; i++)
                x_pv[i] = cpl.NumVar(0, this.b_solar_area[i]);              // max available patch area in m2
            INumVar[] y1 = new INumVar[this.horizon];                       // binary control variable. to avoid selling and purchasing from and to the grid at the same time. from portia. 1 = electricity is being purchased
            INumVar[] x_purchase = new INumVar[this.horizon];               // purchase electricity from grid
            INumVar[] x_feedin = new INumVar[this.horizon];                 // feeding in PV electricity into the grid

            // HP
            INumVar x_hp = cpl.NumVar(0.0, System.Double.MaxValue);         // capacity, in kW
            INumVar[] x_hp_op = new INumVar[this.horizon];                  // operation, in kWh

            // Boiler
            INumVar x_boi = cpl.NumVar(0.0, System.Double.MaxValue);        // capacity, in kW
            INumVar[] x_boi_op = new INumVar[this.horizon];                 // operation, in kWh

            // CHP
            INumVar x_chp = cpl.NumVar(0.0, System.Double.MaxValue);        // capacity, in kW
            INumVar[] x_chp_op_e = new INumVar[this.horizon];               // operation electricity, in kWh
            INumVar[] x_chp_op_h = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_dump = new INumVar[this.horizon];               // dumping heat, in kWh

            // Thermal storage
            INumVar x_tes = cpl.NumVar(0.0, 1400);                        // 40 cubic meter size. thats like 4m x 5m x 2m. (x m3 * 35)
            INumVar[] x_tes_soc = new INumVar[this.horizon];              // state-of-charge, in kWh
            INumVar[] x_tes_ch = new INumVar[this.horizon];               // charging storage, in kW
            INumVar[] x_tes_dis = new INumVar[this.horizon];              // discharging storage, in kW

            // Battery
            INumVar x_bat = cpl.NumVar(0.0, 500.0);                         // 5 floors times 100 kWh per floor. 80 kWh has a Tesla car
            INumVar[] x_bat_ch = new INumVar[this.horizon];                 // charge battery [kW]
            INumVar[] x_bat_dis = new INumVar[this.horizon];                // discharge battery [kW]
            INumVar[] x_bat_soc = new INumVar[this.horizon];                // state-of-charge, stored electricity [kW]

            for (int t = 0; t < this.horizon; t++)
            {
                y1[t] = cpl.BoolVar();
                x_purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_feedin[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_op[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_op[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_op_e[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_op_h[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_dump[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_tes_ch[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_tes_dis[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_tes_soc[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_bat_ch[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_bat_dis[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_bat_soc[t] = cpl.NumVar(0, System.Double.MaxValue);
            }
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////





            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // CONSTRAINTS


            // meetings demands
            for (int t = 0; t < this.horizon; t++)
            {
                // electricity demand must be met by pv, grid, battery, and chp
                //      minus electricty sold to the grid, charging the battery, and for operating the heat pump
                ILinearNumExpr pvproduction = cpl.LinearNumExpr();
                ILinearNumExpr elecgeneration = cpl.LinearNumExpr();
                ILinearNumExpr elecadditionaldemand = cpl.LinearNumExpr();
                for (int i = 0; i < pvspots; i++)
                {
                    elecgeneration.AddTerm(this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t], x_pv[i]);
                    pvproduction.AddTerm(this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t], x_pv[i]);     // in kW
                }
                elecgeneration.AddTerm(1, x_purchase[t]);
                elecgeneration.AddTerm(1, x_bat_dis[t]);
                elecgeneration.AddTerm(c_chp_htp, x_chp_op_e[t]);
                elecadditionaldemand.AddTerm(1, x_feedin[t]);
                elecadditionaldemand.AddTerm(1, x_bat_ch[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff[t], x_hp_op[t]);
                cpl.AddEq(cpl.Diff(elecgeneration, elecadditionaldemand), this.b_elec_typ[t]);


                // heat demand must be met by boiler, hp, chp, and TES
                //      minus heat for TES 
                ILinearNumExpr heatgeneration = cpl.LinearNumExpr();
                ILinearNumExpr heatadditionaldemand = cpl.LinearNumExpr();
                heatadditionaldemand.AddTerm(1, x_tes_ch[t]);
                heatgeneration.AddTerm(1, x_tes_dis[t]);
                heatgeneration.AddTerm(1, x_boi_op[t]);
                heatgeneration.AddTerm(1, x_hp_op[t]);
                heatgeneration.AddTerm(1, x_chp_op_h[t]);
                cpl.AddEq(cpl.Diff(heatgeneration, heatadditionaldemand), this.b_heat_typ[t]);


                //pv production must be greater or equal feedin
                cpl.AddGe(pvproduction, x_feedin[t]);

                // donnot allow feedin and purchase at the same time. y = 1 means electricity is being purchased
                cpl.AddLe(x_purchase[t], cpl.Prod(M, y1[t]));
                cpl.AddLe(x_feedin[t], cpl.Prod(M, cpl.Diff(1, y1[t])));
            }



            for (int t = 0; t < this.horizon; t++)
            {
                // CHP 
                // BUG!!!!!!!!!!!!!!! this will cause the chp to operate the entire time. and the min operation will grow with the capacity
                cpl.AddGe(x_chp_op_e[t], cpl.Prod(x_chp, c_chp_minload));
                //cpl.AddGe(x_chp_op_e[t], cpl.Prod(y_chp[t], b_chp_minload));  // this is how it should be. b_chp_minload is min load of minimum size of a chp
                //cpl.AddGe(x_chp, cpl.Prod(y_chp_selected, b_chp_minload));
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                
                cpl.AddLe(x_chp_op_e[t], x_chp);
                // heat recovery and heat dump from CHP is equal to electricity generation by CHP times heat to power ratio
                ILinearNumExpr chpheatrecov = cpl.LinearNumExpr();
                ILinearNumExpr chpheatfromelec = cpl.LinearNumExpr();
                chpheatrecov.AddTerm(1, x_chp_op_h[t]);
                chpheatrecov.AddTerm(1, x_chp_dump[t]);
                chpheatfromelec.AddTerm(c_chp_htp, x_chp_op_e[t]);
                cpl.AddEq(chpheatrecov, chpheatfromelec);
                // Limiting the amount of heat that chps can dump
                cpl.AddLe(x_chp_dump[t], cpl.Prod(c_chp_heatdump, x_chp_op_h[t]));

                // Boiler 
                cpl.AddLe(x_boi_op[t], x_boi);

                // HP 
                cpl.AddLe(x_hp_op[t], x_hp);
            }

            for (int t = 0; t < this.horizon - 1; t++)
            {
                ILinearNumExpr tesstate = cpl.LinearNumExpr();
                tesstate.AddTerm((1 - tes_decay), x_tes_soc[t]);
                tesstate.AddTerm(tes_ch_eff, x_tes_ch[t]);
                tesstate.AddTerm(-1 / tes_disch_eff, x_tes_dis[t]);
                cpl.AddEq(x_tes_soc[t + 1], tesstate);
            }
            cpl.AddEq(x_tes_soc[0], x_tes_soc[this.horizon - 1]);
            cpl.AddEq(x_tes_dis[0], 0);
            for (int t = 0; t < this.horizon; t++)
            {
                cpl.AddLe(x_tes_ch[t], cpl.Prod(x_tes, tes_max_ch));
                cpl.AddLe(x_tes_dis[t], cpl.Prod(x_tes, tes_max_disch));
                cpl.AddLe(x_tes_soc[t], x_tes);
            }


            // Battery model
            for (int t = 0; t < this.horizon - 1; t++)
            {
                ILinearNumExpr batstate = cpl.LinearNumExpr();          // losses when charging, discharging, and decay
                batstate.AddTerm((1 - bat_decay), x_bat_soc[t]);
                batstate.AddTerm(bat_ch_eff, x_bat_ch[t]);
                batstate.AddTerm(-1 / bat_disch_eff, x_bat_dis[t]);
                cpl.AddEq(x_bat_soc[t + 1], batstate);
            }
            cpl.AddGe(x_bat_soc[0], cpl.Prod(x_bat, bat_min_state));    // initial state of battery >= min_state
            cpl.AddEq(x_bat_soc[0], cpl.Diff(x_bat_soc[this.horizon - 1], x_bat_dis[this.horizon - 1])); //initial state also = state(end of year)
            cpl.AddEq(x_bat_dis[0], 0);                                  // initial discharging of battery

            for (int t = 0; t < this.horizon; t++)
            {
                cpl.AddGe(x_bat_soc[t], cpl.Prod(x_bat, bat_min_state));    // min state of charge
                cpl.AddLe(x_bat_ch[t], cpl.Prod(x_bat, bat_max_ch));         // battery charging
                cpl.AddLe(x_bat_dis[t], cpl.Prod(x_bat, bat_max_disch));     // battery discharging
                cpl.AddLe(x_bat_soc[t], x_bat);                             // battery sizing
            }


            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // CARBON FEED IN?
            // co2 constraint
            ILinearNumExpr a_co2 = cpl.LinearNumExpr();
            for (int t = 0; t < this.horizon; t++)
            {
                // co2 emissions by purchase from grid and heat pump
                a_co2.AddTerm((this.a_carbon_typ[t] / 1000) * this.c_num_of_days[t], x_purchase[t]);
                a_co2.AddTerm((this.a_carbon_typ[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff[t]), x_hp_op[t]);

                // co2 by natural gas
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_op[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_chp_eff), x_chp_op_e[t]);
            }

            for (int i = 0; i < pvspots - 6; i++)
                a_co2.AddTerm(lca_pv_facade, x_pv[i]);
            for (int i = pvspots - 6; i < pvspots; i++)
                a_co2.AddTerm(lca_pv_flat, x_pv[i]);

            a_co2.AddTerm(lca_boiler, x_boi);
            a_co2.AddTerm(lca_hp, x_hp);
            a_co2.AddTerm(lca_chp, x_chp);
            a_co2.AddTerm(lca_therm, x_tes);
            a_co2.AddTerm(lca_battery, x_bat);


            if (bln_co2target && !bln_mincarbon) cpl.AddLe(cpl.Sum(lca_ac * x_AC, a_co2), b_co2_target);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // PROBLEM OBJECTIVE FUNCTION
            ILinearNumExpr OPEX = cpl.LinearNumExpr();
            ILinearNumExpr CAPEX = cpl.LinearNumExpr();
            for (int i = 0; i < pvspots; i++)
            {
                CAPEX.AddTerm(c_pv, x_pv[i]);
            }
            CAPEX.AddTerm(c_hp, x_hp);
            CAPEX.AddTerm(c_boi, x_boi);
            CAPEX.AddTerm(c_chp, x_chp);
            CAPEX.AddTerm(c_tes, x_tes);
            CAPEX.AddTerm(c_bat, x_bat);

            for (int t = 0; t < this.horizon; t++)
            {
                OPEX.AddTerm(this.c_grid_typ[t] * this.c_num_of_days[t], x_purchase[t]);
                OPEX.AddTerm(-1.0 * this.c_feedin_typ[t] * this.c_num_of_days[t], x_feedin[t]);

                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_op[t]);
                OPEX.AddTerm((c_gas / c_chp_eff) * this.c_num_of_days[t], x_chp_op_e[t]);

                for (int i = 0; i < pvspots; i++)
                {
                    OPEX.AddTerm((c_pv_om * this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t] * this.c_num_of_days[t]), x_pv[i]);
                }
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_op[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_op[t]);
                OPEX.AddTerm(c_chp_om * this.c_num_of_days[t], x_chp_op_e[t]);
            }
            // cost minimization
            if (!bln_mincarbon) cpl.AddMinimize(cpl.Sum(OPEX, cpl.Sum(CAPEX, c_ac * x_AC)));
            // co2 minimization
            if (bln_mincarbon) cpl.AddMinimize(a_co2);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // SOLVE and CPLEX SETTINGS
            cpl.SetParam(Cplex.Param.ClockType, 2);     // 2 = measuring time in wall clock time. 1 = cpu time
            cpl.SetParam(Cplex.Param.TimeLimit, 60);
            //cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.05);
            cpl.SetOut(null);
            cpl.Solve();

            //Console.WriteLine("mip gap: {0}", cpl.GetMIPRelativeGap());
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // OUTPUTS
            //for (int i = 0; i < pvspots; i++)
            //    Console.WriteLine("pv spot {0}, area installed: {1}", i, cpl.GetValue(x_pv[i]));


            //if (bln_mincarbon)
            //{
            //    Console.WriteLine("cost: " + cpl.GetValue(cpl.Sum(OPEX, CAPEX)));
            //}
            //else
            //    Console.WriteLine("cost: " + cpl.ObjValue);

            //Console.WriteLine("OPEX: {0}... CAPEX: {1}", cpl.GetValue(OPEX), cpl.GetValue(CAPEX));

            //Console.WriteLine("total co2 emissions: {0}", cpl.GetValue(a_co2));
            //Console.WriteLine("tot pur: {0}, tot sold: {1}", cpl.GetValue(cpl.Sum(x_purchase)), cpl.GetValue(cpl.Sum(x_feedin)));
            //Console.ReadKey();

            if (bln_mincarbon)
            {
                outs.cost = cpl.GetValue(cpl.Sum(OPEX, CAPEX));
                outs.carbon = cpl.ObjValue;
            }
            else
            {
                outs.cost = cpl.ObjValue;
                outs.carbon = cpl.GetValue(a_co2);
            }

            outs.x_elecpur = new double[this.horizon];
            outs.x_feedin = new double[this.horizon];
            outs.b_pvprod = new double[this.horizon];
            outs.b_pvprod_S_a = new double[this.horizon];
            outs.b_pvprod_S_b = new double[this.horizon];
            outs.b_pvprod_W_a = new double[this.horizon];
            outs.b_pvprod_W_b = new double[this.horizon];
            outs.b_pvprod_E = new double[this.horizon];
            outs.b_pvprod_Roof = new double[this.horizon];

            outs.x_batcharge = new double[this.horizon];
            outs.x_batdischarge = new double[this.horizon];
            outs.x_batsoc = new double[this.horizon];
            outs.x_tescharge = new double[this.horizon];
            outs.x_tesdischarge = new double[this.horizon];
            outs.x_tessoc = new double[this.horizon];
            outs.x_hp_op = new double[this.horizon];
            outs.x_boi_op = new double[this.horizon];
            outs.x_chp_op_e = new double[this.horizon];
            outs.x_chp_op_h = new double[this.horizon];
            outs.x_chp_dump = new double[this.horizon];

            outs.x_pv = new double[this.b_solar_area.Length];
            for (int t = 0; t < this.horizon; t++)
            {
                outs.x_elecpur[t] = cpl.GetValue(x_purchase[t]);
                outs.x_feedin[t] = cpl.GetValue(x_feedin[t]);
                outs.x_batcharge[t] = cpl.GetValue(x_bat_ch[t]);
                outs.x_batdischarge[t] = cpl.GetValue(x_bat_dis[t]);
                outs.x_batsoc[t] = cpl.GetValue(x_bat_soc[t]);
                outs.x_tescharge[t] = cpl.GetValue(x_tes_ch[t]);
                outs.x_tesdischarge[t] = cpl.GetValue(x_tes_dis[t]);
                outs.x_tessoc[t] = cpl.GetValue(x_tes_soc[t]);
                outs.x_hp_op[t] = cpl.GetValue(x_hp_op[t]);
                outs.x_boi_op[t] = cpl.GetValue(x_boi_op[t]);
                outs.x_chp_op_e[t] = cpl.GetValue(x_chp_op_e[t]);
                outs.x_chp_op_h[t] = cpl.GetValue(x_chp_op_h[t]);
                outs.x_chp_dump[t] = cpl.GetValue(x_chp_dump[t]);

                outs.b_pvprod[t] = 0;
                outs.b_pvprod_S_a[t] = 0;
                outs.b_pvprod_S_b[t] = 0;
                outs.b_pvprod_W_a[t] = 0;
                outs.b_pvprod_W_b[t] = 0;
                outs.b_pvprod_E[t] = 0;
                outs.b_pvprod_Roof[t] = 0;
                for (int i = 0; i < this.b_solar_area.Length; i++)
                {
                    double pvprodnow = this.a_solar_typ[i][t] * 0.001 * this.c_solar_eff[i][t] * cpl.GetValue(x_pv[i]);
                    outs.b_pvprod[t] += pvprodnow;
                    if (i >= 0 && i <= 17)
                        outs.b_pvprod_S_a[t] += pvprodnow;
                    else if (i >= 18 && i <= 22)
                        outs.b_pvprod_S_b[t] += pvprodnow;
                    else if (i >= 23 && i <= 32)
                        outs.b_pvprod_W_a[t] += pvprodnow;
                    else if (i >= 33 && i <= 38)
                        outs.b_pvprod_W_b[t] += pvprodnow;
                    else if (i >= 39 && i <= 53)
                        outs.b_pvprod_E[t] += pvprodnow;
                    else if (i >= 54 && i <= 59)
                        outs.b_pvprod_Roof[t] += pvprodnow;
                }
            }
            for (int i = 0; i < this.b_solar_area.Length; i++)
                outs.x_pv[i] = cpl.GetValue(x_pv[i]);

            outs.x_hp = cpl.GetValue(x_hp);
            outs.x_boi = cpl.GetValue(x_boi);
            outs.x_chp = cpl.GetValue(x_chp);
            outs.x_ac = x_AC;
            outs.x_tes = cpl.GetValue(x_tes);
            outs.x_bat = cpl.GetValue(x_bat);


            outs.OPEX = cpl.GetValue(OPEX);
            outs.CAPEX = cpl.GetValue(CAPEX);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////




            return outs;
        }





        internal void writeOutputCSV(string pathout)
        {

        }




        /// <summary>
        /// Sorting profiles of typical days according to day of the year. So I can assums seasonal storage.
        /// </summary>
        /// <param name="loc_days"></param>
        /// <param name="num_of_days"></param>
        /// <param name="elec_demand"></param>
        /// <param name="heat_demand"></param>
        /// <param name="cool_demand"></param>
        /// <param name="grid_price"></param>
        /// <param name="feedin_price"></param>
        /// <param name="carbon"></param>
        /// <param name="solar"></param>
        /// <param name="solareff"></param>
        private void SortInputsSeasons(int[] loc_days,
            ref int[] num_of_days,
            ref double[] elec_demand, ref double[] heat_demand, ref double[] cool_demand,
            ref double[] grid_price, ref double[] feedin_price, ref double[] carbon,
          ref double[][] solar, ref double[][] solareff, ref double[] hpeff)
        {
            int horizon = elec_demand.Length;

            int[] indices = new int[loc_days.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;

            Array.Sort(loc_days, indices);


            double[] elec_copy = new double[horizon];
            double[] heat_copy = new double[horizon];
            double[] cool_copy = new double[horizon];
            double[] grid_copy = new double[horizon];
            double[] feedin_copy = new double[horizon];
            double[] carbon_copy = new double[horizon];
            double[][] solar_copy = new double[solar.Length][];
            double[][] solareff_copy = new double[solar.Length][];
            double[] hpeff_copy = new double[horizon];
            int[] numdays_copy = new int[horizon];


            elec_demand.CopyTo(elec_copy, 0);
            heat_demand.CopyTo(heat_copy, 0);
            cool_demand.CopyTo(cool_copy, 0);
            grid_price.CopyTo(grid_copy, 0);
            feedin_price.CopyTo(feedin_copy, 0);
            carbon.CopyTo(carbon_copy, 0);
            hpeff.CopyTo(hpeff_copy, 0);
            num_of_days.CopyTo(numdays_copy, 0);
            for (int s = 0; s < solar.Length; s++)
            {
                solar_copy[s] = new double[solar[s].Length];
                solareff_copy[s] = new double[solareff[s].Length];
                solar[s].CopyTo(solar_copy[s], 0);
                solareff[s].CopyTo(solareff_copy[s], 0);
            }

            elec_demand = new double[horizon];
            heat_demand = new double[horizon];
            cool_demand = new double[horizon];
            grid_price = new double[horizon];
            feedin_price = new double[horizon];
            carbon = new double[horizon];
            hpeff = new double[horizon];
            num_of_days = new int[horizon];
            solar = new double[solar_copy.Length][];
            solareff = new double[solareff_copy.Length][];
            for (int s = 0; s < solar.Length; s++)
            {
                solar[s] = new double[horizon];
                solareff[s] = new double[horizon];
            }

            int t = 0;
            for (int i = 0; i < indices.Length; i++)
            {
                for (int h = 0; h < 24; h++)
                {
                    elec_demand[t] = elec_copy[h + (indices[i] * 24)];
                    heat_demand[t] = heat_copy[h + (indices[i] * 24)];
                    cool_demand[t] = cool_copy[h + (indices[i] * 24)];
                    grid_price[t] = grid_copy[h + (indices[i] * 24)];
                    feedin_price[t] = feedin_copy[h + (indices[i] * 24)];
                    carbon[t] = carbon_copy[h + (indices[i] * 24)];
                    hpeff[t] = hpeff_copy[h + (indices[i] * 24)];
                    num_of_days[t] = numdays_copy[h + (indices[i] * 24)];
                    for (int s = 0; s < solar_copy.Length; s++)
                    {
                        solar[s][t] = solar_copy[s][h + (indices[i] * 24)];
                        solareff[s][t] = solareff_copy[s][h + (indices[i] * 24)];
                    }

                    t++;
                }
            }
        }


        /// <summary>
        /// Read inputs
        /// </summary>
        /// <param name="path"></param>
        /// <param name="morris"></param>
        /// <param name="elec_demand"></param>
        /// <param name="heat_demand"></param>
        /// <param name="cool_demand"></param>
        /// <param name="grid_price"></param>
        /// <param name="feedin_price"></param>
        /// <param name="carbon"></param>
        /// <param name="solar"></param>
        /// <param name="pv_area"></param>
        /// <param name="PV_efficiency"></param>
        /// <param name="num_of_days"></param>
        /// <param name="loc_days"></param>
        private void ReadInput(string path, int morris,
            out double[] elec_demand, out double[] heat_demand, out double[] cool_demand,
            out double[] grid_price, out double[] feedin_price, out double[] carbon,
          out double[][] solar, out double[] pv_area, out double[][] PV_efficiency,
            out double[] HP_COP,
            out int[] num_of_days, out int[] loc_days)
        {
            // 7 input profiles:
            // 0: elec 
            // 1: heat
            // 2: cool
            // 3: grid
            // 4: feedin
            // 5: carbon
            // 6: solar. contains all facade patches




            // 0: elec_demand
            using (var reader = new StreamReader(path + @"stochastic\elec_typ.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    listA.Add(Convert.ToDouble(parts[morris]));
                }
                elec_demand = listA.ToArray();
            }
            int horizon = elec_demand.Length;

            // 1: heat_demand
            using (var reader = new StreamReader(path + @"stochastic\heat_typ.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    listA.Add(Convert.ToDouble(parts[morris]));
                }
                heat_demand = listA.ToArray();
            }

            // 2: cool_demand
            using (var reader = new StreamReader(path + @"stochastic\cool_typ.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    listA.Add(Convert.ToDouble(parts[morris]));
                }
                cool_demand = listA.ToArray();
            }

            // 3: grid_price
            using (var reader = new StreamReader(path + @"stochastic\grid_typ.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    listA.Add(Convert.ToDouble(parts[morris]));
                }
                grid_price = listA.ToArray();
            }

            // 4: feedin_price
            using (var reader = new StreamReader(path + @"stochastic\feedin_typ.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    listA.Add(Convert.ToDouble(parts[morris]));
                }
                feedin_price = listA.ToArray();
            }

            // 5: carbon
            using (var reader = new StreamReader(path + @"stochastic\carbon_typ.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    listA.Add(Convert.ToDouble(parts[morris]));
                }
                carbon = listA.ToArray();
            }


            //pv containts 60 sensor point profiles at south, west, east and horizontal.
            // 6: pv potentials
            solar = new double[60][];
            // S_A, S_B, W_A, W_B, E, H_A, H_B
            int[] solar_ind_len = new int[6] { 18, 5, 10, 6, 15, 6 };
            using (var reader = new StreamReader(path + @"stochastic\solar_typ_S_A.csv"))       //lines -> timesteps. then 80x stochastic profile for first sensor point, then next 80x, etc.
            {
                for (int i = 0; i < 18; i++)
                {
                    solar[i] = new double[horizon];
                }

                int t = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    for (int i = 0; i < solar_ind_len[0]; i++)
                    {
                        solar[i][t] = Convert.ToDouble(parts[i * this.morris_length + morris]);
                    }
                    t++;
                }
            }
            using (var reader = new StreamReader(path + @"stochastic\solar_typ_S_B.csv"))
            {
                for (int i = 0 + 18; i < 18 + 5; i++)
                {
                    solar[i] = new double[horizon];
                }

                int t = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    for (int i = 0; i < solar_ind_len[1]; i++)
                    {
                        solar[i + 18][t] = Convert.ToDouble(parts[i * this.morris_length + morris]);
                    }
                    t++;
                }
            }
            using (var reader = new StreamReader(path + @"stochastic\solar_typ_W_A.csv"))
            {
                for (int i = 0 + 18 + 5; i < 18 + 5 + 10; i++)
                {
                    solar[i] = new double[horizon];
                }

                int t = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    for (int i = 0; i < solar_ind_len[2]; i++)
                    {
                        solar[i + 18 + 5][t] = Convert.ToDouble(parts[i * this.morris_length + morris]);
                    }
                    t++;
                }
            }
            using (var reader = new StreamReader(path + @"stochastic\solar_typ_W_B.csv"))
            {
                for (int i = 0 + 18 + 5 + 10; i < 18 + 5 + 10 + 6; i++)
                {
                    solar[i] = new double[horizon];
                }

                int t = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    for (int i = 0; i < solar_ind_len[3]; i++)
                    {
                        solar[i + 18 + 5 + 10][t] = Convert.ToDouble(parts[i * this.morris_length + morris]);
                    }
                    t++;
                }
            }
            using (var reader = new StreamReader(path + @"stochastic\solar_typ_E.csv"))
            {
                for (int i = 0 + 18 + 5 + 10 + 6; i < 18 + 5 + 10 + 6 + 15; i++)
                {
                    solar[i] = new double[horizon];
                }

                int t = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    for (int i = 0; i < solar_ind_len[4]; i++)
                    {
                        solar[i + 18 + 5 + 10 + 6][t] = Convert.ToDouble(parts[i * this.morris_length + morris]);
                    }
                    t++;
                }
            }
            using (var reader = new StreamReader(path + @"stochastic\solar_typ_H.csv"))
            {
                for (int i = 0 + 18 + 5 + 10 + 6 + 15; i < 18 + 5 + 10 + 6 + 15 + 6; i++)
                {
                    solar[i] = new double[horizon];
                }

                int t = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    for (int i = 0; i < solar_ind_len[5]; i++)
                    {
                        solar[i + 18 + 5 + 10 + 6 + 15][t] = Convert.ToDouble(parts[i * this.morris_length + morris]);
                    }
                    t++;
                }
            }





            //pv area;
            pv_area = new double[0];
            using (StreamReader file = new StreamReader(path + @"deterministic\solar_AREAS.txt"))
            {
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    char[] delimiters = new char[] { ';' };
                    string[] parts = line.Split(delimiters);
                    pv_area = new double[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        pv_area[i] = Convert.ToDouble(parts[i]);
                    }
                }
                file.Close();
            }



            //num of days
            using (var reader = new StreamReader(path + @"stochastic\num_of_days.csv"))
            {
                List<int> listA = new List<int>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    listA.Add(Convert.ToInt16(parts[morris]));
                }
                num_of_days = listA.ToArray();
            }

            //locs of days
            using (var reader = new StreamReader(path + @"stochastic\locs.csv"))
            {
                List<int> listA = new List<int>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ',' };
                    string[] parts = line.Split(delimiters);
                    listA.Add(Convert.ToInt16(parts[morris]));
                }
                loc_days = listA.ToArray();
            }



            //load ambient temperature
            double[] temperature;
            using (var reader = new StreamReader(path + @"deterministic\ambient_temperature.txt"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                temperature = listA.ToArray();
            }


            // PV_efficiency
            PV_efficiency = new double[solar.Length][];
            for (int i = 0; i < PV_efficiency.Length; i++)
            {
                PV_efficiency[i] = calc_PV_efficiency(temperature, solar[i], loc_days);
            }


            // HP efficiency
            HP_COP = calc_HP_COP(temperature, loc_days);



        }

        /// <summary>
        /// Calculate time resolved PV efficiency
        /// </summary>
        /// <remarks>Source: Garcia-Domingo et al. (2014), found in Mavromatidis et al (2015).</remarks>
        /// <param name="temperature">Ambient temperature. 8760 Time series.</param>
        /// <param name="irradiation">Solar irradiation on PV cell. Time series of length of the horizon.</param>
        /// <param name="loc_days">day of the year for each typical day. required to match the temperatures to the solar potentials.</param>
        /// <returns>Time resolved PV efficiency.</returns>
        private double[] calc_PV_efficiency(double[] temperature, double[] irradiation, int[] loc_days)
        {
            int horizon = irradiation.Length;

            double[] nPV = new double[horizon];

            int t = 0;
            for (int l = 0; l < loc_days.Length; l++)
            {
                for (int h = 0; h < 24; h++)
                {
                    double temp = temperature[h + (24 * (loc_days[l] - 1))];
                    double Tcell = temp + ((pv_NCOT - pv_T_aNCOT) / pv_P_NCOT) * irradiation[t];
                    nPV[t] = pv_n_ref * (1 - pv_beta_ref * (Tcell - 25));
                    t++;
                }
            }
            return nPV;
        }


        /// <summary>
        /// Calculate COP of heat pump. depends on constants and ambient temperature.
        /// </summary>
        /// <param name="temperature"></param>
        /// <param name="loc_days"></param>
        /// <returns></returns>
        private double[] calc_HP_COP(double[] temperature, int[] loc_days)
        {
            int horizon = loc_days.Length * 24;

            double[] nHP = new double[horizon];

            int t = 0;
            for (int l = 0; l < loc_days.Length; l++)
            {
                for (int h = 0; h < 24; h++)
                {
                    double temp = temperature[h + (24 * (loc_days[l] - 1))];
                    nHP[t] = hp_pi1 * Math.Exp(hp_pi2 * (hp_dist - temp)) + hp_pi3 * Math.Exp(hp_pi4 * (hp_dist - temp));
                    t++;
                }
            }
            return nHP;
        }


    }


    public static class Misc
    {
        public static bool IsNullOrDefault<T>(this Nullable<T> value) where T : struct
        {
            return default(T).Equals(value.GetValueOrDefault());
        }
    }
}
