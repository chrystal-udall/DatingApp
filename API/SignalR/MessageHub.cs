using System.Security.AccessControl;
using API.Data;
using API.DTO;
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
        private readonly IMessageRepository _messageRepository;
        public IUserRepository UserRepository { get; }
        private readonly IMapper _mapper;
        private readonly IHubContext<PrescenceHub> _prescenceHub;

        public MessageHub(IMessageRepository messageRepository, IUserRepository userRepository, IMapper mapper, IHubContext<PrescenceHub> prescenceHub)
        {
            this._prescenceHub = prescenceHub;
            this._mapper = mapper;
            this.UserRepository = userRepository;
            this._messageRepository = messageRepository; 
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"];
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName); // adds the user to the hub group - when they disconnect, they will be automatically removed from the group
            var group = await AddToGroup(groupName); // adds the user and connection Id to the group in our Database (needed for correct read/unread functionality)

            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _messageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);

            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto) 
        {
            var username = Context.User.GetUsername();

            if(username == createMessageDto.RecipientUsername.ToLower()) 
                throw new HubException("You cannot send messages to yourself");
            
            var sender = await UserRepository.GetUserByUsernameAsync(username);
            var recipient = await UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if(recipient == null) throw new HubException("Recipient not found");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);

            var group = await _messageRepository.GetMessageGroup(groupName);

            if (group.Connections.Any(x => x.Username == recipient.UserName)) 
            {
                message.DateRead = DateTime.UtcNow;
            }
            else //notify user that they have a new message when they're not on the message tab
            {
                var connections = await PrescenceTracker.GetConnectionsForUser(recipient.UserName);
                if (connections != null)
                {
                    await _prescenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", 
                        new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }

            _messageRepository.AddMessage(message);

            if (await _messageRepository.SaveAllAsync()) 
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            };
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }

        private async Task<Group> AddToGroup(string groupName) 
        {
            var group = await _messageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            if(group == null) 
            {
                group = new Group(groupName);
                _messageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);

            if(await _messageRepository.SaveAllAsync()) return group;

            throw new HubException("failed to add to group");

        }

        private async Task<Group> RemoveFromMessageGroup() 
        {
            var group = await _messageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _messageRepository.RemoveConnection(connection);

            if(await _messageRepository.SaveAllAsync()) return group;
            
            throw new HubException("failed to remove from group");
        }
    }
}