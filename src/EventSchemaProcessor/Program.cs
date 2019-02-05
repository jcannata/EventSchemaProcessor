using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EventSchemaProcessor
{
    public enum LoggingLevels
    {
        Verbose = 0,
        Error = 2,
        Warning = 4,
        Information = 8
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            if (args == null || args.Length < 1)
                throw new ArgumentException("Missing input file(s).");

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                cts.Cancel();
            };

            var src = args[0];
            var outputXmlFile = "Output.xml";
            var level = LoggingLevels.Information;

            try
            {
                using (var eventProcessor = new TraceProcessor(outputXmlFile))
                {
                    // Add as many related event data files as needed.
                    eventProcessor.AddEventData(src);
                    await eventProcessor.ProcessAsync(cts.Token);
                }

                await TraceFormatters.WriteAsCsvAsync(outputXmlFile, Console.OpenStandardOutput(), (int)level);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadLine();
        }
    }
}
