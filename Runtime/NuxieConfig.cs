using System;
using System.Collections.Generic;

namespace Nuxie.Unity;

public enum NuxieEnvironment
{
  Production,
  Staging,
  Development,
  Custom,
}

public enum NuxieLogLevel
{
  Verbose,
  Debug,
  Info,
  Warning,
  Error,
  None,
}

public enum EventLinkingPolicy
{
  KeepSeparate,
  MigrateOnIdentify,
}

public sealed class NuxieConfig
{
  public NuxieConfig(string apiKey)
  {
    ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
  }

  public string ApiKey { get; }

  public NuxieEnvironment? Environment { get; init; }
  public string? ApiEndpoint { get; init; }
  public NuxieLogLevel? LogLevel { get; init; }
  public bool? EnableConsoleLogging { get; init; }
  public bool? EnableFileLogging { get; init; }
  public bool? RedactSensitiveData { get; init; }
  public int? RequestTimeoutSeconds { get; init; }
  public int? RetryCount { get; init; }
  public int? RetryDelaySeconds { get; init; }
  public int? SyncIntervalSeconds { get; init; }
  public bool? EnableCompression { get; init; }
  public int? EventBatchSize { get; init; }
  public int? FlushAt { get; init; }
  public int? FlushIntervalSeconds { get; init; }
  public int? MaxQueueSize { get; init; }
  public long? MaxCacheSizeBytes { get; init; }
  public int? CacheExpirationSeconds { get; init; }
  public bool? EnableEncryption { get; init; }
  public string? CustomStoragePath { get; init; }
  public int? FeatureCacheTtlSeconds { get; init; }
  public int? DefaultPaywallTimeoutSeconds { get; init; }
  public bool? RespectDoNotTrack { get; init; }
  public EventLinkingPolicy? EventLinkingPolicy { get; init; }
  public string? LocaleIdentifier { get; init; }
  public bool? IsDebugMode { get; init; }
  public bool? EnablePlugins { get; init; }
  public long? MaxFlowCacheSizeBytes { get; init; }
  public int? FlowCacheExpirationSeconds { get; init; }
  public int? MaxConcurrentFlowDownloads { get; init; }
  public int? FlowDownloadTimeoutSeconds { get; init; }
  public string? FlowCacheDirectory { get; init; }
  public int PurchaseRequestTimeoutSeconds { get; init; } = 60;
  public int RestoreRequestTimeoutSeconds { get; init; } = 60;

  internal Dictionary<string, object?> ToBridgeOptions()
  {
    var options = new Dictionary<string, object?>(StringComparer.Ordinal);

    if (Environment.HasValue)
    {
      options["environment"] = Environment.Value switch
      {
        NuxieEnvironment.Production => "production",
        NuxieEnvironment.Staging => "staging",
        NuxieEnvironment.Development => "development",
        NuxieEnvironment.Custom => "custom",
        _ => "production",
      };
    }

    AddIfNotNull(options, "apiEndpoint", ApiEndpoint);
    if (LogLevel.HasValue)
    {
      options["logLevel"] = LogLevel.Value switch
      {
        NuxieLogLevel.Verbose => "verbose",
        NuxieLogLevel.Debug => "debug",
        NuxieLogLevel.Info => "info",
        NuxieLogLevel.Warning => "warning",
        NuxieLogLevel.Error => "error",
        NuxieLogLevel.None => "none",
        _ => "warning",
      };
    }

    AddIfNotNull(options, "enableConsoleLogging", EnableConsoleLogging);
    AddIfNotNull(options, "enableFileLogging", EnableFileLogging);
    AddIfNotNull(options, "redactSensitiveData", RedactSensitiveData);
    AddIfNotNull(options, "requestTimeoutSeconds", RequestTimeoutSeconds);
    AddIfNotNull(options, "retryCount", RetryCount);
    AddIfNotNull(options, "retryDelaySeconds", RetryDelaySeconds);
    AddIfNotNull(options, "syncIntervalSeconds", SyncIntervalSeconds);
    AddIfNotNull(options, "enableCompression", EnableCompression);
    AddIfNotNull(options, "eventBatchSize", EventBatchSize);
    AddIfNotNull(options, "flushAt", FlushAt);
    AddIfNotNull(options, "flushIntervalSeconds", FlushIntervalSeconds);
    AddIfNotNull(options, "maxQueueSize", MaxQueueSize);
    AddIfNotNull(options, "maxCacheSizeBytes", MaxCacheSizeBytes);
    AddIfNotNull(options, "cacheExpirationSeconds", CacheExpirationSeconds);
    AddIfNotNull(options, "enableEncryption", EnableEncryption);
    AddIfNotNull(options, "customStoragePath", CustomStoragePath);
    AddIfNotNull(options, "featureCacheTtlSeconds", FeatureCacheTtlSeconds);
    AddIfNotNull(options, "defaultPaywallTimeoutSeconds", DefaultPaywallTimeoutSeconds);
    AddIfNotNull(options, "respectDoNotTrack", RespectDoNotTrack);
    if (EventLinkingPolicy.HasValue)
    {
      options["eventLinkingPolicy"] = EventLinkingPolicy.Value switch
      {
        global::Nuxie.Unity.EventLinkingPolicy.KeepSeparate => "keep_separate",
        global::Nuxie.Unity.EventLinkingPolicy.MigrateOnIdentify => "migrate_on_identify",
        _ => "migrate_on_identify",
      };
    }

    AddIfNotNull(options, "localeIdentifier", LocaleIdentifier);
    AddIfNotNull(options, "isDebugMode", IsDebugMode);
    AddIfNotNull(options, "enablePlugins", EnablePlugins);
    AddIfNotNull(options, "maxFlowCacheSizeBytes", MaxFlowCacheSizeBytes);
    AddIfNotNull(options, "flowCacheExpirationSeconds", FlowCacheExpirationSeconds);
    AddIfNotNull(options, "maxConcurrentFlowDownloads", MaxConcurrentFlowDownloads);
    AddIfNotNull(options, "flowDownloadTimeoutSeconds", FlowDownloadTimeoutSeconds);
    AddIfNotNull(options, "flowCacheDirectory", FlowCacheDirectory);

    return options;
  }

  private static void AddIfNotNull(Dictionary<string, object?> options, string key, object? value)
  {
    if (value is null)
    {
      return;
    }

    options[key] = value;
  }
}
