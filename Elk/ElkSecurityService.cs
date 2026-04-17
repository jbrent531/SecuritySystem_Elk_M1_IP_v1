using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkSecurityService : IElkSecurityService
    {
        private readonly TcpElkTransport _transport;
        private readonly ElkMessageBuffer _buffer;
        private readonly ElkSystemState _state;
        private readonly ElkCommandRouter _router;
        private readonly ElkM1Client _client;

        private CancellationTokenSource _pollCts;
        private bool _started;

        public event Action<ElkZone> ZoneChanged;
        public event Action<ElkZone> ZoneNameChanged;
        public event Action<ElkZone> ZoneBypassChanged;
        public event Action<ElkArea> AreaChanged;
        public event Action<bool> SystemReadyChanged;
        public event Action<bool> AlarmActiveChanged;

        public IReadOnlyDictionary<int, ElkZone> Zones
        {
            get { return _state.Zones; }
        }

        public IReadOnlyDictionary<int, ElkArea> Areas
        {
            get { return _state.Areas; }
        }

        public bool IsSystemReady
        {
            get { return _state.IsSystemReady; }
        }

        public bool IsAlarmActive
        {
            get { return _state.IsAlarmActive; }
        }

        public ElkSecurityService()
        {
            _transport = new TcpElkTransport();
            _buffer = new ElkMessageBuffer();
            _state = new ElkSystemState();
            _router = new ElkCommandRouter(_state);
            _client = new ElkM1Client(_transport);

            _transport.DataReceived += OnTransportDataReceived;

            _state.ZoneChanged += OnStateZoneChanged;
            _state.ZoneNameChanged += OnStateZoneNameChanged;
            _state.ZoneBypassChanged += OnStateZoneBypassChanged;
            _state.AreaChanged += OnStateAreaChanged;
            _state.SystemReadyChanged += OnStateSystemReadyChanged;
            _state.AlarmActiveChanged += OnStateAlarmActiveChanged;
        }

        public async Task StartAsync(string host, int port)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _pollCts = new CancellationTokenSource();

            await _transport.ConnectAsync(host, port);
            await InitialSyncAsync();
            await _client.RequestZonePartitionsAsync().ConfigureAwait(false);

            _ = Task.Run(() => PollLoopAsync(_pollCts.Token));
        }

        public async Task StopAsync()
        {
            _started = false;

            if (_pollCts != null)
            {
                try
                {
                    _pollCts.Cancel();
                    _pollCts.Dispose();
                }
                catch
                {
                }

                _pollCts = null;
            }

            await _transport.DisconnectAsync();
        }

        public Task RefreshZonesAsync()
        {
            return _client.RequestZoneStatusAsync();
        }

        public Task RefreshAreasAsync()
        {
            return _client.RequestArmingStatusAsync();
        }

        public async Task RefreshZoneNamesAsync()
        {
            int zone;

            for (zone = 1; zone <= 208; zone++)
            {
                await _client.RequestTextDescriptionAsync(0, zone);
                await Task.Delay(20);
            }
        }

        public Task ArmStayAsync(int area, string userCode)
        {
            return _client.ArmStayAsync(area, userCode);
        }

        public Task ArmAwayAsync(int area, string userCode)
        {
            return _client.ArmAwayAsync(area, userCode);
        }

        public Task DisarmAsync(int area, string userCode)
        {
            return _client.DisarmAsync(area, userCode);
        }

        private async Task InitialSyncAsync()
        {
            await _client.RequestArmingStatusAsync().ConfigureAwait(false);
            await Task.Delay(50).ConfigureAwait(false);

            await _client.RequestZoneStatusAsync().ConfigureAwait(false);
            await Task.Delay(50).ConfigureAwait(false);

            await _client.RequestZoneDefinitionsAsync().ConfigureAwait(false);
            await Task.Delay(50).ConfigureAwait(false);

            await _client.RequestZonePartitionsAsync().ConfigureAwait(false);
            await Task.Delay(50).ConfigureAwait(false);

            await _client.RequestSystemTroubleStatusAsync().ConfigureAwait(false);
            await Task.Delay(50).ConfigureAwait(false);

            for (int area = 1; area <= 8; area++)
            {
                await _client.RequestTextDescriptionAsync(1, area).ConfigureAwait(false);
                await Task.Delay(20).ConfigureAwait(false);
            }

            for (int zone = 1; zone <= 208; zone++)
            {
                await _client.RequestTextDescriptionAsync(0, zone).ConfigureAwait(false);
                await Task.Delay(20).ConfigureAwait(false);
            }
        }

        public async Task RefreshAreaNamesAsync()
        {
            int area;
            for (area = 1; area <= 8; area++)
            {
                await _client.RequestTextDescriptionAsync(1, area);
                await Task.Delay(20);
            }
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _client.RequestArmingStatusAsync();
                    await Task.Delay(500, token);

                    await _client.RequestZoneStatusAsync();
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private void OnTransportDataReceived(byte[] data)
        {
            string ascii = Encoding.ASCII.GetString(data);
            List<string> messages = _buffer.AppendAndExtract(ascii);

            foreach (string raw in messages)
            {
                try
                {
                    if (!ElkPacketParser.ValidateChecksum(raw))
                    {
                        continue;
                    }

                    ElkPacket packet = ElkPacketParser.Parse(raw);
                    _router.Handle(packet);
                }
                catch
                {
                }
            }
        }

        private void OnStateZoneChanged(ElkZone zone)
        {
            var handler = ZoneChanged;
            if (handler != null)
            {
                handler(zone);
            }
        }

        private void OnStateZoneNameChanged(ElkZone zone)
        {
            var handler = ZoneNameChanged;
            if (handler != null)
            {
                handler(zone);
            }
        }

        private void OnStateZoneBypassChanged(ElkZone zone)
        {
            var handler = ZoneBypassChanged;
            if (handler != null)
            {
                handler(zone);
            }
        }

        private void OnStateAreaChanged(ElkArea area)
        {
            var handler = AreaChanged;
            if (handler != null)
            {
                handler(area);
            }
        }

        private void OnStateSystemReadyChanged(bool ready)
        {
            var handler = SystemReadyChanged;
            if (handler != null)
            {
                handler(ready);
            }
        }

        private void OnStateAlarmActiveChanged(bool active)
        {
            var handler = AlarmActiveChanged;
            if (handler != null)
            {
                handler(active);
            }
        }
    }
}