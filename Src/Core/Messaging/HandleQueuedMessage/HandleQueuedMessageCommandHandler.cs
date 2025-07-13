using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleQueuedMessage
{
    public class HandleQueuedMessageCommandHandler : ICommandHandler<HandleQueuedMessageCommand>
    {
        private readonly IAgentServiceAdapter _agentServiceAdapter;
        private readonly IMessageServiceAdapter _messageServiceAdapter;

        public HandleQueuedMessageCommandHandler(IAgentServiceAdapter agentServiceAdapter, IMessageServiceAdapter messageServiceAdapter)
        {
            _agentServiceAdapter = agentServiceAdapter;
            _messageServiceAdapter = messageServiceAdapter;
        }

        public async Task Handle(HandleQueuedMessageCommand command)
        {
            var result = await _agentServiceAdapter.HandleMessageAsync(new Message
            {
                Content = command.Message
            });

            var response = await _messageServiceAdapter.SendMessageAsync(new Message()
            {
                Content = result
            });
        }
    }
}
