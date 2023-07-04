using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace VisualizationServer.Server.Hubs
{
    public class HeartbeatHub : Hub
    {
        public async Task SendHeartbeat(int heartbeat)
        {
            await Clients.All.SendAsync("ReceiveHeartbeat", heartbeat);
        }
    }
}