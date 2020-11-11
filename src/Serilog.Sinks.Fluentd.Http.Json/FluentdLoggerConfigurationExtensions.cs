using System;
using System.Globalization;
using System.Net.Http;
using Serilog.Configuration;
using Serilog.Formatting;
using Serilog.Formatting.Elasticsearch;

namespace Serilog.Sinks.Fluentd
{
    public static class FluentdLoggerConfigurationExtensions
    {
        /// <summary>Adds sink that sends messages in JSON format to http input of the Fluentd service using POST method.</summary>
        /// <param name="sinkConfiguration">The sink configuration.</param>
        /// <param name="bodySizeLimit">The body size limit. Default value is 32Mb (33554432 bytes).</param>
        /// <param name="flushPeriod">The flush period. Default value is 4 sec.</param>
        /// <param name="httpTimeout">Time to wait for submitting to complete. Default value is 3 sec.</param>
        /// <param name="maxQueueSize">Maximum size of the queue. Default value is 10000.</param>
        /// <param name="jsonFormatter">The JSON formatter. By default <see cref="ElasticsearchJsonFormatter"/></param>
        /// <param name="url">The URL of the fluentd HTTP input.</param>
        /// <returns><paramref name="sinkConfiguration"/></returns>
        /// <exception cref="ArgumentNullException">
        /// <list type="bullet">
        /// <item><paramref name="sinkConfiguration"/> is null</item>
        /// <item><paramref name="url"/> is null</item>
        /// </list>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <list type="bullet">
        /// <item><paramref name="flushPeriod"/> must be longer than zero</item>
        /// <item><paramref name="httpTimeout"/> must be longer than zero</item>
        /// <item><paramref name="maxQueueSize"/> must be null or a positive number</item>
        /// <item><paramref name="bodySizeLimit"/> must be a positive number</item>
        /// </list>
        /// </exception>
        public static LoggerConfiguration Fluentd(
            this LoggerSinkConfiguration sinkConfiguration,
            Int32 bodySizeLimit = 32 * 1024 * 1024,
            TimeSpan? flushPeriod = null,
            TimeSpan? httpTimeout = null,
            Int32? maxQueueSize = 10000,
            ITextFormatter jsonFormatter = null,
            String url = "http://localhost:8888/logging.log")
        {
            if (sinkConfiguration == null)
            {
                throw new ArgumentNullException(nameof(sinkConfiguration));
            }

            var httpClient = CreateHttpClient(url, httpTimeout ?? TimeSpan.FromSeconds(3));
            var batchMessagesWriter = new FluentdHttpPostJsonSink(httpClient, jsonFormatter ?? CreateFormatter(), bodySizeLimit);
            var sink = new PeriodicBatchingSink(
                flushPeriod: flushPeriod ?? TimeSpan.FromSeconds(4),
                maxQueueSize: maxQueueSize,
                batchMessagesWriter: batchMessagesWriter);

            return sinkConfiguration.Sink(sink);
        }

        internal static HttpClient CreateHttpClient(String url, TimeSpan httpTimeout)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }
            if (httpTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(httpTimeout), "must be longer than zero.");
            }

            return new HttpClient() {
                BaseAddress = new Uri(url),
                Timeout = httpTimeout,
                DefaultRequestHeaders = {
                    ConnectionClose = false
                }
            };
        }

        internal static ITextFormatter CreateFormatter()
        {
            return new ElasticsearchJsonFormatter(
                omitEnclosingObject: false,
                formatProvider: CultureInfo.InvariantCulture,
                closingDelimiter: String.Empty,
                renderMessage: true,
                serializer: null,
                inlineFields: false,
                renderMessageTemplate: true,
                formatStackTraceAsArray: false);
        }
    }
}