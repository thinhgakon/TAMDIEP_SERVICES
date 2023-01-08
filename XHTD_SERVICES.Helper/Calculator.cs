using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper
{
    public static class Calculator
    {
        private const int MAX_LENGTH_SCALE_VALUE = 10;

        public static bool CheckBalanceValues(List<int> numbers, int saiso)
        {
            if (numbers.Count < MAX_LENGTH_SCALE_VALUE)
            {
                return false;
            }

            var tbc = TrungBinhCong(numbers);

            foreach(int number in numbers)
            {
                if(Math.Abs(number - tbc) > saiso)
                {
                    return false;
                }
            }

            return true;
        }

        public static double TrungBinhCong(List<int> numbers)
        {
            int size = numbers.Count;
            if (size == 0)
            {
                return 0;
            }

            int total = 0;
            foreach(int number in numbers)
            {
                total = total + number;
            }

            return (float)total / size;
        }
    }
}
