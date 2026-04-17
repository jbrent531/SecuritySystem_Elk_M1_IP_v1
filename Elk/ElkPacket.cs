namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkPacket
    {
        public string Raw { get; set; } = "";
        public string Length { get; set; } = "";
        public string Command { get; set; } = "";
        public string Data { get; set; } = "";
        public string Checksum { get; set; } = "";

        public override string ToString()
        {
            return $"Len={Length}, Cmd={Command}, Data={Data}, Chk={Checksum}";
        }
    }
}
