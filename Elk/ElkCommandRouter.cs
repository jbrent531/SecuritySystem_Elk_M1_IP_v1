using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkCommandRouter
    {
        private const int MaxAreas = 8;
        private const int MaxZones = 208;

        private readonly ElkSystemState _state;
        private readonly Dictionary<int, char> _lastLoggedZoneRawStatus = new Dictionary<int, char>();

        public ElkCommandRouter(ElkSystemState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            _state = state;
        }

        public void Handle(ElkPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }

            switch (packet.Command)
            {
                case "RP":
                    HandleRp(packet);
                    break;

                case "XK":
                    HandleXk(packet);
                    break;

                case "AS":
                    HandleAs(packet);
                    break;

                case "ZS":
                    HandleZs(packet);
                    break;

                case "ZD":
                    HandleZd(packet);
                    break;

                case "ZP":
                    HandleZp(packet);
                    break;

                case "ZC":
                    HandleZc(packet);
                    break;

                case "AM":
                    HandleAm(packet);
                    break;

                case "ZB":
                    HandleZb(packet);
                    break;

                case "SD":
                    HandleSd(packet);
                    break;

                case "KA":
                    HandleKa(packet);
                    break;

                case "KC":
                case "IC":
                case "LD":
                case "EE":
                    CrestronConsole.PrintLine("Ignoring " + packet.Command + ": " + packet.Data);
                    break;

                default:
                    CrestronConsole.PrintLine("UNHANDLED CMD: " + packet.Command + " DATA=" + packet.Data);
                    break;
            }
        }

        private void HandleRp(ElkPacket packet)
        {
        }

        private void HandleXk(ElkPacket packet)
        {
        }

        private void HandleAs(ElkPacket packet)
        {
            if (string.IsNullOrEmpty(packet.Data) || packet.Data.Length < 24)
            {
                return;
            }

            string armStates = packet.Data.Substring(0, 8);
            string armUpStates = packet.Data.Substring(8, 8);
            string alarmStates = packet.Data.Substring(16, 8);

            int delaySeconds = 0;
            if (packet.Data.Length >= 26)
            {
                int.TryParse(packet.Data.Substring(24, 2), System.Globalization.NumberStyles.HexNumber, null, out delaySeconds);
            }

            for (int i = 0; i < MaxAreas; i++)
            {
                ElkArea area = _state.GetOrCreateArea(i + 1);

                char oldArmState = area.RawArmState;
                char oldArmUpState = area.RawArmUpState;
                char oldAlarmState = area.RawAlarmState;
                int oldDelaySeconds = area.DelaySeconds;

                area.RawArmState = armStates[i];
                area.RawArmUpState = armUpStates[i];
                area.RawAlarmState = alarmStates[i];

                area.ArmStateText = ElkAreaStateDecoder.DecodeArmState(area.RawArmState);
                area.ArmUpStateText = ElkAreaStateDecoder.DecodeArmUpState(area.RawArmUpState);
                area.AlarmStateText = ElkAreaStateDecoder.DecodeAlarmState(area.RawAlarmState);

                area.IsArmed = ElkAreaStateDecoder.IsArmed(area.RawArmState);
                area.IsReady = ElkAreaStateDecoder.IsReady(area.RawArmUpState);
                area.CanForceArm = ElkAreaStateDecoder.CanForceArm(area.RawArmUpState);
                area.IsExitDelayActive = ElkAreaStateDecoder.IsExitDelayActive(area.RawArmUpState);
                area.IsFullyArmed = ElkAreaStateDecoder.IsFullyArmed(area.RawArmUpState);
                area.IsBypassedArmed = ElkAreaStateDecoder.IsBypassedArmed(area.RawArmUpState);
                area.IsEntryDelayActive = ElkAreaStateDecoder.IsEntryDelayActive(area.RawAlarmState);
                area.IsAlarm = ElkAreaStateDecoder.IsAlarm(area.RawAlarmState);
                area.DelaySeconds = delaySeconds;

                if (oldArmState == '\0' ||
                    oldArmState != area.RawArmState ||
                    oldArmUpState != area.RawArmUpState ||
                    oldAlarmState != area.RawAlarmState ||
                    oldDelaySeconds != area.DelaySeconds)
                {
                    _state.RaiseAreaChanged(area);
                }
            }

            _state.RecalculateDerivedState();
        }

        private void HandleZs(ElkPacket packet)
        {
            int zoneCount = Get208ArrayLength(packet.Data);
            if (zoneCount <= 0)
            {
                return;
            }

            for (int i = 0; i < zoneCount; i++)
            {
                int zoneNumber = i + 1;
                char rawStatus = packet.Data[i];

                ElkZone zone = _state.GetOrCreateZone(zoneNumber);
                char oldRaw = zone.RawStatus;

                ElkZoneMapper.ApplyStatus(zone, rawStatus);

                if (zone.IsConfigured && (oldRaw == '\0' || oldRaw != rawStatus))
                {
                    LogZoneChangeUpdate(zone);
                    _state.RaiseZoneChanged(zone);
                }
            }

            _state.RecalculateDerivedState();
        }

        private void HandleZd(ElkPacket packet)
        {
            int zoneCount = Get208ArrayLength(packet.Data);
            if (zoneCount <= 0)
            {
                return;
            }

            for (int i = 0; i < zoneCount; i++)
            {
                int zoneNumber = i + 1;
                char rawDef = packet.Data[i];

                ElkZone zone = _state.GetOrCreateZone(zoneNumber);
                zone.Definition = rawDef;
                zone.IsConfigured = rawDef != '0';
            }

            CrestronConsole.PrintLine("ZONE DEFINITIONS UPDATED.");
            _state.RecalculateDerivedState();
        }

        private void HandleZp(ElkPacket packet)
        {
            int zoneCount = Get208ArrayLength(packet.Data);
            if (zoneCount <= 0)
            {
                return;
            }

            for (int i = 0; i < zoneCount; i++)
            {
                int zoneNumber = i + 1;
                char partitionChar = packet.Data[i];

                ElkZone zone = _state.GetOrCreateZone(zoneNumber);
                int oldPartition = zone.Partition;

                if (partitionChar >= '1' && partitionChar <= '8')
                {
                    zone.Partition = partitionChar - '0';
                }
                else
                {
                    zone.Partition = 0;
                }

                if (oldPartition != zone.Partition && zone.IsConfigured)
                {
                    _state.RaiseZoneChanged(zone);
                }
            }
        }

        private void HandleZc(ElkPacket packet)
        {
            if (string.IsNullOrWhiteSpace(packet.Data) || packet.Data.Length < 4)
            {
                return;
            }

            string zoneText = packet.Data.Substring(0, 3);
            char rawStatus = packet.Data[3];

            int zoneNumber;
            if (!int.TryParse(zoneText, out zoneNumber))
            {
                return;
            }

            ElkZone zone = _state.GetOrCreateZone(zoneNumber);
            char oldRaw = zone.RawStatus;

            ElkZoneMapper.ApplyStatus(zone, rawStatus);

            if (zone.IsConfigured)
            {
                LogZoneChangeUpdate(zone);

                if (oldRaw != rawStatus)
                {
                    _state.RaiseZoneChanged(zone);
                }
            }

            _state.RecalculateDerivedState();
        }

        private void HandleAm(ElkPacket packet)
        {
        }

        private void HandleZb(ElkPacket packet)
        {
            if (string.IsNullOrWhiteSpace(packet.Data) || packet.Data.Length < 4)
            {
                return;
            }

            string zoneText = packet.Data.Substring(0, 3);
            char bypassFlag = packet.Data[3];

            int zoneNumber;
            if (!int.TryParse(zoneText, out zoneNumber))
            {
                return;
            }

            ElkZone zone = _state.GetOrCreateZone(zoneNumber);
            bool oldBypassed = zone.IsBypassed;

            zone.IsBypassed = bypassFlag != '0';

            if (oldBypassed != zone.IsBypassed)
            {
                CrestronConsole.PrintLine("ZONE BYPASS CHANGED: " + zone.Number.ToString("D3") + " -> " + zone.IsBypassed);
                _state.RaiseZoneBypassChanged(zone);
            }

            _state.RecalculateDerivedState();
        }

        private void HandleSd(ElkPacket packet)
        {
            if (string.IsNullOrWhiteSpace(packet.Data) || packet.Data.Length < 21)
            {
                return;
            }

            string typeText = packet.Data.Substring(0, 2);
            string numberText = packet.Data.Substring(2, 3);
            string text = packet.Data.Substring(5, 16);

            int type;
            if (!int.TryParse(typeText, out type))
            {
                return;
            }

            int number;
            if (!int.TryParse(numberText, out number))
            {
                return;
            }

            char[] chars = text.ToCharArray();
            if (chars.Length > 0)
            {
                chars[0] = (char)(chars[0] & 0x7F);
            }

            string name = new string(chars).TrimEnd();

            if (type == 0)
            {
                ElkZone zone = _state.GetOrCreateZone(number);
                string oldName = zone.Name;

                ElkZoneNameMapper.ApplyName(zone, name);

                if (!string.Equals(oldName, zone.Name, StringComparison.Ordinal))
                {
                    _state.RaiseZoneNameChanged(zone);
                }

                return;
            }

            if (type == 1)
            {
                ElkArea area = _state.GetOrCreateArea(number);
                string oldName = area.Name;
                area.Name = name;

                if (!string.Equals(oldName, area.Name, StringComparison.Ordinal))
                {
                    _state.RaiseAreaChanged(area);
                }
            }
        }

        private int Get208ArrayLength(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return 0;
            }

            if (data.Length >= MaxZones + 2)
            {
                return MaxZones;
            }

            return data.Length > MaxZones ? MaxZones : data.Length;
        }

        private void LogZoneChangeUpdate(ElkZone zone)
        {
            char lastRaw;
            if (_lastLoggedZoneRawStatus.TryGetValue(zone.Number, out lastRaw) && lastRaw == zone.RawStatus)
            {
                return;
            }

            _lastLoggedZoneRawStatus[zone.Number] = zone.RawStatus;
            CrestronConsole.PrintLine("ZONE CHANGE UPDATE: " + zone);
        }

        private void HandleKa(ElkPacket packet)
        {
            if (string.IsNullOrEmpty(packet.Data))
            {
                return;
            }

            int count = packet.Data.Length >= 16 ? 16 : packet.Data.Length;

            for (int i = 0; i < count; i++)
            {
                int keypadNumber = i + 1;
                char raw = packet.Data[i];

                int areaNumber = 0;
                if (raw >= '1' && raw <= '8')
                {
                    areaNumber = raw - '0';
                }

                int oldArea;
                if (!_state.KeypadAreas.TryGetValue(keypadNumber, out oldArea) || oldArea != areaNumber)
                {
                    _state.KeypadAreas[keypadNumber] = areaNumber;
                    _state.RaiseKeypadAreaChanged(keypadNumber, areaNumber);
                }
                else
                {
                    _state.KeypadAreas[keypadNumber] = areaNumber;
                }
            }
        }
    }
}
