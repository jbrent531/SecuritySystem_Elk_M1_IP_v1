using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public static class ElkZoneMapper
    {
        public static void ApplyStatus(ElkZone zone, char rawStatus)
        {
            if (zone == null)
            {
                return;
            }

            zone.RawStatus = rawStatus;
            zone.StatusText = ElkZoneStatusDecoder.Decode(rawStatus);
            zone.IsOpen = ElkZoneStatusDecoder.IsOpen(rawStatus);
            zone.IsViolated = ElkZoneStatusDecoder.IsViolated(rawStatus);
            zone.IsBypassed = ElkZoneStatusDecoder.IsBypassed(rawStatus);
            zone.IsTrouble = ElkZoneStatusDecoder.IsTrouble(rawStatus);

            /*CrestronConsole.PrintLine(
                "ZONE MAPPER zone=" + zone.Number +
                " raw=" + rawStatus +
                " text=" + zone.StatusText +
                " open=" + zone.IsOpen +
                " violated=" + zone.IsViolated +
                " bypassed=" + zone.IsBypassed);
            */
        }
    }
}
