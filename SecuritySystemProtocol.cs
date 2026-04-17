using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private int _keypadNumber;
        private int _startupDelaySeconds;
        private bool _doubleDisarm;
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

            _keypadNumber = 1;
            _startupDelaySeconds = 0;
            _doubleDisarm = false;
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
            return Areas;
        }

        public void PrintAttributeValue()
        {
            LogMessage("KeypadNumber = " + _keypadNumber);
            LogMessage("StartupDelaySeconds = " + _startupDelaySeconds);
            LogMessage("DoubleDisarm = " + _doubleDisarm);
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
            List<int> targets = areaIndexes ?? new List<int>();
            if (targets.Count == 0)
            {
                targets.Add(1);
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
                    FireAndForget(_elkService.ArmStayAsync(1, string.Empty));
                    break;

                case 2:
                    FireAndForget(_elkService.ArmAwayAsync(1, string.Empty));
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
                foreach (SecuritySystemArea area in _areas)
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

                foreach (SecuritySystemZone zone in _zones)
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
                if (_startupDelaySeconds > 0)
                {
                    await Task.Delay(_startupDelaySeconds * 1000).ConfigureAwait(false);
                }

                TryInitializeStructure();

                await _elkService.StartAsync(_host, _port).ConfigureAwait(false);

                SetConnected(true);

                try
                {
                    PublishCurrentState();
                }
                catch (Exception ex)
                {
                    LogMessage("PublishCurrentState failed: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                LogMessage("StartElkServiceAsync failed: " + ex.Message);
                HandleDisconnected();
            }
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

            _structureInitialized = true;
            LogMessage("Structure initialized");
        }

        private void PublishCurrentState()
        {
            if (_elkService == null || !_structureInitialized)
            {
                return;
            }

            foreach (ElkArea area in _elkService.Areas.Values)
            {
                OnElkAreaChanged(area);
            }

            foreach (ElkZone zone in _elkService.Zones.Values)
            {
                OnElkZoneChanged(zone);
            }

            OnElkSystemReadyChanged(_elkService.IsSystemReady);
            OnElkAlarmActiveChanged(_elkService.IsAlarmActive);
        }

        private void OnElkAreaChanged(ElkArea elkArea)
        {
            CrestronConsole.PrintLine(string.Format(
                "OnElkAreaChanged num={0} name={1} arm={2}",
                elkArea == null ? -1 : elkArea.Number,
                elkArea == null ? "<null>" : elkArea.Name,
                elkArea == null ? "<null>" : elkArea.ArmStateText));

            if (elkArea == null)
            {
                return;
            }

            SecuritySystemArea area = EnsureAreaExists(elkArea.Number, true);

            if (!string.IsNullOrWhiteSpace(elkArea.Name) &&
                !string.Equals(area.Name, elkArea.Name, StringComparison.Ordinal))
            {
                area.Name = elkArea.Name;
            }

            bool isStay = IsStay(elkArea);
            bool isAway = IsAway(elkArea);
            bool isDisarmed = !isStay && !isAway;

            if (elkArea.Number == 1)
            {
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
            if (elkZone == null || elkZone.Number < 1 || elkZone.Number > 208)
            {
                return;
            }

            if (!elkZone.IsConfigured || elkZone.Definition == 0 || elkZone.Partition < 1)
            {
                return;
            }

            CrestronConsole.PrintLine(
                "OnElkZoneChanged zone={0} name={1} partition={2} bypassed={3} definition={4}",
                elkZone.Number,
                elkZone.Name ?? string.Empty,
                elkZone.Partition,
                elkZone.IsBypassed,
                elkZone.Definition);

            bool isNew;
            ISecuritySystemZone zone = FindOrCreateZone(elkZone, out isNew);
            SecuritySystemZone securityZone = zone as SecuritySystemZone;

            if (securityZone != null)
            {
                string zoneName = string.IsNullOrEmpty(elkZone.Name)
                    ? "Zone " + elkZone.Number.ToString("D3")
                    : elkZone.Name;

                bool isFaulted = elkZone.IsOpen || elkZone.IsViolated || elkZone.IsTrouble;

                securityZone.ApplyElkState(
                    zoneName,
                    elkZone.Partition,
                    isFaulted,
                    elkZone.IsBypassed,
                    elkZone.Definition);
            }
        }

        private ISecuritySystemZone FindOrCreateZone(ElkZone elkZone, out bool isNew)
        {
            SecuritySystemZone existing;
            if (_zoneLookup.TryGetValue(elkZone.Number, out existing))
            {
                isNew = false;
                return existing;
            }

            int areaIndex = elkZone.Partition > 0 ? elkZone.Partition : 1;

            SecuritySystemZone zone = new SecuritySystemZone(
                "Zone " + elkZone.Number.ToString("D3"),
                elkZone.Number,
                areaIndex);

            zone.SecuritySystemZoneStateChanged += OnSecuritySystemZoneStateChanged;

            zone.BypassDelegate = delegate (int zoneIdx, int areaIdx, string password)
            {
                SetZoneBypass(zoneIdx, true, password);
            };

            zone.UnbypassDelegate = delegate (int zoneIdx, int areaIdx, string password)
            {
                SetZoneBypass(zoneIdx, false, password);
            };

            _zoneLookup[elkZone.Number] = zone;
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
                handler(
                    this,
                    new ListChangedEventArgs<ISecuritySystemZone>(
                        ListChangedAction.Added,
                        null,
                        zone,
                        -1));
            }
        }

        private SecuritySystemArea EnsureAreaExists(int areaIndex, bool publishEvent)
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

            if (publishEvent)
            {
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
            }

            return area;
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
                return 1;
            }

            return areaIndexes[0] > 0 ? areaIndexes[0] : 1;
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