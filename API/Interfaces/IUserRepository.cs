using API.DTO;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface IUserRepository
    {
        void Update(AppUser user);
        Task<Boolean> SaveAllAsync();
        Task<IEnumerable<AppUser>> GetUsersAsync();
        Task<PagedList<MemberDTO>> GetMembersAsync(UserParams userParams);
        Task<AppUser> GetUserByIdAsync(int id);
        Task<AppUser> GetUserByUsernameAsync(string username);
        Task<MemberDTO> GetMemberAsync(string username);

    }
}