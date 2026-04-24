namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkUserCodeEvent
    {

        #region Properties
        public int KeypadNumber { get; set; }
        public int UserNumber { get; set; }
        public int AreaNumber { get; set; }
        public bool IsValid { get; set; }
        public string RawData { get; set; }
        public string Description { get; set; }
        #endregion

        //Contructor
        public ElkUserCodeEvent()
        {
            RawData = string.Empty;
            Description = string.Empty;
        }

        public override string ToString()
        {
            return "Keypad=" + KeypadNumber +
                   ", User=" + UserNumber +
                   ", Area=" + AreaNumber +
                   ", Valid=" + IsValid +
                   ", Description=" + Description;
        }
    }
}