using Microsoft.AspNetCore.SignalR;
using Microsoft.CSharp.RuntimeBinder;
using Plurby.Web.SignalR.Hubs;
using Plurby.Web.SignalR.Hubs.Events;
using System;
using System.Threading.Tasks;

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
            catch (RuntimeBinderException)
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
