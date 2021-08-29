using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _data;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext data, IMapper mapper)
        {
            _mapper = mapper;
            _data = data;
        }

        public void AddGroup(Group group)
        {
            _data.Groups.Add(group);
        }

        public void AddMessage(Message message)
        {
            _data.Messages.Add(message);
        }
        public void DeleteMessage(Message message)
        {
            _data.Messages.Remove(message);
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            return await _data.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _data.Groups
                .Include(c => c.Connections)
                .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _data.Messages.FindAsync(id);
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await _data.Groups
                    .Include(x => x.Connections)
                    .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<PageList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var messages = _data.Messages
                .OrderByDescending(m => m.MessageSent)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .Where(m => m.SenderUserName == messageParams.UserName || m.RecipientUserName == messageParams.UserName)
                .AsQueryable();

            messages = messageParams.Container switch
            {
                "Inbox" => messages.Where(m => m.RecipientUserName == messageParams.UserName && m.RecipientDeleted == false),
                "Outbox" => messages.Where(m => m.SenderUserName == messageParams.UserName && m.SenderDeleted == false ),
                _ => messages.Where(m => m.RecipientUserName == messageParams.UserName && m.RecipientDeleted == false && m.DateRead == null)
            };

            

            return await PageList<MessageDto>.CreateAsync(messages,messageParams.PageNumber,messageParams.PageSize);

        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string reciepentUserName)
        {
            var messages = await _data.Messages
                .Where(m => m.RecipientUserName == reciepentUserName && m.SenderUserName == currentUserName
                        || m.RecipientUserName == currentUserName && m.SenderUserName == reciepentUserName
                ).OrderBy(m => m.MessageSent)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && m.RecipientUserName == currentUserName).ToList();

            if(unreadMessages.Any())
            {
                foreach(var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }
            }

            await _data.SaveChangesAsync();

            return messages;
        }

        public void RemoveConnection(Connection connection)
        {
            _data.Connections.Remove(connection);
        }

        public void RemoveGroup(Group group)
        {
            _data.Groups.Remove(group);
        }

    }
}