# Azure AD Temporary Access Pass (TAP) Onboarding

[![.NET](https://github.com/damienbod/AzureAdTapOnboarding/actions/workflows/dotnet.yml/badge.svg)](https://github.com/damienbod/AzureAdTapOnboarding/actions/workflows/dotnet.yml)

## Using Temporary Access Pass (TAP) with members

Note: TAP only works with members and a passwordless authentication once setup

## Creating AAD member users

```csharp
// TODO
// 1. When do I use Identities? ie: federated
// 2. Do members need this? => no
Identities = new List<ObjectIdentity>()
{
    new ObjectIdentity
    {
        SignInType = "federated",
        Issuer = "ExternalAzureAD", //_aadIssuerDomain, // "ExternalAzureAD", "MicrosoftAccount", 
        IssuerAssignedId = userModel.Email // TODO do I need this?
    }
},
// TODO
// 3. Do I need a password for guests without a federated identity? 
// 4. Do I need a password for guests with a federated identity? 
// 5. Are passwords required for members? => yes
PasswordProfile = new PasswordProfile
{
    Password = password,
    ForceChangePasswordNextSignIn = ForcePasswordChange(userModel)
},
```

## Creating AAD guest users


```csharp
// TODO
// 1. When do I use Identities? ie: federated
// 2. Do members need this? => no
Identities = new List<ObjectIdentity>()
{
    new ObjectIdentity
    {
        SignInType = "federated",
        Issuer = _aadIssuerDomain, // "ExternalAzureAD", "MicrosoftAccount", 
        IssuerAssignedId = userModel.Email // TODO do I need this?
    }
},
// TODO
// 3. Do I need a password for guests without a federated identity? => yes
// 4. Do I need a password for guests with a federated identity? => no if the _aadIssuerDomain is used
// 5. Are passwords required for members? => yes
PasswordProfile = new PasswordProfile
{
    Password = password,
    ForceChangePasswordNextSignIn = ForcePasswordChange(userModel)
},
```

# Links

https://learn.microsoft.com/en-us/azure/active-directory/authentication/howto-authentication-temporary-access-pass

https://learn.microsoft.com/en-us/graph/api/authentication-post-temporaryaccesspassmethods?view=graph-rest-1.0&tabs=csharp

https://learn.microsoft.com/en-us/graph/authenticationmethods-get-started

https://learn.microsoft.com/en-us/azure/active-directory/authentication/howto-authentication-passwordless-security-key-on-premises

https://damienbod.com/2022/03/11/create-azure-b2c-users-with-microsoft-graph-and-asp-net-core/

https://damienbod.com/2022/03/24/onboarding-new-users-in-an-asp-net-core-application-using-azure-b2c/

https://damienbod.com/2022/08/02/workarounds-to-disable-azure-ad-user-using-microsoft-graph-and-an-application-scope/

https://damienbod.com/2022/07/11/invite-external-users-to-azure-ad-using-microsoft-graph-and-asp-net-core/

https://www.youtube.com/watch?v=SuBeZ9VH8dI&t=1207s

https://learn.microsoft.com/en-us/azure/active-directory/external-identities/external-identities-overview