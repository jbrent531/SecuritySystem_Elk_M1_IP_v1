namespace SecuritySystem_Elk_M1_IP_v1
{

    // Maps SD text-description responses into the correct zone records.

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
