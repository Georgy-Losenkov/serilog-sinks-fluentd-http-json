# Serilog.Sinks.Fluentd.Http.Json [![verify][]](https://github.com/Georgy-Losenkov/serilog-sinks-fluentd-http-json)

[verify]: https://github.com/Georgy-Losenkov/serilog-sinks-fluentd-http-json/workflows/verify/badge.svg

[nuget]: https://github.com/Georgy-Losenkov/serilog-sinks-fluentd-http-json/workflows/verify/badge.svg

## Table of contents

* [What is this sink](#what-is-this-sink)
* [Features](#features)
* [Quick start](#quick-start)
* [Sink parameters](#sink-parameters)

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

## Sink parameters

