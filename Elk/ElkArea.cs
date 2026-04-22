namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkArea
    {
        public int Number { get; set; }
        public string Name { get; set; }

        public char RawArmState { get; set; }
        public char RawArmUpState { get; set; }
        public char RawAlarmState { get; set; }

        public string ArmStateText { get; set; }
        public string ArmUpStateText { get; set; }
        public string AlarmStateText { get; set; }

        public bool IsArmed { get; set; }
        public bool IsReady { get; set; }
        public bool CanForceArm { get; set; }
        public bool IsExitDelayActive { get; set; }
        public bool IsFullyArmed { get; set; }
        public bool IsBypassedArmed { get; set; }
        public bool IsAlarm { get; set; }
        public bool IsEntryDelayActive { get; set; }
        public int DelaySeconds { get; set; }

        public char ChimeModeRaw { get; set; }
        public string ChimeModeText { get; set; }
        public bool IsChimeEnabled { get; set; }
    }
}
