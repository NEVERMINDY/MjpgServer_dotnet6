using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjpgServerDotnet6
{
    internal class FileNameSort:IComparer<object>
    {
        [System.Runtime.InteropServices.DllImport("Shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string s1, string s2);

        public int Compare(object name1,object name2)
        {
            if(name1==null && name2 == null)
            {
                return 0;
            }
            if(name1 == null)
            {
                return -1;
            }
            if(name2 == null)
            {
                return 1;
            }
            return StrCmpLogicalW(name1.ToString(), name2.ToString());
        }
    }
}
