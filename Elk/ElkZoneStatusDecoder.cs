using Crestron.SimplSharp;

public static class ElkZoneStatusDecoder
    {
        public static string Decode(char raw)
        {
            switch (raw)
            {
                case '0': return "Unused/Unknown";
                case '1': return "EOL";
                case '2': return "Short";
                case '3': return "Closed";
                case '4': return "Open EOL";
                case '5': return "Open Short";
                case '6': return "Trouble";
                case '7': return "Violated";
                case '8': return "Bypassed";
                case '9': return "Open";
                case 'A': return "Alarm";
                case 'B': return "Trouble";
                case 'C': return "Bypassed Trouble";
                case 'D': return "Bypassed";
                case 'E': return "Bypassed Open";
                case 'F': return "Bypassed Trouble";
                default: return "Unknown (" + raw + ")";
            }
        }

    public static bool IsFaultLikeTrouble(char raw)
    {
        return raw == '6'
            || raw == 'B'
            || raw == 'C'
            || raw == 'F';
    }

    public static bool IsOpen(char raw)
    {
        return raw == '9'
            || raw == '4'
            || raw == '5'
            || raw == 'E';
    }

    public static bool IsClosed(char raw)
        {
            return raw == '3' || raw == 'D';
        }

        public static bool IsBypassed(char raw)
        {
            return raw == '8'
                || raw == 'C'
                || raw == 'D'
                || raw == 'E'
                || raw == 'F';
        }

        public static bool IsTrouble(char raw)
        {
            return raw == '6'
                || raw == 'B'
                || raw == 'C'
                || raw == 'F';
        }

        public static bool IsViolated(char raw)
        {
            return raw == '7'
                || raw == 'A';
        }
}