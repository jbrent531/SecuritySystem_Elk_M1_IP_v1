using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Events;
using Crestron.RAD.Common.Interfaces;
using Crestron.RAD.Common.Transports;
using Crestron.RAD.DeviceTypes.SecuritySystem;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public class SecuritySystemProtocol : ABaseDriverProtocol, IDisposable
    {
        private readonly SecuritySystemDriverIP _securitySystem;
        private readonly List<ISecuritySystemZone> _zones;
        private readonly List<ISecuritySystemArea> _areas;
        private readonly Dictionary<int, SecuritySystemZone> _zoneLookup;
        private readonly Dictionary<int, SecuritySystemArea> _areaLookup;

        private IElkSecurityService _elkService;
        private string _host;
        private int _port;
        private int _selectedArea;
        private int _keypadNumber;
        private int _startupDelaySeconds;
        private bool _doubleDisarm;
        private string _monitoredZones;
        private HashSet<int> _configuredZoneSet;

        private bool _hasAreaNumber;
        private bool _hasMonitoredZones;
        private bool _structureInitialized;

        private bool _disposed;
        private bool _started;
        private bool _currentReady;
        private bool _currentAlarm;
        private bool _currentConnected;

        private readonly object _sync = new object();

        public SecuritySystemProtocol(SecuritySystemDriverIP system, ISerialTransport transportDriver, byte id)
            : base(transportDriver, id)
        {
            _securitySystem = system;
            _zones = new List<ISecuritySystemZone>();
            _areas = new List<ISecuritySystemArea>();
            _zoneLookup = new Dictionary<int, SecuritySystemZone>();
            _areaLookup = new Dictionary<int, SecuritySystemArea>();

            Zones = new ReadOnlyCollection<ISecuritySystemZone>(_zones);
            Areas = new ReadOnlyCollection<ISecuritySystemArea>(_areas);

            _selectedArea = 1;
            _keypadNumber = 1;
            _startupDelaySeconds = 0;
            _doubleDisarm = false;
            _monitoredZones = string.Empty;
            _configuredZoneSet = new HashSet<int>();

            _hasAreaNumber = false;
            _hasMonitoredZones = false;
            _structureInitialized = false;

            _host = string.Empty;
            _port = 2101;

            InitializeElkService();
        }

        public ReadOnlyCollection<ISecuritySystemArea> Areas { get; private set; }
        public ReadOnlyCollection<ISecuritySystemZone> Zones { get; private set; }

        public event StateChangeHandler StateChange;
        public event AlarmChangeHandler AlarmChange;
        public event EventHandler<ValueEventArgs<bool>> ConnectedChanged;
        public event EventHandler<ListChangedEventArgs<ISecuritySystemArea>> AreaListChanged;
        public event EventHandler<ListChangedEventArgs<ISecuritySystemZone>> ZoneListChanged;
        public event EventHandler<SecuritySystemCommandResultEventArgs> SystemCommandResult;
        public event StateChangeHandler KeypadChange;
        public event AlarmChangeHandler KeypadAlarmChange;
        public event EventHandler<ValueEventArgs<bool>> ReadyChanged;

        public void ConfigureConnection(string host, int port)
        {
            _host = host ?? string.Empty;
            _port = port > 0 ? port : 2101;
        }

        public IEnumerable<ISecuritySystemArea> GetVisibleAreas()
        {
            if (_hasAreaNumber)
            {
                SecuritySystemArea selectedArea;
                if (_areaLookup.TryGetValue(_selectedArea, out selectedArea))
                {
                    return new List<ISecuritySystemArea> { selectedArea };
                }
            }

            return Areas;
        }

        public void PrintAttributeValue()
        {
            LogMessage("Selected area = " + _selectedArea);
            LogMessage("MonitoredZones = " + _monitoredZones);
            LogMessage("KeypadNumber = " + _keypadNumber);
            LogMessage("StartupDelaySeconds = " + _startupDelaySeconds);
            LogMessage("DoubleDisarm = " + _doubleDisarm);
            LogMessage("HasAreaNumber = " + _hasAreaNumber);
            LogMessage("HasMonitoredZones = " + _hasMonitoredZones);
            LogMessage("StructureInitialized = " + _structureInitialized);
        }

        public override void SetUserAttribute(string attributeId, string attributeValue)
        {
            if (string.IsNullOrWhiteSpace(attributeId))
            {
                return;
            }

            string value = attributeValue ?? string.Empty;

            switch (attributeId)
            {
                case "MonitoredZones":
                    {
                        _monitoredZones = value.Trim();
                        _configuredZoneSet = ParseZoneExpression(_monitoredZones);
                        _hasMonitoredZones = true;

                        LogMessage("MonitoredZones set to " + _monitoredZones);
                        break;
                    }

                case "AreaNumber":
                    {
                        int area;
                        if (!int.TryParse(value, out area) || area < 1 || area > 8)
                        {
                            area = 1;
                        }

                        _selectedArea = area;
                        _hasAreaNumber = true;

                        LogMessage("AreaNumber set to " + _selectedArea);
                        break;
                    }

                case "KeypadNumber":
                    {
                        int keypad;
                        if (!int.TryParse(value, out keypad) || keypad < 1 || keypad > 16)
                        {
                            keypad = 1;
                        }

                        _keypadNumber = keypad;
                        LogMessage("KeypadNumber set to " + _keypadNumber);
                        break;
                    }

                case "StartupDelaySeconds":
                    {
                        int delay;
                        if (!int.TryParse(value, out delay) || delay < 0)
                        {
                            delay = 0;
                        }

                        _startupDelaySeconds = delay;
                        LogMessage("StartupDelaySeconds set to " + _startupDelaySeconds);
                        break;
                    }

                case "DoubleDisarm":
                    {
                        bool enabled;
                        _doubleDisarm = bool.TryParse(value, out enabled) && enabled;
                        LogMessage("DoubleDisarm set to " + _doubleDisarm);
                        break;
                    }
            }
        }

        public override void DataHandler(string rx)
        {
            if (string.IsNullOrWhiteSpace(rx))
            {
                return;
            }

            string normalized = rx.Trim();

            if (string.Equals(normalized, "InitializationComplete", StringComparison.OrdinalIgnoreCase))
            {
                StartElkService();
                return;
            }

            if (string.Equals(normalized, "IsOffline", StringComparison.OrdinalIgnoreCase))
            {
                HandleDisconnected();
            }
        }

        public SecuritySystemOperationalResult ExecuteSecurityCommands(List<int> areaIndexes, int commandIndex, string password)
        {
            List<int> targets = areaIndexes ?? new List<int> { _selectedArea };
            if (targets.Count == 0)
            {
                targets.Add(_selectedArea);
            }

            SecuritySystemAreaCommand executeCommand = null;
            foreach (SecuritySystemAreaCommand command in _securitySystem.GetAvailableAreaCommands())
            {
                if (command.Index == commandIndex)
                {
                    executeCommand = command;
                    break;
                }
            }

            if (executeCommand == null)
            {
                SecuritySystemOperationalResult invalidResult = new SecuritySystemOperationalResult(1)
                {
                    Result = SecuritySystemOperationalResultCode.InvalidIdParameters,
                    TargetComponentId = targets
                };

                RaiseSystemCommandResult(invalidResult);
                return invalidResult;
            }

            try
            {
                int area = GetPrimaryArea(targets);
                string passcode = password ?? string.Empty;

                switch (executeCommand.CommandType)
                {
                    case SecuritySystemCommandType.Away:
                    case SecuritySystemCommandType.ForceAway:
                        FireAndForget(_elkService.ArmAwayAsync(area, passcode));
                        break;

                    case SecuritySystemCommandType.Stay:
                    case SecuritySystemCommandType.ForceStay:
                        FireAndForget(_elkService.ArmStayAsync(area, passcode));
                        break;

                    case SecuritySystemCommandType.Disarm:
                        FireAndForget(_elkService.DisarmAsync(area, passcode));
                        if (_doubleDisarm)
                        {
                            FireAndForget(DelayedSecondDisarmAsync(area, passcode));
                        }
                        break;

                    default:
                        {
                            SecuritySystemOperationalResult unsupported = new SecuritySystemOperationalResult(1)
                            {
                                CommandType = executeCommand.CommandType,
                                Result = SecuritySystemOperationalResultCode.InvalidIdParameters,
                                TargetComponentId = targets
                            };
                            RaiseSystemCommandResult(unsupported);
                            return unsupported;
                        }
                }

                SecuritySystemOperationalResult success = new SecuritySystemOperationalResult(1)
                {
                    CommandType = executeCommand.CommandType,
                    Result = SecuritySystemOperationalResultCode.Success,
                    TargetComponentId = targets
                };

                RaiseSystemCommandResult(success);
                return success;
            }
            catch (Exception ex)
            {
                LogMessage("ExecuteSecurityCommands failed: " + ex.Message);

                SecuritySystemOperationalResult failed = new SecuritySystemOperationalResult(1)
                {
                    CommandType = executeCommand.CommandType,
                    Result = SecuritySystemOperationalResultCode.InvalidIdParameters,
                    TargetComponentId = targets
                };

                RaiseSystemCommandResult(failed);
                return failed;
            }
        }

        public void SendKeypadNumber(uint num)
        {
            SendKeypadString(num.ToString());
        }

        public void SendKeypadPound()
        {
            SendKeypadString("#");
        }

        public void SendKeypadAsterisk()
        {
            SendKeypadString("*");
        }

        public void SendKeypadPeriod()
        {
            SendKeypadString(".");
        }

        public void SendKeypadDash()
        {
            SendKeypadString("-");
        }

        public void SendKeypadString(string keys)
        {
            if (string.IsNullOrEmpty(keys))
            {
                return;
            }

            if (keys.Length > 20)
            {
                keys = keys.Substring(0, 20);
            }
        }

        public void SendKeypadBackSpace()
        {
        }

        public void SendKeypadArrowKeys(ArrowDirections direction)
        {
        }

        public void SendKeypadEnter()
        {
        }

        public void SendKeypadClear()
        {
        }

        public void SendKeypadExit()
        {
        }

        public void SendKeypadHome()
        {
        }

        public void SendKeypadMenu()
        {
        }

        public void TriggerFunctionButton(int buttonNumber)
        {
            switch (buttonNumber)
            {
                case 1:
                    FireAndForget(_elkService.ArmStayAsync(_selectedArea, string.Empty));
                    break;

                case 2:
                    FireAndForget(_elkService.ArmAwayAsync(_selectedArea, string.Empty));
                    break;

                case 3:
                    RaiseAlarmStateChangedEvent(SecuritySystemAlarmType.Fire, true);
                    RaiseKeypadAlarmChangedEvent(SecuritySystemAlarmType.Fire, true);
                    break;
            }
        }

        public SecuritySystemOperationalResult SetZoneBypass(int zoneIndex, bool bypass, string password)
        {
            SecuritySystemZone zone = GetZone(zoneIndex);
            if (zone == null)
            {
                return new SecuritySystemOperationalResult(1)
                {
                    Result = SecuritySystemOperationalResultCode.InvalidIdParameters,
                    TargetComponentId = new List<int> { zoneIndex }
                };
            }

            try
            {
                string methodName = bypass ? "BypassZoneAsync" : "UnbypassZoneAsync";
                MethodInfo method = _elkService.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

                if (method == null)
                {
                    return new SecuritySystemOperationalResult(1)
                    {
                        Result = SecuritySystemOperationalResultCode.InvalidIdParameters,
                        TargetComponentId = new List<int> { zoneIndex }
                    };
                }

                Task task = method.Invoke(_elkService, new object[] { zoneIndex, password ?? string.Empty }) as Task;
                if (task != null)
                {
                    FireAndForget(task);
                }

                return new SecuritySystemOperationalResult(1)
                {
                    Result = SecuritySystemOperationalResultCode.Success,
                    TargetComponentId = new List<int> { zoneIndex }
                };
            }
            catch (Exception ex)
            {
                LogMessage("SetZoneBypass failed: " + ex.Message);
                return new SecuritySystemOperationalResult(1)
                {
                    Result = SecuritySystemOperationalResultCode.InvalidIdParameters,
                    TargetComponentId = new List<int> { zoneIndex }
                };
            }
        }

        protected override void ChooseDeconstructMethod(ValidatedRxData validatedData)
        {
        }

        protected override void ConnectionChangedEvent(bool connection)
        {
            EventHandler<ValueEventArgs<bool>> handler = ConnectedChanged;
            if (handler != null)
            {
                handler(this, new ValueEventArgs<bool>(connection));
            }
        }

        protected override void ConnectionChanged(bool connection)
        {
            if (connection == IsConnected)
            {
                return;
            }

            base.ConnectionChanged(connection);
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                base.Dispose();
                return;
            }

            _disposed = true;

            try
            {
                if (_elkService != null)
                {
                    _elkService.ZoneChanged -= OnElkZoneChanged;
                    _elkService.ZoneNameChanged -= OnElkZoneChanged;
                    _elkService.ZoneBypassChanged -= OnElkZoneChanged;
                    _elkService.AreaChanged -= OnElkAreaChanged;
                    _elkService.SystemReadyChanged -= OnElkSystemReadyChanged;
                    _elkService.AlarmActiveChanged -= OnElkAlarmActiveChanged;

                    try
                    {
                        Task stopTask = _elkService.StopAsync();
                        if (stopTask != null)
                        {
                            stopTask.GetAwaiter().GetResult();
                        }
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                foreach (SecuritySystemArea area in _areas.OfType<SecuritySystemArea>())
                {
                    try
                    {
                        area.SecuritysystemAreaStateChangedEvent -= OnSecuritysystemAreaStateChangedEvent;
                        area.SecuritysystemAlarmStateChangedEvent -= OnSecuritysystemAlarmStateChangedEvent;
                        area.Dispose();
                    }
                    catch
                    {
                    }
                }

                foreach (SecuritySystemZone zone in _zones.OfType<SecuritySystemZone>())
                {
                    try
                    {
                        zone.SecuritySystemZoneStateChanged -= OnSecuritySystemZoneStateChanged;
                        zone.BypassDelegate = null;
                        zone.UnbypassDelegate = null;
                    }
                    catch
                    {
                    }
                }

                _areas.Clear();
                _zones.Clear();
                _zoneLookup.Clear();
                _areaLookup.Clear();

                base.Dispose();
            }
        }

        private void InitializeElkService()
        {
            _elkService = new ElkSecurityService();
            _elkService.ZoneChanged += OnElkZoneChanged;
            _elkService.ZoneNameChanged += OnElkZoneChanged;
            _elkService.ZoneBypassChanged += OnElkZoneChanged;
            _elkService.AreaChanged += OnElkAreaChanged;
            _elkService.SystemReadyChanged += OnElkSystemReadyChanged;
            _elkService.AlarmActiveChanged += OnElkAlarmActiveChanged;
        }

        private void StartElkService()
        {
            lock (_sync)
            {
                if (_started || string.IsNullOrWhiteSpace(_host) || _port <= 0)
                {
                    return;
                }

                _started = true;
            }

            FireAndForget(StartElkServiceAsync());
        }

        private async Task StartElkServiceAsync()
        {
            try
            {
                await _elkService.StartAsync(_host, _port).ConfigureAwait(false);
                LogMessage("Elk connection established");

                if (_startupDelaySeconds > 0)
                {
                    LogMessage("Delaying area startup by " + _startupDelaySeconds + " seconds");
                    await Task.Delay(_startupDelaySeconds * 1000).ConfigureAwait(false);
                }

                TryInitializeStructure();
                LogMessage("Area structure created");

                await Task.Delay(2000).ConfigureAwait(false);

                SetConnected(true);
                LogMessage("Connected set true");

                FireAndForget(PublishConfiguredZonesAfterReadyAsync());
            }
            catch (Exception ex)
            {
                LogMessage("StartElkServiceAsync failed: " + ex.Message);
                HandleDisconnected();
            }
        }

        private async Task PublishConfiguredZonesAfterReadyAsync()
        {
            try
            {
                await Task.Delay(5000).ConfigureAwait(false);

                lock (_sync)
                {
                    if (!_started || !_structureInitialized)
                    {
                        return;
                    }
                }

                foreach (int zoneNumber in _configuredZoneSet.OrderBy(x => x))
                {
                    ElkZone elkZone;
                    if (_elkService == null || !_elkService.Zones.TryGetValue(zoneNumber, out elkZone) || elkZone == null)
                    {
                        continue;
                    }

                    bool isNew;
                    FindOrCreateZone(zoneNumber, out isNew);
                    OnElkZoneChanged(elkZone);

                    LogMessage("Published zone " + zoneNumber);

                    await Task.Delay(500).ConfigureAwait(false);
                }

                PublishAreaStateAfterZones();
            }
            catch (Exception ex)
            {
                LogMessage("PublishConfiguredZonesAfterReadyAsync failed: " + ex.Message);
            }
        }


        private void PublishAreaStateAfterZones()
        {
            if (_elkService == null || !_structureInitialized)
            {
                return;
            }

            ElkArea area;
            if (_elkService.Areas.TryGetValue(_selectedArea, out area) && area != null)
            {
                OnElkAreaChanged(area);
            }

            OnElkSystemReadyChanged(_elkService.IsSystemReady);
            OnElkAlarmActiveChanged(_elkService.IsAlarmActive);

            LogMessage("Area/state published after zones");
        }



        private async Task DelayedSecondDisarmAsync(int area, string password)
        {
            await Task.Delay(500).ConfigureAwait(false);
            await _elkService.DisarmAsync(area, password).ConfigureAwait(false);
        }

        private void HandleDisconnected()
        {
            LogMessage("HandleDisconnected called");

            lock (_sync)
            {
                _started = false;
            }

            SetConnected(false);
        }

        private void TryInitializeStructure()
        {
            if (_structureInitialized)
            {
                return;
            }

            if (!_hasAreaNumber || !_hasMonitoredZones)
            {
                return;
            }

            EnsureSelectedAreaContainer();

            _structureInitialized = true;

            LogMessage("Structure initialized for area " + _selectedArea + " zones=" + _monitoredZones);
        }


        private void PublishCurrentState()
        {
            if (_elkService == null || !_structureInitialized)
            {
                return;
            }

            ElkArea area;
            if (_elkService.Areas.TryGetValue(_selectedArea, out area) && area != null)
            {
                OnElkAreaChanged(area);
            }

            OnElkSystemReadyChanged(_elkService.IsSystemReady);
            OnElkAlarmActiveChanged(_elkService.IsAlarmActive);
        }

        private void OnElkAreaChanged(ElkArea elkArea)
        {
            if (elkArea == null || !_structureInitialized)
            {
                return;
            }

            if (elkArea.Number != _selectedArea)
            {
                return;
            }

            SecuritySystemArea area = EnsureAreaExists(elkArea.Number);

            if (!string.IsNullOrWhiteSpace(elkArea.Name) &&
                !string.Equals(area.Name, elkArea.Name, StringComparison.Ordinal))
            {
                area.Name = elkArea.Name;
            }

            bool isStay = IsStay(elkArea);
            bool isAway = IsAway(elkArea);
            bool isDisarmed = !isStay && !isAway;

            RaiseAreaStateChangedEvent(SecuritySystemState.Disarmed, isDisarmed);
            RaiseAreaStateChangedEvent(SecuritySystemState.ArmedStay, isStay);
            RaiseAreaStateChangedEvent(SecuritySystemState.ArmedAway, isAway);

            RaiseKeypadStateChangedEvent(SecuritySystemState.Disarmed, isDisarmed);
            RaiseKeypadStateChangedEvent(SecuritySystemState.ArmedStay, isStay);
            RaiseKeypadStateChangedEvent(SecuritySystemState.ArmedAway, isAway);

            bool burglaryActive = elkArea.IsAlarm;
            RaiseAlarmStateChangedEvent(SecuritySystemAlarmType.Alarm, burglaryActive);
            RaiseAlarmStateChangedEvent(SecuritySystemAlarmType.Burglary, burglaryActive);

            RaiseKeypadAlarmChangedEvent(SecuritySystemAlarmType.Alarm, burglaryActive);
            RaiseKeypadAlarmChangedEvent(SecuritySystemAlarmType.Burglary, burglaryActive);
        }

        private void OnElkSystemReadyChanged(bool ready)
        {
            if (_currentReady == ready)
            {
                return;
            }

            _currentReady = ready;

            EventHandler<ValueEventArgs<bool>> handler = ReadyChanged;
            if (handler != null)
            {
                handler(this, new ValueEventArgs<bool>(ready));
            }
        }

        private void OnElkAlarmActiveChanged(bool alarmActive)
        {
            if (_currentAlarm == alarmActive)
            {
                return;
            }

            _currentAlarm = alarmActive;
            RaiseAlarmStateChangedEvent(SecuritySystemAlarmType.Alarm, alarmActive);
            RaiseKeypadAlarmChangedEvent(SecuritySystemAlarmType.Alarm, alarmActive);
        }

        private void OnElkZoneChanged(ElkZone elkZone)
        {
            if (elkZone == null || !_structureInitialized)
            {
                return;
            }

            if (!ShouldExposeZone(elkZone.Number))
            {
                return;
            }

            bool isNew;
            ISecuritySystemZone zone = FindOrCreateZone(elkZone.Number, out isNew);
            SecuritySystemZone securityZone = zone as SecuritySystemZone;

            if (securityZone != null)
            {
                string zoneName = string.IsNullOrEmpty(elkZone.Name)
                    ? "Zone " + elkZone.Number.ToString("D3")
                    : elkZone.Name;

                bool isFaulted = elkZone.IsOpen || elkZone.IsViolated || elkZone.IsTrouble;
                securityZone.ApplyElkState(zoneName, isFaulted, elkZone.IsBypassed, elkZone.Definition);
            }
        }

        private ISecuritySystemZone FindOrCreateZone(int zoneNumber, out bool isNew)
        {
            SecuritySystemZone existing;
            if (_zoneLookup.TryGetValue(zoneNumber, out existing))
            {
                existing.SetAreaIndex(_selectedArea);
                isNew = false;
                return existing;
            }

            SecuritySystemZone zone = new SecuritySystemZone("Zone " + zoneNumber.ToString("D3"), zoneNumber, _selectedArea);
            zone.SecuritySystemZoneStateChanged += OnSecuritySystemZoneStateChanged;

            zone.BypassDelegate = delegate (int zoneIdx, int areaIdx, string password)
            {
                SetZoneBypass(zoneIdx, true, password);
            };

            zone.UnbypassDelegate = delegate (int zoneIdx, int areaIdx, string password)
            {
                SetZoneBypass(zoneIdx, false, password);
            };

            _zoneLookup[zoneNumber] = zone;
            _zones.Add(zone);

            isNew = true;
            PublishZoneAdded(zone);
            return zone;
        }

        private void PublishZoneAdded(ISecuritySystemZone zone)
        {
            EventHandler<ListChangedEventArgs<ISecuritySystemZone>> handler = ZoneListChanged;
            if (handler != null)
            {
                int listIndex = _zones.IndexOf(zone);
                handler(this, new ListChangedEventArgs<ISecuritySystemZone>(
                    ListChangedAction.Added,
                    null,
                    zone,
                    listIndex));
            }
        }

        private SecuritySystemArea EnsureSelectedAreaContainer()
        {
            SecuritySystemArea existing;
            if (_areaLookup.TryGetValue(_selectedArea, out existing))
            {
                return existing;
            }

            return EnsureAreaExists(_selectedArea);
        }

        private SecuritySystemArea EnsureAreaExists(int areaIndex)
        {
            SecuritySystemArea area;
            if (_areaLookup.TryGetValue(areaIndex, out area))
            {
                return area;
            }

            ReadOnlyCollection<SecuritySystemState> supportedStates = new List<SecuritySystemState>
            {
                SecuritySystemState.ArmedAway,
                SecuritySystemState.ArmedStay,
                SecuritySystemState.Disarmed
            }.AsReadOnly();

            ReadOnlyCollection<SecuritySystemAlarmType> supportedAlarms = new List<SecuritySystemAlarmType>
            {
                SecuritySystemAlarmType.Alarm,
                SecuritySystemAlarmType.Burglary,
                SecuritySystemAlarmType.Fire,
                SecuritySystemAlarmType.Tamper
            }.AsReadOnly();

            ReadOnlyCollection<SecuritySystemAreaCommand> areaCommands = new List<SecuritySystemAreaCommand>
            {
                new SecuritySystemAreaCommand(1, SecuritySystemCommandType.Disarm, true),
                new SecuritySystemAreaCommand(2, SecuritySystemCommandType.Away, true),
                new SecuritySystemAreaCommand(3, SecuritySystemCommandType.Stay, true)
            }.AsReadOnly();

            area = new SecuritySystemArea("Area " + areaIndex, areaIndex, supportedStates, supportedAlarms, areaCommands, this);
            area.SecuritysystemAreaStateChangedEvent += OnSecuritysystemAreaStateChangedEvent;
            area.SecuritysystemAlarmStateChangedEvent += OnSecuritysystemAlarmStateChangedEvent;

            _areaLookup[areaIndex] = area;
            _areas.Add(area);

            EventHandler<ListChangedEventArgs<ISecuritySystemArea>> handler = AreaListChanged;
            if (handler != null)
            {
                handler(
                    this,
                    new ListChangedEventArgs<ISecuritySystemArea>(
                        ListChangedAction.Added,
                        null,
                        area,
                        _areas.Count - 1));
            }

            return area;
        }

        private void EnsureConfiguredZonesExist()
        {
            foreach (int zoneNumber in _configuredZoneSet.OrderBy(x => x))
            {
                bool isNew;
                FindOrCreateZone(zoneNumber, out isNew);
            }
        }

        private bool ShouldExposeZone(int zoneNumber)
        {
            return _configuredZoneSet.Contains(zoneNumber);
        }

        private SecuritySystemZone GetZone(int zoneIndex)
        {
            SecuritySystemZone zone;
            return _zoneLookup.TryGetValue(zoneIndex, out zone) ? zone : null;
        }

        private int GetPrimaryArea(List<int> areaIndexes)
        {
            if (areaIndexes == null || areaIndexes.Count == 0)
            {
                return _selectedArea;
            }

            return areaIndexes[0] > 0 ? areaIndexes[0] : _selectedArea;
        }

        private void SetConnected(bool connected)
        {
            if (_currentConnected == connected)
            {
                return;
            }

            _currentConnected = connected;
            ConnectionChanged(connected);
        }

        private static bool IsStay(ElkArea area)
        {
            string text = (area.ArmStateText ?? string.Empty).Trim().ToLowerInvariant();
            return text == "armed stay" || text == "stay";
        }

        private static bool IsAway(ElkArea area)
        {
            string text = (area.ArmStateText ?? string.Empty).Trim().ToLowerInvariant();
            return text == "armed away" || text == "away";
        }

        private void RaiseAreaStateChangedEvent(SecuritySystemState eventType, bool state)
        {
            SecuritySystemStateArgs stateObj = new SecuritySystemStateArgs
            {
                EventType = eventType,
                State = state
            };

            StateChangeHandler handler = StateChange;
            if (handler != null)
            {
                handler(stateObj);
            }
        }

        private void RaiseKeypadStateChangedEvent(SecuritySystemState eventType, bool state)
        {
            SecuritySystemStateArgs stateObj = new SecuritySystemStateArgs
            {
                EventType = eventType,
                State = state
            };

            StateChangeHandler handler = KeypadChange;
            if (handler != null)
            {
                handler(stateObj);
            }
        }

        private void RaiseKeypadAlarmChangedEvent(SecuritySystemAlarmType eventType, bool state)
        {
            SecuritySystemAlarmStateArgs stateObj = new SecuritySystemAlarmStateArgs
            {
                Alarm = new SecuritySystemAlarm(eventType, state),
                State = state
            };

            AlarmChangeHandler handler = KeypadAlarmChange;
            if (handler != null)
            {
                handler(stateObj);
            }
        }

        private void RaiseAlarmStateChangedEvent(SecuritySystemAlarmType eventType, bool state)
        {
            SecuritySystemAlarmStateArgs stateObj = new SecuritySystemAlarmStateArgs
            {
                Alarm = new SecuritySystemAlarm(eventType, state),
                State = state
            };

            AlarmChangeHandler handler = AlarmChange;
            if (handler != null)
            {
                handler(stateObj);
            }
        }

        private void RaiseSystemCommandResult(SecuritySystemOperationalResult result)
        {
            EventHandler<SecuritySystemCommandResultEventArgs> handler = SystemCommandResult;
            if (handler != null)
            {
                handler(this, new SecuritySystemCommandResultEventArgs { CommandResult = result });
            }
        }

        private void OnSecuritySystemZoneStateChanged(object sender, ListChangedEventArgs<SecuritySystemZoneState> args)
        {
        }

        private void OnSecuritysystemAreaStateChangedEvent(object sender, ListChangedEventArgs<SecuritySystemState> args)
        {
            if (args == null)
            {
                return;
            }

            bool updatedState = false;
            SecuritySystemState state = SecuritySystemState.Unknown;

            switch (args.ChangedAction)
            {
                case ListChangedAction.Added:
                    state = args.NewItem;
                    updatedState = true;
                    break;

                case ListChangedAction.Removed:
                    state = args.OldItem;
                    break;
            }

            RaiseAreaStateChangedEvent(state, updatedState);
        }

        private void OnSecuritysystemAlarmStateChangedEvent(object sender, ListChangedEventArgs<SecuritySystemAlarmType> args)
        {
            if (args == null)
            {
                return;
            }

            SecuritySystemAlarmType alarmType = SecuritySystemAlarmType.Unknown;
            bool active = false;

            switch (args.ChangedAction)
            {
                case ListChangedAction.Added:
                    alarmType = args.NewItem;
                    active = true;
                    break;

                case ListChangedAction.Removed:
                    alarmType = args.OldItem;
                    break;
            }

            RaiseAlarmStateChangedEvent(alarmType, active);
        }

        public void LogMessage(string message)
        {
            if (!EnableLogging)
            {
                return;
            }

            if (CustomLogger == null)
            {
                CrestronConsole.PrintLine(message);
            }
            else
            {
                CustomLogger(message + "\n");
            }
        }

        private static void FireAndForget(Task task)
        {
            if (task == null)
            {
                return;
            }

            task.ContinueWith(delegate (Task t)
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    CrestronConsole.PrintLine("Async task failed: " + t.Exception.GetBaseException().Message);
                }
            });
        }

        private static HashSet<int> ParseZoneExpression(string expression)
        {
            HashSet<int> zones = new HashSet<int>();

            if (string.IsNullOrWhiteSpace(expression))
            {
                return zones;
            }

            string[] parts = expression.Split(',');
            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                int dashIndex = part.IndexOf('-');
                if (dashIndex > 0)
                {
                    string startText = part.Substring(0, dashIndex).Trim();
                    string endText = part.Substring(dashIndex + 1).Trim();

                    int start;
                    int end;

                    if (int.TryParse(startText, out start) && int.TryParse(endText, out end))
                    {
                        if (start > end)
                        {
                            int temp = start;
                            start = end;
                            end = temp;
                        }

                        for (int i = start; i <= end; i++)
                        {
                            if (i >= 1 && i <= 208)
                            {
                                zones.Add(i);
                            }
                        }
                    }
                }
                else
                {
                    int zone;
                    if (int.TryParse(part, out zone))
                    {
                        if (zone >= 1 && zone <= 208)
                        {
                            zones.Add(zone);
                        }
                    }
                }
            }

            return zones;
        }
    }

    public delegate void StateChangeHandler(object changedObject);
    public delegate void AlarmChangeHandler(object changedObject);

    internal class SecuritySystemStateArgs
    {
        public SecuritySystemState EventType { get; set; }
        public bool State { get; set; }
    }

    internal class SecuritySystemAlarmStateArgs
    {
        public SecuritySystemAlarm Alarm { get; set; }
        public bool State { get; set; }
    }
}