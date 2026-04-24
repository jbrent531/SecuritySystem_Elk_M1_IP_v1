using System;
using System.Threading.Tasks;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkM1Client
    {
        private readonly TcpElkTransport _transport;


        //Constructor
        public ElkM1Client(TcpElkTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException("transport");
        }

        public Task SendRawCommandAsync(string command, string data = "")
        {
            string packet = ElkPacketBuilder.Build(command, data);
            return _transport.SendAsciiAsync(packet);
        }


        public Task RequestArmingStatusAsync() { return SendRawCommandAsync("as", "00"); }
        public Task RequestZoneStatusAsync() { return SendRawCommandAsync("zs", "00"); }
        public Task RequestZoneDefinitionsAsync() { return SendRawCommandAsync("zd", "00"); }
        public Task RequestZonePartitionsAsync() { return SendRawCommandAsync("zp", "00"); }
        public Task RequestKeypadAreaAssignmentsAsync() { return SendRawCommandAsync("ka", "00"); }
        public Task RequestSystemTroubleStatusAsync() { return SendRawCommandAsync("ss", "00"); }
        public Task RequestVersionNumberAsync() { return SendRawCommandAsync("vn", "00"); }



        public Task RequestTextDescriptionAsync(int textType, int number)
        {
            if (textType < 0 || textType > 99)
                throw new ArgumentOutOfRangeException("textType");

            if (number < 0 || number > 999)
                throw new ArgumentOutOfRangeException("number");

            string data = string.Format("{0:D2}{1:D3}00", textType, number);
            return SendRawCommandAsync("sd", data);
        }
        
        
        public Task RequestFunctionKeyStatusAsync(int keypadNumber)
        {
            if (keypadNumber < 1 || keypadNumber > 16)
                throw new ArgumentOutOfRangeException("keypadNumber");

            string data = string.Format("{0:D2}00", keypadNumber);
            return SendRawCommandAsync("kc", data);
        }
        public Task PressFunctionKeyAsync(int keypadNumber, int functionKeyNumber)
        {
            if (keypadNumber < 1 || keypadNumber > 16)
                throw new ArgumentOutOfRangeException("keypadNumber");

            if (functionKeyNumber < 1 || functionKeyNumber > 6)
                throw new ArgumentOutOfRangeException("functionKeyNumber");

            string data = string.Format("{0:D2}{1}00", keypadNumber, functionKeyNumber);
            CrestronConsole.PrintLine("ELK FUNCTION KEY CMD: kf " + data);
            return SendRawCommandAsync("kf", data);
        }


        public Task ToggleChimeAsync(int keypadNumber)
        {
            if (keypadNumber < 1 || keypadNumber > 16)
                throw new ArgumentOutOfRangeException("keypadNumber");

            string data = string.Format("{0:D2}C00", keypadNumber);
            CrestronConsole.PrintLine("ELK CHIME CMD: kf " + data);
            return SendRawCommandAsync("kf", data);
        }
        public Task RequestChimeModeAsync(int keypadNumber)
        {
            if (keypadNumber < 1 || keypadNumber > 16)
                throw new ArgumentOutOfRangeException("keypadNumber");

            string data = string.Format("{0:D2}000", keypadNumber);
            CrestronConsole.PrintLine("ELK CHIME STATUS CMD: kf " + data);
            return SendRawCommandAsync("kf", data);
        }

        
        public Task ArmStayAsync(int area, string code) { return SendArmCommandAsync("a2", area, code); }
        public Task ArmAwayAsync(int area, string code) { return SendArmCommandAsync("a1", area, code); }
        public Task DisarmAsync(int area, string code) { return SendArmCommandAsync("a0", area, code); }

        public Task ToggleZoneBypassAsync(int zoneNumber, int area, string code)
        {
            if (zoneNumber < 1 || zoneNumber > 208)
                throw new ArgumentOutOfRangeException("zoneNumber");

            if (area < 1 || area > 8)
                area = 1;

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("A user code is required.", "code");

            code = code.Trim();
            if (code.Length > 6)
                throw new ArgumentException("User code cannot be longer than 6 digits.", "code");

            foreach (char c in code)
            {
                if (!char.IsDigit(c))
                    throw new ArgumentException("User code must contain digits only.", "code");
            }

            string code6 = code.PadLeft(6, '0');
            string data = string.Format("{0:D3}{1}{2}00", zoneNumber, area, code6);

            CrestronConsole.PrintLine("ELK BYPASS CMD: zb " + data);
            return SendRawCommandAsync("zb", data);
        }

        private Task SendArmCommandAsync(string command, int area, string code)
        {
            if (string.IsNullOrWhiteSpace(command) || command.Length != 2)
                throw new ArgumentException("Command must be exactly 2 characters.", "command");

            if (area < 1 || area > 8)
                throw new ArgumentOutOfRangeException("area");

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("A user code is required.", "code");

            code = code.Trim();
            if (code.Length > 6)
                throw new ArgumentException("User code cannot be longer than 6 digits.", "code");

            foreach (char c in code)
            {
                if (!char.IsDigit(c))
                    throw new ArgumentException("User code must contain digits only.", "code");
            }

            string code6 = code.PadLeft(6, '0');
            string data = string.Format("{0}{1}", area, code6);
            CrestronConsole.PrintLine("ELK ARM CMD: command=" + command + " data=" + data);
            return SendRawCommandAsync(command, data);
        }
    }
}