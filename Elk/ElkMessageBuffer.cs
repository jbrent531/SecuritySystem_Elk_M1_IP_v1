using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkMessageBuffer
    {
        private readonly StringBuilder _buffer = new StringBuilder();

        public List<string> AppendAndExtract(string asciiChunk)
        {
            var packets = new List<string>();

            if (!string.IsNullOrEmpty(asciiChunk))
                _buffer.Append(asciiChunk);

            while (true)
            {
                if (_buffer.Length < 4)
                    break;

                string current = _buffer.ToString();

                if (!TryParseHexByte(current.Substring(0, 2), out int bodyLength))
                {
                    // Not aligned to a valid packet start, shift one char and retry
                    _buffer.Remove(0, 1);
                    continue;
                }

                // ELK packet structure:
                // [LL][CC][DATA][KK][CR][LF]
                //
                // LL = 2 chars hex length
                // bodyLength = length of LL+CC+DATA (based on what your builder/parser is using)
                // plus checksum (2 chars)
                // plus CRLF (2 chars)
                int totalPacketLength = bodyLength + 2 + 2;

                if (_buffer.Length < totalPacketLength)
                    break;

                string candidate = current.Substring(0, totalPacketLength);

                if (!candidate.EndsWith("\r\n", StringComparison.Ordinal))
                {
                    _buffer.Remove(0, 1);
                    continue;
                }

                packets.Add(candidate);
                _buffer.Remove(0, totalPacketLength);
            }

            return packets;
        }

        private static bool TryParseHexByte(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }
    }
}