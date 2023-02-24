using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

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
            MailNickname = userModel.UserName
        };

        try
        {
            await graphServiceClient.Users.PostAsync(user);
        }
        catch(Exception ex)
        {
            var test = ex.Message;
            throw ex;
        }

        // Needs an SPO license
        //var patchValues = new User()
        //{
        //    Birthday = userModel.BirthDate.ToUniversalTime()
        //};

        //var request = _graphServiceClient.Users[createdUser.Id].Request();
        //await request.UpdateAsync(patchValues);

        return (user.UserPrincipalName, user.Id);
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
            .PostAsync(user);

        return (user.UserPrincipalName, user.Id);
    }
}
