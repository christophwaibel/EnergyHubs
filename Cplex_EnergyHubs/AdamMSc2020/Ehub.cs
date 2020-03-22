using System;
using System.Collections.Generic;
using System.Text;

using ILOG.CPLEX;
using ILOG.Concert;

namespace AdamMSc2020
{
    internal class Ehub
    {
        /// <summary>
        /// Optimal variables
        /// </summary>
        public List<double> XOptimal { get; private set; }

        
        internal Ehub()
        {
        
        }


        internal void Solve()
        {
            Cplex cpl = new Cplex();


            INumVar[] x = new INumVar[5];
            for (int i=0; i<x.Length; i++)
                x[i] = cpl.NumVar(0, double.MaxValue);
            
            ILinearNumExpr c1 = cpl.LinearNumExpr();
            c1.AddTerm(1, x[0]);
            c1.AddTerm(1, x[1]);
            c1.AddTerm(1, x[2]);
            cpl.AddEq(c1, 100.0);

            ILinearNumExpr c2 = cpl.LinearNumExpr();
            c2.AddTerm(6, x[0]);
            c2.AddTerm(9, x[1]);
            c2.AddTerm(1, x[3]);
            cpl.AddEq(c2, 720);

            ILinearNumExpr c3 = cpl.LinearNumExpr();
            c3.AddTerm(1, x[1]);
            c3.AddTerm(1, x[4]);
            cpl.AddEq(c3, 60);

            ILinearNumExpr f1 = cpl.LinearNumExpr();
            f1.AddTerm(10, x[0]);
            ILinearNumExpr f2 = cpl.LinearNumExpr();
            f2.AddTerm(20, x[1]);

            cpl.AddMaximize(cpl.Sum(f1, f2));

            cpl.Solve();

            this.XOptimal = new List<double>();
            foreach (INumVar xin in x)
                this.XOptimal.Add(cpl.GetValue(xin));

            
        }
    }
}
