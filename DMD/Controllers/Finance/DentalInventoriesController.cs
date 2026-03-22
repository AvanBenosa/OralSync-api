using DMD.APPLICATION.Finances.DentalInventories.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Commands = DMD.APPLICATION.Finances.DentalInventories.Commands;
using Queries = DMD.APPLICATION.Finances.DentalInventories.Queries;

namespace DMD.API.Controllers.Finance
{
    [Route("api/dmd/dental-inventories")]
    public class DentalInventoriesController : BaseController
    {
        [HttpGet("get-dental-inventories")]
        [Description("Query returns dental inventory records for the authenticated clinic")]
        [ProducesResponseType(typeof(DentalInventoryResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDentalInventories([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<DentalInventoryResponseModel>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-dental-inventories")]
        [Description("Create dental inventory based on json body")]
        [ProducesResponseType(typeof(DentalInventoryModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreateDentalInventories([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<DentalInventoryModel>)result).Data;
            return Created("", data);
        }

        [HttpPut("put-dental-inventories")]
        [Description("Update dental inventory based on json body")]
        [ProducesResponseType(typeof(DentalInventoryModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateDentalInventories([FromBody] Commands.Update.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<DentalInventoryModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-dental-inventories")]
        [Description("Delete dental inventory, returns boolean")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteDentalInventories([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<bool>)result).Data;
            return Ok(data);
        }
    }
}
