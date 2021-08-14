using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;
        public MessageController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var userName = User.GetUserName();

            if (createMessageDto.RecipientUserName.ToLower() == userName) return BadRequest("You can't sent a message to yuor self!");

            var sender = await _userRepository.GetUserByUserNameAsync(userName);
            var recipient = await _userRepository.GetUserByUserNameAsync(createMessageDto.RecipientUserName);

            if (recipient == null) return NotFound("The Recipient is not existed");

            var message = new Message
            {
                SenderUserName = sender.UserName,
                Sender = sender,
                RecipientUserName = recipient.UserName,
                Recipient = recipient,
                Content = createMessageDto.Content
            };

            _messageRepository.AddMessage(message);

            if (await _messageRepository.SaveAllAsync()) return Ok(_mapper.Map<MessageDto>(message));

            return BadRequest("Faild to send the message!!");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
        {
            messageParams.UserName = User.GetUserName();

            var messages = await _messageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize,messages.TotalCount, messages.TotalPages);

            return messages; 
        }

        [HttpGet("thread/{userName}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesThread(string userName)
        {
            var currentUserName = User.GetUserName();

            var messages = await _messageRepository.GetMessageThread(currentUserName,userName);

            return Ok(messages);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userName = User.GetUserName();
            var message = await _messageRepository.GetMessage(messageId);
            
            if (message.Sender.UserName != userName && message.Recipient.UserName != userName) return Unauthorized();

            if (message == null) BadRequest("The message is not existed!");

            if (userName == message.SenderUserName && !message.SenderDeleted) message.SenderDeleted = true;

            if (userName == message.RecipientUserName && !message.RecipentDeleted) message.RecipentDeleted = true;

            if (message.SenderDeleted && message.RecipentDeleted) _messageRepository.DeleteMessage(message);
            
            if (await _messageRepository.SaveAllAsync()) return Ok();

            return BadRequest("Faild to delete message");

        }
    }
}