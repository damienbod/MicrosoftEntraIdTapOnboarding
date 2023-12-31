using Microsoft.Graph.Models;
using OnboardingTap.Pages;
using System.Security.Cryptography;

namespace OnboardingTap;

public class AadGraphSdkManagedIdentityAppClient
{
    private readonly GraphApplicationClientService _graphService;
    private readonly string _aadIssuerDomain = "damienbodsharepoint.onmicrosoft.com";

    public AadGraphSdkManagedIdentityAppClient(IConfiguration configuration,
        GraphApplicationClientService graphService)
    {
        _graphService = graphService;

        var aadDomain = configuration.GetValue<string>("AadIssuerDomain");
        if (aadDomain != null)
        {
            _aadIssuerDomain = aadDomain;
        }
    }

    public async Task<TemporaryAccessPassAuthenticationMethod?> AddTapForUserAsync(string userId)
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        TemporaryAccessPassAuthenticationMethod tempAccessPassAuthMethod = new()
        {
            //StartDateTime = DateTimeOffset.Now,
            LifetimeInMinutes = 60,
            IsUsableOnce = true,
        };

        var result = await graphServiceClient.Users[userId]
            .Authentication
            .TemporaryAccessPassMethods
            .PostAsync(tempAccessPassAuthMethod);

        return result;
    }

    public async Task<CreatedUserModel> CreateGraphMemberUserAsync(UserModel userModel)
    {
        if (!userModel.Email.ToLower().EndsWith(_aadIssuerDomain.ToLower()))
        {
            throw new ArgumentException("A guest user must be invited!");
        }

        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        var password = GetRandomString();
        var user = new User
        {
            DisplayName = userModel.UserName,
            Surname = userModel.LastName,
            GivenName = userModel.FirstName,
            OtherMails = [userModel.Email],
            UserType = "member",
            AccountEnabled = true,
            UserPrincipalName = userModel.Email,
            MailNickname = userModel.UserName,
            //Identities = new List<ObjectIdentity>()
            //{
            //    new ObjectIdentity
            //    {
            //        SignInType = "federated",
            //        Issuer = _aadIssuerDomain, 
            //        IssuerAssignedId = userModel.Email
            //    }
            //},
            PasswordProfile = new PasswordProfile
            {
                Password = password,
                // We use TAP if a paswordless onboarding is used
                ForceChangePasswordNextSignIn = !userModel.UsePasswordless
            },
            PasswordPolicies = "DisablePasswordExpiration"
        };

        var createdUser = await graphServiceClient.Users
            .PostAsync(user);

        return new CreatedUserModel
        {
            Email = createdUser!.UserPrincipalName!,
            Id = createdUser.Id!,
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

        var invite = await graphServiceClient
            .Invitations
            .PostAsync(invitation);

        return invite;
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
