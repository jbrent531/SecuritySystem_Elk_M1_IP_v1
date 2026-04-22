using System;
using System.Text;

namespace SecuritySystem_Elk_M1_IP_v1.Elk
{
    /// <summary>
    /// Represents a transport payload sent to the Elk system.
    /// Handles formatting and checksum generation for outgoing messages.
    /// </summary>
    public class SendTransportData
    {
        #region Fields

        private readonly string _command;
        private readonly string _data;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new transport packet.
        /// </summary>
        /// <param name="command">Two-character Elk command (e.g. "zs")</param>
        /// <param name="data">Command payload (already formatted)</param>
        public SendTransportData(string command, string data)
        {
            _command = command ?? string.Empty;
            _data = data ?? string.Empty;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds the final ASCII message including length + checksum.
        /// </summary>
        public string BuildMessage()
        {
            string payload = _command + _data + "00"; // "00" reserved by Elk protocol

            string length = GetLength(payload);
            string message = length + payload;

            string checksum = GetChecksum(message);

            return message + checksum + "\r\n";
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Calculates the 2-byte ASCII hex length field.
        /// Elk requires length of payload excluding length + CRLF.
        /// </summary>
        private string GetLength(string payload)
        {
            int length = payload.Length;
            return length.ToString("X2");
        }

        /// <summary>
        /// Calculates Elk checksum (2's complement of sum).
        /// Required for all outgoing messages.
        /// </summary>
        private string GetChecksum(string message)
        {
            int sum = 0;

            foreach (char c in message)
            {
                sum += c;
            }

            int checksum = ((~sum + 1) & 0xFF);

            return checksum.ToString("X2");
        }

        #endregion
    }
}