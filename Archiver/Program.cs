using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Utility.CommandLine;

namespace Archiver
{
    class Program
    {
        [Argument('s', "source", "Source directory")]
        private static string Source { get; set; }

        [Argument('d', "destination", "Destination directory")]
        private static string Destination { get; set; }

        [Argument('e', "extensions", "File extensions to archive, example: \".mp4|.avi|.mpeg\", default is \".*\"")]
        private static string Extensions { get; set; } = ArchivingMechanism.DefaultExtensions;

        [Argument('r', "retention", "Number of days to retain locally, after which files will be archived")]
        private static int Retention { get; set; } = ArchivingMechanism.DefaultRetentionDays;

        [Argument('f', "format", "File name date format. Date can only be at the front or the end.")]
        private static List<string> FileNameDateFormats { get; set; } = new List<string> { ArchivingMechanism.DefaultFileNameDateFormat };

        [Argument('R', "recurse", "Recurse sub directories in source path. Default value is false.")]
        private static bool RecurseSubDirectories { get; set; } = false;

        static int Main(string[] args)
        {
            if(args.Any(a => a == "--help"))
            {
                var helpAttributes = Arguments.GetArgumentInfo();
                Console.WriteLine("Short\tLong\tFunction");
                Console.WriteLine("-----\t----\t--------");

                foreach (var item in helpAttributes)
                {
                    var result = item.ShortName + "\t" + item.LongName + "\t" + item.HelpText;
                    Console.WriteLine(result);
                }
                return 0;
            }
            var enableDebug = args.Any(a => a == "--debug");
            
            var isDemo = args.Any(a => a == "--demo");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(enableDebug ? LogEventLevel.Debug : LogEventLevel.Information)
                .WriteTo.Console()
                .WriteTo.File("Logs/Archiver.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Information($"Starting {Environment.CommandLine} from {Environment.CurrentDirectory}");
            Log.Debug("Debug logging enabled");
            if (isDemo)
            {
                Log.Information("Demo mode enabled");
            }
            Arguments.Populate();
            var possibleArguments = typeof(Program).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(p => p.GetCustomAttribute(typeof(ArgumentAttribute)) != null).Select(p => new { Name = p.Name, Value = p.GetValue(null) });
            var argumentsText = string.Join("\r\n", possibleArguments.Select(a => $"{a.Name}={a.Value}"));
            Log.Debug("Arguments:\r\n" + argumentsText);
            if (Source == null || Destination == null)
            {
                Log.Warning("Invalid arguments");
                return 1;
            }
            var archivingMechanism = new ArchivingMechanism();
            int retCode = 0;
            try
            {
                retCode = archivingMechanism.Archive(Source,
                    Destination,
                    FileNameDateFormats?.ToArray(),
                    extensions: Extensions,
                    retentionDays: Retention,
                    isDemo: isDemo,
                    recurseSubDirs: RecurseSubDirectories);
            }
            catch(Exception ex)
            {
                Log.Error($"Exception during archiving - {ex}");
                retCode = 10;
            }
            Log.Information($"Exiting with code {retCode}");
            return retCode;
        }

        private string ToString(object argValue)
        {
            if (!(argValue is string) && (argValue is IEnumerable collection))
            {
                return string.Join(", ", collection);
            }
            return argValue?.ToString();
        }
    }
}
