using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleNewSignIn
{
    public class HandleNewPersonalSignInCommandHandler : ICommandHandler<HandleNewPersonalSignInCommand>
    {
        private readonly ITokenStoreServiceAdapter _tokenStoreServiceAdapter;
        private readonly IServiceBusServiceAdapter _serviceBusServiceAdapter;
        private const string ReceiveMessagesQueueName = "receivedmessages";

        public HandleNewPersonalSignInCommandHandler(ITokenStoreServiceAdapter tokenStoreServiceAdapter, IServiceBusServiceAdapter serviceBusServiceAdapter)
        {
            _tokenStoreServiceAdapter = tokenStoreServiceAdapter;
            _serviceBusServiceAdapter = serviceBusServiceAdapter;
        }

        public async Task Handle(HandleNewPersonalSignInCommand command)
        {
            await _tokenStoreServiceAdapter.StoreToken(command.User.PersonalMailTag, command.Token);
            await _serviceBusServiceAdapter.SendMessageAsync(ReceiveMessagesQueueName, "Logged in successfull. Agenda events can be retrieved.");
        }
    }
}
