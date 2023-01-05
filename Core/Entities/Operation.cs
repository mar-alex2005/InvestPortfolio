using System;
using System.Collections.Generic;
using System.Linq;
using Invest.Core.Enums;
using Newtonsoft.Json;

namespace Invest.Core.Entities
{
	public class Operation
	{
		public BaseAccount Account;
		public AccountType AccountType;
		public DateTime Date;
		public DateTime? DeliveryDate; // плановая дата поставки
		public OperationType Type;
		//[JsonIgnore]
		public BaseStock Stock;
		public int? Qty;               // кол-во акций
		public decimal? Price;
		public decimal? PriceInRur;  // operation price in RUR if currency == usd or eur
		public decimal? PosPrice;	 // Цена позиции (avg)
		public int Index;
		public decimal? Summa;
		public decimal? RurSumma;  // operation summa in RUR if currency == usd or eur
		public decimal FinResult;
		public decimal TotalFinResult; // нарастающий итог
		public bool IsClosed;
		public int ClosedCount;         // count of closed (sell) positions of asset
		//B3062822828
		public string TransId;
		public string OrderId;
		public Currency Currency;
		public string Comment;

		public decimal? BankCommission1; // Комиссия Банка за расчет по сделке
		public decimal? BankCommission2; // Комиссия Банка за заключение сделки
		public int? PositionNum;
		public decimal? Nkd;			 // coupon 

		// fifo saldo for sells and buys (default value = qty)
		public int QtySaldo;
		// fin result for sell position
		public FifoResult FifoResult;
		public Position Position;

		public int OffsetQty
		{
			get {
				return Qty.HasValue 
					? Type == OperationType.Buy ? Qty.Value : -Qty.Value 
					: 0;
			}
		}

		/// <summary>Кол-во лотов</summary>
		public int LotCount =>
			Qty != null && Stock != null && Stock.LotSize != 0 
				? Qty.Value / Stock.LotSize 
				: 0;

		/// <summary>BankCommission1 + BankCommission2</summary>
		public decimal? Commission => Builder.SumValues(BankCommission1, BankCommission2);
	}
}