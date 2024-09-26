using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace FS_Web_Stuff.Server.RoutingLogic
{
    public static class Routing
    {
        // Map streamer IDs to their game WebSocket connections
        private static ConcurrentDictionary<string, WebSocket> gameSockets = new ConcurrentDictionary<string, WebSocket>();

        // Map streamer IDs to a dictionary of viewer IDs to their WebSocket connections
        private static ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> twitchSockets = new ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>>();

        // Map streamer IDs to their game state (started or not)
        private static ConcurrentDictionary<string, bool> gameStates = new ConcurrentDictionary<string, bool>();

        // Add game socket for a streamer
        public static void AddGameSocket(string streamerId, WebSocket socket)
        {
            gameSockets[streamerId] = socket;
        }

        // Remove game socket for a streamer
        public static void RemoveGameSocket(string streamerId)
        {
            gameSockets.TryRemove(streamerId, out _);
        }

        // Get game socket for a streamer
        public static WebSocket GetGameSocket(string streamerId)
        {
            gameSockets.TryGetValue(streamerId, out var socket);
            return socket;
        }

        // Add a viewer socket for a streamer
        public static void AddViewerSocket(string streamerId, string viewerId, WebSocket socket)
        {
            var viewers = twitchSockets.GetOrAdd(streamerId, new ConcurrentDictionary<string, WebSocket>());
            viewers[viewerId] = socket;
        }

        // Remove a viewer socket for a streamer
        public static void RemoveViewerSocket(string streamerId, string viewerId)
        {
            if (twitchSockets.TryGetValue(streamerId, out var viewers))
            {
                viewers.TryRemove(viewerId, out _);
            }
        }

        // Get viewer sockets for a streamer
        public static ConcurrentDictionary<string, WebSocket> GetViewerSockets(string streamerId)
        {
            twitchSockets.TryGetValue(streamerId, out var viewers);
            return viewers;
        }

        // Game state management
        public static bool IsGameStarted(string streamerId)
        {
            return gameStates.TryGetValue(streamerId, out var started) && started;
        }

        public static void SetGameStarted(string streamerId, bool started)
        {
            gameStates[streamerId] = started;
        }
    }
}
