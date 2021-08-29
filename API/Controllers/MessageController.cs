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
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public MessageController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var userName = User.GetUserName();

            if (createMessageDto.RecipientUserName.ToLower() == userName) return BadRequest("You can't sent a message to yuor self!");

            var sender = await _unitOfWork.UserRepository.GetUserByUserNameAsync(userName);
            var recipient = await _unitOfWork.UserRepository.GetUserByUserNameAsync(createMessageDto.RecipientUserName);

            if (recipient == null) return NotFound("The Recipient is not existed");

            var message = new Message
            {
                SenderUserName = sender.UserName,
                Sender = sender,
                RecipientUserName = recipient.UserName,
                Recipient = recipient,
                Content = createMessageDto.Content
            };

            _unitOfWork.MessageRepository.AddMessage(message);

            if (await _unitOfWork.Complete()) return Ok(_mapper.Map<MessageDto>(message));

            return BadRequest("Faild to send the message!!");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
        {
            messageParams.UserName = User.GetUserName();

            var messages = await _unitOfWork.MessageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize,messages.TotalCount, messages.TotalPages);

            return messages; 
        }

        [HttpGet("thread/{userName}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesThread(string userName)
        {
            var currentUserName = User.GetUserName();

            var messages = await _unitOfWork.MessageRepository.GetMessageThread(currentUserName,userName);

            return Ok(messages);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userName = User.GetUserName();
            var message = await _unitOfWork.MessageRepository.GetMessage(messageId);
            
            if (message.Sender.UserName != userName && message.Recipient.UserName != userName) return Unauthorized();

            if (message == null) BadRequest("The message is not existed!");

            if (userName == message.SenderUserName && !message.SenderDeleted) message.SenderDeleted = true;

            if (userName == message.RecipientUserName && !message.RecipentDeleted) message.RecipentDeleted = true;

            if (message.SenderDeleted && message.RecipentDeleted) _unitOfWork.MessageRepository.DeleteMessage(message);
            
            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Faild to delete message");

        }
    }
}