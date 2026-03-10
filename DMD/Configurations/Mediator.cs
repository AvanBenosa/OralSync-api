
using AutoMapper;
using DMD.APPLICATION.Responses;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;

namespace DMD.API.Configurations
{
    public class Mediator
    {
        internal static void RegisterMediatr(WebApplicationBuilder builder)
        {
            builder.Services.AddMediatR(typeof(NoDataFoundException).Assembly);
        }

        internal static void AddFluentValidation(WebApplicationBuilder builder)
        {
            builder.Services.AddFluentValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<NoDataFoundException>(); // replace with your validator
        }

        internal static void RegisterAutoMapper(WebApplicationBuilder builder)
        {
            builder.Services.AddAutoMapper(
                configAction =>
                {
                    configAction.ValidateInlineMaps = false;
                },
                typeof(Response)
            );
        }
    }
}
