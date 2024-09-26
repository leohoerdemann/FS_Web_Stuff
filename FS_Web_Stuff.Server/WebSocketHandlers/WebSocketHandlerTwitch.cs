using FS_Web_Stuff.Server.BettingLogic;
using FS_Web_Stuff.Server.RoutingLogic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FS_Web_Stuff.Server.WebSocketHandlers
{
    public static class WebSocketHandlerTwitch
    {
        public static async Task HandleTwitchWebSocket(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var query = context.Request.Query;

                if (!query.ContainsKey("streamer") || !query.ContainsKey("viewer"))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing streamer or viewer ID");
                    return;
                }

                var streamerId = query["streamer"].ToString();
                var viewerId = query["viewer"].ToString();
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await Handle(context, webSocket, streamerId, viewerId);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private static async Task Handle(HttpContext context, WebSocket webSocket, string streamerId, string viewerId)
        {
            // Add the viewer socket to the routing
            Routing.AddViewerSocket(streamerId, viewerId, webSocket);

            var buffer = new byte[1024 * 4];

            try
            {
                // Send initial shared stamina to the viewer
                await StaminaManager.SendSharedStamina(streamerId, webSocket);

                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    // Handle incoming messages from the viewer
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    try
                    {
                        var json = JObject.Parse(message);
                        var command = json["command"].ToString();
                        var data = json["data"];

                        if (command == "USE_PERSONAL_STAMINA")
                        {
                            int cost = data["cost"].ToObject<int>();
                            await StaminaManager.IncreaseSharedStamina(streamerId, cost);
                        }
                        else if (command == "USE_SHARED_STAMINA")
                        {
                            int cost = data["cost"].ToObject<int>();
                            await StaminaManager.DecreaseSharedStamina(streamerId, cost);
                        }

                        // Forward the message to the game
                        var gameSocket = Routing.GetGameSocket(streamerId);
                        if (gameSocket != null && gameSocket.State == WebSocketState.Open)
                        {
                            await gameSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling message from viewer: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Viewer socket error: {ex.Message}");
            }
            finally
            {
                // Remove the viewer socket when done
                Routing.RemoveViewerSocket(streamerId, viewerId);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
}
