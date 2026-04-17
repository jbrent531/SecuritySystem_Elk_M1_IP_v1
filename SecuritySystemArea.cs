using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Events;
using Crestron.RAD.Common.Interfaces;
using Crestron.RAD.DeviceTypes.SecuritySystem;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public class SecuritySystemArea : ISecuritySystemArea, IDisposable
    {
        private readonly List<SecuritySystemAreaCommand> _availableAreaCommands;
        private readonly ReadOnlyCollection<SecuritySystemState> _supportedArmingStates;
        private readonly ReadOnlyCollection<SecuritySystemAlarmType> _supportedAlarmTypes;
        private readonly SecuritySystemProtocol _securitySystemProtocol;

        private readonly List<SecuritySystemState> _activeAreaStates;
        private readonly List<SecuritySystemAlarmType> _activeAreaAlarms;
        private readonly Dictionary<int, ISecuritySystemZone> _zonesInArea;

        private bool _disposed;
        private string _name;

        public SecuritySystemArea(
            string name,
            int index,
            ReadOnlyCollection<SecuritySystemState> supportedArmingStates,
            ReadOnlyCollection<SecuritySystemAlarmType> supportedAlarmTypes,
            ReadOnlyCollection<SecuritySystemAreaCommand> areaCommands,
            SecuritySystemProtocol protocol)
        {
            _activeAreaStates = new List<SecuritySystemState>();
            _activeAreaAlarms = new List<SecuritySystemAlarmType>();
            _supportedArmingStates = supportedArmingStates;
            _supportedAlarmTypes = supportedAlarmTypes;
            _availableAreaCommands = areaCommands.ToList();
            _securitySystemProtocol = protocol;
            _zonesInArea = new Dictionary<int, ISecuritySystemZone>();

            Index = index;
            Name = name;

            if (_securitySystemProtocol != null)
            {
                _securitySystemProtocol.ZoneListChanged += OnProtocolZoneListChanged;
                _securitySystemProtocol.KeypadChange += OnProtocolKeypadChanged;
                _securitySystemProtocol.KeypadAlarmChange += OnProtocolAlarmChanged;
            }

            EnsureAreaZonesLoaded();
        }

        public int Index { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaiseNameChangedEvent();
            }
        }

        public event EventHandler<ValueEventArgs<string>> NameChanged;
        public event EventHandler<ListChangedEventArgs<SecuritySystemAlarmType>> SecuritysystemAlarmStateChangedEvent;
        public event EventHandler<ListChangedEventArgs<SecuritySystemState>> SecuritysystemAreaStateChangedEvent;
        public event EventHandler<ListChangedEventArgs<ISecuritySystemZone>> ZoneListChangedEvent;

        public IEnumerable<SecuritySystemAlarmType> GetActiveAlarms()
        {
            return _activeAreaAlarms;
        }

        public IEnumerable<SecuritySystemState> GetActiveStates()
        {
            return _activeAreaStates;
        }

        public ReadOnlyCollection<SecuritySystemAlarmType> GetSupportedAlarmTypes()
        {
            return _supportedAlarmTypes;
        }

        public ReadOnlyCollection<SecuritySystemState> GetSupportedArmingStates()
        {
            return _supportedArmingStates;
        }

        public IEnumerable<KeyValuePair<int, ISecuritySystemZone>> GetZones()
        {
            EnsureAreaZonesLoaded();
            return _zonesInArea.OrderBy(x => x.Key);
        }

        public SecuritySystemOperationalResult SendAreaCommand(int commandIndex, string password)
        {
            SecuritySystemAreaCommand areaCommand = null;

            if (_availableAreaCommands != null)
            {
                foreach (SecuritySystemAreaCommand command in _availableAreaCommands)
                {
                    if (command.Index == commandIndex)
                    {
                        areaCommand = command;
                        break;
                    }
                }
            }

            if (areaCommand == null)
            {
                SecuritySystemOperationalResult invalid = new SecuritySystemOperationalResult(0)
                {
                    Result = SecuritySystemOperationalResultCode.InvalidIdParameters
                };
                return invalid;
            }

            if (areaCommand.PasswordRequired && string.IsNullOrWhiteSpace(password))
            {
                SecuritySystemOperationalResult missingPassword = new SecuritySystemOperationalResult(0)
                {
                    Result = SecuritySystemOperationalResultCode.InvalidPasscode
                };
                return missingPassword;
            }

            List<int> areaIndexes = new List<int> { Index };
            return _securitySystemProtocol.ExecuteSecurityCommands(areaIndexes, commandIndex, password);
        }

        private void EnsureAreaZonesLoaded()
        {
            if (_securitySystemProtocol == null || _securitySystemProtocol.Zones == null)
            {
                return;
            }

            foreach (ISecuritySystemZone zone in _securitySystemProtocol.Zones)
            {
                if (zone == null)
                {
                    continue;
                }

                SecuritySystemZone concreteZone = zone as SecuritySystemZone;
                if (concreteZone != null && concreteZone.AreaIndex != Index)
                {
                    continue;
                }

                if (!_zonesInArea.ContainsKey(zone.Index))
                {
                    _zonesInArea.Add(zone.Index, zone);
                }
            }
        }

        private void OnProtocolZoneListChanged(object sender, ListChangedEventArgs<ISecuritySystemZone> e)
        {
            if (e == null || e.ChangedAction != ListChangedAction.Added || e.NewItem == null)
            {
                return;
            }

            ISecuritySystemZone zone = e.NewItem;

            SecuritySystemZone concreteZone = zone as SecuritySystemZone;
            if (concreteZone != null && concreteZone.AreaIndex != Index)
            {
                return;
            }

            if (!_zonesInArea.ContainsKey(zone.Index))
            {
                _zonesInArea.Add(zone.Index, zone);
            }

            EventHandler<ListChangedEventArgs<ISecuritySystemZone>> handler = ZoneListChangedEvent;
            if (handler != null)
            {
                int addedIndex = GetZoneIndex(zone.Index);

                handler(this, new ListChangedEventArgs<ISecuritySystemZone>(
                    ListChangedAction.Added,
                    null,
                    zone,
                    addedIndex));
            }
        }

        private int GetZoneIndex(int zoneIndex)
        {
            int i = 0;
            foreach (KeyValuePair<int, ISecuritySystemZone> kvp in _zonesInArea.OrderBy(x => x.Key))
            {
                if (kvp.Key == zoneIndex)
                {
                    return i;
                }

                i++;
            }

            return 0;
        }

        private void OnProtocolKeypadChanged(object changedObject)
        {
            SecuritySystemStateArgs obj = changedObject as SecuritySystemStateArgs;
            if (obj == null)
            {
                return;
            }

            switch (obj.EventType)
            {
                case SecuritySystemState.ArmedAway:
                case SecuritySystemState.ArmedStay:
                case SecuritySystemState.Disarmed:
                    UpdateArmingState(obj.EventType, obj.State);
                    break;
            }
        }

        private void OnProtocolAlarmChanged(object changedObject)
        {
            SecuritySystemAlarmStateArgs obj = changedObject as SecuritySystemAlarmStateArgs;
            if (obj != null && obj.Alarm != null)
            {
                UpdateAlarmState(obj.Alarm.AlarmType, obj.State);
            }
        }

        private void UpdateArmingState(SecuritySystemState state, bool active)
        {
            ListChangedEventArgs<SecuritySystemState> e = null;

            if (active)
            {
                if (!_activeAreaStates.Contains(state))
                {
                    int count = _activeAreaStates.Count;
                    _activeAreaStates.Add(state);
                    e = new ListChangedEventArgs<SecuritySystemState>(
                        ListChangedAction.Added,
                        SecuritySystemState.Unknown,
                        state,
                        count);
                }
            }
            else
            {
                if (_activeAreaStates.Contains(state))
                {
                    int index = _activeAreaStates.IndexOf(state);
                    if (index >= 0)
                    {
                        _activeAreaStates.Remove(state);
                        e = new ListChangedEventArgs<SecuritySystemState>(
                            ListChangedAction.Removed,
                            state,
                            SecuritySystemState.Unknown,
                            index);
                    }
                }
            }

            if (e != null)
            {
                RaiseStateChangedEvent(e);
            }
        }

        private void UpdateAlarmState(SecuritySystemAlarmType type, bool active)
        {
            ListChangedEventArgs<SecuritySystemAlarmType> e = null;

            if (active)
            {
                if (!_activeAreaAlarms.Contains(type))
                {
                    int count = _activeAreaAlarms.Count;
                    _activeAreaAlarms.Add(type);
                    e = new ListChangedEventArgs<SecuritySystemAlarmType>(
                        ListChangedAction.Added,
                        SecuritySystemAlarmType.Unknown,
                        type,
                        count);
                }
            }
            else
            {
                if (_activeAreaAlarms.Contains(type))
                {
                    int index = _activeAreaAlarms.IndexOf(type);
                    if (index >= 0)
                    {
                        _activeAreaAlarms.Remove(type);
                        e = new ListChangedEventArgs<SecuritySystemAlarmType>(
                            ListChangedAction.Removed,
                            type,
                            SecuritySystemAlarmType.Unknown,
                            index);
                    }
                }
            }

            if (e != null)
            {
                RaiseAlarmStateChangedEvent(e);
            }
        }

        private void RaiseNameChangedEvent()
        {
            EventHandler<ValueEventArgs<string>> handler = NameChanged;
            if (handler != null)
            {
                handler(this, new ValueEventArgs<string>(_name));
            }
        }

        private void RaiseStateChangedEvent(ListChangedEventArgs<SecuritySystemState> e)
        {
            EventHandler<ListChangedEventArgs<SecuritySystemState>> handler = SecuritysystemAreaStateChangedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void RaiseAlarmStateChangedEvent(ListChangedEventArgs<SecuritySystemAlarmType> e)
        {
            EventHandler<ListChangedEventArgs<SecuritySystemAlarmType>> handler = SecuritysystemAlarmStateChangedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_securitySystemProtocol != null)
            {
                _securitySystemProtocol.ZoneListChanged -= OnProtocolZoneListChanged;
                _securitySystemProtocol.KeypadChange -= OnProtocolKeypadChanged;
                _securitySystemProtocol.KeypadAlarmChange -= OnProtocolAlarmChanged;
            }

            _disposed = true;
        }
    }
}