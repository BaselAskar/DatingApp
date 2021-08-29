using System.Threading.Tasks;
using API.Data;
using API.Interfaces;
using AutoMapper;

namespace API.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _data;
        private readonly IMapper _mapper;
        public UnitOfWork(DataContext data, IMapper mapper)
        {
            _data = data;
            _mapper = mapper;
        }
        public IUserRepository UserRepository => new UserRepository(_data, _mapper);
        public IMessageRepository MessageRepository => new MessageRepository(_data,_mapper);
        public ILikesRepository LikesRepository => new LikesRepository(_data);
        public async Task<bool> Complete()
        {
            return await _data.SaveChangesAsync() > 0;
        }
        public bool HasChanged()
        {
            return _data.ChangeTracker.HasChanges();
        }
    }
}