using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace FS_Web_Stuff.Server.WebSocketHandlers
{
    public class WebSocketHandlerTwitch
    {
        private static ConcurrentDictionary<string, WebSocket> reactClients = new ConcurrentDictionary<string, WebSocket>();
        private WebSocketHandlerGame unityWebSocketHandler;

        public WebSocketHandlerTwitch(WebSocketHandlerGame unityWebSocketHandler)
        {
            this.unityWebSocketHandler = unityWebSocketHandler;
        }

        public async Task HandleWebSocketAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await ReceiveReactMessagesAsync(webSocket);
            }
            else
            {
                context.Response.StatusCode = 400; // Bad Request
            }
        }

        private async Task ReceiveReactMessagesAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(message);

                if (dict.ContainsKey("target"))
                {
                    var targetId = dict["target"].ToString();
                    // Forward the message to the Unity client with the corresponding Twitch username
                    await unityWebSocketHandler.SendMessageToUnityClient(targetId, message);
                }

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
