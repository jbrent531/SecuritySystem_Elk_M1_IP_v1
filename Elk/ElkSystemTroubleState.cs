using System;
using System.Collections.Generic;
using System.Linq;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkSystemTroubleState
    {
        public char[] RawFields { get; private set; }
        public string KeypadLine1 { get; set; }
        public string KeypadLine2 { get; set; }

        public bool AcFail { get; set; }
        public int BoxTamperZone { get; set; }
        public bool FailToCommunicate { get; set; }
        public bool EepromMemoryError { get; set; }
        public bool LowBatteryControl { get; set; }
        public int TransmitterLowBatteryZone { get; set; }
        public bool OverCurrent { get; set; }
        public bool TelephoneFault { get; set; }
        public bool Output2Trouble { get; set; }
        public int MissingKeypadNumber { get; set; }
        public bool ZoneExpanderTrouble { get; set; }
        public bool OutputExpanderTrouble { get; set; }
        public bool ElkRpRemoteAccessTrouble { get; set; }
        public bool CommonAreaNotArmed { get; set; }
        public bool FlashMemoryError { get; set; }
        public int SecurityAlertZone { get; set; }
        public int SerialPortExpanderNumber { get; set; }
        public int LostTransmitterZone { get; set; }
        public bool GeSmokeCleanMe { get; set; }
        public bool EthernetTrouble { get; set; }
        public int FireTroubleZone { get; set; }

        public ElkSystemTroubleState()
        {
            RawFields = new char[32];
            for (int i = 0; i < RawFields.Length; i++)
            {
                RawFields[i] = '0';
            }

            KeypadLine1 = string.Empty;
            KeypadLine2 = string.Empty;
        }

        public bool HasAnyTrouble
        {
            get
            {
                return AcFail ||
                       BoxTamperZone > 0 ||
                       FailToCommunicate ||
                       EepromMemoryError ||
                       LowBatteryControl ||
                       TransmitterLowBatteryZone > 0 ||
                       OverCurrent ||
                       TelephoneFault ||
                       Output2Trouble ||
                       MissingKeypadNumber > 0 ||
                       ZoneExpanderTrouble ||
                       OutputExpanderTrouble ||
                       ElkRpRemoteAccessTrouble ||
                       CommonAreaNotArmed ||
                       FlashMemoryError ||
                       SecurityAlertZone > 0 ||
                       SerialPortExpanderNumber > 0 ||
                       LostTransmitterZone > 0 ||
                       GeSmokeCleanMe ||
                       EthernetTrouble ||
                       FireTroubleZone > 0;
            }
        }

        public string Summary
        {
            get
            {
                List<string> parts = new List<string>();

                if (AcFail) parts.Add("AC Fail");
                if (BoxTamperZone > 0) parts.Add("Box Tamper Z" + BoxTamperZone);
                if (FailToCommunicate) parts.Add("Fail To Communicate");
                if (EepromMemoryError) parts.Add("EEPROM Error");
                if (LowBatteryControl) parts.Add("Control Low Battery");
                if (TransmitterLowBatteryZone > 0) parts.Add("Transmitter Low Battery Z" + TransmitterLowBatteryZone);
                if (OverCurrent) parts.Add("Over Current");
                if (TelephoneFault) parts.Add("Telephone Fault");
                if (Output2Trouble) parts.Add("Output 2 Trouble");
                if (MissingKeypadNumber > 0) parts.Add("Missing Keypad " + MissingKeypadNumber);
                if (ZoneExpanderTrouble) parts.Add("Zone Expander Trouble");
                if (OutputExpanderTrouble) parts.Add("Output Expander Trouble");
                if (ElkRpRemoteAccessTrouble) parts.Add("ELKRP Remote Access");
                if (CommonAreaNotArmed) parts.Add("Common Area Not Armed");
                if (FlashMemoryError) parts.Add("Flash Memory Error");
                if (SecurityAlertZone > 0) parts.Add("Security Alert Z" + SecurityAlertZone);
                if (SerialPortExpanderNumber > 0) parts.Add("Serial Port Expander " + SerialPortExpanderNumber);
                if (LostTransmitterZone > 0) parts.Add("Lost Transmitter Z" + LostTransmitterZone);
                if (GeSmokeCleanMe) parts.Add("GE Smoke CleanMe");
                if (EthernetTrouble) parts.Add("Ethernet Trouble");
                if (FireTroubleZone > 0) parts.Add("Fire Trouble Z" + FireTroubleZone);

                return parts.Count == 0 ? "No Trouble" : string.Join(", ", parts.ToArray());
            }
        }
    }
}