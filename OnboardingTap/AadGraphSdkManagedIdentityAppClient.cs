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