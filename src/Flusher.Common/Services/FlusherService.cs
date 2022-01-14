using System;
using System.Threading.Tasks;
using Flusher.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Flusher.Common.Services
{
    public class FlusherService : IDisposable
    {
        private HubConnection connection;
        public event EventHandler<string> ConnectionChanged;

        public delegate void OnMessageReceived(string message);
        public event OnMessageReceived MessageReceived;

        public delegate void OnFlushRequested(string requester);
        public event OnFlushRequested FlushRequested;

        public delegate void OnAnalyzeRequested(string requester);
        public event OnAnalyzeRequested AnalyzeRequested;

        public delegate void OnAnalyzeResultReceived(string message, string imageUrl);
        public event OnAnalyzeResultReceived AnalyzeResultReceived;

        public delegate void OnPhotoRequested(string requester);
        public event OnPhotoRequested PhotoRequested;

        public delegate void OnPhotoReceived(string message, string imageUrl);
        public event OnPhotoReceived PhotoReceived;

        public FlusherService(string serverUrl)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                throw new Exception("MISSING SERVER URL - This class needs a path to an ASP.NET application running the flusher service");
            }

            connection = new HubConnectionBuilder().WithUrl(serverUrl).Build();
            connection.Closed += Connection_Closed;
            connection.Reconnecting += Connection_Reconnecting;
            connection.Reconnected += Connection_Reconnected;

            // Incoming signalR events. These invoke events for subscribers' event handlers
            // Some are only implemented in the IoT client (i.e. the request actions)

            connection.On<string>(ActionNames.ReceiveMessageName, ReceivedMessage);

            connection.On<string>(ActionNames.ReceiveFlushRequestName, ReceivedFlushRequest);

            connection.On<string>(ActionNames.ReceivePhotoRequestName, ReceivedPhotoRequest);
            connection.On<string, string>(ActionNames.ReceivePhotoResultName, ReceivedPhotoResult);

            connection.On<string>(ActionNames.ReceiveAnalyzeRequestName, ReceivedAnalyzeRequest);
            connection.On<string, string>(ActionNames.ReceiveAnalyzeResultName, ReceivedAnalyzeResult);
        }
        private Task Connection_Closed(Exception arg)
        {
            return Task.Run(async () =>
            {
                ConnectionChanged?.Invoke(this, connection.State.ToString());

                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            });
        }

        private Task Connection_Reconnecting(Exception arg)
        {
            return Task.Run(() => { ConnectionChanged?.Invoke(this, connection.State.ToString()); });
        }

        private Task Connection_Reconnected(string arg)
        {
            return Task.Run(() => { ConnectionChanged?.Invoke(this, connection.State.ToString()); });
        }

        public Task StartAsync()
        {
            return connection.StartAsync();
        }

        // *************** Incoming Messages *************** //

        private void ReceivedMessage(string message)
        {
            MessageReceived?.Invoke(message);
        }

        private void ReceivedFlushRequest(string requester)
        {
            FlushRequested?.Invoke(requester);
        }

        private void ReceivedPhotoRequest(string requester)
        {
            PhotoRequested?.Invoke(requester);
        }
        private void ReceivedPhotoResult(string message, string imageUrl)
        {
            PhotoReceived?.Invoke(message, imageUrl);
        }

        private void ReceivedAnalyzeRequest(string requester)
        {
            AnalyzeRequested?.Invoke(requester);
        }

        private void ReceivedAnalyzeResult(string message, string imageUrl)
        {
            AnalyzeResultReceived?.Invoke(message, imageUrl);
        }

        // *************** Outgoing Messages *************** //

        public async Task SendMessageAsync(string message)
        {
            await connection.InvokeAsync(ActionNames.SendMessageName, message);
        }

        public async Task SendFlushRequestAsync(string requester)
        {
            await connection.InvokeAsync(ActionNames.SendFlushRequestName, requester);
        }

        public async Task SendPhotoRequestAsync(string requester)
        {
            await connection.InvokeAsync(ActionNames.SendPhotoRequestName, requester);
        }

        public async Task SendPhotoResultAsync(string message, string imageUrl)
        {
            await connection.InvokeAsync(ActionNames.SendPhotoResultName, message, imageUrl);
        }

        public async Task SendAnalyzeRequestAsync(string requester)
        {
            await connection.InvokeAsync(ActionNames.SendAnalyzeRequestName, requester);
        }

        public async Task SendAnalyzeResultAsync(string message, string imageUrl)
        {
            await connection.InvokeAsync(ActionNames.SendAnalyzeResultName, message, imageUrl);
        }

        public void Dispose()
        {
            connection.DisposeAsync();
            connection = null;
        }
    }
}
