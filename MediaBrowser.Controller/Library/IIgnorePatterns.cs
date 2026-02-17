using System;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Provides glob-based path ignore rules.
/// </summary>
public interface IIgnorePatterns
{
    /// <summary>
    /// Returns true if the supplied path should be ignored.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>Whether to ignore the path.</returns>
    bool ShouldIgnore(ReadOnlySpan<char> path);
}
