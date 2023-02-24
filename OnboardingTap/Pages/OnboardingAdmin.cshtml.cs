using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OnboardingTap.Pages;

public class OnboardingAdminModel : PageModel
{
    private readonly AadGraphSdkManagedIdentityAppClient _aadGraphSdkManagedIdentityAppClient;
    private readonly string _aadIssuerDomain = "damienbodsharepoint.onmicrosoft.com";

    public OnboardingAdminModel(AadGraphSdkManagedIdentityAppClient aadGraphSdkManagedIdentityAppClient)
    {
        _aadGraphSdkManagedIdentityAppClient = aadGraphSdkManagedIdentityAppClient;
    }

    [BindProperty]
    public UserModel? UserData { get; set; } = new UserModel();

    [BindProperty]
    public CreatedAccessModel? AccessInfo { get; set; } = new CreatedAccessModel();

    public void OnGet()
    {
        UserData = new UserModel
        {
            Email = "tst5@damienbodsharepoint.onmicrosoft.com",
            UserName = "tst5",
            LastName = "last-tst5",
            FirstName = "first-tst5"
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        (string? Upn, string? Id, string Password) createdUser;
        if (UserData != null)
        {
            if (UserData.Email.ToLower().EndsWith(_aadIssuerDomain.ToLower()))
            {
                createdUser = await _aadGraphSdkManagedIdentityAppClient
                    .CreateMemberUserAsync(UserData);

                if (createdUser!.Id != null)
                {
                    var tap = await _aadGraphSdkManagedIdentityAppClient
                        .AddTapForUserAsync(createdUser.Id);

                    AccessInfo = new CreatedAccessModel
                    {
                        Email = createdUser.Upn,
                        TemporaryAccessPass = tap!.TemporaryAccessPass
                    };
                }
            }
            else
            {
                createdUser = await _aadGraphSdkManagedIdentityAppClient
                    .CreateGuestAsync(UserData);

                if (createdUser!.Id != null)
                {
                    AccessInfo = new CreatedAccessModel
                    {
                        Email = createdUser.Upn,
                        Password = createdUser.Password
                    };
                }
            }
        }

        return Page();
    }
}
