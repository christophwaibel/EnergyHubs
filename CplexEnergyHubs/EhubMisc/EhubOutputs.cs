namespace EhubMisc
{
    /// <summary>
    /// Outputs. new struct per epsilon cut
    /// </summary>
    public struct EhubOutputs
    {
        internal bool infeasible;           // true, if no solution exists

        internal double carbon;             // annual carbon.
        internal double cost;               // cost. levelized.
        internal double OPEX;               // annual operation cost.
        internal double CAPEX;              // capital cost. levelized.
        internal double cost_dh;            // district heating network cost (heat exchangers and network)

        // Technology sizing
        internal double[] x_pv;             // pv sizing [m2]
        internal double x_bat;              // battery 
        internal double x_hp;               // heat pump. assume it reaches peak heat temperatures as simplification.
        internal double x_tes;              // thermal storage
        internal double x_clgtes;       // cool storage
        internal double x_chp;              // combined heat and power
        internal double x_ac;               // air condition
        internal double x_boi;              // gas boiler
        internal double x_bmboi;            // biomass boiler
        internal double[] x_hx_dh;          // district heating heat exchanger. per building that is connected to grid [kW]
        internal double[] x_hx_clg_dh;      // district cooling heat exchanger. per building [kW]
        internal double x_dh;               // district heating network length [m]
        internal double x_clgtower;         // cooling tower [kWh]

        // Operation. Time resolved.
        internal double[] x_elecpur;        // purchase from grid
        internal double[] x_feedin;         // feedin
        internal double[] x_bat_discharge;   // battery discharge
        internal double[] x_bat_charge;      // battery charge
        internal double[] x_bat_soc;         // battery state of charge
        internal double[] x_tes_discharge;   // thermal energy storage (tes) discharge
        internal double[] x_tes_charge;      // tes charge
        internal double[] x_tes_soc;         // tes state of charge
        internal double[] x_clgtes_discharge;   // cool storage discharge
        internal double[] x_clgtes_charge;  // cool storage charge
        internal double[] x_clgtes_soc;     // cool storage state of charge
        internal double[] x_hp_op;          // heat pump operation
        internal double[] x_boi_op;         // boiler operation
        internal double[] x_bmboi_op;       // biomass boiler operation
        internal double[] x_chp_op_e;       // chp operation electricity
        internal double[] x_chp_op_h;       // chp operation heat
        internal double[] x_chp_dump;       // chp heat dumped
        internal double[] x_ac_op;          // air con operation

        internal double[] b_pvprod;     // total pv production
        internal double[] b_pvprod_Roof;// pv production roof
        internal double[] b_pvprod_E;   // pv prod East
        internal double[] b_pvprod_S_a; // pv prod South A
        internal double[] b_pvprod_S_b; // pv prod South B
        internal double[] b_pvprod_W_a; // pv prod West A
        internal double[] b_pvprod_W_b; // pv prod West B

        // typical days related 
        internal int[] clustersize;         // cluster size per timestep. used as scalar

        internal double biomassConsumed;        // consumed Biomass in kWh
    }
}