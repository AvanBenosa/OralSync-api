using DMD.APPLICATION.Auth.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using LoginAuthResponse = DMD.APPLICATION.Auth.Models.AuthResponse;

using Commands = DMD.APPLICATION.Auth.Commands;

namespace DMD.API.Controllers.Auth
{
    public class AuthController : BaseController
    {
        [HttpPost("login")]
        [Description("Login")]
        [ProducesResponseType(typeof(LoginAuthResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> LoginUser([FromBody] Commands.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<LoginAuthResponse>)result).Data;
            return Ok(data);
        }
    }
}
