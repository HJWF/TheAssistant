using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleNewSignIn
{
    public class HandleNewSignInCommandHandler : ICommandHandler<HandleNewSignInCommand>
    {
        private readonly ITokenStoreServiceAdapter _tokenStoreServiceAdapter;
        private readonly IServiceBusServiceAdapter _serviceBusServiceAdapter;
        private const string ReceiveMessagesQueueName = "receivedmessages";

        public HandleNewSignInCommandHandler(ITokenStoreServiceAdapter tokenStoreServiceAdapter, IServiceBusServiceAdapter serviceBusServiceAdapter)
        {
            _tokenStoreServiceAdapter = tokenStoreServiceAdapter;
            _serviceBusServiceAdapter = serviceBusServiceAdapter;
        }

        public async Task Handle(HandleNewSignInCommand command)
        {
            await _tokenStoreServiceAdapter.StoreToken(command.UserId, command.Token);
            await _serviceBusServiceAdapter.SendMessageAsync(ReceiveMessagesQueueName, "Logged in successfull. Agenda events can be retrieved.");
        }
    }
}
