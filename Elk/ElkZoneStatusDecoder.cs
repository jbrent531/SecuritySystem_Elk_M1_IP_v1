namespace SecuritySystem_Elk_M1_IP_v1
{
    public static class ElkZoneStatusDecoder
    {
        public static string Decode(char raw)
        {
            switch (char.ToUpperInvariant(raw))
            {
                case '0': return "Normal Unconfigured";
                case '1': return "Normal Open";
                case '2': return "Normal EOL";
                case '3': return "Normal Short";
                case '4': return "Not Used";
                case '5': return "Trouble Open";
                case '6': return "Trouble EOL";
                case '7': return "Trouble Short";
                case '8': return "Not Used";
                case '9': return "Violated Open";
                case 'A': return "Violated EOL";
                case 'B': return "Violated Short";
                case 'C': return "Soft Bypassed";
                case 'D': return "Bypassed Open";
                case 'E': return "Bypassed EOL";
                case 'F': return "Bypassed Short";
                default: return "Unknown (" + raw + ")";
            }
        }

        public static bool IsNormal(char raw)
        {
            raw = char.ToUpperInvariant(raw);
            return raw == '1' || raw == '2' || raw == '3';
        }

        public static bool IsTrouble(char raw)
        {
            int nibble;
            if (!TryGetNibble(raw, out nibble))
            {
                return false;
            }

            return nibble >= 5 && nibble <= 7;
        }

        public static bool IsViolated(char raw)
        {
            int nibble;
            if (!TryGetNibble(raw, out nibble))
            {
                return false;
            }

            return nibble >= 9 && nibble <= 11;
        }

        public static bool IsBypassed(char raw)
        {
            int nibble;
            if (!TryGetNibble(raw, out nibble))
            {
                return false;
            }

            return nibble >= 12 && nibble <= 15;
        }

        public static bool IsOpen(char raw)
        {
            int nibble;
            if (!TryGetNibble(raw, out nibble))
            {
                return false;
            }

            int physical = nibble & 0x3;
            return physical == 1;
        }

        private static bool TryGetNibble(char raw, out int nibble)
        {
            raw = char.ToUpperInvariant(raw);

            if (raw >= '0' && raw <= '9')
            {
                nibble = raw - '0';
                return true;
            }

            if (raw >= 'A' && raw <= 'F')
            {
                nibble = 10 + (raw - 'A');
                return true;
            }

            nibble = 0;
            return false;
        }

        public static int GetPhysicalState(char raw)
        {
            raw = char.ToUpperInvariant(raw);

            switch (raw)
            {
                case '1':
                case '5':
                case '9':
                case 'D':
                    return 1; // Open

                case '2':
                case '6':
                case 'A':
                case 'E':
                    return 2; // EOL

                case '3':
                case '7':
                case 'B':
                case 'F':
                    return 3; // Short

                default:
                    return 0;
            }
        }
    }
}