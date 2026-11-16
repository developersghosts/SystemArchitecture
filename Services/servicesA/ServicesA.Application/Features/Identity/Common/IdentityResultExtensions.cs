namespace ServicesA.Application.Features.Identity.Common;

public static class IdentityResultExtensions
{
    /// <summary>Throws BadRequestException with the Identity error descriptions when the result failed.</summary>
    public static void EnsureSucceeded(this IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
