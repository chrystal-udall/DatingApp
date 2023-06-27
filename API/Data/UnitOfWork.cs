using API.Interfaces;
using AutoMapper;

namespace API.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        public DataContext Context { get; }
        private readonly IMapper _mapper;
        public UnitOfWork(DataContext context, IMapper mapper)
        {
            this._mapper = mapper;
            this.Context = context;
            
        }

        public IUserRepository UserRepository => new UserRepository(Context, _mapper);

        public IMessageRepository MessageRepository => new MessageRepository(Context, _mapper);

        public ILikesRepository LikesRepository => new LikesRepository(Context);

        public async Task<bool> Complete()
        {
            return await Context.SaveChangesAsync() > 0;
        }

        public bool HasChanges()
        {
            return Context.ChangeTracker.HasChanges();
        }
    }
}