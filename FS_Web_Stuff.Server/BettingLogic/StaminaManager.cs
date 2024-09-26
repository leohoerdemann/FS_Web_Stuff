using FS_Web_Stuff.Server.RoutingLogic;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace FS_Web_Stuff.Server.BettingLogic
{
    public static class StaminaManager
    {
        private static ConcurrentDictionary<string, int> sharedStamina = new ConcurrentDictionary<string, int>();

        public static int GetSharedStamina(string streamerId)
        {
            sharedStamina.TryGetValue(streamerId, out var stamina);
            return stamina;
        }

        public static async Task IncreaseSharedStamina(string streamerId, int amount)
        {
            sharedStamina.AddOrUpdate(streamerId, amount, (key, oldValue) => oldValue + amount);

            // Broadcast new shared stamina to all viewers
            await BroadcastSharedStamina(streamerId);
        }

        public static async Task DecreaseSharedStamina(string streamerId, int amount)
        {
            sharedStamina.AddOrUpdate(streamerId, 0, (key, oldValue) => oldValue - amount);

            // Ensure stamina doesn't go below 0
            if (sharedStamina[streamerId] < 0)
                sharedStamina[streamerId] = 0;

            // Broadcast new shared stamina to all viewers
            await BroadcastSharedStamina(streamerId);
        }

        public static async Task SendSharedStamina(string streamerId, WebSocket socket)
        {
            var stamina = GetSharedStamina(streamerId);
            var message = new
            {
                command = "UPDATE_SHARED_STAMINA",
                stamina = stamina
            };
            var messageString = JsonConvert.SerializeObject(message);
            var messageBuffer = Encoding.UTF8.GetBytes(messageString);

            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            }
        }

        private static async Task BroadcastSharedStamina(string streamerId)
        {
            var viewers = Routing.GetViewerSockets(streamerId);
            if (viewers != null)
            {
                var stamina = GetSharedStamina(streamerId);
                var message = new
                {
                    command = "UPDATE_SHARED_STAMINA",
                    stamina = stamina
                };
                var messageString = JsonConvert.SerializeObject(message);
                var messageBuffer = Encoding.UTF8.GetBytes(messageString);

                foreach (var viewerSocket in viewers.Values)
                {
                    if (viewerSocket.State == WebSocketState.Open)
                    {
                        await viewerSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                    }
                }
            }
        }
    }
}
