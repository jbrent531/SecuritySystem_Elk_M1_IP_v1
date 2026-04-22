using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public interface IElkSecurityService
    {
        event Action<ElkZone> ZoneChanged;
        event Action<ElkZone> ZoneNameChanged;
        event Action<ElkZone> ZoneBypassChanged;
        event Action<ElkArea> AreaChanged;
        event Action<bool> SystemReadyChanged;
        event Action<bool> AlarmActiveChanged;

        IReadOnlyDictionary<int, ElkZone> Zones { get; }
        IReadOnlyDictionary<int, ElkArea> Areas { get; }

        bool IsSystemReady { get; }
        bool IsAlarmActive { get; }
        int GetKeypadArea(int keypadNumber);


        Task RefreshKeypadAreasAsync();

        Task ActivateTaskAsync(int taskNumber);

        Task ToggleChimeAsync(int keypadNumber);

        Task StartAsync(string host, int port);
        Task StopAsync();

        Task RefreshZonesAsync();
        Task RefreshAreasAsync();
        Task RefreshZoneNamesAsync();
        Task PressFunctionKeyAsync(int keypadNumber, int functionKeyNumber);
        Task RefreshFunctionKeyStatusAsync(int keypadNumber);

        Task ArmStayAsync(int area, string userCode);
        Task ArmAwayAsync(int area, string userCode);
        Task DisarmAsync(int area, string userCode);

        Task BypassZoneAsync(int zoneNumber, string userCode);
        Task UnbypassZoneAsync(int zoneNumber, string userCode);
    }
}
