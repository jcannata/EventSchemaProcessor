using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace EventSchemaProcessor
{
    /// <summary>
    /// Assembles event trace files into a single time-ordered XML compliant file.
    /// </summary>
    public class TraceProcessor : IDisposable
    {
        private readonly string _outputFilePath;
        private readonly List<string> _eventTraceFiles;
        private readonly string _tempFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceProcessor"/> class.
        /// </summary>
        /// <param name="outputFilePath">The location of the XML file.</param>
        /// <exception cref="ArgumentNullException">The specified output file is missing.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The specified output file is not a valid filename.</exception>
        public TraceProcessor(string outputFilePath)
        {
            _eventTraceFiles = new List<string>();
            _outputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));

            if (_outputFilePath.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentOutOfRangeException(nameof(outputFilePath));
            
            if (File.Exists(_outputFilePath))
                File.Delete(_outputFilePath);

            _tempFile = Path.GetTempFileName();
        }

        public void AddEventData(string eventDataFilePath)
        {
            if (string.IsNullOrEmpty(eventDataFilePath))
                throw new ArgumentException(nameof(eventDataFilePath));
            if (!File.Exists(eventDataFilePath))
                throw new FileNotFoundException("Missing event data file.", eventDataFilePath);

            // now add the contents of the trace file
            _eventTraceFiles.Add(eventDataFilePath);
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            // Order the file(s) in ascending creation timestamp...
            var fileInfo = _eventTraceFiles.Select(f => new FileInfo(f)).OrderBy(fi => fi.CreationTime);

            using (var fileStream = File.Open(_tempFile, FileMode.Open))
            {
                using (var writer = new StreamWriter(fileStream))
                {
                    await writer.WriteLineAsync("<Events>");

                    foreach (var f in fileInfo)
                    {
                        var xml = await File.ReadAllTextAsync(f.FullName, cancellationToken);
                        string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                        if (xml.StartsWith(_byteOrderMarkUtf8))
                        {
                            var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length - 1;
                            xml = xml.Remove(0, lastIndexOfUtf8);
                        }

                        await writer.WriteAsync(xml.AsMemory(), cancellationToken);
                    }

                    await writer.WriteLineAsync("</Events>");
                }
            }

            // Now read the wrapped file into an XML document
            var fs = await File.ReadAllTextAsync(_tempFile);
            var xd = new XmlDocument();
            xd.LoadXml(fs);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");
            settings.CloseOutput = true;
            settings.OmitXmlDeclaration = true;
            using (var doc = XmlWriter.Create(_outputFilePath, settings))
            {
                xd.WriteTo(doc);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (!string.IsNullOrEmpty(_tempFile) && File.Exists(_tempFile))
                    {
                        try
                        {
                            File.Delete(_tempFile);
                        }
                        catch (Exception)
                        {
                            // Must not throw exception when disposing...
                        }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~EventDataProcessor() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
