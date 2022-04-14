using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Invest.Core.Entities;

namespace Invest.WebApp.Models
{

	public class BaseViewModel
	{
		public HtmlString Value(decimal? value, bool useZnak = true, string css = null)
		{
			var znak = "";
			if (value > 0)
				znak = " +";
			if (value < 0)
				znak = " ";

			if (!useZnak)
				znak = "";

			var s = "font-weight: bold;";
			if (value > 0)
				s += "color: limegreen;";
			if (value < 0)
				s += "color: red;";
			if (value == 0)
				s += "color: grey;";

			var z = $"{znak}{value:N2}";

			return new HtmlString("<span style='" + s + ";" + css + "'>" + z + "</span>");
		}
	}

	public class OperationViewModel : BaseViewModel
	{
		public string Ticker;
		public List<Operation> Operations;
		public List<BaseStock> Stocks;
	}


}
