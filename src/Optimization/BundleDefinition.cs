// MIT License.

namespace System.Web.Optimization;

/// <summary>
/// Represents a bundle definition as specified by the bundle manifest
/// </summary>
public sealed class BundleDefinition
{
    /// <summary>
    /// Virtual path for the bundle.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// CDN path for the bundle.
    /// </summary>
    public string? CdnPath { get; set; }

    /// <summary>
    /// CDN fallback expression for the bundle.
    /// </summary>
    public string? CdnFallbackExpression { get; set; }

    /// <summary>
    /// Files to be included in the bundle.
    /// </summary>
    public IList<string> Includes { get; internal set; } = [];
}
