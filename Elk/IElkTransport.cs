namespace SecuritySystem_Elk_M1_IP_v1
{
    /// <summary>
    /// Abstraction over the Crestron transport layer used by <see cref="ElkM1Client"/> to transmit
    /// raw ASCII command strings to the ELK M1 panel. Implemented by <see cref="SecuritySystemProtocol"/>.
    /// </summary>
    public interface IElkTransport
    {
        /// <summary>
        /// Sends a fully-formed ELK ASCII packet string over the TCP connection.
        /// The string should already include the checksum and CRLF terminator.
        /// </summary>
        void SendAscii(string data);
    }
}
