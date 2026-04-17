namespace SecuritySystem_Elk_M1_IP_v1
{
    public static class ElkZoneNameMapper
    {
        public static void ApplyName(ElkZone zone, string name)
        {
            if (zone == null)
                return;

            zone.Name = (name ?? string.Empty).Trim();
        }
    }
}
