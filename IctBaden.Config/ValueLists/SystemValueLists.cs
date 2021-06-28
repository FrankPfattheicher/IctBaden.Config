using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;
using IctBaden.Framework.AppUtils;

namespace IctBaden.Config.ValueLists
{
    public static class SystemValueLists
    {
        public static void InitializeSystemValues(ConfigurationSession session)
        {
            InitializeAvailableComPorts(session);
        }

        private static void InitializeAvailableComPorts(ConfigurationSession session)
        {
            var valueList = new List<SelectionValue>();
                
            if(SystemInfo.Platform == Platform.Windows)
            {
                valueList = SerialPort.GetPortNames()
                    .Select(port => new SelectionValue {DisplayText = port, Value = port})
                    .ToList();
            }
            else if (SystemInfo.Platform == Platform.Linux)
            {
                //TODO: add available serial ports
            }

            session.RegisterValueListProvider("AvailableComPorts", new InMemoryValueListProvider(valueList));
        }
        
    }
}