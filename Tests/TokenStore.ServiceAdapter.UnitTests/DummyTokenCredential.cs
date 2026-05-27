using Azure.Core;

namespace TheAssistant.TokenStore.ServiceAdapter.UnitTests;

public class DummyTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken("token", DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(new AccessToken("token", DateTimeOffset.UtcNow.AddHours(1)));
        }
    }


