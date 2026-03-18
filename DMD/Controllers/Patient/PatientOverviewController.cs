
using DMD.APPLICATION.PatientsModule.PatientProgressNotes.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Queries = DMD.APPLICATION.PatientsModule.PatientOverview.Queries;
namespace DMD.API.Controllers.Patient
{
    [Route("api/dmd/patient-overview")]
    public class PatientOverviewController : BaseController
    {
        [HttpGet("get-patient-overview")]
        [Description("Query return Patient OverView model")]
        [ProducesResponseType(typeof(List<PatientProgressNoteModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientOverview([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<List<PatientProgressNoteModel>>)result).Data;
            return Ok(data);
        }
    }
}
