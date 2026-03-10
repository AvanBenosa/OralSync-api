using DMD.APPLICATION.Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.Responses
{
    public abstract class Response
    {
        protected Response(string message = null)
        {
            Message = message;
        }
        public string Message { get; set; }
    }

    public class NotFoundResponse : Response
    {
        public NotFoundResponse(string message = null) : base(message)
        {
        }
    }

    public class BadRequestResponse : Response
    {
        public BadRequestResponse(string message = null) : base(message)
        {
        }
    }

    public class InvalidParameterResponse : Response
    {
        public InvalidParameterResponse(string message = null) : base(message)
        {
        }
    }

    public class SuccessResponse : Response
    {
        public SuccessResponse(string message = null) : base(message)
        {
        }
    }

    public class SuccessResponse<T> : Response
    {
        public SuccessResponse(T data)
        {
            Data = data;
            Message = "Success";
        }

        public SuccessResponse(List<object> response)
        {
            Response = response;
        }

        public T Data { get; set; }
        public List<object> Response { get; }
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }

}
