using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace EventSchemaProcessor
{
    public static class TraceFormatters
    {
        public const string EventUri = "http://schemas.microsoft.com/win/2004/08/events/event";
        public const string ExtendedDataUri = "http://schemas.microsoft.com/2006/09/System.Diagnostics/ExtendedData";

        public static async Task WriteAsCsvAsync(string filePath, Stream outputStream, int minimumLevel = 8, CancellationToken cancellationToken = default)
        {
            // Load the XML file.
            var xd = new XmlDocument();
            xd.Load(filePath);

            // There are a couple of namespaces defined. In order to use XPath we must add them here.
            var mgr = new XmlNamespaceManager(xd.NameTable);
            mgr.AddNamespace("ns1", EventUri);
            mgr.AddNamespace("ns2", ExtendedDataUri);

            using (var sw = new StreamWriter(outputStream))
            {
                var filteredNodes = xd.SelectNodes($"//ns1:Event[ns1:System/ns1:Level <= {minimumLevel}]", mgr);

                foreach (XmlNode node in filteredNodes)
                {
                    var time = DateTime.Parse(node["System"]["TimeCreated"].GetAttribute("SystemTime"));
                    var level = node["RenderingInfo"]["Level"].InnerText;
                    var message = node["EventData"].InnerText.Replace(Environment.NewLine, ";");
                    var msg = $"{time}|{level}|{message}{Environment.NewLine}";
                    await sw.WriteAsync(msg.AsMemory(), cancellationToken);
                }
            }
        }
    }
}