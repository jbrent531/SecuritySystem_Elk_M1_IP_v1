using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkCommandRouter
    {
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

             //CrestronConsole.PrintLine("ELK RX CMD=" + packet.Command + " DATA=" + packet.Data);


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

                case "ZC":
                    HandleZc(packet);
                    break;

                case "AM":
                    HandleAm(packet);
                    break;

                case "NZ":
                    HandleNz(packet);
                    break;

                case "ZB":
                    HandleZb(packet);
                    break;

                case "SD":
                    HandleSd(packet);
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
            if (string.IsNullOrEmpty(packet.Data))
            {
                return;
            }

            int maxAreas = packet.Data.Length;
            if (maxAreas > 8)
            {
                maxAreas = 8;
            }

            for (int i = 0; i < maxAreas; i++)
            {
                int areaNumber = i + 1;
                char raw = packet.Data[i];

                var area = _state.GetOrCreateArea(areaNumber);
                char oldRaw = area.RawArmState;

                area.RawArmState = raw;
                area.ArmStateText = ElkAreaStateDecoder.Decode(raw);
                area.IsArmed = ElkAreaStateDecoder.IsArmed(raw);
                area.IsAlarm = ElkAreaStateDecoder.IsAlarm(raw);

                if (oldRaw != '\0' && oldRaw != raw)
                {
                    Console.WriteLine("AREA CHANGED: " + area);
                    _state.RaiseAreaChanged(area);
                }
                else if (oldRaw == '\0')
                {
                    _state.RaiseAreaChanged(area);
                }
            }

            _state.RecalculateDerivedState();
        }

        private void HandleZs(ElkPacket packet)
        {
             //CrestronConsole.PrintLine("HANDLE ZS RAW DATA=" + packet.Data);

            for (int i = 0; i < packet.Data.Length; i++)
            {
                int zoneNumber = i + 1;
                char rawStatus = packet.Data[i];

                var zone = _state.GetOrCreateZone(zoneNumber);
                char oldRaw = zone.RawStatus;

                ElkZoneMapper.ApplyStatus(zone, rawStatus);

                /*
                 CrestronConsole.PrintLine(
                    "ZS APPLY zone=" + zoneNumber +
                    " raw=" + rawStatus +
                    " text=" + ElkZoneStatusDecoder.Decode(rawStatus) +
                    " open=" + zone.IsOpen +
                    " violated=" + zone.IsViolated +
                    " bypassed=" + zone.IsBypassed +
                    " configured=" + zone.IsConfigured);
                */

                if (zone.IsConfigured && oldRaw != '\0' && oldRaw != rawStatus)
                {
                    LogZoneChangeUpdate(zone);
                    _state.RaiseZoneChanged(zone);
                }
                else if (zone.IsConfigured && oldRaw == '\0')
                {
                    LogZoneChangeUpdate(zone);
                    _state.RaiseZoneChanged(zone);
                }
            }

            _state.RecalculateDerivedState();
        }

        private void HandleZd(ElkPacket packet)
        {
            for (int i = 0; i < packet.Data.Length; i++)
            {
                int zoneNumber = i + 1;
                char rawDef = packet.Data[i];

                var zone = _state.GetOrCreateZone(zoneNumber);
                zone.Definition = rawDef;
                zone.IsConfigured = rawDef != '0';
            }

             CrestronConsole.PrintLine("ZONE DEFINITIONS UPDATED.");
            _state.RecalculateDerivedState();
        }

        private void HandleZc(ElkPacket packet)
        {
            //CrestronConsole.PrintLine("HANDLE ZC RAW DATA=" + packet.Data);

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

            var zone = _state.GetOrCreateZone(zoneNumber);
            char oldRaw = zone.RawStatus;

            ElkZoneMapper.ApplyStatus(zone, rawStatus);

            CrestronConsole.PrintLine(
                "ZC APPLY zone=" + zoneNumber +
                " raw=" + rawStatus +
                " text=" + ElkZoneStatusDecoder.Decode(rawStatus) +
                " open=" + zone.IsOpen +
                " violated=" + zone.IsViolated +
                " bypassed=" + zone.IsBypassed +
                " configured=" + zone.IsConfigured);

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

        private void HandleNz(ElkPacket packet)
        {
            if (string.IsNullOrWhiteSpace(packet.Data) || packet.Data.Length < 3)
            {
                return;
            }

            string zoneText = packet.Data.Substring(0, 3);
            string name = packet.Data.Substring(3).Trim();

            int zoneNumber;
            if (!int.TryParse(zoneText, out zoneNumber))
            {
                return;
            }

            var zone = _state.GetOrCreateZone(zoneNumber);
            string oldName = zone.Name;

            ElkZoneNameMapper.ApplyName(zone, name);

            if (!string.Equals(oldName, zone.Name, StringComparison.Ordinal))
            {
                CrestronConsole.PrintLine("ZONE NAME UPDATED: " + zone.Number.ToString("D3") + " -> [" + zone.Name + "]");
                _state.RaiseZoneNameChanged(zone);
            }
        }

        private void HandleZb(ElkPacket packet)
        {
             //CrestronConsole.PrintLine("ZB RECEIVED: DATA=" + packet.Data);

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

            var zone = _state.GetOrCreateZone(zoneNumber);
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
    }
}