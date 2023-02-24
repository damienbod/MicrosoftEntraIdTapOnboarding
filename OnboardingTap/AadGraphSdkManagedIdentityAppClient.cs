using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace OnboardingTap;

public class AadGraphSdkManagedIdentityAppClient
{
    private readonly IConfiguration _configuration;
    private readonly GraphApplicationClientService _graphService;

    public AadGraphSdkManagedIdentityAppClient(IConfiguration configuration, 
        GraphApplicationClientService graphService)
    {
        _configuration = configuration;
        _graphService = graphService;
    }

    public async Task<long?> GetUsersAsync()
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        UserCollectionResponse? users = await graphServiceClient.Users
            .GetAsync();

        //IGraphServiceUsersCollectionPage users = await graphServiceClient.Users
        //    .Request()
        //    .GetAsync();

        return users!.OdataCount;
    }

    public async Task<Invitation?> InviteUser(UserModel userModel, string redirectUrl, bool asGuest)
    {
        var userType = "Guest";
        if(!asGuest) userType = "Member";

        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        var invitation = new Invitation
        {
            InvitedUserEmailAddress = userModel.Email,
            InvitedUser = new User
            {
                GivenName = userModel.FirstName,
                Surname = userModel.LastName,
                DisplayName = $"{userModel.FirstName} {userModel.LastName}",
                Mail = userModel.Email,
                UserType = userType,
                OtherMails = new List<string> { userModel.Email },
                //Identities = new List<ObjectIdentity>
                //{
                //    new ObjectIdentity
                //    {
                //        SignInType = "federated",
                //        Issuer = _federatedDomainDomain,
                //        IssuerAssignedId = userModel.Email
                //    },
                //},
                //PasswordPolicies = "DisablePasswordExpiration"
            },
            SendInvitationMessage = true,
            InviteRedirectUrl = redirectUrl,
            InvitedUserType = "guest" // default is guest,member
        };

        var invite = await graphServiceClient.Invitations
            .PostAsync(invitation);

        return invite;
    }

    public async Task<TemporaryAccessPassAuthenticationMethod?> AddTapForUserAsync(string userId)
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        var temporaryAccessPassAuthenticationMethod = new TemporaryAccessPassAuthenticationMethod
        {
            StartDateTime = DateTimeOffset.UtcNow,
            LifetimeInMinutes = 60,
            IsUsableOnce = true
        };

        var result = await graphServiceClient.Users[userId]
            .Authentication
            .TemporaryAccessPassMethods
            .PostAsync(temporaryAccessPassAuthenticationMethod);

        return result;
    }
}
