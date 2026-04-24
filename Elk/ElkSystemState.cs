using System;
using System.Collections.Generic;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkSystemState
    {
        public Dictionary<int, ElkZone> Zones { get; private set; }
        public Dictionary<int, ElkArea> Areas { get; private set; }
        public Dictionary<int, int> KeypadAreas { get; private set; }
        public Dictionary<int, ElkKeypad> Keypads { get; private set; }
        public ElkSystemTroubleState SystemTrouble { get; private set; }
        public ElkUserCodeEvent LastUserCodeEvent { get; private set; }

        public bool IsSystemReady { get; private set; }
        public bool IsAlarmActive { get; private set; }

        public event Action<ElkZone> ZoneChanged;
        public event Action<ElkZone> ZoneNameChanged;
        public event Action<ElkZone> ZoneBypassChanged;
        public event Action<ElkArea> AreaChanged;
        public event Action<bool> SystemReadyChanged;
        public event Action<bool> AlarmActiveChanged;
        public event Action<int, int> KeypadAreaChanged;
        public event Action<ElkKeypad> KeypadChanged;
        public event Action<ElkSystemTroubleState> SystemTroubleChanged;
        public event Action<ElkUserCodeEvent> UserCodeEventReceived;

        public ElkSystemState()
        {
            Zones = new Dictionary<int, ElkZone>();
            Areas = new Dictionary<int, ElkArea>();
            KeypadAreas = new Dictionary<int, int>();
            Keypads = new Dictionary<int, ElkKeypad>();
            SystemTrouble = new ElkSystemTroubleState();
            LastUserCodeEvent = new ElkUserCodeEvent();
        }

        public void SetLastUserCodeEvent(ElkUserCodeEvent userCodeEvent)
        {
            LastUserCodeEvent = userCodeEvent ?? new ElkUserCodeEvent();

            var handler = UserCodeEventReceived;
            if (handler != null)
            {
                handler(LastUserCodeEvent);
            }
        }


        public ElkZone GetOrCreateZone(int zoneNumber)
        {
            ElkZone zone;
            if (!Zones.TryGetValue(zoneNumber, out zone))
            {
                zone = new ElkZone
                {
                    Number = zoneNumber,
                    Name = string.Empty,
                    RawStatus = '\0',
                    StatusText = string.Empty,
                    IsOpen = false,
                    IsTrouble = false,
                    IsViolated = false,
                    IsBypassed = false,
                    IsFaulted = false,
                    NormalPhysicalState = 0,
                    Partition = 0,
                    Definition = '\0',
                    IsConfigured = false,
                    DefinitionText = string.Empty,
                    ZoneTypeText = string.Empty
                };

                Zones[zoneNumber] = zone;
            }

            return zone;
        }

        public ElkArea GetOrCreateArea(int areaNumber)
        {
            ElkArea area;
            if (!Areas.TryGetValue(areaNumber, out area))
            {
                area = new ElkArea
                {
                    Number = areaNumber,
                    Name = string.Empty,
                    RawArmState = '\0',
                    RawArmUpState = '\0',
                    RawAlarmState = '\0',
                    ArmStateText = string.Empty,
                    ArmUpStateText = string.Empty,
                    AlarmStateText = string.Empty,
                    IsArmed = false,
                    IsReady = false,
                    CanForceArm = false,
                    IsExitDelayActive = false,
                    IsFullyArmed = false,
                    IsBypassedArmed = false,
                    IsAlarm = false,
                    IsEntryDelayActive = false,
                    DelaySeconds = 0,
                    ChimeModeRaw = '0',
                    ChimeModeText = "Off",
                    IsChimeEnabled = false
                };

                Areas[areaNumber] = area;
            }

            return area;
        }

        public ElkKeypad GetOrCreateKeypad(int keypadNumber)
        {
            ElkKeypad keypad;
            if (!Keypads.TryGetValue(keypadNumber, out keypad))
            {
                keypad = new ElkKeypad
                {
                    Number = keypadNumber,
                    LastKeyNumber = 0,
                    LastKeyText = "None"
                };

                Keypads[keypadNumber] = keypad;
            }

            return keypad;
        }

        public void RaiseZoneChanged(ElkZone zone)
        {
            var handler = ZoneChanged;
            if (handler != null)
            {
                handler(zone);
            }
        }

        public void RaiseZoneNameChanged(ElkZone zone)
        {
            var handler = ZoneNameChanged;
            if (handler != null)
            {
                handler(zone);
            }
        }

        public void RaiseZoneBypassChanged(ElkZone zone)
        {
            var handler = ZoneBypassChanged;
            if (handler != null)
            {
                handler(zone);
            }
        }

        public void RaiseAreaChanged(ElkArea area)
        {
            var handler = AreaChanged;
            if (handler != null)
            {
                handler(area);
            }
        }

        public void RaiseKeypadAreaChanged(int keypadNumber, int areaNumber)
        {
            var handler = KeypadAreaChanged;
            if (handler != null)
            {
                handler(keypadNumber, areaNumber);
            }
        }

        public void RaiseKeypadChanged(ElkKeypad keypad)
        {
            var handler = KeypadChanged;
            if (handler != null)
            {
                handler(keypad);
            }
        }

        public void SetSystemTrouble(ElkSystemTroubleState state)
        {
            SystemTrouble = state ?? new ElkSystemTroubleState();

            var handler = SystemTroubleChanged;
            if (handler != null)
            {
                handler(SystemTrouble);
            }
        }

        public void RecalculateDerivedState()
        {
            bool ready = true;

            foreach (var zone in Zones.Values)
            {
                if (!zone.IsConfigured || zone.IsBypassed)
                {
                    continue;
                }

                if (zone.IsViolated || zone.IsTrouble)
                {
                    ready = false;
                    break;
                }
            }

            bool alarm = false;

            foreach (var area in Areas.Values)
            {
                if (area.IsAlarm)
                {
                    alarm = true;
                    break;
                }
            }

            if (ready != IsSystemReady)
            {
                IsSystemReady = ready;
                var readyHandler = SystemReadyChanged;
                if (readyHandler != null)
                {
                    readyHandler(IsSystemReady);
                }
            }

            if (alarm != IsAlarmActive)
            {
                IsAlarmActive = alarm;
                var alarmHandler = AlarmActiveChanged;
                if (alarmHandler != null)
                {
                    alarmHandler(IsAlarmActive);
                }
            }
        }
    }
}