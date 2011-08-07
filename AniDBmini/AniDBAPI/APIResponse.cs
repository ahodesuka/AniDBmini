namespace AniDBmini
{
    public class APIResponse
    {
        public string Message { get; private set; }
        public int Code { get; private set; }

        public APIResponse(string msg, int code)
        {
            Message = msg;
            Code = code;
        }
    }
}
