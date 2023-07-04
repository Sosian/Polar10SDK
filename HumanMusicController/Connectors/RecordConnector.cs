using System.Diagnostics;

namespace HumanMusicController.Connectors
{
    public class RecordConnector : IConnector
    {
        private readonly Stopwatch stopwatch;
        private readonly string fullPath;

        public RecordConnector(string parentPath)
        {
            this.stopwatch = new Stopwatch();
            this.fullPath = Path.Combine(parentPath, DateTime.Now.ToString("yyyyMMddTHHmm"));
        }

        public void ReceiveData(HrPayload hrPayload)
        {
            if (!stopwatch.IsRunning)
                stopwatch.Start();


            var logLine = $"{stopwatch.ElapsedMilliseconds};{hrPayload.ToString()}";
            File.AppendAllLines(fullPath, new string[] { logLine });
        }
    }
}