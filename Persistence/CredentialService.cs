using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.Runtime;
using Newtonsoft.Json;
using Amazon.DynamoDBv2;

public class CredentialService
{
    private readonly IAmazonSecretsManager _secretsManagerClient;

    public CredentialService(IAmazonSecretsManager secretsManagerClient)
    {
        _secretsManagerClient = secretsManagerClient;
    }

    public async Task<IAmazonDynamoDB> GetInitializedDynamoClient()
    {
        var credentials = await GetAwsCredentialsAsync();
        var dynamoDBConfig = new AmazonDynamoDBConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast2 // Replace YOUR_REGION with your AWS region
        };
        return new AmazonDynamoDBClient(credentials, dynamoDBConfig);
    }

    private async Task<BasicAWSCredentials> GetAwsCredentialsAsync()
    {
        string secretName = "DDB-Jubilado";
        var secretValueRequest = new GetSecretValueRequest
        {
            SecretId = secretName
        };
        var secretValueResponse = await _secretsManagerClient.GetSecretValueAsync(secretValueRequest);

        // Parse the JSON to extract the access key and secret
        var secretData = JsonConvert.DeserializeObject<Dictionary<string, string>>(secretValueResponse.SecretString);
        return new BasicAWSCredentials(secretData.Keys.First(), secretData.Values.First());
    }
}