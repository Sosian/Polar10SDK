using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PolarH10
{
    public class HrPayload
    {
        public int Heartrate { get; private set; }

        public HrPayload(int heartrate)
        {
            this.Heartrate = heartrate;
        }
    }
}