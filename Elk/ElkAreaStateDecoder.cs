namespace SecuritySystem_Elk_M1_IP_v1
{
    public static class ElkAreaStateDecoder
    {

        // Decodes the AS arm-up and alarm bytes and exposes convenience checks the rest of the driver uses.
        public static string DecodeArmState(char raw)
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
                default: return "Unknown (" + raw + ")";
            }
        }

        public static string DecodeArmUpState(char raw)
        {
            switch (raw)
            {
                case '0': return "Not Ready To Arm";
                case '1': return "Ready To Arm";
                case '2': return "Ready To Arm (Force Arm Available)";
                case '3': return "Armed With Exit Timer";
                case '4': return "Armed Fully";
                case '5': return "Force Armed With Violated Zone";
                case '6': return "Armed With Bypass";
                default: return "Unknown (" + raw + ")";
            }
        }

        public static string DecodeAlarmState(char raw)
        {
            switch (raw)
            {
                case '0': return "No Alarm Active";
                case '1': return "Entry Delay Active";
                case '2': return "Alarm Abort Delay Active";
                case '3': return "Fire Alarm";
                case '4': return "Medical Alarm";
                case '5': return "Police Alarm";
                case '6': return "Burglar Alarm";
                case '7': return "Aux 1 Alarm";
                case '8': return "Aux 2 Alarm";
                case '9': return "Aux 3 Alarm";
                case ':': return "Aux 4 Alarm";
                case ';': return "Carbon Monoxide Alarm";
                case '<': return "Emergency Alarm";
                case '=': return "Freeze Alarm";
                case '>': return "Gas Alarm";
                case '?': return "Heat Alarm";
                case '@': return "Water Alarm";
                case 'A': return "Fire Supervisory";
                case 'B': return "Verify Fire";
                default: return "Unknown (" + raw + ")";
            }
        }

        public static bool IsArmed(char raw)
        {
            return raw >= '1' && raw <= '6';
        }

        public static bool IsReady(char raw)
        {
            return raw == '1' || raw == '2';
        }

        public static bool CanForceArm(char raw)
        {
            return raw == '2';
        }

        public static bool IsExitDelayActive(char raw)
        {
            return raw == '3';
        }

        public static bool IsFullyArmed(char raw)
        {
            return raw == '4';
        }

        public static bool IsBypassedArmed(char raw)
        {
            return raw == '6';
        }

        public static bool IsEntryDelayActive(char raw)
        {
            return raw == '1';
        }

        public static bool IsAlarm(char raw)
        {
            return raw != '0' && raw != '1';
        }
    }
}