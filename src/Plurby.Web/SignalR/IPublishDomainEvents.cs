using System.Threading.Tasks;

namespace Plurby.Web.SignalR
{
    public interface IPublishDomainEvents
    {
        Task Publish(object evnt);
    }
}
