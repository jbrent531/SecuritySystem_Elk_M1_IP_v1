using System;
using System.Collections.Generic;
using Crestron.SimplSharp;


namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkSystemState
    {
        public Dictionary<int, ElkZone> Zones { get; private set; }
        public Dictionary<int, ElkArea> Areas { get; private set; }

        public bool IsSystemReady { get; private set; }
        public bool IsAlarmActive { get; private set; }

        public event Action<ElkZone> ZoneChanged;
        public event Action<ElkZone> ZoneNameChanged;
        public event Action<ElkZone> ZoneBypassChanged;
        public event Action<ElkArea> AreaChanged;
        public event Action<bool> SystemReadyChanged;
        public event Action<bool> AlarmActiveChanged;

        public ElkSystemState()
        {
            Zones = new Dictionary<int, ElkZone>();
            Areas = new Dictionary<int, ElkArea>();
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
                    Definition = '\0',
                    IsConfigured = false,
                    IsBypassed = false
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
                    RawArmState = '\0',
                    ArmStateText = string.Empty,
                    IsArmed = false,
                    IsAlarm = false
                };

                Areas[areaNumber] = area;
            }

            return area;
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

        public void RecalculateDerivedState()
        {
            bool ready = true;

            foreach (var zone in Zones.Values)
            {
                bool isFaulted = zone.IsOpen || zone.IsViolated || zone.IsTrouble;

                if (zone.IsConfigured && isFaulted && !zone.IsBypassed)
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