using DMD.APPLICATION.PatientsModule.PatientDentalPhotos.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Queries = DMD.APPLICATION.PatientsModule.PatientDentalPhotos.Queries;

namespace DMD.API.Controllers.Patient
{
    [Route("api/dmd/patient-dental-photo")]
    public class PatientDentalPhotoController : BaseController
    {
        [HttpGet("get-patient-dental-photo")]
        [Description("Query return Patient Dental Photo model")]
        [ProducesResponseType(typeof(List<PatientDentalPhotoModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientDentalPhoto([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<List<PatientDentalPhotoModel>>)result).Data;
            return Ok(data);
        }
    }
}
