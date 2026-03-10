namespace DMD.APPLICATION.Responses
{
    public class NoDataFoundException : Exception
    {
        public NoDataFoundException(string name, object key) : base($"Could not find record for Entity '{name}' with Id of {key}")
        {
        }
        public NoDataFoundException(string message) : base(message)
        {
        }
    }
}
