using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatRoom.Server.Services
{
    public class ServerListener
    {
        private TcpListener? _listener;
        private bool _isRunning;
        private int _port;

        public List<ClientHandler> ConnectedClients { get; private set; }

        private Action<string> _logAction;

        public ServerListener(int port, Action<string> logAction)
        {
            _port = port;
            _logAction = logAction;
            ConnectedClients = new List<ClientHandler>();
        }

        public async Task StartAsync()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _isRunning = true;

                _logAction?.Invoke($"Server started on port {_port}. Waiting for connections...");
                while (_isRunning)
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync();

                    ClientHandler newClient = new ClientHandler(tcpClient, _logAction);
                    ConnectedClients.Add(newClient);

                    _logAction?.Invoke($"Client connected! IP: {tcpClient.Client.RemoteEndPoint}");

                    _ = newClient.StartReadingLoop();
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                    _logAction?.Invoke($"Server Error: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();

            foreach (var client in ConnectedClients)
            {
                client.ClientSocket.Close();
            }
            ConnectedClients.Clear();
            _logAction?.Invoke("Server stopped.");
        }
    }
}