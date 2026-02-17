using System;
using System.IO;
using System.Text.Json;
using DotNet.Globbing;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Glob patterns for files to ignore, loaded from an external JSON configuration file.
    /// </summary>
    public class IgnorePatterns : IIgnorePatterns
    {
        private const string ConfigFileName = "ignorepatterns.json";

        private static readonly string[] _defaultPatterns =
        {
            "**/small.jpg",
            "**/albumart.jpg",

            // We have neither non-greedy matching or character group repetitions, working around that here.
            // https://github.com/dazinator/DotNet.Glob#patterns
            // .*/sample\..{1,5}
            "**/sample.?",
            "**/sample.??",
            "**/sample.???", // Matches sample.mkv
            "**/sample.????", // Matches sample.webm
            "**/sample.?????",
            "**/*.sample.?",
            "**/*.sample.??",
            "**/*.sample.???",
            "**/*.sample.????",
            "**/*.sample.?????",
            "**/sample/*",

            // Directories
            "**/metadata/**",
            "**/metadata",
            "**/ps3_update/**",
            "**/ps3_update",
            "**/ps3_vprm/**",
            "**/ps3_vprm",
            "**/extrafanart/**",
            "**/extrafanart",
            "**/extrathumbs/**",
            "**/extrathumbs",
            "**/.actors/**",
            "**/.actors",
            "**/.wd_tv/**",
            "**/.wd_tv",
            "**/lost+found/**",
            "**/lost+found",
            "**/subs/**",
            "**/subs",
            "**/.snapshots/**",
            "**/.snapshots",
            "**/.snapshot/**",
            "**/.snapshot",

            // Trickplay files
            "**/*.trickplay",
            "**/*.trickplay/**",

            // WMC temp recording directories that will constantly be written to
            "**/TempRec/**",
            "**/TempRec",
            "**/TempSBE/**",
            "**/TempSBE",

            // Synology
            "**/eaDir/**",
            "**/eaDir",
            "**/@eaDir/**",
            "**/@eaDir",
            "**/#recycle/**",
            "**/#recycle",

            // Qnap
            "**/@Recycle/**",
            "**/@Recycle",
            "**/.@__thumb/**",
            "**/.@__thumb",
            "**/$RECYCLE.BIN/**",
            "**/$RECYCLE.BIN",
            "**/System Volume Information/**",
            "**/System Volume Information",
            "**/.grab/**",
            "**/.grab",

            // Unix hidden files
            "**/.*",

            // thumbs.db
            "**/thumbs.db",

            // bts sync files
            "**/*.bts",
            "**/*.sync",

            // zfs
            "**/.zfs/**",
            "**/.zfs"
        };

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        private static readonly GlobOptions _globOptions = new GlobOptions
        {
            Evaluation =
            {
                CaseInsensitive = true
            }
        };

        private readonly Glob[] _globs;

        /// <summary>
        /// Initializes a new instance of the <see cref="IgnorePatterns"/> class.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="logger">The logger.</param>
        public IgnorePatterns(IApplicationPaths appPaths, ILogger<IgnorePatterns> logger)
        {
            var patterns = LoadPatterns(appPaths.ConfigurationDirectoryPath, logger);
            _globs = Array.ConvertAll(patterns, p => Glob.Parse(p, _globOptions));
        }

        /// <inheritdoc />
        public bool ShouldIgnore(ReadOnlySpan<char> path)
        {
            int len = _globs.Length;
            for (int i = 0; i < len; i++)
            {
                if (_globs[i].IsMatch(path))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] LoadPatterns(string configDir, ILogger logger)
        {
            var configPath = Path.Combine(configDir, ConfigFileName);

            if (!File.Exists(configPath))
            {
                try
                {
                    Directory.CreateDirectory(configDir);
                    var json = JsonSerializer.Serialize(_defaultPatterns, _jsonOptions);
                    File.WriteAllText(configPath, json);
                    logger.LogInformation("Created default ignore patterns configuration at {Path}", configPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to write default ignore patterns to {Path}", configPath);
                }

                return _defaultPatterns;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var patterns = JsonSerializer.Deserialize<string[]>(json);
                if (patterns is null || patterns.Length == 0)
                {
                    logger.LogWarning("Ignore patterns file {Path} is empty, using defaults", configPath);
                    return _defaultPatterns;
                }

                logger.LogInformation("Loaded {Count} ignore patterns from {Path}", patterns.Length, configPath);
                return patterns;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read ignore patterns from {Path}, using defaults", configPath);
                return _defaultPatterns;
            }
        }
    }
}
