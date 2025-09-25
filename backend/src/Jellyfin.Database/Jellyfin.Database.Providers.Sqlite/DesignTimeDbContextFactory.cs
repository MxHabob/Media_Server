using System;
using System.IO;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// Design-time factory for creating JellyfinDbContext instances for migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
{
    /// <inheritdoc />
    public JellyfinDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();

        // Create a temporary data path for design-time
        var tempDataPath = Path.Combine(Path.GetTempPath(), "jellyfin-migrations");
        Directory.CreateDirectory(tempDataPath);

        var connectionString = $"Data Source={Path.Combine(tempDataPath, "jellyfin.db")}";

        optionsBuilder.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly);
        });

        // Create mock services for design-time
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<JellyfinDbContext>();
        var sqliteLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SqliteDatabaseProvider>();
        var lockLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<NoLockBehavior>();
        var applicationPaths = new MockApplicationPaths(tempDataPath);
        var databaseProvider = new SqliteDatabaseProvider(applicationPaths, sqliteLogger);
        var lockingBehavior = new NoLockBehavior(lockLogger);

        return new JellyfinDbContext(optionsBuilder.Options, logger, databaseProvider, lockingBehavior);
    }

    /// <summary>
    /// Mock implementation of IApplicationPaths for design-time.
    /// </summary>
    private class MockApplicationPaths : IApplicationPaths
    {
        public MockApplicationPaths(string dataPath)
        {
            DataPath = dataPath;
            ConfigurationDirectoryPath = dataPath;
            LogDirectoryPath = dataPath;
            CacheDirectoryPath = dataPath;
            TempDirectoryPath = dataPath;
            WebPath = dataPath;
            ProgramDataPath = dataPath;
            ProgramSystemPath = dataPath;
            ImageCachePath = dataPath;
            PluginsPath = dataPath;
            PluginConfigurationsPath = dataPath;
            SystemConfigurationFilePath = Path.Combine(dataPath, "system.xml");
            CachePath = dataPath;
            TempDirectory = dataPath;
            VirtualDataPath = dataPath;
            TrickplayPath = dataPath;
            BackupPath = dataPath;
        }

        public string DataPath { get; }

        public string ConfigurationDirectoryPath { get; }

        public string LogDirectoryPath { get; }

        public string CacheDirectoryPath { get; }

        public string TempDirectoryPath { get; }

        public string WebPath { get; }

        public string ProgramDataPath { get; }

        public string ProgramSystemPath { get; }

        public string ImageCachePath { get; }

        public string PluginsPath { get; }

        public string PluginConfigurationsPath { get; }

        public string SystemConfigurationFilePath { get; }

        public string CachePath { get; }

        public string TempDirectory { get; }

        public string VirtualDataPath { get; }

        public string TrickplayPath { get; }

        public string BackupPath { get; }

        public void MakeSanityCheckOrThrow()
        {
            // No-op for design-time
        }

        public void CreateAndCheckMarker(string markerPath, string markerContent, bool isDirectory)
        {
            // No-op for design-time
        }
    }
}
