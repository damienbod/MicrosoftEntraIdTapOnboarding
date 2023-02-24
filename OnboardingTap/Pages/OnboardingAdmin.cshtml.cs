using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OnboardingTap.Pages;

public class OnboardingAdminModel : PageModel
{
    private readonly AadGraphSdkManagedIdentityAppClient _aadGraphSdkManagedIdentityAppClient;

    public OnboardingAdminModel(AadGraphSdkManagedIdentityAppClient aadGraphSdkManagedIdentityAppClient)
    {
        _aadGraphSdkManagedIdentityAppClient = aadGraphSdkManagedIdentityAppClient;
    }

    [BindProperty]
    public UserModel? UserData { get; set; } = new UserModel();

    [BindProperty]
    public TapDataModel? Tap { get; set; } = new TapDataModel();

    public void OnGet()
    {
        UserData.Email = "tst@damienbodsharepoint.onmicrosoft.com";
    }

    public async Task<IActionResult> OnPostAsync()
    {

        if(UserData != null)
        {
            var invitedUser = await _aadGraphSdkManagedIdentityAppClient.InviteUser(
                UserData, "https://localhost:5002", false);

            if (invitedUser!.Id != null)
            {
                var tap = await _aadGraphSdkManagedIdentityAppClient.AddTapForUserAsync(invitedUser.Id);

                Tap = new TapDataModel
                {
                    Email = invitedUser.InvitedUserEmailAddress,
                    AccessCode = tap!.TemporaryAccessPass
                };
            }
        }

        return Page();
    }

}