using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Plurby.Web.SignalR.Hubs;
using Plurby.Web.SignalR.Hubs.Events;

namespace Plurby.Web.SignalR
{
    public class SignalrPublishDomainEvents : IPublishDomainEvents
    {
        IHubContext<PlurbyHub, IPlurbyClientEvent> _PlurbyHub;

        public SignalrPublishDomainEvents(IHubContext<PlurbyHub, IPlurbyClientEvent> PlurbyHub)
        {
            _PlurbyHub = PlurbyHub;
        }

        private IPlurbyClientEvent GetPlurbyGroup(Guid id)
        {
            return _PlurbyHub.Clients.Group(id.ToString());
        }

        public Task Publish(object evnt)
        {
            try
            {
                return ((dynamic)this).When((dynamic)evnt);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                return Task.CompletedTask;
            }
        }

        public Task When(NewMessageEvent e)
        {
            return GetPlurbyGroup(e.IdGroup).NewMessage(e.IdUser, e.IdMessage);
        }
    }
}
