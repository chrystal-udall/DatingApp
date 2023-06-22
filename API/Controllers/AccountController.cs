using API.Data;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        public ITokenService _tokenService { get; }
        private readonly IMapper _mapper;
        public UserManager<AppUser> UserManager { get; }

        public AccountController(ITokenService tokenService, IMapper mapper, UserManager<AppUser> userManager)
        {
            this.UserManager = userManager;
            this._mapper = mapper;
            this._tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDTO registerDTO) 
        {
            if(await UserExists(registerDTO.Username)) return BadRequest("username is taken");

            var user = _mapper.Map<AppUser>(registerDTO);

            user.UserName = registerDTO.Username.ToLower();

            var result = await UserManager.CreateAsync(user, registerDTO.Password);

            if(!result.Succeeded) return BadRequest(result.Errors);

            var roleResult = await UserManager.AddToRoleAsync(user, "Member");

            if(!roleResult.Succeeded) return BadRequest(result.Errors);

            return new UserDto
            {
              Username = user.UserName,
              Token = await _tokenService.CreateToken(user),
              KnownAs = user.KnownAs,
              Gender = user.Gender
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login (LoginDto loginDto)
        {
          var user = await UserManager.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == loginDto.Username);

          if(user is null) return Unauthorized("Invalid username");

          var result = await UserManager.CheckPasswordAsync(user, loginDto.Password);

          if(!result) return Unauthorized("Invalid password");

          return new UserDto
            {
              Username = user.UserName,
              Token = await _tokenService.CreateToken(user),
              PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
              KnownAs = user.KnownAs,
              Gender = user.Gender
            };
        }

        private async Task<bool> UserExists(string username) {
          return await UserManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}