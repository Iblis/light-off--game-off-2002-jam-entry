// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Nerdbank.Streams;
using RailgunNet.Connection.Traffic;
using System.Buffers;
using System.Net.WebSockets;

namespace LightOff.Host.Client
{
    public class ClientPeer : IClient
    {
        public string ConnectionId { get; init; }

        public string PlayerName { get; init; }

        public event RailNetPeerEvent PayloadReceived;

        public event Action<IClient> ClientRemoved;

        public ClientPeer(WebSocket socket, string playerName, ILogger logger)
        {
            _socket = socket;
            PlayerName = playerName;
            _logger = logger;
            ConnectionId = Guid.NewGuid().ToString();
        }

        public object PlayerData { get ; set ; }

        public float? Ping => 0;
        
        public void Disconnect()
        {
            _cancellation.Cancel();
        }

        public void SendPayload(ArraySegment<byte> buffer)
        {
            _ = _socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public Task StartListeningAsync()
        {
            return Task.Run(ListenAsync /*, this.DisconnectedToken*/);
        }

        async Task ListenAsync()
        {
            _cancellation = new CancellationTokenSource();
            while(_socket.State != WebSocketState.Closed && !_cancellation.IsCancellationRequested)
            {
                try
                {
                    await ReadAsync(_cancellation.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading client stream, disconnect peer now");
                    ClientRemoved?.Invoke(this);
                    return;
                }
            }
        }

        public async Task ReadAsync(CancellationToken cancellationToken)
        {
            ValueWebSocketReceiveResult result;

            do
            {
                Memory<byte> memory = _contentSequenceBuilder.GetMemory(SIZE_HINT);
                result = await _socket.ReceiveAsync(memory, cancellationToken);
                _contentSequenceBuilder.Advance(result.Count);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed as requested.", CancellationToken.None).ConfigureAwait(false);
                    return;
                }
            }
            while (!result.EndOfMessage);

            var readonlySequence = _contentSequenceBuilder.AsReadOnlySequence;
            if (readonlySequence.Length > 0)
            {
                try
                {
                    PayloadReceived.Invoke(this, readonlySequence.ToArray());
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error parsing RailgunPacket");
                }
                finally
                {
                    _contentSequenceBuilder.Reset();
                }
            }
        }

        readonly WebSocket _socket;
        readonly ILogger _logger;
        readonly Sequence<byte> _contentSequenceBuilder = new Sequence<byte>();
        CancellationTokenSource _cancellation;
        const int SIZE_HINT = 4096;
    }
}
