using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaCity.Planning.NetworkAnalysis
{
    public static class Extension
    {
        public static List<T>[] ToList<T>(this LinkedList<T>[] lls)
        {
            List<T>[] list = new List<T>[lls.Length];
            for (int i = 0; i < lls.Length; i++)
            {
                var ll = lls[i];
                list[i]=ll.ToList();
            }
            return list;
        }
    }
}
