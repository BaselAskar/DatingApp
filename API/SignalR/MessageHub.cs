using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        private readonly IUnitOfWork _unitOfwork;
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _presencehub;
        private readonly PresenceTracker _tracker;
        public MessageHub(IUnitOfWork unitOfWork, IMapper mapper, 
            IHubContext<PresenceHub> presencehub,PresenceTracker tracker)
        {
            _unitOfwork = unitOfWork;
            _presencehub = presencehub;
            _mapper = mapper;
            _tracker = tracker;
        }
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUserName(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdateGroup",group);

            var message = await _unitOfwork.MessageRepository.GetMessageThread(Context.User.GetUserName(), otherUser);
            await Clients.Caller.SendAsync("ReceiveMessageThread", message);
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await RemoveFromMessageGroup(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var userName = Context.User.GetUserName();
            if (createMessageDto.RecipientUserName.ToLower() == userName) throw new HubException("You can't sent a message to yuor self!");
            var sender = await _unitOfwork.UserRepository.GetUserByUserNameAsync(userName);
            var recipient = await _unitOfwork.UserRepository.GetUserByUserNameAsync(createMessageDto.RecipientUserName);

            if (recipient == null) throw new HubException("The Recipient is not existed");

            var message = new Message
            {
                SenderUserName = sender.UserName,
                Sender = sender,
                RecipientUserName = recipient.UserName,
                Recipient = recipient,
                Content = createMessageDto.Content
            };
            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await _unitOfwork.MessageRepository.GetMessageGroup(groupName);

            if (group.Connections.Any(c => c.UserName == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }

            _unitOfwork.MessageRepository.AddMessage(message);
            if (await _unitOfwork.Complete())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            }
            else
            {
                var connections = await _tracker.GetConnectionForUser(recipient.UserName);

                if (connections != null)
                {
                    await _presencehub.Clients.Clients(connections).SendAsync("NewMessageReceived", new {userName = sender.UserName,
                         knownAs = sender.KnownAs});
                }
            }
        }
        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await _unitOfwork.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUserName());
            if (group == null)
            {
                group = new Group(groupName);
                _unitOfwork.MessageRepository.AddGroup(group);
            }
            group.Connections.Add(connection);

            if (await _unitOfwork.Complete()) return group;

            throw new HubException("Faild to get the group!");

        }
        private async Task RemoveFromMessageGroup(string connectionId)
        {
            var connection = await _unitOfwork.MessageRepository.GetConnection(connectionId);
            _unitOfwork.MessageRepository.RemoveConnection(connection);
            await _unitOfwork.Complete();
        }
        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}