namespace TheAssistant.TheAssistantApi.Login.Notion;

public static class NotionTokenMapper
{
    // Currently Notion tokens do not expire, so we set the expiry to DateTime.MaxValue
    public static Core.Authentication.Token ToModel(this NotionTokenResponse source) => new(source.AccessToken, source.RefreshToken, DateTime.MaxValue);
}
