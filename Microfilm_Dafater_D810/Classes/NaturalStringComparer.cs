using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microfilm_Dafater_D810.Classes
{
    class NaturalStringComparer : IComparer<String>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);

        #region IComparer<string> Members

        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }

        #endregion
    }
}
