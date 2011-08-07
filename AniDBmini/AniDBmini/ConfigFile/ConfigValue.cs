using System;

namespace AniDBmini
{
    public class ConfigValue
    {
        private string value;

        public ConfigValue(string val)
        {
            value = val;
        }

        public bool ToBoolean() { return Convert.ToBoolean(value); }
        public int ToInt32() { return int.Parse(value); }
        public override string ToString() { return value; }
    }
}
