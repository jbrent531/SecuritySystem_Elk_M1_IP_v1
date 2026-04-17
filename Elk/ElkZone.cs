namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkZone
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;

        public char RawStatus { get; set; }
        public string StatusText { get; set; } = string.Empty;

        public bool IsOpen { get; set; }
        public bool IsTrouble { get; set; }
        public bool IsViolated { get; set; }
        public bool IsBypassed { get; set; }

        public int Partition { get; set; }

        public char Definition { get; set; }
        public bool IsConfigured { get; set; }



        public override string ToString()
        {
            if (!IsConfigured)
                return $"Zone {Number:D3}: UNUSED";

            string label = string.IsNullOrWhiteSpace(Name)
                ? $"Zone {Number:D3}"
                : $"Zone {Number:D3} [{Name}]";

            string partitionText = Partition > 0 ? $" Area {Partition}" : "";
            return $"{label}: {StatusText}{partitionText}";
        }
    }
}
