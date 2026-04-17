namespace SecuritySystem_Elk_M1_IP_v1
{
    public static class ElkAreaStateDecoder
    {
        public static string Decode(char raw)
        {
            switch (raw)
            {
                case '0': return "Disarmed";
                case '1': return "Armed Away";
                case '2': return "Armed Stay";
                case '3': return "Armed Stay Instant";
                case '4': return "Armed Night";
                case '5': return "Armed Night Instant";
                case '6': return "Armed Vacation";
                case '7': return "Exit Delay";
                case '8': return "Entry Delay";
                case '9': return "Alarm";
                default: return "Unknown (" + raw + ")";
            }
        }

        public static bool IsArmed(char raw)
        {
            return raw == '1'
                || raw == '2'
                || raw == '3'
                || raw == '4'
                || raw == '5'
                || raw == '6'
                || raw == '7'
                || raw == '8'
                || raw == '9';
        }

        public static bool IsAlarm(char raw)
        {
            return raw == '9';
        }
    }
}