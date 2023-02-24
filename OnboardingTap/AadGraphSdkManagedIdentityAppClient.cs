
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
            //StartDateTime = DateTimeOffset.Parse(DateTimeOffset.Now.ToString("s")),
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

    public async Task<(string? Upn, string? Id)> CreateUser(UserModel userModel,  bool asGuest)
    {
        if (!asGuest)
        {
            var user = await CreateSameDomainUserAsync(userModel);
            return user;
        }
        else
        {
            var user = await CreateFederatedNoPasswordAsync(userModel);
            return user;
        }
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
            PasswordProfile = new PasswordProfile
            {
                Password = "ffDs2a2rf-2Wf(_",
                ForceChangePasswordNextSignIn = false
            },
            PasswordPolicies = "DisablePasswordExpiration"
        };

        var createdUser = await graphServiceClient.Users.Request().AddAsync(user);
  
        // Needs an SPO license
        //var patchValues = new User()
        //{
        //    Birthday = userModel.BirthDate.ToUniversalTime()
        //};

        //var request = _graphServiceClient.Users[createdUser.Id].Request();
        //await request.UpdateAsync(patchValues);

        return (createdUser.UserPrincipalName, createdUser.Id);
    }

    private async Task<(string? Upn, string? Id)> CreateFederatedNoPasswordAsync(UserModel userModel)
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        // User must already exist in AAD
        var user = new User
        {
            DisplayName = userModel.UserName,
            Surname = userModel.LastName,
            GivenName = userModel.FirstName,
            OtherMails = new List<string> { userModel.Email },
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
}
