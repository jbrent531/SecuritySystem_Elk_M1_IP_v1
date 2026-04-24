namespace SecuritySystem_Elk_M1_IP_v1
{

    // Decodes keypad-related values returned by KC and KF messages.
    // This includes function key LEDs, area chime modes, and keypad beep/chime indicators.
    public static class ElkKeypadDecoder
    {
        public static string DecodeKeyNumber(int keyNumber)
        {
            switch (keyNumber)
            {
                case 0: return "None";
                case 11: return "*";
                case 12: return "#";
                case 13: return "F1";
                case 14: return "F2";
                case 15: return "F3";
                case 16: return "F4";
                case 17: return "Stay";
                case 18: return "Exit";
                case 19: return "Chime";
                case 20: return "Bypass";
                case 21: return "Elk";
                case 22: return "Down";
                case 23: return "Up";
                case 24: return "Right";
                case 25: return "Left";
                case 26: return "F6";
                case 27: return "F5";
                case 28: return "DataKeyMode";
                default: return "Unknown (" + keyNumber + ")";
            }
        }

        public static string DecodeFunctionLedState(char raw)
        {
            switch (raw)
            {
                case '0': return "Off";
                case '1': return "On";
                case '2': return "Blinking";
                default: return "Unknown (" + raw + ")";
            }
        }

        public static bool IsFunctionLedOn(char raw)
        {
            return raw == '1' || raw == '2';
        }

        public static bool IsFunctionLedBlinking(char raw)
        {
            return raw == '2';
        }

        public static string DecodeAreaChimeMode(char raw)
        {
            switch (raw)
            {
                case '0': return "Off";
                case '1': return "Chime";
                case '2': return "Voice";
                case '3': return "Chime+Voice";
                default: return "Unknown (" + raw + ")";
            }
        }

        public static bool IsAreaChimeEnabled(char raw)
        {
            return raw == '1' || raw == '3';
        }

        public static bool HasSingleBeep(char raw)
        {
            return (raw & 0x0F) == 0x01;
        }

        public static bool HasConstantBeep(char raw)
        {
            return (raw & 0x0F) == 0x02;
        }

        public static bool HasChimePulse(char raw)
        {
            return (raw & 0x0F) == 0x04;
        }
    }
}