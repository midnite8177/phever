using System;
using System.Collections.Generic;
using System.Text;

namespace gentleman
{
    public class Algorithm
    {
        public static List<T> RemoveRepeat<T>(IEnumerable<T> ilist)
        {
            List<T> alist = new List<T>();
            Dictionary<T, bool> Record = new Dictionary<T, bool>();
            foreach (T item in ilist)
            {
                if (Record.ContainsKey(item)) continue;
                alist.Add(item);
                Record[item] = true;
            }
            return alist;
        }
        public static List<TOutput> ConvertAll<T, TOutput>(IEnumerable<T> alist, Converter<T, TOutput> converter)
        {
            List<TOutput> output = new List<TOutput>();
            foreach (T a in alist)
                output.Add(converter(a));
            return output;
        }
        public static List<T> DiffSet<T>(IEnumerable<T> alist, IEnumerable<T> blist)
        {
            List<T> rlist = new List<T>();
            Dictionary<T, int> map = new Dictionary<T, int>();
            
            foreach(T a in alist) {
                if (!map.ContainsKey(a)) map[a] = 0;
                map[a] += 1;
            }
            foreach (T b in blist)
            {
                if (!map.ContainsKey(b)) map[b] = 0;
                map[b] += 1;
            }
            foreach (var item in map)
            {
                if (item.Value == 1)
                    rlist.Add(item.Key);
            }
            return rlist;
        }
        public static List<string> TagSet(IEnumerable<string> tags)
        {
            Dictionary<string, bool> tagresult = new Dictionary<string, bool>();

            foreach (string t in tags)
            {
                if(t.Trim().Length > 0) 
                    tagresult[t.Trim()] = true;
            }
            return new List<string>(tagresult.Keys);
        }
    }
}
