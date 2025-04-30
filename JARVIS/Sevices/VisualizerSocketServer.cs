using Fleck;

public class VisualizerSocketServer
{
    private WebSocketServer _server;
    private List<IWebSocketConnection> _clients = new();

    public void Start()
    {
        _server = new WebSocketServer("ws://0.0.0.0:8181");

        _server.Start(socket =>
        {
            socket.OnOpen = () =>
            {
                Console.WriteLine("[WebSocket] Visualizer connected");
                _clients.Add(socket);
            };

            socket.OnClose = () =>
            {
                Console.WriteLine("[WebSocket] Visualizer disconnected");
                _clients.Remove(socket);
            };

            socket.OnError = ex =>
            {
                Console.WriteLine($"[WebSocket] Error: {ex.Message}");
            };

            socket.OnMessage = msg =>
            {
                Console.WriteLine($"[WebSocket] Message from client: {msg}");
                // Optional: handle inbound messages from the Visualizer here
            };
        });

        Console.WriteLine("[WebSocket] VisualizerSocketServer started on ws://localhost:8181");
    }

    public void Broadcast(string message)
    {
        Console.WriteLine($"[Broadcast → Visualizer] {message}");
        foreach (var client in _clients.ToList())
        {
            if (client.IsAvailable)
                client.Send(message);
        }
    }
}
