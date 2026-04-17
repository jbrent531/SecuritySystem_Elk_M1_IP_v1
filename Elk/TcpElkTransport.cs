using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class TcpElkTransport : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _readLoopCts;

        public bool IsConnected
        {
            get { return _client != null && _client.Connected; }
        }

        public event Action<byte[]> DataReceived;
        public event Action<string> Log;

        public async Task ConnectAsync(string host, int port, CancellationToken ct)
        {
            _client = new TcpClient();
            RaiseLog("Connecting to " + host + ":" + port + " ...");
            await _client.ConnectAsync(host, port);

            _stream = _client.GetStream();
            _readLoopCts = new CancellationTokenSource();

            RaiseLog("Connected.");
            Task.Run(() => ReadLoopAsync(_readLoopCts.Token));
        }

        public Task ConnectAsync(string host, int port)
        {
            return ConnectAsync(host, port, CancellationToken.None);
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_readLoopCts != null)
                {
                    _readLoopCts.Cancel();
                }
            }
            catch
            {
            }

            try
            {
                if (_stream != null)
                {
                    _stream.Close();
                    _stream.Dispose();
                    _stream = null;
                }
            }
            catch
            {
            }

            try
            {
                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
            }
            catch
            {
            }

            RaiseLog("Disconnected.");
            await Task.CompletedTask;
        }

        public async Task SendAsciiAsync(string text, CancellationToken ct)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Not connected.");
            }

            byte[] data = Encoding.ASCII.GetBytes(text);
            RaiseLog("TX ASCII: " + EscapeForLog(text));
            RaiseLog("TX HEX  : " + BitConverter.ToString(data));

            await _stream.WriteAsync(data, 0, data.Length, ct);
            await _stream.FlushAsync(ct);
        }

        public Task SendAsciiAsync(string text)
        {
            return SendAsciiAsync(text, CancellationToken.None);
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            if (_stream == null)
            {
                return;
            }

            byte[] buffer = new byte[4096];

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (bytesRead <= 0)
                    {
                        RaiseLog("Remote side closed connection.");
                        break;
                    }

                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);

                    RaiseLog("RX HEX  : " + BitConverter.ToString(data));
                    RaiseLog("RX ASCII: " + EscapeForLog(Encoding.ASCII.GetString(data)));

                    var handler = DataReceived;
                    if (handler != null)
                    {
                        handler(data);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                RaiseLog("Read loop cancelled.");
            }
            catch (Exception ex)
            {
                RaiseLog("Read loop error: " + ex.Message);
            }
        }

        private static string EscapeForLog(string value)
        {
            return value.Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private void RaiseLog(string message)
        {
            var handler = Log;
            if (handler != null)
            {
                handler(message);
            }
        }

        public void Dispose()
        {
            try
            {
                if (_readLoopCts != null)
                {
                    _readLoopCts.Cancel();
                    _readLoopCts.Dispose();
                    _readLoopCts = null;
                }
            }
            catch
            {
            }

            try
            {
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
            }
            catch
            {
            }

            try
            {
                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
            }
            catch
            {
            }
        }
    }
}