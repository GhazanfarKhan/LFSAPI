using System.Linq;
using System.Threading.Tasks;
using LFSAPI.Entities;
using LFSAPI.Extension;
using LFSAPI.Models;
using LFSAPI.Responses;
using LFSAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace LFSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly SignInManager<User> _signInManager;
        private readonly TokenService _tokenService;
        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, TokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Account Login
        /// </summary>
        /// <param name="model"></param>
        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody]LoginViewModel model)
        {
            if (!(ModelState.IsValid))
            {
                string errorMessage = string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errorMessage ?? "Bad Request");
            }


            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var applicationUser = await _userManager.FindByNameAsync(model.Username);
                var roles = await _userManager.GetRolesAsync(applicationUser);


                var user = new LoginResponse { Id = applicationUser.Id.ToString(), FirstName = applicationUser.FirstName, LastName = applicationUser.LastName, Email = applicationUser.Email, UserName = applicationUser.UserName, Role = string.Join(",", roles) };
                //todo claims handling and object creation
                var token = _tokenService.GenerateToken(user);
                // todo response object
                return Ok(new { token = token, user = user, Succeeded = true });
            }
            return BadRequest(result.ToApplicationResult());
        }


        /// <summary>
        /// Account Register
        /// </summary>
        /// <param name="model"></param>
        [HttpPost]
        [AllowAnonymous]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody]RegisterViewModel model)
        {
            if (!(ModelState.IsValid))
            {
                string errorMessage = string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(errorMessage ?? "Bad Request");
            }
          
            var user = new User { FirstName = model.FirstName, LastName = model.LastName, Email = model.Email, UserName = model.Email, };
            var password = new PasswordHasher<User>();
            var hashed = password.HashPassword(user, model.Password);
            user.PasswordHash = hashed;

            var result = (await _userManager.CreateAsync(user)).ToApplicationResult();

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    var role = new IdentityRole(model.Role);
                    role.NormalizedName = model.Role;
                    await _roleManager.CreateAsync(role);
                }
                await _userManager.AddToRoleAsync(user, model.Role);

                var applicationUser = await _userManager.FindByNameAsync(user.UserName);

                var SignUpResponse = new LoginResponse { Id = applicationUser.Id.ToString(), FirstName = applicationUser.FirstName, LastName = applicationUser.LastName, Email = applicationUser.Email, UserName = applicationUser.UserName, Role = model.Role };


                //var rootData = new SignUpResponse(token, user.UserName, user.Email);
                return Created("api/account/register", new { user = SignUpResponse, Succeeded = true });
            }
            return BadRequest(result);
        }


    }
}