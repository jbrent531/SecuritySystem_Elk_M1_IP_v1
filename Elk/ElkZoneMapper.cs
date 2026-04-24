using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{

    // Applies decoded ELK zone status to the in-memory zone model.
    // This is where raw ZS/ZC values become the driver's open, trouble, violated, bypassed, and faulted flags.
    public static class ElkZoneMapper
    {
        public static void ApplyStatus(ElkZone zone, char rawStatus)
        {
            zone.RawStatus = char.ToUpperInvariant(rawStatus);
            zone.StatusText = ElkZoneStatusDecoder.Decode(zone.RawStatus);

            zone.IsOpen = ElkZoneStatusDecoder.IsOpen(zone.RawStatus);
            zone.IsTrouble = ElkZoneStatusDecoder.IsTrouble(zone.RawStatus);
            zone.IsViolated = ElkZoneStatusDecoder.IsViolated(zone.RawStatus);
            zone.IsBypassed = ElkZoneStatusDecoder.IsBypassed(zone.RawStatus);

            if (ElkZoneStatusDecoder.IsNormal(zone.RawStatus))
            {
                zone.NormalPhysicalState = ElkZoneStatusDecoder.GetPhysicalState(zone.RawStatus);
            }

            zone.IsFaulted = ComputeFaulted(zone);

            CrestronConsole.PrintLine(
                "ZONE MAP #{0}: raw='{1}' text='{2}' faulted={3} bypassed={4} normalPhysical={5}",
                zone.Number,
                zone.RawStatus,
                zone.StatusText,
                zone.IsFaulted,
                zone.IsBypassed,
                zone.NormalPhysicalState);
        }

        private static bool ComputeFaulted(ElkZone zone)
        {
            if (zone.IsTrouble || zone.IsViolated)
            {
                return true;
            }

            if (!zone.IsBypassed)
            {
                return false;
            }

            if (zone.RawStatus == 'C')
            {
                return zone.IsFaulted;
            }

            int currentPhysical = ElkZoneStatusDecoder.GetPhysicalState(zone.RawStatus);
            if (currentPhysical == 0 || zone.NormalPhysicalState == 0)
            {
                return zone.IsFaulted;
            }

            return currentPhysical != zone.NormalPhysicalState;
        }
    }
}