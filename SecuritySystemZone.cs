using System;
using System.Collections.Generic;
using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Events;
using Crestron.RAD.Common.Interfaces;
using Crestron.RAD.DeviceTypes.SecuritySystem;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    /// <summary>
    /// This is used to define security system zone
    /// </summary>
    public class SecuritySystemZone : ISecuritySystemZone4
    {
        private readonly List<SecuritySystemZoneState> _activeZoneState;
        private readonly List<SecuritySystemZoneState> _supportedStates;
        private string _name;

        public int NormalPhysicalState { get; set; }
        public bool IsFaulted { get; set; }

        internal Action<int, int, string> BypassDelegate;
        internal Action<int, int, string> UnbypassDelegate;

        public SecuritySystemZone(string name, int index, int areaId)
        {
            Name = name;
            Index = index;
            AreaId = areaId;
            AreaIndex = areaId;

            _activeZoneState = new List<SecuritySystemZoneState>();
            _supportedStates = new List<SecuritySystemZoneState>
            {
                SecuritySystemZoneState.Bypassed,
                SecuritySystemZoneState.Faulted,
                SecuritySystemZoneState.LowBattery,
                SecuritySystemZoneState.Tamper,
                SecuritySystemZoneState.Ok
            };

            CrestronConsole.PrintLine("SecuritySystemZone : Ctor : Called ");
        }

        public bool SupportsPoll { get; private set; }

        public IEnumerable<SecuritySystemZoneState> GetSupportedStates()
        {
            return _supportedStates;
        }

        public IEnumerable<SecuritySystemZoneState> GetActiveStates()
        {
            return _activeZoneState;
        }

        public SecuritySystemOperationalResult BypassZone(string password)
        {
            CrestronConsole.PrintLine("SecuritySystemZone: BypassZone is called");

            if (string.IsNullOrEmpty(password))
            {
                return new SecuritySystemOperationalResult(0)
                {
                    Result = SecuritySystemOperationalResultCode.InvalidPasscode
                };
            }

            Action<int, int, string> del = BypassDelegate;
            if (del != null)
            {
                del(Index, AreaIndex, password);
            }

            return new SecuritySystemOperationalResult(1)
            {
                Result = SecuritySystemOperationalResultCode.Success
            };
        }

        public SecuritySystemOperationalResult UnbypassZone(string password)
        {
            CrestronConsole.PrintLine("SecuritySystemZone: UnbypassZone is called");

            if (string.IsNullOrEmpty(password))
            {
                return new SecuritySystemOperationalResult(0)
                {
                    Result = SecuritySystemOperationalResultCode.InvalidPasscode
                };
            }

            Action<int, int, string> del = UnbypassDelegate;
            if (del != null)
            {
                del(Index, AreaIndex, password);
            }

            return new SecuritySystemOperationalResult(1)
            {
                Result = SecuritySystemOperationalResultCode.Success
            };
        }

        public SecuritySystemOperationalResult Poll()
        {
            CrestronConsole.PrintLine("SecuritySystemZone: Poll is called");
            return new SecuritySystemOperationalResult(0);
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;

                EventHandler<ValueEventArgs<string>> handler = NameChanged;
                if (handler != null)
                {
                    handler(this, new ValueEventArgs<string>(_name));
                }
            }
        }

        public int Index { get; protected set; }
        public int AreaIndex { get; private set; }
        public int AreaId { get; private set; }
        public SecuritySystemZoneType Type { get; protected set; }

        public event EventHandler<ListChangedEventArgs<SecuritySystemZoneState>> SecuritySystemZoneStateChanged;
        public event EventHandler<ValueEventArgs<string>> NameChanged;

        public bool SupportsBypassZone
        {
            get { return true; }
        }

        public void SetAreaIndex(int areaIndex)
        {
            AreaIndex = areaIndex;
            AreaId = areaIndex;
        }

        public void ApplyElkState(string zoneName, bool isFaulted, bool isBypassed, char definition)
        {
            CrestronConsole.PrintLine(
                "ApplyElkState BEFORE name='" + zoneName +
                "' definition='" + definition +
                "' type=" + Type);

            Name = zoneName;

            ApplyElkDefinition(zoneName, definition);

            CrestronConsole.PrintLine(
                "ApplyElkState AFTER name='" + zoneName +
                "' definition='" + definition +
                "' type=" + Type);

            bool isOk = !isFaulted;

            UpdateActiveState(SecuritySystemZoneState.Faulted, isFaulted);
            UpdateActiveState(SecuritySystemZoneState.Bypassed, isBypassed);
            UpdateActiveState(SecuritySystemZoneState.Ok, isOk);
        }

        private void ApplyElkDefinition(string zoneName, char definition)
        {
            string name = (zoneName ?? string.Empty).ToLowerInvariant();

            switch (definition)
            {
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                    if (name.Contains("window"))
                    {
                        Type = SecuritySystemZoneType.Window;
                    }
                    else if (name.Contains("door"))
                    {
                        Type = SecuritySystemZoneType.Door;
                    }
                    else
                    {
                        Type = SecuritySystemZoneType.Door;
                    }
                    break;

                case 'E':
                case 'F':
                case 'G':
                case 'H':
                    Type = SecuritySystemZoneType.Motion;
                    break;

                case 'I':
                case 'J':
                case 'K':
                    Type = SecuritySystemZoneType.Other;
                    break;

                default:
                    if (name.Contains("window"))
                    {
                        Type = SecuritySystemZoneType.Window;
                    }
                    else if (name.Contains("door"))
                    {
                        Type = SecuritySystemZoneType.Door;
                    }
                    else if (name.Contains("motion"))
                    {
                        Type = SecuritySystemZoneType.Motion;
                    }
                    else if (name.Contains("smoke"))
                    {
                        Type = SecuritySystemZoneType.Other;
                    }
                    else
                    {
                        Type = SecuritySystemZoneType.Unknown;
                    }
                    break;
            }
        }

        private void UpdateActiveState(SecuritySystemZoneState state, bool active)
        {
            CrestronConsole.PrintLine(
                "SecuritySystemZone: UpdateActiveState zone={0} state={1} active={2}",
                Index,
                state,
                active);

            ListChangedEventArgs<SecuritySystemZoneState> e = null;

            if (active)
            {
                if (!_activeZoneState.Contains(state))
                {
                    int count = _activeZoneState.Count;
                    _activeZoneState.Add(state);
                    e = new ListChangedEventArgs<SecuritySystemZoneState>(
                        ListChangedAction.Added,
                        SecuritySystemZoneState.Unknown,
                        state,
                        count);
                }
            }
            else
            {
                if (_activeZoneState.Contains(state))
                {
                    int index = _activeZoneState.IndexOf(state);
                    if (index >= 0)
                    {
                        _activeZoneState.Remove(state);
                        e = new ListChangedEventArgs<SecuritySystemZoneState>(
                            ListChangedAction.Removed,
                            state,
                            SecuritySystemZoneState.Unknown,
                            index);
                    }
                }
            }

            if (e != null)
            {
                EventHandler<ListChangedEventArgs<SecuritySystemZoneState>> handler = SecuritySystemZoneStateChanged;
                if (handler != null)
                {
                    handler(this, e);
                }
            }
        }

        private SecuritySystemOperationalResult SetBypassState(string password, bool active, SecuritySystemZoneState state)
        {
            if (string.IsNullOrEmpty(password))
            {
                CrestronConsole.PrintLine("SecuritySystemZone : SetBypassState failed - no passcode provided.");
                SecuritySystemOperationalResult missingPassword = new SecuritySystemOperationalResult(0);
                missingPassword.Result = SecuritySystemOperationalResultCode.InvalidPasscode;
                return missingPassword;
            }

            UpdateActiveState(state, active);

            return new SecuritySystemOperationalResult(1)
            {
                Result = SecuritySystemOperationalResultCode.Success
            };
        }
    }
}