using System;
using System.Threading.Tasks;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    public sealed class ElkM1Client
    {
        private readonly TcpElkTransport _transport;

        public ElkM1Client(TcpElkTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task SendRawCommandAsync(string command, string data = "")
        {
            string packet = ElkPacketBuilder.Build(command, data);
            return _transport.SendAsciiAsync(packet);
        }

        public Task RequestKeypadAreaAssignmentsAsync()
        {
            return SendRawCommandAsync("ka", "00");
        }

        public Task RequestArmingStatusAsync() => SendRawCommandAsync("as", "00");
        public Task RequestZoneStatusAsync() => SendRawCommandAsync("zs", "00");
        public Task RequestZoneDefinitionsAsync() => SendRawCommandAsync("zd", "00");
        public Task RequestZonePartitionsAsync() => SendRawCommandAsync("zp", "00");
        public Task RequestSystemTroubleStatusAsync() => SendRawCommandAsync("ss", "00");
        public Task RequestVersionNumberAsync() => SendRawCommandAsync("vn", "00");

        public Task RequestTextDescriptionAsync(int textType, int number)
        {
            if (textType < 0 || textType > 99)
                throw new ArgumentOutOfRangeException(nameof(textType));

            if (number < 0 || number > 999)
                throw new ArgumentOutOfRangeException(nameof(number));

            string data = $"{textType:D2}{number:D3}00";
            return SendRawCommandAsync("sd", data);
        }

        public Task PressFunctionKeyAsync(int keypadNumber, int functionKeyNumber)
        {
            if (keypadNumber < 1 || keypadNumber > 16)
                throw new ArgumentOutOfRangeException(nameof(keypadNumber), "Keypad must be between 1 and 16.");

            if (functionKeyNumber < 1 || functionKeyNumber > 6)
                throw new ArgumentOutOfRangeException(nameof(functionKeyNumber), "Function key must be between 1 and 6.");

            string data = string.Format("{0:D2}{1}", keypadNumber, functionKeyNumber);
            CrestronConsole.PrintLine("ELK FUNCTION KEY CMD: kf " + data);
            return SendRawCommandAsync("kf", data);
        }

        public Task RequestFunctionKeyStatusAsync(int keypadNumber)
        {
            if (keypadNumber < 1 || keypadNumber > 16)
                throw new ArgumentOutOfRangeException(nameof(keypadNumber), "Keypad must be between 1 and 16.");

            string data = string.Format("{0:D2}00", keypadNumber);
            return SendRawCommandAsync("kc", data);
        }

        public Task ActivateTaskAsync(int taskNumber)
        {
            if (taskNumber < 1 || taskNumber > 32)
                throw new ArgumentOutOfRangeException(nameof(taskNumber), "Task must be between 1 and 32.");

            string data = string.Format("{0:D3}00", taskNumber);
            return SendRawCommandAsync("tn", data);
        }

        public Task ToggleChimeAsync(int keypadNumber)
        {
            if (keypadNumber < 1 || keypadNumber > 16)
                throw new ArgumentOutOfRangeException(nameof(keypadNumber), "Keypad must be between 1 and 16.");

            string data = string.Format("{0:D2}C00", keypadNumber);
            CrestronConsole.PrintLine("ELK CHIME CMD: kf " + data);
            return SendRawCommandAsync("kf", data);
        }

        public Task ArmStayAsync(int area, string code) => SendArmCommandAsync("a2", area, code);
        public Task ArmAwayAsync(int area, string code) => SendArmCommandAsync("a1", area, code);
        public Task DisarmAsync(int area, string code) => SendArmCommandAsync("a0", area, code);

        public Task ToggleZoneBypassAsync(int zoneNumber, int area, string code)
        {
            if (zoneNumber < 1 || zoneNumber > 208)
                throw new ArgumentOutOfRangeException(nameof(zoneNumber), "Zone must be between 1 and 208.");

            if (area < 1 || area > 8)
                area = 1;

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("A user code is required.", nameof(code));

            code = code.Trim();
            if (code.Length > 6)
                throw new ArgumentException("User code cannot be longer than 6 digits.", nameof(code));

            foreach (char c in code)
            {
                if (!char.IsDigit(c))
                    throw new ArgumentException("User code must contain digits only.", nameof(code));
            }

            string code6 = code.PadLeft(6, '0');
            string data = $"{zoneNumber:D3}{area}{code6}00";

            CrestronConsole.PrintLine("ELK BYPASS CMD: zb " + data);
            return SendRawCommandAsync("zb", data);
        }

        private Task SendArmCommandAsync(string command, int area, string code)
        {
            if (string.IsNullOrWhiteSpace(command) || command.Length != 2)
                throw new ArgumentException("Command must be exactly 2 characters.", nameof(command));

            if (area < 1 || area > 8)
                throw new ArgumentOutOfRangeException(nameof(area), "Area must be between 1 and 8.");

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("A user code is required.", nameof(code));

            code = code.Trim();
            if (code.Length > 6)
                throw new ArgumentException("User code cannot be longer than 6 digits.", nameof(code));

            foreach (char c in code)
            {
                if (!char.IsDigit(c))
                    throw new ArgumentException("User code must contain digits only.", nameof(code));
            }

            string code6 = code.PadLeft(6, '0');
            string data = $"{area}{code6}";
            CrestronConsole.PrintLine("ELK ARM CMD: command=" + command + " data=" + data);
            return SendRawCommandAsync(command, data);
        }
    }
}