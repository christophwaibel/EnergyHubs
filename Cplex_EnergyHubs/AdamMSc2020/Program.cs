using System;

namespace AdamMSc2020
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Cplex test");

            Ehub ehub = new Ehub();
            ehub.Solve();

            Console.WriteLine("Solution is:");

            foreach (double xop in ehub.XOptimal)
                Console.WriteLine("x: {0}", xop);

            Console.WriteLine("Press button to exit");
            Console.ReadKey();
        }
    }
}
