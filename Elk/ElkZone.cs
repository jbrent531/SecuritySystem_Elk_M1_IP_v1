// Elk/ElkZone.cs
namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkZone
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public char RawStatus { get; set; }
        public string StatusText { get; set; }
        public bool IsOpen { get; set; }
        public bool IsTrouble { get; set; }
        public bool IsViolated { get; set; }
        public bool IsBypassed { get; set; }
        public bool IsFaulted { get; set; }
        public int NormalPhysicalState { get; set; }
        public int Partition { get; set; }
        public char Definition { get; set; }
        public bool IsConfigured { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Zone {0:D3} '{1}' raw={2} text='{3}' open={4} violated={5} trouble={6} bypassed={7} faulted={8} normalPhysical={9} partition={10} def={11} configured={12}",
                Number,
                Name ?? string.Empty,
                RawStatus,
                StatusText ?? string.Empty,
                IsOpen,
                IsViolated,
                IsTrouble,
                IsBypassed,
                IsFaulted,
                NormalPhysicalState,
                Partition,
                Definition,
                IsConfigured);
        }
    }
}