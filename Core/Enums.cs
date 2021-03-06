using System;

namespace Invest.Core.Enums
{
	[Flags]
	public enum PortfolioType
	{
		IIs = 1,
		Vbr = 2,
		Usd = 4,
		Rur = 8,
		BlueRu = 16,
		BlueUs = 32,
		BlueEur = 64
	}


	[Flags]
	public enum OperationType
	{
		//CasheIn = 1,   // not Used
		/// <summary>payment of dividends </summary>
		Dividend = 2,
		Buy = 4,
		Sell = 8,
		//CasheOut = 16,

		CacheIn = 32,
		/// <summary>Списание денежных средств</summary>
		CacheOut = 64,
		BrokerFee = 128,
		
		UsdExchange = 256,

		Ndfl = 512,

		CurBuy = 1024,	// Operation: Завершенные в отчетном периоде сделки с иностранной валютой (обязательства прекращены)
		CurSell = 2048,

		/// <summary>coupon income</summary>
		Coupon = 4096
	}

	[Flags]
	public enum AccountType
	{
		Iis = 1,
		VBr = 2,
		SBr = 4
	}

	[Flags]
	public enum Currency
	{
		Rur = 1,
		Usd = 2,
		Eur = 4,
		Cny = 8
	}

	[Flags]
	public enum Exchange
	{
		Mos = 1,
		Spb = 2,
		Nyse = 4
	}

	/// <summary>Asset types</summary>
	[Flags]
	public enum StockType
	{
		Share = 1,
		Bond = 2,
		ETF = 4,
	}

	/// <summary>Type of position</summary>
	public enum PositionType
	{
		Long = 1,
		Short = 2
	}
}