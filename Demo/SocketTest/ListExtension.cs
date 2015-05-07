using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketTest
{
    public static class ListExtension
    {
        public static double GetBiaozhunCha(this List<long> num)
        {
            double avg = num.GetAverage();
            double count = num.Count();

            double ff = 0.0;

            for (int i = 0; i < num.Count(); i++)
            {

                ff = ff + (num[i] - avg) * (num[i] - avg);

            }

            ff = ff / count;

            return Math.Sqrt(ff);

        }

        public static double GetAverage(this List<long> list)
        {

            double sum = 0;

            int len = list.Count();

            for (int i = 0; i < len; i++)
            {

                sum += list[i];

            }

            return sum / len;

        }
    }
}
