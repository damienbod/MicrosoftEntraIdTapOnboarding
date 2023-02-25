using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OnboardingTap.Pages;

public class OnboardingAdminModel : PageModel
{
    private readonly AadGraphSdkManagedIdentityAppClient _aadGraphSdkManagedIdentityAppClient;
    private readonly string _aadIssuerDomain = "damienbodsharepoint.onmicrosoft.com";

    public OnboardingAdminModel(AadGraphSdkManagedIdentityAppClient aadGraphSdkManagedIdentityAppClient,
        IConfiguration configuration)
    {
        _aadGraphSdkManagedIdentityAppClient = aadGraphSdkManagedIdentityAppClient;
        var aadDomain = configuration.GetValue<string>("AadIssuerDomain");
        if (aadDomain != null)
        {
            _aadIssuerDomain = aadDomain;
        }
    }

    [BindProperty]
    public UserModel? UserData { get; set; } = new UserModel();

    [BindProperty]
    public CreatedAccessModel? AccessInfo { get; set; } = new CreatedAccessModel();

    public void OnGet()
    {
        UserData = new UserModel
        {
            Email = $"tst5@{_aadIssuerDomain}",
            UserName = "tst5",
            LastName = "last-tst5",
            FirstName = "first-tst5"
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UserData == null) return Page();

        if (UserData.Email.ToLower().EndsWith(_aadIssuerDomain.ToLower()))
        {
            // member user, can use a TAP
            await CreateMember(UserData);
        }
        else
        {
            await InviteGuest(UserData);
        }

        return Page();
    }

    private async Task CreateMember(UserModel userData)
    {
        var createdUser = await _aadGraphSdkManagedIdentityAppClient
                        .CreateGraphMemberUserAsync(userData);

        if (createdUser!.Id != null)
        {
            // Graph needs a pause here...
            Thread.Sleep(5000);

            if (userData.UsePasswordless)
            {
                var tap = await _aadGraphSdkManagedIdentityAppClient
                    .AddTapForUserAsync(createdUser.Id);

                AccessInfo = new CreatedAccessModel
                {
                    Email = createdUser.Email,
                    TemporaryAccessPass = tap!.TemporaryAccessPass
                };
            }
            else
            {
                AccessInfo = new CreatedAccessModel
                {
                    Email = createdUser.Email,
                    Password = createdUser.Password
                };
            }
        }
    }

    private async Task InviteGuest(UserModel userData)
    {
        var invitedGuestUser = await _aadGraphSdkManagedIdentityAppClient
                        .InviteGuestUser(userData, "https://localhost:5002");

        if (invitedGuestUser!.Id != null)
        {
            AccessInfo = new CreatedAccessModel
            {
                Email = invitedGuestUser.InvitedUserEmailAddress,
                InviteRedeemUrl = invitedGuestUser.InviteRedeemUrl
            };
        }
    }
}
