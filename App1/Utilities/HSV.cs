using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashBoard
{
    public class HSV
    {
        public double H { get; set; }
        public double S { get; set; }
        public double V { get; set; }

        public HSV(double h, double s, double v) { H = h; S = s; V = v; }
    }
}
