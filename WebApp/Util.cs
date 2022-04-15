using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Invest.WebApp
{
    public class Util
    {
	    public static string KFormat(int qty) {
		    //if (qty >= 100000)
		    //    return (qty / 100000) + "M";
		    //if (qty >= 10000) {
		    //    return (qty / 1000D).ToString("0.#") + "K";
		    //}
		    if (qty >= 1000)
			    return (qty / 1000D).ToString("N0") + "K";
            
		    return qty.ToString("N0");
	    }

	    public static string GetSum(decimal? value, string emptyValue = "")
	    {
		    if (value != null && value != 0 && value != .0m)
			    return $"{value:N2}";

		    return emptyValue;
	    }

	    public static string GetSumWithZnak(decimal? value, string emptyValue = "")
	    {
		    if (value != null && value != 0) 
			    return value > 0 
				    ? $"+{value:N2}" 
				    : $"{value:N2}";

		    return emptyValue;
	    }

	    public static string GetPercent(decimal? value, string emptyValue = "", bool isZnak = true)
	    {
		    if (value != null && value != 0) 
			    return value > 0 
				    ? $"+{value:N2}%" 
				    : $"{value:N2}%";

		    return emptyValue;
	    }
    }
}
