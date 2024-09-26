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
    public static class WebSocketHandlerGame
    {

        public static async Task HandleGameWebSocket(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var query = context.Request.Query;

                if (!query.ContainsKey("streamer"))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing streamer ID");
                    return;
                }

                var streamerId = query["streamer"].ToString();
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await Handle(context, webSocket, streamerId);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private static async Task Handle(HttpContext context, WebSocket webSocket, string streamerId)
        {
            // Add the game socket to the routing
            Routing.AddGameSocket(streamerId, webSocket);

            // Set the game as started
            Routing.SetGameStarted(streamerId, true);

            var buffer = new byte[1024 * 4];

            try
            {
                // Notify viewers that the game has started
                await HandleGameStarted(streamerId);

                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    // Handle incoming messages from the game
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    try
                    {
                        var json = JObject.Parse(message);
                        var command = json["command"]?.ToString();

                        switch (command)
                        {
                            case "SET_SITE_VALUES":
                                await HandleSetSiteValues(streamerId, message);
                                break;

                            // Add more cases as needed for other commands
                            default:
                                Console.WriteLine($"Unknown command received from game: {command}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing message from game: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Game socket error: {ex.Message}");
            }
            finally
            {
                // Remove the game socket when done
                Routing.RemoveGameSocket(streamerId);

                // Set the game as stopped
                Routing.SetGameStarted(streamerId, false);

                // Notify viewers that the game has stopped
                await HandleGameStopped(streamerId);

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }

        private static async Task HandleGameStarted(string streamerId)
        {
            var message = new
            {
                command = "GAME_STARTED"
            };
            var messageString = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            await BroadcastToViewers(streamerId, messageString);
        }

        private static async Task HandleGameStopped(string streamerId)
        {
            var message = new
            {
                command = "GAME_STOPPED"
            };
            var messageString = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            await BroadcastToViewers(streamerId, messageString);
        }

        private static async Task HandleSetSiteValues(string streamerId, string message)
        {
            // Broadcast the "SET_SITE_VALUES" message to all connected viewers
            await BroadcastToViewers(streamerId, message);
        }

        private static async Task BroadcastToViewers(string streamerId, string message)
        {
            var viewers = Routing.GetViewerSockets(streamerId);
            if (viewers != null)
            {
                foreach (var viewerSocket in viewers.Values)
                {
                    if (viewerSocket.State == WebSocketState.Open)
                    {
                        await viewerSocket.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                }
            }
        }
    }
}