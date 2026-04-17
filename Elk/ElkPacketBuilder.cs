using System;
using System.Text;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public static class ElkPacketBuilder
    {
        public static string Build(string command, string data)
        {
            if (string.IsNullOrWhiteSpace(command) || command.Length != 2)
            {
                throw new ArgumentException("Command must be exactly 2 ASCII characters.", "command");
            }

            if (data == null)
            {
                data = string.Empty;
            }

            int lengthValue = 2 + 2 + data.Length;
            string length = lengthValue.ToString("X2");

            string body = length + command + data;
            string checksum = CalculateChecksum(body);

            return body + checksum + "\r\n";
        }

        public static string Build(string command)
        {
            return Build(command, string.Empty);
        }

        public static string CalculateChecksum(string asciiText)
        {
            byte sum = 0;
            byte[] bytes = Encoding.ASCII.GetBytes(asciiText);

            foreach (byte b in bytes)
            {
                sum += b;
            }

            byte checksum = (byte)(0 - sum);
            return checksum.ToString("X2");
        }
    }
}