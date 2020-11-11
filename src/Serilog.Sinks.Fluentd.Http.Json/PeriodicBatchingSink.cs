using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.Fluentd
{
    internal class PeriodicBatchingSink : ILogEventSink, IDisposable
    {
        private readonly List<LogEvent> m_currentBatch;
        private readonly TimeSpan m_flushPeriod;
        private readonly IBatchedLogEventSink m_batchMessagesWriter;
        private readonly BlockingCollection<LogEvent> m_messageQueue;
        private readonly CancellationTokenSource m_outputStoppingTokenSource;
        private readonly Task m_outputTask;

        public PeriodicBatchingSink(TimeSpan flushPeriod, Int32? maxQueueSize, IBatchedLogEventSink batchMessagesWriter)
        {
            if (flushPeriod <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(flushPeriod), "must be longer than zero.");
            }
            if (maxQueueSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxQueueSize), "must be a positive number.");
            }
            if (batchMessagesWriter is null)
            {
                throw new ArgumentNullException(nameof(batchMessagesWriter));
            }

            m_currentBatch = new List<LogEvent>();
            m_flushPeriod = flushPeriod;

            m_batchMessagesWriter = batchMessagesWriter;

            m_messageQueue = maxQueueSize == null ?
                new BlockingCollection<LogEvent>(new ConcurrentQueue<LogEvent>()) :
                new BlockingCollection<LogEvent>(new ConcurrentQueue<LogEvent>(), maxQueueSize.Value);

            m_outputStoppingTokenSource = new CancellationTokenSource();
            m_outputTask = Task.Run(() => ProcessLogQueue(m_outputStoppingTokenSource.Token));
            m_outputTask.Wait(50);
        }

        private async Task ProcessLogQueue(CancellationToken cancellationToken)
        {
            var stopWrittingTokenSource = new CancellationTokenSource();
            cancellationToken.Register(() => {
                stopWrittingTokenSource.CancelAfter(m_flushPeriod);
            });

            var nextTime = DateTime.MinValue;

            while (true)
            {
                var cancellationRequested = cancellationToken.IsCancellationRequested;
                var currTime = DateTime.UtcNow;

                if (nextTime <= currTime || cancellationRequested)
                {
                    nextTime = currTime.Add(m_flushPeriod);

                    while (m_messageQueue.TryTake(out var message))
                    {
                        m_currentBatch.Add(message);
                    }

                    if (0 < m_currentBatch.Count)
                    {
                        try
                        {
                            await m_batchMessagesWriter.PushEventsAsync(m_currentBatch, stopWrittingTokenSource.Token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            SelfLog.WriteLine(ex.ToString());
                        }

                        m_currentBatch.Clear();
                    }

                    if (cancellationRequested)
                    {
                        break;
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            m_outputStoppingTokenSource.Cancel();
            m_messageQueue.CompleteAdding();

            try
            {
                m_outputTask.Wait();
            }
            catch (TaskCanceledException)
            {
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException)
            {
            }

            m_batchMessagesWriter.Dispose();
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            if (!m_messageQueue.IsAddingCompleted)
            {
                try
                {
                    if (!m_messageQueue.TryAdd(logEvent, millisecondsTimeout: 0, cancellationToken: m_outputStoppingTokenSource.Token))
                    {
                        // message is dropped
                    }
                }
                catch
                {
                    //cancellation token canceled or CompleteAdding called
                }
            }
        }
    }
}