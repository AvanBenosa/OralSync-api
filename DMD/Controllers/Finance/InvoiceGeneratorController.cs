using DMD.APPLICATION.Finances.InvoiceGenerator.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Queries = DMD.APPLICATION.Finances.InvoiceGenerator.Queries;

namespace DMD.API.Controllers.Finance
{
    [Route("api/dmd/invoice-generator")]
    public class InvoiceGeneratorController : BaseController
    {
        [HttpGet("get-invoice-generator")]
        [Description("Query returns invoice generator items from patient progress notes")]
        [ProducesResponseType(typeof(List<InvoiceGeneratorModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetInvoiceGenerator([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<List<InvoiceGeneratorModel>>)result).Data;
            return Ok(data);
        }
    }
}
