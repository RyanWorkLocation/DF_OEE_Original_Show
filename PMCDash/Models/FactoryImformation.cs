using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class ProductionLineImformation
    {
        public ProductionLineImformation(string productionLineName, string displayName)
        {
            ProductionLineName = productionLineName;
            DisplayName = displayName;
        }

        public string ProductionLineName { get; set; }

        public string DisplayName { get; set; }
    }

    public class MachineInformation
    {
        public MachineInformation(string machineName, string status, string displayName)
        {
            MachineName = machineName;
            Status = status;
            DiplayName = displayName;
        }

        public string MachineName { get; set; }

        public string Status { get; set; }

        public string DiplayName { get; set; }
    }


    public class Device
    {
        public Device(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Value { get; set; }
        public string Text { get; set; }
    }

    public class ProductionLine
    {
        public ProductionLine(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Value { get; set; }
        public string Text { get; set; }
        public List<Device> Devices { get; set; }
    }

    public class Factory
    {
        public Factory(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Value { get; set; }
        public string Text { get; set; }
        public List<ProductionLine> ProductionLines { get; set; }
    }

    public class FactoryDefine
    {
        public List<Factory> Factorys { get; set; }
    }
}
