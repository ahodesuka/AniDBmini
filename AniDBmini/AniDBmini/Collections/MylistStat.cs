namespace AniDBmini.Collections
{
    public class MylistStat
    {
        public string sText { get; private set; }
        public string sValue { get; private set; }

        public MylistStat(string _t, string _v)
        {
            sText = _t;
            sValue = _v;
        }
    }
}