
using Microsoft.Graph;

namespace OnboardingTap;

public class AadGraphSdkManagedIdentityAppClient
{
    private readonly IConfiguration _configuration;
    private readonly GraphApplicationClientService _graphService;
    private readonly string _aadIssuerDomain = "damienbodsharepoint.onmicrosoft.com";

    public AadGraphSdkManagedIdentityAppClient(IConfiguration configuration, 
        GraphApplicationClientService graphService)
    {
        _configuration = configuration;
        _graphService = graphService;
    }

    public async Task<int> GetUsersAsync()
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        IGraphServiceUsersCollectionPage users = await graphServiceClient.Users
            .Request()
            .GetAsync();

        return users.Count;
    }

    public async Task<TemporaryAccessPassAuthenticationMethod?> AddTapForUserAsync(string userId)
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        var tempAccessPassAuthMethod = new TemporaryAccessPassAuthenticationMethod
        {
            //StartDateTime = DateTimeOffset.Now,
            LifetimeInMinutes = 60,
            IsUsableOnce = true, 
        };

        var result = await graphServiceClient.Users[userId]
            .Authentication
            .TemporaryAccessPassMethods
            .Request()
            .AddAsync(tempAccessPassAuthMethod);

        return result;
    }

    public async Task<(string? Upn, string? Id)> CreateUser(UserModel userModel)
    {
        //var invitedUser = await InviteGuestUser(userModel, "https://localhost:5002");
        //return (invitedUser!.InvitedUserEmailAddress, invitedUser!.Id);

        var createdUser = await CreateFederatedNoPasswordAsync(userModel);
        return createdUser;

        //var createdUser = await CreateSameDomainUserAsync(userModel);
        //return createdUser;

    }

    private async Task<(string? Upn, string? Id)> CreateSameDomainUserAsync(UserModel userModel)
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        if (!userModel.Email.ToLower().EndsWith(_aadIssuerDomain.ToLower()))
        {
            throw new ArgumentException("incorrect Email domain");
        }

        var user = new User
        {
            AccountEnabled = true,
            UserPrincipalName = userModel.Email,
            DisplayName = userModel.UserName,
            Surname = userModel.LastName,
            GivenName = userModel.FirstName,
            MailNickname = userModel.UserName,  
            UserType = GetUserType(userModel),
            PasswordProfile = new PasswordProfile
            {
                Password = "ffDs2a2rf-2Wf(_",
                ForceChangePasswordNextSignIn = false
            },
            PasswordPolicies = "DisablePasswordExpiration"
        };

        var createdUser = await graphServiceClient.Users.Request().AddAsync(user);

        return (createdUser.UserPrincipalName, createdUser.Id);
    }

    private async Task<(string? Upn, string? Id)> CreateFederatedNoPasswordAsync(UserModel userModel)
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        var user = new User
        {
            DisplayName = userModel.UserName,
            Surname = userModel.LastName,
            GivenName = userModel.FirstName,
            OtherMails = new List<string> { userModel.Email },
            UserType = GetUserType(userModel),

            AccountEnabled = true,
            UserPrincipalName = userModel.Email,
            MailNickname = userModel.UserName,

            Identities = new List<ObjectIdentity>()
            {
                new ObjectIdentity
                {
                    SignInType = "federated",
                    Issuer = _aadIssuerDomain,
                    IssuerAssignedId = userModel.Email
                },
            }
        };

        var createdUser = await graphServiceClient.Users
            .Request()
            .AddAsync(user);

        return (createdUser.UserPrincipalName, createdUser.Id);
    }

    public async Task<Invitation?> InviteGuestUser(UserModel userModel, string redirectUrl)
    {
        if (userModel.Email.ToLower().EndsWith(_aadIssuerDomain.ToLower()))
        {
            throw new ArgumentException("user must be from a different domain!");
        }
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        var invitation = new Invitation
        {
            InvitedUserEmailAddress = userModel.Email,
            SendInvitationMessage = true,
            InvitedUserDisplayName = $"{userModel.FirstName} {userModel.LastName}",
            InviteRedirectUrl = redirectUrl,
            InvitedUserType = "guest"
        };

        var invite = await graphServiceClient.Invitations
            .Request()
            .AddAsync(invitation);

        return invite;
    }

    private string GetUserType(UserModel userModel)
    {
        var userType = "guest";
        if (userModel.Email.ToLower().EndsWith(_aadIssuerDomain.ToLower()))
        {
            userType = "member";
        }

        return userType;
    }

}
