using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace T2B
{
    public sealed class ExplorerWatcher : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private bool _wasRunning;
        private bool _disposed;

        public event Action? ExplorerRestarted;

        public ExplorerWatcher()
        {
            _timer = new System.Timers.Timer(2000);
            _timer.AutoReset = true;
            _timer.Elapsed += CheckExplorer;
        }

        public void Start()
        {
            ThrowIfDisposed();
            
            _wasRunning = IsExplorerRunning();
            _timer.Start();
        }

        public void Stop()
        {
            if (_disposed)
                return;

            _timer.Stop();
        }

        private void CheckExplorer(object? sender, ElapsedEventArgs e)
        {
            if (_disposed)
                return;

            bool running = IsExplorerRunning();

            if (_wasRunning && !running)
            {
                _wasRunning = false;
            }
            else if (!_wasRunning && running)
            {
                _wasRunning = true;
                ExplorerRestarted?.Invoke();
            }
        }

        private bool IsExplorerRunning()
        {
            try
            {
                return Process.GetProcessesByName("explorer").Any();
            }
            catch
            {
                return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ExplorerWatcher));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _timer.Stop();
            _timer.Dispose();
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
