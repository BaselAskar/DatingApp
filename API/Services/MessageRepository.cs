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


        public void AddMessage(Message message)
        {
            _data.Messages.Add(message);
        }
        public void DeleteMessage(Message message)
        {
            _data.Messages.Remove(message);
        }
        public async Task<Message> GetMessage(int id)
        {
            return await _data.Messages.FindAsync(id);
        }
        public async Task<PageList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _data.Messages
                .OrderByDescending(m => m.MessageSent)
                .Where(m => m.SenderUserName == messageParams.UserName || m.RecipientUserName == messageParams.UserName)
                .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(m => m.RecipientUserName == messageParams.UserName && m.RecipentDeleted == false),
                "Outbox" => query.Where(m => m.SenderUserName == messageParams.UserName && m.SenderDeleted == false ),
                _ => query.Where(m => m.RecipientUserName == messageParams.UserName && m.RecipentDeleted == false && m.DateRead == null)
            };

            var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

            return await PageList<MessageDto>.CreateAsync(messages,messageParams.PageNumber,messageParams.PageSize);

        }

         public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string reciepentUserName)
         {
            var messages = await _data.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .Where(m => m.RecipientUserName == reciepentUserName && m.SenderUserName == currentUserName
                        || m.RecipientUserName == currentUserName && m.SenderUserName == reciepentUserName
                ).OrderBy(m => m.MessageSent)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && m.RecipientUserName == currentUserName).ToList();

            if(unreadMessages.Any())
            {
                foreach(var message in unreadMessages)
                {
                    message.DateRead = DateTime.Now;
                }
            }

            await _data.SaveChangesAsync();

            return _mapper.Map<IEnumerable<MessageDto>>(messages);
         }

        public async Task<bool> SaveAllAsync()
        {
            return await _data.SaveChangesAsync() > 0;
        }
    }
}