using System;

namespace SecuritySystem_Elk_M1_IP_v1
{
    /// <summary>
    /// Elk zone definition types (what kind of zone it is).
    /// This is NOT the live state — only the configured role of the zone.
    /// </summary>
    public enum ElkZoneDefinition
    {
        Unknown = 0,

        Disabled = 1,

        BurglarEntryExit1 = 2,
        BurglarEntryExit2 = 3,

        BurglarPerimeterInstant = 4,
        BurglarInterior = 5,
        BurglarInteriorFollower = 6,
        BurglarInteriorNight = 7,
        BurglarInteriorNightDelay = 8,

        Burglar24Hour = 9,
        BurglarBoxTamper = 10,

        FireAlarm = 11,
        FireVerified = 12,
        FireSupervisory = 13,

        AuxAlarm1 = 14,
        AuxAlarm2 = 15,

        CarbonMonoxide = 16,
        EmergencyAlarm = 17,
        FreezeAlarm = 18,
        GasAlarm = 19,
        HeatAlarm = 20,
        MedicalAlarm = 21,

        PoliceAlarm = 22,
        PoliceNoIndication = 23,

        WaterAlarm = 24
    }
}