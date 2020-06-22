using System;
using System.Collections.Generic;
using System.Text;
using CsvHelper;
namespace Qwack.CLI
{
    public class CommandFileRow
    {
        public string Command { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
        public string Param4 { get; set; }
    }
}
