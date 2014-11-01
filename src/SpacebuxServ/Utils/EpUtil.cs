using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpacebuxServ.Utils
{
    public class EpUtil
    {
		const string EP_FORMAT_CRYPTONITE = "0.0000000000ep";
		const string EP_FORMAT = EP_FORMAT_CRYPTONITE;

        public static decimal EpToDecimal(string epValue)
        {
            var epStr = epValue.Trim();

            if (epStr.EndsWith("ep"))
                epStr = epStr.Substring(0, epStr.Length - 2);

            return Convert.ToDecimal(epStr);
        }

        public static string DecimalToEp(decimal value)
        {
            return value.ToString(EP_FORMAT);
        }
    }
}
