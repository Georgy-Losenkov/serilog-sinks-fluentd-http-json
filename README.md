# Serilog.Sinks.Fluentd.Http.Json [![verify][]](https://github.com/Georgy-Losenkov/serilog-sinks-fluentd-http-json) [![Nuget][]](https://www.nuget.org/packages/Serilog.Sinks.Fluentd.Http.Json)

[verify]: https://github.com/Georgy-Losenkov/serilog-sinks-fluentd-http-json/workflows/verify/badge.svg

[nuget]: https://img.shields.io/nuget/v/Serilog.Sinks.Fluentd.Http.Json

## Table of contents

* [What is this sink](#what-is-this-sink)
* [Features](#features)
* [Quick start](#quick-start)
* [Parameters](#parameters)
  * [bodySizeLimit](#bodysizelimit)
  * [flushPeriod](#flushperiod)
  * [httpTimeout](#httptimeout)
  * [maxQueueSize](#maxqueuesize)
  * [jsonFormatter](#jsonformatter)
  * [url](#url)

## What is this sink

The Serilog Fluentd Http Json sink project is a sink (basically a writer) for the Serilog logging framework. Structured log events are written to sinks and each sink is responsible for writing it to its own backend, database, store etc. This sink delivers the data to the http endpoint of the Fluentd daemon in json format. By default it uses Serilog.Formatting.ElasticSearch json formatter, making it suitable for forwarding to ElasticSearch.

## Features

* Simple configuration to get log events published to Fluentd daemon. Only endpoint url is needed.
* Log events are accumulated in the memory and periodically are sent in batches  LoPeriodic batching 

## Quick start

Install the Serilog.Sinks.Fluentd.Http.Json package from NuGet:

```powershell
Install-Package Serilog.Sinks.Fluentd.Http.Json
```

To configure the sink in C# code, call WriteTo.Fluentd() during logger configuration:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.Fluentd()
    .CreateLogger();
```

To configure the sink through configuration file using [Serilog.Settings.Configuration][], just add { "Name": "Fluentd" } into "WriteTo" section:

[Serilog.Settings.Configuration]: https://github.com/serilog/serilog-settings-configuration#serilogsettingsconfiguration--

```json
"WriteTo": [
  ...
  { "Name": "Fluentd" }
]
```

## Parameters
```csharp
public static LoggerConfiguration Fluentd(
    this LoggerSinkConfiguration sinkConfiguration,
    Int32 bodySizeLimit = 32 * 1024 * 1024,
    TimeSpan? flushPeriod = null,
    TimeSpan? httpTimeout = null,
    Int32? maxQueueSize = 10000,
    ITextFormatter jsonFormatter = null,
    String url = "http://localhost:8888/logging.log")
```

### bodySizeLimit
The size limit of the json sent to fluentd. Default value is 32Mb (33554432 bytes). This corresponds to default value of parameter [body_size_limit][] of fluentd http input plugin.

[body_size_limit]: https://docs.fluentd.org/input/http#body_size_limit

### flushPeriod
Interval between communication sessions with fluentd. Default value is null value. That corresponds to 4 sec.<br/>When sink is being disposed this value is used as timeout for sending remaining log events to fluentd.

### httpTimeout
Time to wait for submitting to complete. Default value is null value. That corresponds to 3 sec.

### maxQueueSize
Maximum size of the queue accumulating log events. Default value is 10000. Null value means no limits.

### jsonFormatter
The JSON formatter. By default [Serilog.Formatting.ElasticSearch][] formatter is used.

[Serilog.Formatting.ElasticSearch]: https://github.com/serilog/serilog-sinks-elasticsearch#elasticsearch-formatters

### url
The URL of the fluentd http input endpoint appended by the tag name.<br/>Because of limitaions of the fluentd http input plugin tag must contain period, e.g logging.log.
