using System.Diagnostics;
using HumanMusicController.Connectors;
using Microsoft.Extensions.Logging;

namespace HumanMusicController
{
    public class ReplayRecordfile
    {
        private readonly IConnector connector;
        private readonly ILogger<ReplayRecordfile> logger;

        public ReplayRecordfile(ILogger<ReplayRecordfile> logger, IConnector connector)
        {
            this.connector = connector;
            this.logger = logger;
        }

        public void Play(string recordFileFullPath)
        {
            var listOfReceivedPackages = File.ReadAllLines(recordFileFullPath).ToList<string>();
            var listOfParsedReceivedPackages = new List<(long elapsedMilliseconds, int heartrate)>();

            foreach (var package in listOfReceivedPackages)
            {
                var split = package.Split(";");
                listOfParsedReceivedPackages.Add((long.Parse(split[0]), int.Parse(split[1])));
            }

            var currentPackage = listOfParsedReceivedPackages.First();
            var stopwatch = Stopwatch.StartNew();
            var waitIntervalMiliseconds = 50;
            var index = 0;

            while (index < listOfParsedReceivedPackages.Count)
            {
                Thread.Sleep(waitIntervalMiliseconds);


                if (currentPackage.elapsedMilliseconds <= stopwatch.ElapsedMilliseconds)
                {
                    logger.LogDebug("Elapsed Milliseconds: " + stopwatch.ElapsedMilliseconds);
                    logger.LogDebug("Send Package: " + currentPackage.heartrate);
                    connector.ReceiveData(new HrPayload(currentPackage.heartrate));
                    index++;
                    currentPackage = listOfParsedReceivedPackages[index];
                }
            }
        }
    }
}