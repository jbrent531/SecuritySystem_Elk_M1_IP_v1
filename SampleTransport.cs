using Crestron.RAD.Common.Transports;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{

    // Transport wrapper used by the RAD driver runtime.
    // It configures Crestron transport behavior while the ELK-specific framing is handled elsewhere.
    public class SampleTransport : TcpTransport
    {
        public override void Start()
        {
            CrestronConsole.PrintLine("SampleTransport Start method is called");

            base.Start();

            var handler = DataHandler;
            if (handler != null)
            {
                handler("InitializationComplete");
            }
        }
    }
}