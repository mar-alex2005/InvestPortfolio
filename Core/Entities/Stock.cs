using System;
using System.Collections.Generic;
using System.Linq;
using Invest.Core.Enums;
using Newtonsoft.Json;

namespace Invest.Core.Entities
{
	public class BaseStock
	{
		[JsonProperty(PropertyName = "t")]
	    public string Ticker;
		public string[] Isin; // array of isin`s. In order to store some isins, if company changes own fin.params
		public string RegNum; // reg num for bonds
		[JsonProperty(PropertyName = "type")]
		public StockType Type;
        public Company Company;
        public int SortIndex;
        [JsonProperty(PropertyName = "lot")]
        public int LotSize;
        public decimal? CurPrice;
		public DateTime CurPriceUpdatedOn;
        // Name of stock in excel (brokerName)
        public string Name;
        public Currency Currency;
		/// <summary>for divs</summary>
        public Currency DivCurrency;

		public DateTime LastHistotyDate;
		//todo: 
		//[JsonIgnore]
        public Data Data;   
		//todo: 
        //[JsonIgnore]
        public Dictionary<int, AccountData> AccountData;

        public BaseStock() { }
        public BaseStock(string ticker) {
            Ticker = ticker;
        }

        [JsonIgnore]
        public List<Position> Positions { get; set; }

        //public decimal? TotalFinResult(AccountType? account, Currency? cur) 
        //{
        //    if (account != null && cur != null)
        //    { 
        //        var v = Invest.Core.Core.Instance.Operations
        //            .Where(x => x.Stock == this 
        //                && !x.IsClosed
        //                && (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
        //                && x.AccountType == account 
        //                && x.Stock.Currency == cur);

        //        return v.Sum(x => x.FinResult);
        //    }

        //    return Data.FinResultForClosedPositions;    
        //}

        //public decimal? TotalFinResultForClosedPositions(AccountType? account, Currency? cur) 
        //{
        //    var v = Invest.Core.Core.Instance.Operations
        //        .Where(x => x.Stock == this 
        //            && x.IsClosed
        //            && (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
        //            && (account == null || x.AccountType == account)
        //            && (cur == null || x.Stock.Currency == cur));
                
        //    return v.Sum(x => x.FinResult);

        //    //return Data.TotalFinResultForClosedPositions;    
        //}

		public decimal? GetCommission(int? acCode = null)
		{
			if (acCode != null)
				return AccountData[acCode.Value].Commission;
			else
				return Data.Commission;
		}
	}

    public class Stock : BaseStock
    {
    }
}
