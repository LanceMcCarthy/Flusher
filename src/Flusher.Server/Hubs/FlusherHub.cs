using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Flusher.Common.Models;

namespace Flusher.Server.Hubs
{
    public class FlusherHub : Hub
    {
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync(ActionNames.ReceiveMessageName, message);
        }

        public async Task SendFlushRequest(string requester)
        {
            await Clients.All.SendAsync(ActionNames.ReceiveFlushRequestName, requester);
        }

        public async Task SendPhotoRequest(string requester)
        {
            await Clients.All.SendAsync(ActionNames.ReceivePhotoRequestName, requester);
        }

        public async Task SendPhotoResult(string message, string imageUrl)
        {
            await Clients.All.SendAsync(ActionNames.ReceivePhotoResultName, message, imageUrl);
        }

        public async Task SendAnalyzeRequest(string requester)
        {
            await Clients.All.SendAsync(ActionNames.ReceiveAnalyzeRequestName, requester);
        }

        public async Task SendAnalyzeResult(string message, string imageUrl)
        {
            await Clients.All.SendAsync(ActionNames.ReceiveAnalyzeResultName, message, imageUrl);
        }
    }
}
