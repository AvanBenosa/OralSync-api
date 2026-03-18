using DMD.APPLICATION.Responses;
using DMD.APPLICATION.UserProfileModule.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Commands = DMD.APPLICATION.UserProfileModule.Commands;
using Queries = DMD.APPLICATION.UserProfileModule.Queries;

namespace DMD.API.Controllers.UserProfile
{
    [Route("api/dmd/user-profile")]
    public class UserProfileController : BaseController
    {
        [HttpGet("get-user-profiles")]
        [Description("Get clinic user profiles")]
        [ProducesResponseType(typeof(UserProfileResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetUserProfiles([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<UserProfileResponseModel>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-user-profile")]
        [Description("Create clinic user profile")]
        [ProducesResponseType(typeof(UserProfileModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreateUserProfile([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<UserProfileModel>)result).Data;
            return Created("", data);
        }

        [HttpPut("put-user-profile")]
        [Description("Update clinic user profile")]
        [ProducesResponseType(typeof(UserProfileModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateUserProfile([FromBody] Commands.Update.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<UserProfileModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-user-profile/{id}")]
        [Description("Delete clinic user profile")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteUserProfile([FromRoute] string id)
        {
            var result = await Mediator.Send(new Commands.Delete.Command { Id = id });
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<string>)result).Data;
            return Ok(data);
        }
    }
}
