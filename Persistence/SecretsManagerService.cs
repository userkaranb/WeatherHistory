using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class SecretManagerService
{
    private readonly IAmazonSecretsManager _secretsManager;

    public SecretManagerService(IAmazonSecretsManager secretsManager)
    {
        _secretsManager = secretsManager;
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        var request = new GetSecretValueRequest
        {
            SecretId = secretName
        };

        var response = await _secretsManager.GetSecretValueAsync(request);

        if (response.SecretString != null)
        {
            return response.SecretString;
        }

        throw new Exception("Secret value is null");
    }
}