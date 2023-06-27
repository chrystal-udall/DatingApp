using API.DTO;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private IMapper _mapper { get; }
        public IPhotoService PhotoService { get; }
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
        {
            this._unitOfWork = unitOfWork;
            this.PhotoService = photoService;
            this._mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDTO>>> GetUsers([FromQuery] UserParams userParams)
        {
            var gender = await  _unitOfWork.UserRepository.GetUserGender(User.GetUsername());
            userParams.CurrentUsername = User.GetUsername();

            if(string.IsNullOrEmpty(userParams.Gender)){
                userParams.Gender = gender == "male" ? "female" : "male"; //if current user is male then we want female and vice versa
            }

            var users = await  _unitOfWork.UserRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));

            return Ok(users);
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDTO>> GetUser(string username)
        {
            return await  _unitOfWork.UserRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var user = await  _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user is null) return NotFound();

            _mapper.Map(memberUpdateDto, user);

            if (await   _unitOfWork.Complete()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
        {
            var user = await  _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user == null) return NotFound();

            var result = await PhotoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0) photo.IsMain = true;

            user.Photos.Add(photo);

            if (await   _unitOfWork.Complete())
            {
                return CreatedAtAction(nameof(GetUser),
                    new { username = user.UserName }, _mapper.Map<PhotoDTO>(photo));
            }

            return BadRequest("problem adding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await  _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user is null) return NotFound();

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo is null) return NotFound();

            if (photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);

            if (currentMain is not null) currentMain.IsMain = false;

            photo.IsMain = true;

            if (await   _unitOfWork.Complete()) return NoContent();

            return BadRequest("Problem setting main photo");

        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await  _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo is null) return NotFound();

            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            if (photo.PublicId is not null)
            {
                var result = await PhotoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error is not null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if (await   _unitOfWork.Complete()) return Ok();

            return BadRequest("Problem deleting photo");
        }
    }
}