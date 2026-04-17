using System;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public static class ElkPacketParser
    {
        public static ElkPacket Parse(string rawPacket)
        {
            if (string.IsNullOrWhiteSpace(rawPacket))
                throw new ArgumentException("Packet is empty.", nameof(rawPacket));

            string trimmed = rawPacket.Trim('\r', '\n');

            if (trimmed.Length < 6)
                throw new ArgumentException("Packet too short.", nameof(rawPacket));

            string length = trimmed.Substring(0, 2);
            string command = trimmed.Substring(2, 2);
            string checksum = trimmed.Substring(trimmed.Length - 2, 2);
            string data = trimmed.Substring(4, trimmed.Length - 6);

            return new ElkPacket
            {
                Raw = trimmed,
                Length = length,
                Command = command,
                Data = data,
                Checksum = checksum
            };
        }

        public static bool ValidateChecksum(string rawPacket)
        {
            if (string.IsNullOrWhiteSpace(rawPacket))
                return false;

            string trimmed = rawPacket.Trim('\r', '\n');

            if (trimmed.Length < 6)
                return false;

            string bodyWithoutChecksum = trimmed.Substring(0, trimmed.Length - 2);
            string actualChecksum = trimmed.Substring(trimmed.Length - 2, 2);
            string expectedChecksum = ElkPacketBuilder.CalculateChecksum(bodyWithoutChecksum);

            return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }
    }
}