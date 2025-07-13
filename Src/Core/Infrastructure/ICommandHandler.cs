namespace TheAssistant.Core.Infrastructure
{
    public interface ICommandHandler<TCommand>
    {
        Task Handle(TCommand command);
    }
}
