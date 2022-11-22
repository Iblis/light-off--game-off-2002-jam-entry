// Copyright (c) 2022 Philipp Walser
// This file is subject to the terms and conditions defined in file 'LICENSE.md',
// which can be found in the root folder of this source code package.
using Cysharp.Threading;
using LightOff.Host.Session;

namespace LightOff.Host
{
    public class LoopHostedService : IHostedService, IGameLoop
    {
        Task IGameLoop.RegisterActionAsync(LogicLooperActionDelegate loopAction)
        {
            if (_looper != null)
            {
                return _looper.RegisterActionAsync(loopAction);
            }
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            const int targetFps = 60;
            _looper = new LogicLooper(targetFps);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if(_looper != null)
            {
                await _looper.ShutdownAsync(TimeSpan.FromSeconds(2));
                _looper?.Dispose();
                _looper = null;
            }
        }

        LogicLooper? _looper;
    }
}
