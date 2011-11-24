namespace AniDBmini.Collections
{
    public class DebugLine
    {
        public string Time { get; private set; }
        public string Message { get; private set; }

        public DebugLine(string _t, string _m)
        {
            Time = _t;
            Message = _m;
        }
    }
}