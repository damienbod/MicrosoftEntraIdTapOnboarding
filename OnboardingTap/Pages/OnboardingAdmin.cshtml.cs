using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Polly;

namespace OnboardingTap.Pages;

public class OnboardingAdminModel : PageModel
{
    private readonly MeIdGraphSdkManagedIdentityAppClient _meIdGraphSdkManagedIdentityAppClient;
    private readonly string _microsoftEntraIDIssuerDomain = "damienbodsharepoint.onmicrosoft.com";
    private readonly string _inviteUrl = "https://localhost:5002";

    public OnboardingAdminModel(MeIdGraphSdkManagedIdentityAppClient meidGraphSdkManagedIdentityAppClient,
        IConfiguration configuration)
    {
        _meIdGraphSdkManagedIdentityAppClient = meidGraphSdkManagedIdentityAppClient;
        var microsoftEntraIDIssuerDomain = configuration.GetValue<string>("MicrosoftEntraIDIssuerDomain");
        if (microsoftEntraIDIssuerDomain != null)
        {
            _microsoftEntraIDIssuerDomain = microsoftEntraIDIssuerDomain;
        }
        var inviteUrl = configuration.GetValue<string>("InviteUrl");
        if (inviteUrl != null)
        {
            _inviteUrl = inviteUrl;
        }
    }

    [BindProperty]
    public UserModel UserData { get; set; } = new UserModel();

    [BindProperty]
    public CreatedAccessModel? AccessInfo { get; set; } = new CreatedAccessModel();

    public void OnGet()
    {
        UserData = new UserModel
        {
            Email = $"tst5@{_microsoftEntraIDIssuerDomain}",
            UserName = "tst5",
            LastName = "last-tst5",
            FirstName = "first-tst5"
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UserData == null) return Page();

        if (UserData.Email.ToLower().EndsWith(_microsoftEntraIDIssuerDomain.ToLower()))
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
        var createdUser = await _meIdGraphSdkManagedIdentityAppClient
                        .CreateGraphMemberUserAsync(userData);

        if (createdUser!.Id != null)
        {
            if (userData.UsePasswordless)
            {
                var maxRetryAttempts = 20;
                var pauseBetweenFailures = TimeSpan.FromSeconds(3);

                var retryPolicy = Policy
                    .Handle<ArgumentException>()
                    .WaitAndRetryAsync(maxRetryAttempts, i => pauseBetweenFailures);

                await retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var tap = await _meIdGraphSdkManagedIdentityAppClient
                        .AddTapForUserAsync(createdUser.Id);

                        AccessInfo = new CreatedAccessModel
                        {
                            Email = createdUser.Email,
                            TemporaryAccessPass = tap!.TemporaryAccessPass
                        };
                    }
                    catch (Exception ex)
                    {
                        // handle expected errors
                        if(ex.GetType() == typeof(HttpRequestException))
                            throw new ArgumentException(ex.Message);

                        if (ex.GetType() == typeof(Microsoft.Graph.Models.ODataErrors.ODataError))
                        { 
                            throw new ArgumentException(ex.Message);
                        }

                        // return 500 to UI
                        throw;
                    }
                    
                });
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
        var invitedGuestUser = await _meIdGraphSdkManagedIdentityAppClient
                        .InviteGuestUser(userData, _inviteUrl);

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
