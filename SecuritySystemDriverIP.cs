using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Crestron.RAD.Common;
using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Events;
using Crestron.RAD.Common.Interfaces;
using Crestron.RAD.Common.Transports;
using Crestron.RAD.DeviceTypes.SecuritySystem;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using Newtonsoft.Json;

namespace SecuritySystem_Elk_M1_IP_v1
{

    // Crestron RAD driver entry point for the ELK M1 integration.
    // This class exposes RAD security-system capabilities and forwards them to SecuritySystemProtocol.
    public class SecuritySystemDriverIP : ABasicDriver, ISecuritySystem, ITcp
    {
        private SecuritySystemProtocol _securitySystemProtocol;
        private IEmulatedSecuritySystemKeypad _securitySystemKeypad;
        private TcpTransport _tcpTransport;
        public SecuritySystemKeypad KeypadInstance { get; private set; }

        private readonly List<SecuritySystemError> _currentlyActiveErrors = new List<SecuritySystemError>();
        private readonly List<SecuritySystemAlarmType> _currentlyActiveAlarms = new List<SecuritySystemAlarmType>();
        private readonly List<SecuritySystemState> _currentlyActiveStates = new List<SecuritySystemState>();

        private readonly ReadOnlyCollection<SecuritySystemAreaCommand> _availableAreaCommands;
        private readonly ReadOnlyCollection<SecuritySystemAlarmType> _availableAlarms;
        private readonly ReadOnlyCollection<SecuritySystemState> _availableStates;

        private readonly List<SecuritySystemCapabilities> _capabilityList = new List<SecuritySystemCapabilities>
        {
            SecuritySystemCapabilities.DirectControl,
            SecuritySystemCapabilities.KeypadEmulation,
            SecuritySystemCapabilities.Zones
        };


        //Constructor
        public SecuritySystemDriverIP()
            : base()
        {
            var state = new List<SecuritySystemState>
            {
                SecuritySystemState.ArmedAway,
                SecuritySystemState.ArmedStay,
                SecuritySystemState.Disarmed
            };
            _availableStates = state.AsReadOnly();

            var alarmType = new List<SecuritySystemAlarmType>
            {
                SecuritySystemAlarmType.Fire,
                SecuritySystemAlarmType.Alarm,
                SecuritySystemAlarmType.Burglary
            };
            _availableAlarms = alarmType.AsReadOnly();

            var areaCommand = new List<SecuritySystemAreaCommand>
            {
                new SecuritySystemAreaCommand(1, SecuritySystemCommandType.Disarm, true),
                new SecuritySystemAreaCommand(2, SecuritySystemCommandType.Away, true),
                new SecuritySystemAreaCommand(3, SecuritySystemCommandType.Stay, true)
            };
            _availableAreaCommands = areaCommand.AsReadOnly();

            _securitySystemKeypad = new SecuritySystemKeypad();
        }

        void ITcp.Initialize(IPAddress ipAddress, int port)
        {
            Initialize(ipAddress, port);
        }

        public void Initialize(IPAddress ipAddress, int port)
        {
            _tcpTransport = new SampleTransport
            {
                EnableAutoReconnect = EnableAutoReconnect,
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger,
                EnableRxDebug = InternalEnableRxDebug,
                EnableTxDebug = InternalEnableTxDebug
            };

            ConnectionTransport = _tcpTransport;

            _securitySystemProtocol = new SecuritySystemProtocol(this, ConnectionTransport, Id)
            {
                EnableLogging = true,
                CustomLogger = InternalCustomLogger,
                EnableStackTrace = true
            };

            _securitySystemProtocol.ConfigureConnection(ipAddress.ToString(), port);

            DeviceProtocol = _securitySystemProtocol;
            SubscribeToProtocolEvents();

            _tcpTransport.LogTxAndRxAsBytes = true;
            InitializeKeypad();
        }

        public event EventHandler<ListChangedEventArgs<SecuritySystemAlarmType>> SecuritysystemAlarmStateChangedEvent;
        public event EventHandler<ListChangedEventArgs<ISecuritySystemArea>> SecuritySystemAreaListChanged;
        public event EventHandler<SecuritySystemCommandResultEventArgs> SecuritySystemCommandResult;
        public event EventHandler<ListChangedEventArgs<SecuritySystemError>> SecuritySystemErrorChanged;
        public event EventHandler<ListChangedEventArgs<SecuritySystemState>> SecuritysystemStateChangedEvent;
        public event EventHandler<ListChangedEventArgs<ISecuritySystemZone>> SecuritySystemZoneListChanged;





        public IEnumerable<SecuritySystemError> GetActiveErrors()
        {
            return _currentlyActiveErrors;
        }
        public IEnumerable<ISecuritySystemZone> GetAllZones()
        {
            if (_securitySystemProtocol == null)
            {
                return null;
            }

            return _securitySystemProtocol.Zones;
        }
        public ISecuritySystemArea GetArea(int index)
        {
            if (_securitySystemProtocol == null)
            {
                return null;
            }

            foreach (var area in _securitySystemProtocol.GetVisibleAreas())
            {
                if (area.Index == index)
                {
                    return area;
                }
            }

            return null;
        }
        public IEnumerable<ISecuritySystemArea> GetAreas()
        {
            if (_securitySystemProtocol == null)
            {
                return null;
            }

            return _securitySystemProtocol.GetVisibleAreas();
        }
        public IEnumerable<SecuritySystemAlarmType> GetCurrentSystemAlarms()
        {
            return _currentlyActiveAlarms;
        }
        public IEnumerable<SecuritySystemState> GetCurrentSystemStates()
        {
            return _currentlyActiveStates;
        }
        public IEmulatedSecuritySystemKeypad GetEmulatedKeypad()
        {
            return _securitySystemKeypad;
        }



        public ReadOnlyCollection<SecuritySystemAreaCommand> GetAvailableAreaCommands()
        {
            return _availableAreaCommands;
        }
        public ReadOnlyCollection<SecuritySystemAlarmType> GetAvailableSystemAlarms()
        {
            return _availableAlarms;
        }
        public ReadOnlyCollection<SecuritySystemState> GetAvailableSystemStates()
        {
            return _availableStates;
        }
        public ReadOnlyCollection<SecuritySystemCapabilities> GetCapabilities()
        {
            return new ReadOnlyCollection<SecuritySystemCapabilities>(_capabilityList);
        }
        public SecuritySystemOperationalResult SendAreaCommand(List<int> areaIndexes, int commandIndex, string password)
        {
            if (_securitySystemProtocol == null)
            {
                return new SecuritySystemOperationalResult(1)
                {
                    Result = SecuritySystemOperationalResultCode.InvalidIdParameters
                };
            }

            return _securitySystemProtocol.ExecuteSecurityCommands(areaIndexes, commandIndex, password);
        }



        protected override JsonConverter CreateDeviceSupportConverter()
        {
            return new DeviceSupportConverter();
        }

        public override void ConvertJsonFileToDriverData(string jsonString)
        {
            var obj = JsonConvert.DeserializeObject<BaseRootObject>(jsonString, CreateSerializerSettings());

            try
            {
                base.Initialize(obj);
            }
            catch (Exception ex)
            {
                Log(string.Format("SecuritySystemDriverIP.ConvertJsonFileToDriverData Error: {0}", ex.Message));
            }
        }

        public override CType AbstractClassType
        {
            get { return GetType(); }
        }

        public override void Dispose()
        {
            if (_securitySystemProtocol != null)
            {
                _securitySystemProtocol.StateChange -= OnProtocolStateChanged;
                _securitySystemProtocol.AlarmChange -= OnProtocolAlarmChanged;
                _securitySystemProtocol.RxOut -= SendRxOut;
                _securitySystemProtocol.ConnectedChanged -= OnProtocolConnectionChanged;
                _securitySystemProtocol.AreaListChanged -= OnProtocolAreaListChanged;
                _securitySystemProtocol.ZoneListChanged -= OnProtocolZoneListChanged;
                _securitySystemProtocol.SystemCommandResult -= OnProtocolCommandResultChanged;
            }

            base.Dispose();
        }

        private void SubscribeToProtocolEvents()
        {
            _securitySystemProtocol.StateChange += OnProtocolStateChanged;
            _securitySystemProtocol.AlarmChange += OnProtocolAlarmChanged;
            _securitySystemProtocol.RxOut += SendRxOut;
            _securitySystemProtocol.ConnectedChanged += OnProtocolConnectionChanged;
            _securitySystemProtocol.AreaListChanged += OnProtocolAreaListChanged;
            _securitySystemProtocol.ZoneListChanged += OnProtocolZoneListChanged;
            _securitySystemProtocol.SystemCommandResult += OnProtocolCommandResultChanged;
        }

        private void InitializeKeypad()
{
    KeypadInstance = new SecuritySystemKeypad();
    KeypadInstance.Initialize(_securitySystemProtocol);

    _securitySystemKeypad = KeypadInstance;
}

        private void RaiseSystemStateEvent(SecuritySystemState eventType, bool updatedState)
        {
            ListChangedEventArgs<SecuritySystemState> e = null;

            if (updatedState)
            {
                if (!_currentlyActiveStates.Contains(eventType))
                {
                    int count = _currentlyActiveStates.Count;
                    _currentlyActiveStates.Add(eventType);
                    e = new ListChangedEventArgs<SecuritySystemState>(
                        ListChangedAction.Added,
                        SecuritySystemState.Unknown,
                        eventType,
                        count);
                }
            }
            else
            {
                if (_currentlyActiveStates.Contains(eventType))
                {
                    int index = _currentlyActiveStates.IndexOf(eventType);
                    if (index >= 0)
                    {
                        _currentlyActiveStates.Remove(eventType);
                        e = new ListChangedEventArgs<SecuritySystemState>(
                            ListChangedAction.Removed,
                            eventType,
                            SecuritySystemState.Unknown,
                            index);
                    }
                }
            }

            if (e != null)
            {
                var handler = SecuritysystemStateChangedEvent;
                if (handler != null)
                {
                    handler(this, e);
                }
            }
        }
        public void BypassZone(int areaIndex, int zoneIndex, string password)
        {
        }




        private void OnProtocolZoneListChanged(object sender, ListChangedEventArgs<ISecuritySystemZone> e)
        {
            var handler = SecuritySystemZoneListChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        private void OnProtocolStateChanged(object changedObject)
        {
            var newSecuritySystemState = changedObject as SecuritySystemStateArgs;
            if (newSecuritySystemState != null)
            {
                RaiseSystemStateEvent(newSecuritySystemState.EventType, newSecuritySystemState.State);
            }
        }
        private void OnProtocolAlarmChanged(object changedObject)
        {
            var obj = changedObject as SecuritySystemAlarmStateArgs;
            if (obj == null || obj.Alarm == null)
            {
                return;
            }

            var alarmType = obj.Alarm.AlarmType;
            ListChangedEventArgs<SecuritySystemAlarmType> e = null;

            if (obj.Alarm.AlarmActive)
            {
                if (!_currentlyActiveAlarms.Contains(alarmType))
                {
                    int count = _currentlyActiveAlarms.Count;
                    _currentlyActiveAlarms.Add(alarmType);
                    e = new ListChangedEventArgs<SecuritySystemAlarmType>(
                        ListChangedAction.Added,
                        SecuritySystemAlarmType.Unknown,
                        alarmType,
                        count);
                }
            }
            else
            {
                if (_currentlyActiveAlarms.Contains(alarmType))
                {
                    int index = _currentlyActiveAlarms.IndexOf(alarmType);
                    if (index >= 0)
                    {
                        _currentlyActiveAlarms.Remove(alarmType);
                        e = new ListChangedEventArgs<SecuritySystemAlarmType>(
                            ListChangedAction.Removed,
                            alarmType,
                            SecuritySystemAlarmType.Unknown,
                            index);
                    }
                }
            }

            if (e != null)
            {
                var handler = SecuritysystemAlarmStateChangedEvent;
                if (handler != null)
                {
                    handler(this, e);
                }
            }
        }
        private void OnProtocolAreaListChanged(object sender, ListChangedEventArgs<ISecuritySystemArea> listChangedEventArgs)
        {
            var handler = SecuritySystemAreaListChanged;
            if (handler != null)
            {
                handler(this, listChangedEventArgs);
            }
        }
        private void OnProtocolConnectionChanged(object driver, ValueEventArgs<bool> e)
        {
            if (e != null && e.Value != Connected)
            {
                Connected = e.Value;
                IsAuthenticated = true;
                Log(string.Format("ConnectedChanged - new state: {0}", e.Value));
            }
        }
        private void OnProtocolCommandResultChanged(object sender, SecuritySystemCommandResultEventArgs args)
        {
            var handler = SecuritySystemCommandResult;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }
}