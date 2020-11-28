# Serilog.Sinks.Fluentd.Http.Json [![verify][]](https://github.com/Georgy-Losenkov/serilog-sinks-fluentd-http-json)

[verify]: https://github.com/Georgy-Losenkov/serilog-sinks-fluentd-http-json/workflows/verify/badge.svg

[nuget]: https://github.com/Georgy-Losenkov/serilog-sinks-fluentd-http-json/workflows/verify/badge.svg

## Table of contents

* [What is this sink](#what-is-this-sink)
* [Features](#features)
* [Quick start](#quick-start)
* [Sink parameters](#sink-parameters)
  * [Body Size Limit](#body-size-limit)
  * [Flush Period](#flush-period)
  * [Http Timeout](#http-timeout)
  * [Max Queue Size](#max-queue-size)
  * [Json Formatter](#json-formatter)
  * [Url](#url)

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
<a name="body-size-limit"></a>
```csharp
System.Int32 bodySizeLimit = 32 * 1024 * 1024
```
The body size limit. Default value is 32Mb (33554432 bytes).

<a name="flush-period"></a>
```csharp
System.TimeSpan? flushPeriod = null
```
The flush period. Default value is 4 sec.

<a name="http-timeout"></a>
```csharp
System.TimeSpan? httpTimeout = null
```
Time to wait for submitting to complete. Default value is 3 sec.

<a name="max-queue-size"></a>
```csharp
System.Int32? maxQueueSize = 10000
```
Maximum size of the queue accumulating log events. Default value is 10000.

<a name="json-formatter"></a>
```csharp
ITextFormatter jsonFormatter = null
```
The JSON formatter. By default [Serilog.Formatting.ElasticSearch] formatter is used.

[Serilog.Formatting.ElasticSearch]: Serilog.Formatting.ElasticSearch

<a name="url"></a>
```csharp
System.String url = "http://localhost:8888/logging.log"
```
The URL of the fluentd http input endpoint appended by the tag name.<br/>
Because of limitaions of the fluentd http input tag must contain period, e.g logging.log. 
