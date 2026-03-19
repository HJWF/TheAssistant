using Azure;
using Azure.Security.KeyVault.Secrets;

namespace TheAssistant.TokenStore.ServiceAdapter.UnitTests
{
    public class TestSecretClient : SecretClient
        {
            private readonly Response<KeyVaultSecret>? _response;
            private readonly bool _throwNotFound;
            private readonly int _deleteStatusCode;

            public KeyVaultSecret? LastSetSecret { get; private set; }

            public TestSecretClient(Response<KeyVaultSecret> response)
                : base(new Uri("https://localhost"), new DummyTokenCredential())
            {
                _response = response;
                _throwNotFound = false;
            }

            public TestSecretClient(bool throwNotFound)
                : base(new Uri("https://localhost"), new DummyTokenCredential())
            {
                _response = null;
                _throwNotFound = throwNotFound;
            }

            public TestSecretClient(int deleteStatusCode)
                : base(new Uri("https://localhost"), new DummyTokenCredential())
            {
                _response = null;
                _throwNotFound = false;
                _deleteStatusCode = deleteStatusCode;
            }

            public TestSecretClient()
                : base(new Uri("https://localhost"), new DummyTokenCredential())
            {
                _response = null;
                _throwNotFound = false;
            }

            public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default)
            {
                if (_throwNotFound)
                {
                    throw new RequestFailedException(404, "not found");
                }

                return Task.FromResult(_response!);
            }

            public override Task<Response<KeyVaultSecret>> SetSecretAsync(KeyVaultSecret secret, CancellationToken cancellationToken = default)
            {
                LastSetSecret = secret;
                return Task.FromResult(Response.FromValue(secret, null!));
            }

            public override Task<DeleteSecretOperation> StartDeleteSecretAsync(string name, CancellationToken cancellationToken = default)
            {
                if (_deleteStatusCode > 0)
                    throw new RequestFailedException(_deleteStatusCode, "delete error");

                return Task.FromResult<DeleteSecretOperation>(null!);
            }
        }
    }