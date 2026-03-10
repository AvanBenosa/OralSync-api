using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace DMD.API.Configurations
{
    public class ParameterPollutionMiddleware
    {
        private readonly RequestDelegate _next;

        public ParameterPollutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            // Check for query string pollution
            if (!string.IsNullOrEmpty(context.Request.QueryString.Value))
            {
                var queryString = context.Request.QueryString.Value.TrimStart('?');
                var keys = Regex.Matches(queryString, @"([\w\.\-_%\[\]]+)=", RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .ToList();

                if (keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Count())
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Invalid Request");
                    return;
                }
            }



            // Check for JSON body pollution
            else if (context.Request.ContentType?.StartsWith("application/json") == true)
            {
                context.Request.EnableBuffering();

                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    var json = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    // Validate JSON for duplicate keys
                    if (!IsValidJson(json, out string errorMessage))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync(errorMessage);
                        return;
                    }
                }
            }

            //Check for form data body pollution (multipart/form-data or application/x-www-form-urlencoded)
            else if (context.Request.HasFormContentType)
            {
                context.Request.EnableBuffering();
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    var formData = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    // Improved Regex to capture form field names
                    var keys = Regex.Matches(formData, @"name=""([^""]+)""", RegexOptions.IgnoreCase)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .ToList();

                    if (keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Count())
                    {
                        // context.Response.StatusCode = 400;
                        // await context.Response.WriteAsync("Invalid Request");
                        // return;
                    }
                }
            }

            await _next(context);
        }

        private static bool IsValidJson(string json, out string errorMessage)
        {
            try
            {
                var objectKeyStack = new Stack<HashSet<string>>(new[] { new HashSet<string>(StringComparer.OrdinalIgnoreCase) });

                using (var reader = new JsonTextReader(new StringReader(json)))
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            // Push a new HashSet for this object level
                            objectKeyStack.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                        }
                        else if (reader.TokenType == JsonToken.PropertyName)
                        {
                            var currentKeys = objectKeyStack.Peek();
                            string key = reader.Value.ToString();

                            if (!currentKeys.Add(key))
                            {
                                // errorMessage = $"Invalid JSON: Duplicate property '{key}' (case-insensitive) found.";
                                errorMessage = "Invalid Request";
                                return false;
                            }
                        }
                        else if (reader.TokenType == JsonToken.EndObject)
                        {
                            // Pop the stack when leaving an object
                            objectKeyStack.Pop();
                        }
                    }
                }

                errorMessage = null;
                return true;
            }
            catch (JsonReaderException ex)
            {
                errorMessage = $"Invalid JSON: {ex.Message}";
                return false;
            }
        }
    }
}
