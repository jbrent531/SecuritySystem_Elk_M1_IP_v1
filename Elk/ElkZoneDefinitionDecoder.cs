namespace SecuritySystem_Elk_M1_IP_v1
{

    // Decodes ELK zone definition characters from ZD responses.
    // The driver uses these definitions to label zones by purpose, such as entry/exit, motion, smoke, CO, or water.

    public static class ElkZoneDefinitionDecoder
    {
        public static string Decode(char raw)
        {
            switch (raw)
            {
                case '0': return "Disabled";
                case '1': return "Burglar EntryExit 1";
                case '2': return "Burglar EntryExit 2";
                case '3': return "Burglar Perimeter Instant";
                case '4': return "Burglar Interior";
                case '5': return "Burglar Interior Follower";
                case '6': return "Burglar Interior Night";
                case '7': return "Burglar Interior Night Delay";
                case '8': return "24 Hour Burglar";
                case '9': return "Box Tamper";
                case ':': return "Fire Alarm";
                case ';': return "Fire Verified";
                case '<': return "Fire Supervisory";
                case '=': return "Aux Alarm 1";
                case '>': return "Aux Alarm 2";
                case '?': return "Keyfob";
                case '@': return "Non Alarm";
                case 'A': return "Carbon Monoxide";
                case 'B': return "Emergency Alarm";
                case 'C': return "Freeze Alarm";
                case 'D': return "Gas Alarm";
                case 'E': return "Heat Alarm";
                case 'F': return "Medical Alarm";
                case 'G': return "Police Alarm";
                case 'H': return "Police No Indication";
                case 'I': return "Water Alarm";
                case 'J': return "Key Momentary Arm/Disarm";
                case 'K': return "Key Momentary Arm Away";
                case 'L': return "Key Momentary Arm Stay";
                case 'M': return "Key Momentary Disarm";
                case 'N': return "Key On/Off";
                case 'O': return "Mute Audibles";
                case 'P': return "Power Supervisory";
                case 'Q': return "Temperature";
                case 'R': return "Analog Zone";
                case 'S': return "Phone Key";
                case 'T': return "Intercom Key";
                default: return "Unknown (" + raw + ")";
            }
        }

        public static string DecodeCategory(char raw)
        {
            switch (raw)
            {
                case '1':
                case '2':
                case '3':
                    return "DoorWindow";

                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                    return "Motion";

                case ':':
                case ';':
                case '<':
                case 'E':
                    return "Smoke";

                case 'A':
                    return "CarbonMonoxide";

                case 'I':
                    return "Water";

                case 'C':
                case 'Q':
                    return "Temperature";

                case '9':
                case 'P':
                    return "Tamper";

                default:
                    return "Unknown";
            }
        }
    }
}