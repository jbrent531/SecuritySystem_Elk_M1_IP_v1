using System;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public static class ElkSystemTroubleDecoder
    {
        public static ElkSystemTroubleState Decode(string data)
        {
            ElkSystemTroubleState state = new ElkSystemTroubleState();

            if (string.IsNullOrEmpty(data))
            {
                return state;
            }

            if (data.Length < 32)
            {
                return state;
            }

            for (int i = 0; i < 32; i++)
            {
                state.RawFields[i] = data[i];
            }

            state.AcFail = IsTroubleFlag(state.RawFields[0]);
            state.BoxTamperZone = DecodeAsciiOffsetNumber(state.RawFields[1]);
            state.FailToCommunicate = IsTroubleFlag(state.RawFields[2]);
            state.EepromMemoryError = IsTroubleFlag(state.RawFields[3]);
            state.LowBatteryControl = IsTroubleFlag(state.RawFields[4]);
            state.TransmitterLowBatteryZone = DecodeAsciiOffsetNumber(state.RawFields[5]);
            state.OverCurrent = IsTroubleFlag(state.RawFields[6]);
            state.TelephoneFault = IsTroubleFlag(state.RawFields[7]);

            state.Output2Trouble = IsTroubleFlag(state.RawFields[9]);
            state.MissingKeypadNumber = DecodeAsciiOffsetNumber(state.RawFields[10]);
            state.ZoneExpanderTrouble = IsTroubleFlag(state.RawFields[11]);
            state.OutputExpanderTrouble = IsTroubleFlag(state.RawFields[12]);
            state.ElkRpRemoteAccessTrouble = IsTroubleFlag(state.RawFields[14]);
            state.CommonAreaNotArmed = IsTroubleFlag(state.RawFields[16]);
            state.FlashMemoryError = IsTroubleFlag(state.RawFields[17]);
            state.SecurityAlertZone = DecodeAsciiOffsetNumber(state.RawFields[18]);
            state.SerialPortExpanderNumber = DecodeAsciiOffsetNumber(state.RawFields[19]);
            state.LostTransmitterZone = DecodeAsciiOffsetNumber(state.RawFields[20]);
            state.GeSmokeCleanMe = IsTroubleFlag(state.RawFields[21]);
            state.EthernetTrouble = IsTroubleFlag(state.RawFields[22]);

            state.KeypadLine1 = data.Length >= 48 ? data.Substring(32, 16).TrimEnd() : string.Empty;
            state.KeypadLine2 = data.Length >= 64 ? data.Substring(48, 16).TrimEnd() : string.Empty;

            if (data.Length >= 65)
            {
                state.FireTroubleZone = DecodeAsciiOffsetNumber(data[64]);
            }

            return state;
        }

        public static bool IsTroubleFlag(char raw)
        {
            return raw != '0';
        }

        public static int DecodeAsciiOffsetNumber(char raw)
        {
            if (raw <= '0')
            {
                return 0;
            }

            return raw - '0';
        }
    }
}
