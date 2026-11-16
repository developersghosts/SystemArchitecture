using ServicesA.Application.Features.Identity.Account.Queries.ShowAllOtp;

namespace ServicesA.API.Endpoints.Identity.Account.Query;

/// <summary>
/// Development only: lists pending login codes so you can test without an SMTP server.
/// Not mapped at all outside Development, because it would let anyone bypass the OTP step.
/// </summary>
public class ShowAllOTPApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        if (!app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
            return;

        app.MapGet($"{UrlTemplates.AccountRoute}/getOTP",
                async (string fullName, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(new ShowAllOtpQuery(fullName), token)))
            .WithTags("Account (Development)")
            .AllowAnonymous()
            .Produces<List<OtpDto>>();
    }
}
