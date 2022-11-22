// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using RailgunNet.Connection.Traffic;
using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using UniTaskWebSocket;

namespace LightOff.IO.WebSocket
{
    internal class WebSocketPeer : IRailNetPeer
    {
        public object PlayerData { get; set; }

        public float? Ping => 0;

        public event RailNetPeerEvent PayloadReceived;

        public void SendPayload(ArraySegment<byte> buffer)
        {
            _socket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Binary, true, CancellationToken.None);    
        }

        public async UniTask/*UniTask<SessionState>*/ ConnectTo(string hostName, string sessionName, string playerName)
        {
            _completionSource = null;
            if (_cancellation != null)
            {
                throw new InvalidOperationException("Connection already established");
            }

            _cancellation = new CancellationTokenSource();

            var webSocketUri = $"{hostName}/session/{sessionName}/{playerName}";
#if UNITY_WEBGL && !UNITY_EDITOR
            _socket = new UniTaskWebSocket.WebGLWebSocket();
#else
            _socket = new WebSocketWrapper(new ClientWebSocket());
#endif
            try
            {
                await _socket.ConnectAsync(new Uri($"wss://{webSocketUri}"), _timeoutController.Timeout(TimeSpan.FromSeconds(5)));
                _timeoutController.Reset();

                _completionSource = new UniTaskCompletionSource<string>();
                Listen().Forget();
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is WebSocketException)
            {
                _cancellation = null;
                //_logger.LogError($"Connection failed: {ex.Message}");
                //return null;
            }
        }

        async UniTask Listen()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                var isEndOfMessage = false;

                while (!isEndOfMessage && !_cancellation.IsCancellationRequested)
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(SIZE_HINT);
                    var result = await _socket.ReceiveAsync(buffer, _cancellation.Token);
                    isEndOfMessage = Receive(result, buffer, out var frame);
                    if (isEndOfMessage && !frame.IsEmpty)
                    {
                        SequenceMarshal.TryGetArray(frame, out var segment);
                        PayloadReceived.Invoke(this, segment);
                        foreach (var chunk in frame)
                        {
                            if (MemoryMarshal.TryGetArray(chunk, out var sharedSegment))
                            {
                                ArrayPool<byte>.Shared.Return(sharedSegment.Array);
                            }
                        }
                    }
                }
            }
            _cancellation = null;
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
        }

        bool Receive(WebSocketReceiveResult result, ArraySegment<byte> buffer, out ReadOnlySequence<byte> frame)
        {
            if (result.EndOfMessage && result.MessageType == WebSocketMessageType.Close)
            {
                frame = default;
                return false;
            }

            var slice = buffer.Slice(0, result.Count);

            if (startChunk == null)
            {
                startChunk = currentChunk = new Chunk<byte>(slice);
            }
            else
            {
                currentChunk = currentChunk.Add(slice);
            }

            if (result.EndOfMessage && startChunk != null)
            {

                if (startChunk.Next == null)
                {
                    frame = new ReadOnlySequence<byte>(startChunk.Memory);
                }
                else
                {
                    frame = new ReadOnlySequence<byte>(startChunk, 0, currentChunk, currentChunk.Memory.Length);
                }

                startChunk = currentChunk = null; // Reset so we can accept new chunks from scratch.
                return true;
            }
            else
            {
                frame = default;
                return false;
            }
        }

        Chunk<byte> startChunk = null;
        Chunk<byte> currentChunk = null;
        IWebSocket _socket;
        CancellationTokenSource _cancellation;
        UniTaskCompletionSource<string> _completionSource;
        readonly TimeoutController _timeoutController = new TimeoutController();

        const int SIZE_HINT = 4096;
    }
}
