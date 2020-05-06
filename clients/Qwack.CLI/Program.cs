using System;
using Utility.CommandLine;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.CLI
{
    class Program
    {
        [Argument('f', "fileName", "File name for script to process" )]
        private static string FileName { get; set; }

        [Argument('c', "calendar", "Calendar to use for date adjustment")]
        private static string Calendar { get; set; }

        [Argument('d', "date format", "Datetime format string")]
        private static string Format { get; set; }

        [Operands]
        private static string[] Operands { get; set; }

        static void Main(string[] args)
        {
            Arguments.Populate();

            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
            var commandRows = new List<CommandFileRow>();
            using var textReader = File.OpenText(FileName);
            {
                using var csv = new CsvReader(textReader, config);
                {
                    csv.Configuration.HasHeaderRecord = false;
                    commandRows = csv.GetRecords<CommandFileRow>().ToList();
                }
            }

            if(!ContainerStores.CalendarProvider.Collection.TryGetCalendar(Calendar,out var calendar))
            {
                throw new Exception($"Unable to find calendar {Calendar}");
            }

            if (string.IsNullOrWhiteSpace(Format))
                Format = "yyyy-MM-dd";

            var today = DateTime.Today;
            var yesterday = today.AddPeriod(RollType.P, calendar, new Frequency(-1, DatePeriodType.B));
            var tomorrow = today.AddPeriod(RollType.F, calendar, new Frequency(1, DatePeriodType.B));

            foreach (var row in commandRows)
            {
                switch (row.Command.ToLower())
                {
                    case "copy":
                    case "cp":
                        var p1 = row.Param1
                            .Replace("{today}", today.ToString(Format))
                            .Replace("{yesterday}", yesterday.ToString(Format))
                            .Replace("{tomorrow}", tomorrow.ToString(Format));
                        var p2 = row.Param2
                            .Replace("{today}", today.ToString(Format))
                            .Replace("{yesterday}", yesterday.ToString(Format))
                            .Replace("{tomorrow}", tomorrow.ToString(Format));
                        Console.WriteLine($"Copying {p1} to {p2}");
                        File.Copy(p1, p2);
                        break;
                    case "md":
                    case "mkdir":
                        var p1md = row.Param1
                            .Replace("{today}", today.ToString(Format))
                            .Replace("{yesterday}", yesterday.ToString(Format))
                            .Replace("{tomorrow}", tomorrow.ToString(Format));
                        Console.WriteLine($"Creating folder {p1md}");
                        Directory.CreateDirectory(p1md);
                        break;
                }
            }

            Console.WriteLine($"Processed {commandRows.Count} commands");
        }
    }
}
