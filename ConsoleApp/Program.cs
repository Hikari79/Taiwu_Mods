using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string x = "xx";
            RobTomb.RobTomb_ChangeText_Patch.Postfix(ref x);
        }
    }
}
