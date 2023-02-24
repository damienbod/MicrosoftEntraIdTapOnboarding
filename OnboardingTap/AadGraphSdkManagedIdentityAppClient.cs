
using Microsoft.Graph;
using OnboardingTap.Pages;
using System.Security.Cryptography;

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

    /// <summary>
    /// Can be a guest or a member
    /// </summary>
    public async Task<CreatedUserModel> CreateGraphUserAsync(UserModel userModel)
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        var password = GetRandomString();
        var user = new User
        {
            DisplayName = userModel.UserName,
            Surname = userModel.LastName,
            GivenName = userModel.FirstName,
            OtherMails = new List<string> { userModel.Email },
            UserType = GetUserType(userModel),
            AccountEnabled = true,
            UserPrincipalName = GetUpn(userModel),
            MailNickname = userModel.UserName,
            Identities = new List<ObjectIdentity>()
            {
                new ObjectIdentity
                {
                    SignInType = "federated",
                    Issuer = _aadIssuerDomain,
                    IssuerAssignedId = userModel.Email
                }
            },
            PasswordProfile = new PasswordProfile
            {
                Password = password,
                ForceChangePasswordNextSignIn = ForcePasswordChange(userModel)
            },
            PasswordPolicies = "DisablePasswordExpiration"
        };

        var createdUser = await graphServiceClient.Users
            .Request()
            .AddAsync(user);

        return new CreatedUserModel
        {
            Email = createdUser.UserPrincipalName,
            Id = createdUser.Id,
            Password = password
        };
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

    private bool ForcePasswordChange(UserModel userModel)
    {
        if (userModel.Email.ToLower().EndsWith(_aadIssuerDomain.ToLower()))
        {
            return false;
        }

        return true;
    }

    private string GetUpn(UserModel userModel)
    {
        if (!userModel.Email.ToLower().EndsWith(_aadIssuerDomain.ToLower()))
        {
            return $"{userModel.Email.Replace('@', '_')}#EXT#@{_aadIssuerDomain}";
        }

        return userModel.Email;
    }

    private static string GetRandomString()
    {
        var random = $"{GenerateRandom()}{GenerateRandom()}{GenerateRandom()}{GenerateRandom()}-AC";
        return random;
    }

    private static int GenerateRandom()
    {
        return RandomNumberGenerator.GetInt32(100000000, int.MaxValue);
    }
}
