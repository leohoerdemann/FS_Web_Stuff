namespace FS_Web_Stuff.Server.WebSocketHandlers
{
    using System.Collections.Concurrent;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;


    public class WebSocketHandlerGame
    {

        private static ConcurrentDictionary<string, WebSocket> unityClients = new ConcurrentDictionary<string, WebSocket>();

        public async Task HandleWebSocketAsync(HttpContext context, string clientId)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                unityClients.TryAdd(clientId, webSocket);
                await ReceiveUnityMessagesAsync(webSocket, clientId);
            }
            else
            {
                context.Response.StatusCode = 400; // Bad Request
            }
        }

        private async Task ReceiveUnityMessagesAsync(WebSocket webSocket, string clientId)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                // Handle messages from Unity clients

                // Optionally, forward messages to React clients
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            unityClients.TryRemove(clientId, out _);
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        public async Task SendMessageToUnityClient(string clientId, string message)
        {
            if (unityClients.TryGetValue(clientId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                var responseBytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
