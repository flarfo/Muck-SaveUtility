using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveUtility
{
    public class SerializableTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public static implicit operator Tuple<T1, T2>(SerializableTuple<T1, T2> st)
        {
            return Tuple.Create(st.Item1, st.Item2);
        }

        public static implicit operator SerializableTuple<T1, T2>(Tuple<T1, T2> t)
        {
            return new SerializableTuple<T1, T2>()
            {
                Item1 = t.Item1,
                Item2 = t.Item2,
            };
        }

        public SerializableTuple()
        {
        }
    }

    public class SerializableTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public static implicit operator Tuple<T1, T2, T3>(SerializableTuple<T1, T2, T3> st)
        {
            return Tuple.Create(st.Item1, st.Item2, st.Item3);
        }

        public static implicit operator SerializableTuple<T1, T2, T3>(Tuple<T1, T2, T3> t)
        {
            return new SerializableTuple<T1, T2, T3>()
            {
                Item1 = t.Item1,
                Item2 = t.Item2,
                Item3 = t.Item3,
            };
        }

        public SerializableTuple()
        {
        }
    }
}
