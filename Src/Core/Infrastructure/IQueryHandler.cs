namespace TheAssistant.Core.Infrastructure
{
    public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        Task<TResponse> Handle(TQuery command);
    }
}
