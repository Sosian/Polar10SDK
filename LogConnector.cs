using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PolarH10
{
    public class RecordConnector : IConnector
    {
        private readonly Stopwatch stopwatch;
        private readonly string fullPath;

        public RecordConnector (string parentPath)
        {
            this.stopwatch = new Stopwatch();
            this.fullPath = Path.Combine(parentPath, DateTime.Now.ToString();
        }

        public void ReceiveData (HrPayload hrPayload)
        {
            if (!stopwatch.IsRunning)
                stopwatch.Start();


            var logLine = $"{stopwatch.ElapsedMilliseconds};{hrPayload.ToString()}";
            File.AppendAllLines(fullPath, new string[]{logLine});
        }
    }
}