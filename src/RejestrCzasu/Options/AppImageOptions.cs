namespace RejestrCzasu.Options;

/// <summary>
/// The tag of the running application image, reported by <c>/health</c> (AD-20). It comes from the top-level key
/// <see cref="TagKey"/>: the <c>APP_IMAGE_TAG</c> environment variable baked into the image by the Dockerfile
/// (<c>--build-arg APP_IMAGE_TAG=...</c>) or set by the server's <c>.env</c>; without it <see cref="DefaultTag"/>.
/// </summary>
public sealed class AppImageOptions
{
    public const string TagKey = "APP_IMAGE_TAG";
    public const string DefaultTag = "local";

    public string Tag { get; set; } = DefaultTag;
}
