using Azure;
using Azure.Security.KeyVault.Secrets;

namespace TheAssistant.TokenStore.ServiceAdapter.UnitTests
{
    public class TestSecretClient : SecretClient
        {
            private readonly Response<KeyVaultSecret>? _response;
            private readonly bool _throwNotFound;

            public TestSecretClient(Response<KeyVaultSecret> response)
                : base(new Uri("https://vault.v"), new DummyTokenCredential())
            {
                _response = response;
                _throwNotFound = false;
            }

            public TestSecretClient(bool throwNotFound)
                : base(new Uri("https://vault.v"), new DummyTokenCredential())
            {
                _response = null;
                _throwNotFound = throwNotFound;
            }

            public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default)
            {
                if (_throwNotFound)
                {
                    throw new RequestFailedException(404, "not found");
                }

                return Task.FromResult(_response!);
            }
        }
    }

