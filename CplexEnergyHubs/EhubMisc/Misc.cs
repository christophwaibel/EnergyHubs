using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace EhubMisc
{
    public static class Misc
    {
        public static bool IsNullOrDefault<T>(this Nullable<T> value) where T : struct
        {
            return default(T).Equals(value.GetValueOrDefault());
        }


        public static double Distance2Pts(double[] x1, double[] x2)
        {
            int n = x1.Length;
            if (x2.Length < n) n = x2.Length;

            double distance = 0.0;
            for (int i = 0; i < n; i++)
                distance += Math.Pow(x1[i] - x2[i], 2);
            distance = Math.Sqrt(distance);

            return distance;
        }
    }


    /// <summary>
    /// Outputs. new struct per epsilon cut
    /// </summary>
    public struct EhubOutputs
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


    
}
