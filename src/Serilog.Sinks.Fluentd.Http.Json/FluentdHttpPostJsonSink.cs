using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Fluentd
{
    internal sealed partial class FluentdHttpPostJsonSink : IBatchedLogEventSink
    {
        private readonly ITextFormatter m_formatter;
        private readonly HttpClient m_httpClient;
        private readonly Int32 m_bodySizeLimit;
        private readonly MemoryStream m_buffer;
        private readonly Encoding m_encoding;
        private readonly Byte[] m_leftBracket;
        private readonly Byte[] m_comma;
        private readonly Byte[] m_rightBracket;

        public FluentdHttpPostJsonSink(HttpClient client, ITextFormatter formatter, Int32 bodySizeLimit)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            if (bodySizeLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bodySizeLimit), "must be a positive number.");
            }

            m_httpClient = client;
            m_formatter = formatter;
            m_bodySizeLimit = bodySizeLimit;

            m_buffer = new MemoryStream();
            m_encoding = new UTF8Encoding(false, false);

            m_leftBracket = m_encoding.GetBytes("[");
            m_comma = m_encoding.GetBytes(",");
            m_rightBracket = m_encoding.GetBytes("]");
        }

        public async Task PushEventsAsync(IEnumerable<LogEvent> events, CancellationToken cancellationToken)
        {
            if (events != null || events.Any())
            {
                var messages = new List<Byte[]>();
                using (var writer = new StreamWriter(m_buffer, m_encoding, 1024, true))
                {
                    foreach (var @event in events)
                    {
                        m_buffer.Position = 0;
                        m_buffer.SetLength(0);

                        m_formatter.Format(@event, writer);
                        writer.Flush();

                        messages.Add(m_buffer.ToArray());
                    }
                }

                for (Int32 i = 0, len = messages.Count; i < len;)
                {
                    Int32 end = i;
                    Int64 prevSize = m_leftBracket.Length + m_rightBracket.Length;
                    for (; end < len; end++)
                    {
                        Int64 nextSize = prevSize;
                        if (i < end)
                        {
                            nextSize += m_comma.Length;
                        }
                        nextSize += messages[end].Length;

                        if (m_bodySizeLimit < nextSize)
                        {
                            break;
                        }
                    }

                    if (i < end)
                    {
                        m_buffer.Position = 0;
                        m_buffer.SetLength(0);
                        m_buffer.Write(m_leftBracket, 0, m_leftBracket.Length);
                        for (var j = i; j < end; j++)
                        {
                            var message = messages[j];
                            if (i < j)
                            {
                                m_buffer.Write(m_comma, 0, m_comma.Length);
                            }
                            m_buffer.Write(message, 0, message.Length);
                        }
                        m_buffer.Write(m_rightBracket, 0, m_rightBracket.Length);

                        i = end;

                        m_buffer.TryGetBuffer(out var bytes);
                        var content = new ByteArrayContent(bytes.Array, bytes.Offset, bytes.Count);
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") {
                            CharSet = m_encoding.WebName
                        };
                        try
                        {
                            var response = await m_httpClient.PostAsync("", content, cancellationToken).ConfigureAwait(false);
                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                SelfLog.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            SelfLog.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        i++;
                        SelfLog.WriteLine("Message is skipped because its' size ({0:#'##0} bytes) is greater than fluentd http body_size_limit {1:#'##0}", messages[i].Length, m_bodySizeLimit);
                        // message size is greater than configured body_size_limit in fluentd (https://docs.fluentd.org/input/http#body_size_limit)
                        // we will silently skip it
                    }
                }
            }
        }

        public void Dispose()
        {
            m_httpClient.Dispose();
            m_buffer.Dispose();
        }
    }
}