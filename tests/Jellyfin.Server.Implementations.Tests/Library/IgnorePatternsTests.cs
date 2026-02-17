using System;
using System.IO;
using System.Text.Json;
using Emby.Server.Implementations.Library;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public sealed class IgnorePatternsTests : IDisposable
    {
        private readonly string _tempDir;

        public IgnorePatternsTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "jellyfin-test-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }

            GC.SuppressFinalize(this);
        }

        private IgnorePatterns CreateInstance(string? configDir = null)
        {
            var appPaths = new Mock<IApplicationPaths>();
            appPaths.SetupGet(x => x.ConfigurationDirectoryPath).Returns(configDir ?? _tempDir);
            return new IgnorePatterns(appPaths.Object, NullLogger<IgnorePatterns>.Instance);
        }

        [Theory]
        [InlineData("/media/small.jpg", true)]
        [InlineData("/media/albumart.jpg", true)]
        [InlineData("/media/movie.sample.mp4", true)]
        [InlineData("/media/movie/sample.mp4", true)]
        [InlineData("/media/movie/sample/movie.mp4", true)]
        [InlineData("/foo/sample/bar/baz.mkv", false)]
        [InlineData("/media/movies/the sample/the sample.mkv", false)]
        [InlineData("/media/movies/sampler.mkv", false)]
        [InlineData("/media/movies/#Recycle/test.txt", true)]
        [InlineData("/media/movies/#recycle/", true)]
        [InlineData("/media/movies/#recycle", true)]
        [InlineData("thumbs.db", true)]
        [InlineData(@"C:\media\movies\movie.avi", false)]
        [InlineData("/media/.hiddendir/file.mp4", false)]
        [InlineData("/media/dir/.hiddenfile.mp4", true)]
        [InlineData("/media/dir/._macjunk.mp4", true)]
        [InlineData("/volume1/video/Series/@eaDir", true)]
        [InlineData("/volume1/video/Series/@eaDir/file.txt", true)]
        [InlineData("/directory/@Recycle", true)]
        [InlineData("/directory/@Recycle/file.mp3", true)]
        [InlineData("/media/movies/.@__thumb", true)]
        [InlineData("/media/movies/.@__thumb/foo-bar-thumbnail.png", true)]
        [InlineData("/media/music/Foo B.A.R./epic.flac", false)]
        [InlineData("/media/music/Foo B.A.R", false)]
        [InlineData("/media/music/Foo B.A.R.", false)]
        [InlineData("/movies/.zfs/snapshot/AutoM-2023-09", true)]
        public void PathIgnored(string path, bool expected)
        {
            var instance = CreateInstance();
            Assert.Equal(expected, instance.ShouldIgnore(path));
        }

        [Fact]
        public void CreatesDefaultConfigFile()
        {
            var configPath = Path.Combine(_tempDir, "ignorepatterns.json");
            Assert.False(File.Exists(configPath));

            CreateInstance();

            Assert.True(File.Exists(configPath));
            var patterns = JsonSerializer.Deserialize<string[]>(File.ReadAllText(configPath));
            Assert.NotNull(patterns);
            Assert.NotEmpty(patterns);
            Assert.Contains("**/small.jpg", patterns);
        }

        [Fact]
        public void LoadsCustomPatterns()
        {
            var customPatterns = new[] { "**/custom.txt" };
            File.WriteAllText(
                Path.Combine(_tempDir, "ignorepatterns.json"),
                JsonSerializer.Serialize(customPatterns));

            var instance = CreateInstance();

            Assert.True(instance.ShouldIgnore("/media/custom.txt"));
            Assert.False(instance.ShouldIgnore("/media/small.jpg"));
        }

        [Fact]
        public void FallsBackOnInvalidJson()
        {
            File.WriteAllText(
                Path.Combine(_tempDir, "ignorepatterns.json"),
                "not valid json {{{");

            var instance = CreateInstance();

            // Should fall back to defaults
            Assert.True(instance.ShouldIgnore("/media/small.jpg"));
        }

        [Fact]
        public void FallsBackOnEmptyArray()
        {
            File.WriteAllText(
                Path.Combine(_tempDir, "ignorepatterns.json"),
                "[]");

            var instance = CreateInstance();

            // Should fall back to defaults
            Assert.True(instance.ShouldIgnore("/media/small.jpg"));
        }
    }
}
