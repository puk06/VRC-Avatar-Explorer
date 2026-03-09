using ErrorOr;

namespace AvatarExplorer.Core.Extensions;

public static class ErrorExtensions
{
    public static string ToErrorString(this List<Error> errors)
    {
        return string.Join(", ", errors.Select(e => e.Description));
    }
}
