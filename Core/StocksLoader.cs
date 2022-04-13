using System;
using System.Collections.Generic;
using System.Text;
using Invest.Core.Entities;

namespace Invest.Core
{
	public interface IStocksLoader
	{
		List<Stock> Load();
	}

    public class StocksLoader : IStocksLoader
    {
	    public List<Stock> Load()
	    {
		    return null;
	    }
    }
}