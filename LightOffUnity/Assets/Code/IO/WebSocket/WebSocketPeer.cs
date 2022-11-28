// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading.Tasks;
using RailgunNet.Connection.Client;
using RailgunNet.Connection.Traffic;
using System;
using System.Buffers;
using System.Collections.Generic;
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

        public UniTask CompletionDueToDisconnect => _completionSource.Task;

        public WebSocketPeer(Func<RailClient> clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public void SendPayload(ArraySegment<byte> buffer)
        {
            _socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public async UniTask<ConnectionResult> ConnectTo(string hostName, string sessionName, string playerName)
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
                _client = _clientFactory();
                _client.SetPeer(this);
                Listen().Forget();
                var room = _client.StartRoom();
                return new ConnectionResult(_client, room);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is WebSocketException)
            {
                _cancellation = null;
                //_logger.LogError($"Connection failed: {ex.Message}");
                //return null;
            }
            return ConnectionResult.Error;
        }

        async UniTask Listen()
        {
            while (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                var isEndOfMessage = false;

                try
                {
                    while (!isEndOfMessage && _cancellation != null && !_cancellation.IsCancellationRequested)
                    {
                        var buffer = ArrayPool<byte>.Shared.Rent(SIZE_HINT);
                        var result = await _socket.ReceiveAsync(buffer, _cancellation.Token);
                        isEndOfMessage = Receive(result, buffer, out var frame);
                        if (isEndOfMessage && !frame.IsEmpty)
                        {
                            if (SequenceMarshal.TryGetArray(frame, out var segment))
                            {
                                PayloadReceived.Invoke(this, segment);
                                ArrayPool<byte>.Shared.Return(segment.Array);
                            } 
                            else
                            {
                                UnityEngine.Debug.LogError("Can't marshall array data from ReadOnlySequence");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error reading websocket stream. Disconnecting now {ex.Message}");
                    _cancellation.Cancel();
                    _completionSource.TrySetResult(ex.Message);
                }
            }
            _cancellation = null;
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Socket already gone {ex.Message}");
            }
            UnityEngine.Debug.Log("Socket Closed");
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

        internal void Disconnect()
        {
            if(_cancellation != null)
            {
                _cancellation.Cancel();
            }
            _cancellation = null;
        }

        Chunk<byte> startChunk = null;
        Chunk<byte> currentChunk = null;
        IWebSocket _socket;
        CancellationTokenSource _cancellation;
        UniTaskCompletionSource<string> _completionSource;
        RailClient _client;
        readonly Func<RailClient> _clientFactory;
        readonly TimeoutController _timeoutController = new TimeoutController();
        byte[] _buffer = new byte[SIZE_HINT];

        const int SIZE_HINT = 1024;
    }
}
