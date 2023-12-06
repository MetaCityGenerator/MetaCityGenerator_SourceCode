using System;

namespace MetaCity.Planning.RegulatoryPlan
{
    public class IndexPQ<T> where T : IComparable
    {
        private const string MIN = "min";
        private const string MAX = "max";
        private const int DEFAULT_INIT_CAP = 10000;
        private int n = 0;
        private readonly int[] pq = new int[DEFAULT_INIT_CAP];
        private readonly int[] qp = new int[DEFAULT_INIT_CAP];
        private readonly T[] keys = new T[DEFAULT_INIT_CAP];
        private readonly string pqtype;

        public IndexPQ(string type = MIN)
        {
            pqtype = type;
            for (int i = 0; i<DEFAULT_INIT_CAP; i++)
            {
                qp[i] = -1;
            }
        }

        public void Insert(int k, T item)
        {
            n++;
            pq[n] = k;
            qp[k] = n;
            keys[k] = item;
            Swim(n);
        }

        public bool Contains(int k) => qp[k] >= 0;

        public void DecreaseKey(int k, T item)
        {
            keys[k] = item;
            Swim(qp[k]);
        }

        public int DelMin()
        {
            int t = MinIndex;
            Delete(t);
            return t;
        }

        public IndexPQ<T> MinMaxConversion()
        {
            IndexPQ<T> res = (pqtype == MIN) ? new IndexPQ<T>(MAX) : new IndexPQ<T>(MIN);
            for (int i = 0; i < n; i++) res.Insert(pq[i], keys[pq[i]]);
            return res;
        }

        public bool IsEmpty => n == 0;

        private int MinIndex => pq[1];

        private void Delete(int k)
        {
            int p = qp[k];
            Exch(p, n--);
            Swim(p);
            Sink(p);
            qp[k] = -1;
        }

        private bool Compare(int i, int j)
        {
            if (pqtype == "min") { return keys[pq[i]].CompareTo(keys[pq[j]]) <= 0; }
              else { return keys[pq[i]].CompareTo(keys[pq[j]]) >= 0; }
        }

        private void Exch(int i, int j)
        {
            int t = pq[i];
            pq[i] = pq[j];
            pq[j] = t;
            qp[pq[i]] = i;
            qp[pq[j]] = j;
        }

        private void Swim(int k)
        {
            int p = k;
            while ((p > 1) && (Compare(p, p / 2)))
            {
                Exch(p / 2, p);
                p /= 2;
            }
        }

        private void Sink(int k)
        {
            int p = k;
            while (p * 2 <= n)
            {
                int j = 2 * p;
                if ((j < n) && (Compare(j + 1, j))) j++;
                if (Compare(p, j)) break;
                Exch(p, j);
                p = j;
            }
        }
    }
}
