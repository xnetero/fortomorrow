using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text;
using todolist.Helper;
using todolist.Models;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using todolist.UtilityService;
using todolist.Dto;

namespace todolist.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly TodolistContext _dbcontext;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public UserController(TodolistContext dbcontext, IConfiguration configuration,
            IEmailService emailService)
        {
            _emailService=emailService;
            _configuration = configuration;
            _dbcontext = dbcontext;
        }










        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();

            var user = await _dbcontext.Users.FirstOrDefaultAsync(x => x.Email == userObj.Email);

            if (user == null)
                return NotFound(new { Message = "User not found" });


            
            if (!PasswordHasher.VerifyPassword(userObj.Password, user.Password))
            {

                return BadRequest(new {Message="password is incorrect"});
            }

            user.Token=CreateJwt(user);


            return Ok(
                 new
                 {
                     Token = user.Token,


                     Message = "login success"
                 }
                ) ; 


        }




        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userObj) {



            userObj.Iduser = Guid.NewGuid();
            if (userObj == null)
                return BadRequest();

            //check email

            if (await CheckemailExistAsync(userObj.Email))
                return BadRequest(new { Message = "email already exixst" });

            //check password

            var passMessage = CheckPasswordStrength(userObj.Password);
            if (!string.IsNullOrEmpty(passMessage))
                return BadRequest(new { Message = passMessage.ToString() });


            userObj.Password = PasswordHasher.HashPassword(userObj.Password);
            userObj.Role = "User";
            userObj.Token = "";

            userObj.Active = false;


            await _dbcontext.Users.AddAsync(userObj);
            await _dbcontext.SaveChangesAsync();

            return Ok(
                 new
                 {
                     Message = "wa hahowa tzad "
                 }
                );



        }

        private Task<bool> CheckemailExistAsync(string email)
        {
            return _dbcontext.Users.AnyAsync(x => x.Email == email);

        }


        private static string CheckPasswordStrength(string pass)
        {
            StringBuilder sb = new StringBuilder();
            if (pass.Length < 9)
                sb.Append("Minimum password length should be 8" + Environment.NewLine);
            if (!(Regex.IsMatch(pass, "[a-z]") && Regex.IsMatch(pass, "[A-Z]") && Regex.IsMatch(pass, "[0-9]")))
                sb.Append("Password should be AlphaNumeric" + Environment.NewLine);
            if (!Regex.IsMatch(pass, "[<,>,@,!,#,$,%,^,&,*,(,),_,+,\\[,\\],{,},?,:,;,|,',\\,.,/,~,`,-,=]"))
                sb.Append("Password should contain special charcter" + Environment.NewLine);
            return sb.ToString();
        }


        private string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes("veryverysceret.....");

            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, user.Role),

                new Claim(ClaimTypes.Name,$"{user.FirstName} {user.LastName}")


            });

            var credentias = new SigningCredentials
                (new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                /* Expires = DateTime.Now.AddDays(1),*/
                Expires = DateTime.Now.AddSeconds(10),
             
                SigningCredentials = credentias

            };

            var token=jwtTokenHandler.CreateToken(tokenDescriptor);

            return jwtTokenHandler.WriteToken(token);
        }



        [Authorize]
        [HttpGet]
        public async Task<ActionResult<User>> GetAllUsers()
        {
            return Ok(await _dbcontext.Users.ToListAsync());
        }


        [HttpPost("send_reset_email/{email}")]
        public async Task<IActionResult> SendEmail(string email)
        {

            var user = await _dbcontext.Users.FirstOrDefaultAsync(a => a.Email == email);

            if (user is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message="email doesnt exist"
                }) ;
            }
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var emailtoekn = Convert.ToBase64String(tokenBytes);

            user.Resetpasswordtoken = emailtoekn;
            user.Resetpasswordexpiry = DateTime.Now.AddMinutes(15);

            string from = _configuration["EmailSettings:From"];



            var emailModel = new EmailModel(email, "Reset passwrrd", EmailBody.EmailStringBody(email, emailtoekn));


            _emailService.SendEmail(emailModel);
            _dbcontext.Entry(user).State = EntityState.Modified;

            await _dbcontext.SaveChangesAsync();
            return Ok(new
            {
                StatusCode = 200,
                Message = "email sent "
            });



        }

        [HttpPost("reset_password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var newToken = resetPasswordDto.EmailToken.Replace("", "+");

            var user =await _dbcontext.Users.AsNoTracking().FirstOrDefaultAsync(
                  a=>a.Email==resetPasswordDto.Email  );

            if (user is null)
            {
                return NotFound( new
                {

                    StatusCode=404,
                    Message="User doest exist"

                } );
            }
            var tokenCode = user.Resetpasswordtoken;
            DateTime emailTokenExpiry = (DateTime)user.Resetpasswordexpiry;

            if (tokenCode!=resetPasswordDto.EmailToken || emailTokenExpiry<DateTime.Now)
            {
                return BadRequest(new

                {
                    StatusCode = 400,
                    Message = "Invalid reset link",
                }) ;


            }
            user.Password = PasswordHasher.HashPassword(resetPasswordDto.NewPassword);

            _dbcontext.Entry(user).State = EntityState.Modified;

            await _dbcontext.SaveChangesAsync();
            return Ok(new
            {
                StatusCode=200,
                Message = "done"
            });
        }





    }




  





}


