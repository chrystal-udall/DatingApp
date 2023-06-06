using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
  public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly IUserRepository userRepository;
        public ITokenService _tokenService { get; }
        private readonly IMapper _mapper;

        public AccountController(DataContext context, ITokenService tokenService, IUserRepository userRepository, IMapper mapper)
        {
            this._mapper = mapper;
            this._tokenService = tokenService;
            this.userRepository = userRepository;
            this._context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDTO registerDTO) 
        {
            if(await UserExists(registerDTO.Username)) return BadRequest("username is taken");

            var user = _mapper.Map<AppUser>(registerDTO);

            using var hmac = new HMACSHA512(); // salt
            user.UserName = registerDTO.Username.ToLower();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password));
            user.PasswordSalt = hmac.Key;

            _context.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
              Username = user.UserName,
              Token = _tokenService.CreateToken(user),
              KnownAs = user.KnownAs,
              Gender = user.Gender
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login (LoginDto loginDto)
        {
          var user = await userRepository.GetUserByUsernameAsync(loginDto.Username);

          if(user is null) return Unauthorized("Invalid username");

          using var hmac = new HMACSHA512(user.PasswordSalt);
          var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

          for(int i = 0; i<computedHash.Length; i++){
            if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
          }

          return new UserDto
            {
              Username = user.UserName,
              Token = _tokenService.CreateToken(user),
              PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
              KnownAs = user.KnownAs,
              Gender = user.Gender
            };
        }

        private async Task<bool> UserExists(string username) {
          return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}