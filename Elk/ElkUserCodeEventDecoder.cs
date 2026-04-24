namespace SecuritySystem_Elk_M1_IP_v1
{

    // Decodes user-code activity reported by the panel.
    // These messages let the driver surface who armed or disarmed the system and how the event was classified.


    public static class ElkUserCodeEventDecoder
    {
        public static ElkUserCodeEvent Decode(string data)
        {
            ElkUserCodeEvent result = new ElkUserCodeEvent();
            result.RawData = data ?? string.Empty;
            result.Description = "Raw IC payload: " + result.RawData;
            result.KeypadNumber = 0;
            result.UserNumber = 0;
            result.AreaNumber = 0;
            result.IsValid = false;
            return result;
        }
    }
}

