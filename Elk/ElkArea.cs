namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkArea
    {
        public int Number { get; set; }

        // Raw AS packet fields.
        public char RawArmState { get; set; }
        public char RawArmUpState { get; set; }
        public char RawAlarmState { get; set; }

        // Friendly decoded text.
        public string ArmStateText { get; set; } = string.Empty;
        public string ArmUpStateText { get; set; } = string.Empty;
        public string AlarmStateText { get; set; } = string.Empty;
        public string Name { get; set; }

        // High-level booleans that the current adapter already uses.
        public bool IsArmed { get; set; }
        public bool IsReady { get; set; }
        public bool CanForceArm { get; set; }
        public bool IsExitDelayActive { get; set; }
        public bool IsFullyArmed { get; set; }
        public bool IsBypassedArmed { get; set; }
        public bool IsAlarm { get; set; }
        public bool IsEntryDelayActive { get; set; }

        // AS footer byte when present.
        public int DelaySeconds { get; set; }

        public override string ToString()
        {
            return $"Area {Number:D2}: Arm={ArmStateText}, ArmUp={ArmUpStateText}, Alarm={AlarmStateText}, Delay={DelaySeconds}s";
        }
    }
}
