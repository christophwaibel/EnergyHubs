using System;
using System.Collections.Generic;
using System.Text;

namespace EhubMisc
{
    public static class Misc
    {
        public static bool IsNullOrDefault<T>(this Nullable<T> value) where T : struct
        {
            return default(T).Equals(value.GetValueOrDefault());
        }
    }
}
