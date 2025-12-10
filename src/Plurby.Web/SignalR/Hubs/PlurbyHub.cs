using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Plurby.Web.SignalR.Hubs
{
    public interface IPlurbyClientEvent
    {
        public Task NewMessage(Guid idUser, Guid idMessage);
    }

    [Authorize] // Makes the hub usable only by authenticated users
    public class PlurbyHub : Hub<IPlurbyClientEvent>
    {
        private readonly IPublishDomainEvents _publisher;

        public PlurbyHub(IPublishDomainEvents publisher)
        {
            _publisher = publisher;
        }

        public async Task JoinGroup(Guid idGroup)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, idGroup.ToString());
        }
        public async Task LeaveGroup(Guid idGroup)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, idGroup.ToString());
        }
    }
}
