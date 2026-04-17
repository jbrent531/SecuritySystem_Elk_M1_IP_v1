using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    /// <summary>
    /// This is the helper class and used to send tranport data.
    /// </summary>
    public class SendTransportData
    {
        private CTimer _timer;
        List<string> _data;
        const long TIME_MS = 3000;
        private Action<string> _dataHandler;
        public SendTransportData(Action<string> dataHandler)
        {
            _data = new List<string>();
            AddData();
            _timer = new CTimer(OnTimerTick, null, TIME_MS, TIME_MS);
            _dataHandler = dataHandler;
        }

        private void AddData()
        {
            _data.Add("createarea");
            _data.Add("disarm");
            _data.Add("alarmon");
            _data.Add("alarmoff");
            _data.Add("error");
        }

        private void OnTimerTick(object usrObj)
        {
            if (_data.Count > 0)
            {
                var op = _data.FirstOrDefault();
                var handler = _dataHandler;
                if (handler != null && !string.IsNullOrEmpty(op))
                {
                    handler(op);
                    CrestronConsole.PrintLine("SendTransportData :  OnTimerTick : Data Hander for  {0} is done", op);
                    _data.Remove(op);
                }
            }
        }
    }
}