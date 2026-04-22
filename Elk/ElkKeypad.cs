namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkKeypad
    {
        public int Number { get; set; }
        public int LastKeyNumber { get; set; }
        public string LastKeyText { get; set; }
        public char[] FunctionKeyLedStates { get; set; }
        public bool CodeRequiredToBypass { get; set; }
        public char[] BeepChimeModeByArea { get; set; }

        public ElkKeypad()
        {
            FunctionKeyLedStates = new char[6];
            BeepChimeModeByArea = new char[8];
        }
    }
}
