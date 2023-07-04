using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace HumanMusicController.Connectors
{
    public class VisualizationServerConnector : IConnector
    {
        private readonly HubConnection connection;

        public VisualizationServerConnector(string url)
        {
            connection = new HubConnectionBuilder()
                .WithUrl(new Uri(url))
                .WithAutomaticReconnect()
                .Build();

            connection.StartAsync().GetAwaiter().GetResult();
        }

        public async void ReceiveData(HrPayload hrPayload)
        {
            await connection.SendAsync("SendHeartbeat", hrPayload.Heartrate);
        }
    }
}