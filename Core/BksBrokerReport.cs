using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using Invest.Core.Entities;
using Invest.Core.Enums;

namespace Invest.Core
{
    public class BksBrokerReport : IBrokerImport
    {
	    private readonly string _reportDir;
	    private readonly Builder _builder;

	    // load broker reports by year (since 2022)
	    private readonly DateTime _startOperationDate;

	    public BksBrokerReport(string reportDir, Builder builder)
	    {
		    _startOperationDate = new DateTime(2022, 4, 1);
		    _reportDir = reportDir;
		    _builder = builder;

		    if (_builder.Accounts == null)
			    throw new Exception("BksBrokerReport(): Accounts is null or empty");

		    if (!Directory.Exists(reportDir))
			    throw new Exception($"BksBrokerReport(): Dir '{_reportDir}' is not exist");
	    }

	    public void Process()
	    {
		    //load broker reports by year (since 2022)
		    for (var year = _startOperationDate.Year; year <= DateTime.Today.Year; ++year)
		    {
			    foreach(var a in _builder.Accounts.Where(x => x.Broker == "Bks"))
				    LoadReportFile(a, year);
		    }

		    AddExtendedOperations();
	    }

	    private void AddExtendedOperations()
	    {
		    //transfer
		    //var a = _builder.Accounts.FirstOrDefault(x => x.Broker == "Bks");
		    //const string comment = "Перевод ДС из АБ";

		    //_builder.AddOperation(new Operation {
			   // Account = a,
			   // Date = new DateTime(2022,3,21),
			   // Summa = 10000.00m,
			   // Currency = Currency.Rur,
			   // Type = OperationType.CacheIn,
			   // Comment = comment,
			   // TransId="sb001"
		    //});
	    }

	    private void LoadReportFile(BaseAccount account, int year)
	    {
		    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		    var dir = new DirectoryInfo(_reportDir);
			var d = new DateTime(year,1,1);

		    var fileMask = $"B_k-2657758_ALL_{d:yy}-*.xlsx";
		    var xslFiles = dir.GetFiles(fileMask);
			
		    if (xslFiles.Length == 0)
			    throw new Exception($"BKS: There are no xsl files in directory with mask: '{fileMask}'");

		    //if (xslFiles.Length > 1)
			//    throw new Exception($"BKS: There are more than one xsl file found in directory by acc {account.BitCode} and year '{year}' ('{fileMask}')");

		    foreach(var file in xslFiles.OrderBy(x => x.LastWriteTime))
		    {
			    using(var fs = File.Open(file.FullName, FileMode.Open, FileAccess.Read))
			    {
				    Parse(account, year, fs);
			    }
		    }
	    }

	    private void Parse(BaseAccount account, int year, FileStream fs)
		{
			var map = new BksExcelMapping(account.BitCode, year);
			const string firstColName = "B";

			using (var reader = ExcelReaderFactory.CreateReader(fs))
			{
				var emptyCellCount = 0;
				const int startRowIndex = 0;
				var rowIndex = 0;
				
				while (reader.Read())
				{
					if (rowIndex > startRowIndex)
					{
						var titleCell = reader.GetValue(ExcelUtil.ExcelCell(firstColName));
						if (titleCell != null)
						{
							var b = titleCell.ToString().Trim();

							if (!string.IsNullOrEmpty(b) && titleCell.ToString() == "Рубль") //"1. Движение денежных средств")
								ReadCache(reader, account, map);

							else if (!string.IsNullOrEmpty(b) && titleCell.ToString().StartsWith("Акция"))
								ReadShareOperations(reader, account, map);

							else if (!string.IsNullOrEmpty(b) && titleCell.ToString().StartsWith("Облигация"))
								ReadBondsOperations(reader, account, map);

							else if (!string.IsNullOrEmpty(b) && titleCell.ToString().StartsWith("Иностранная валюта"))
								ReadCurrencyOperations(reader, account, map);

							emptyCellCount = 0;
						}

						if (titleCell == null)
							emptyCellCount++;

						if (emptyCellCount >= 30)
							break;
					}
					rowIndex++;
				}
			}
		}

		private void ReadCache(IExcelDataReader rd, BaseAccount account, BksExcelMapping map)
	    {
		    var index = 0;
		    var cells = map.GetMappingForCache();

			// "Заключенные в отчетном периоде сделки с ценными бумагами"
			while (rd.Read())
			{
				var type = ExcelUtil.GetCellValue(cells.Type, rd); // приход
				if (string.IsNullOrEmpty(type))
					break;
				
				DateTime d;
				var opDate = ExcelUtil.GetCellValue(cells.Date, rd);
				if (!DateTime.TryParse(opDate, out d)) {
					continue;
				}

				var opType = ExcelUtil.GetCellValue(cells.Type, rd);
				var opSumma = ExcelUtil.GetCellValue(cells.Summa, rd);
				var opComment = ExcelUtil.GetCellValue(cells.Comment, rd);
				
				var op = new Operation
				{
					Index = ++index,
					Account = account,
					AccountType = (AccountType)account.BitCode,
					Date = d,
					Summa = decimal.Parse(opSumma, CultureInfo.InvariantCulture),
					Currency = Currency.Rur,
					Comment = opComment
				};
				
				if (opType == "Приход ДС")
					op.Type = OperationType.CacheIn;
				else if (opType == "Вывод ДС") {
					op.Type = OperationType.CacheOut;
					op.Summa *= -1;
				}
				else if (opType == "Погашение купона")
					op.Type = OperationType.Coupon;
				else if (opType == "Погашение облигации")
				{
					var s = _builder.Stocks.FirstOrDefault(x => x.Type == StockType.Bond && x.Company != null 
							&& (!string.IsNullOrEmpty(x.RegNum) && opComment.ToLower().Contains(x.RegNum.ToLower()) 
								|| (x.Isin?[0] != null && opComment.ToLower().Contains(x.Isin[0].ToLower())))
					);

					if (s == null)
						throw new Exception($"ReadCache(): Bks, not found stock by {opComment}, {opDate}");

					op.Type = OperationType.Sell;
					op.Currency = Currency.Rur;
					op.Price = 1000;
					op.Qty = (int?)(op.Summa / op.Price);
					op.DeliveryDate = d;
					op.BankCommission1 = 0;
					op.BankCommission2 = 0;
					op.Stock = s;

					//if (s.Ticker == "Татнфт1P1") { var t = 9; }
				}
				else if (opType == "Дивиденды")
				{
					op.Type = OperationType.Dividend;
					op.Currency = Currency.Rur;
					op.BankCommission1 = 0;
					op.BankCommission2 = 0;
				}
				else
					continue;

				_builder.AddOperation(op);
			}
	    }

	    private void ReadShareOperations(IExcelDataReader rd, BaseAccount account, BksExcelMapping map)
	    {
		    //var index = 0;
		    var cells = map.GetMappingForSharesOpeartions();

		    while (rd.Read())
		    {
			    var name = ExcelUtil.GetCellValue(cells.Name, rd);
			    if (string.IsNullOrEmpty(name))
				    break;  // the end of bond section

			    //if (string.IsNullOrEmpty(name) || name.Length > 20 || name.Length < 4)
				   // continue;

			    var isin = ExcelUtil.GetCellValue(cells.Isin, rd);
			    if (string.IsNullOrEmpty(isin) || isin.Contains("Продано"))
					continue;

			    var s = _builder.Stocks.FirstOrDefault(x => x.Company != null 
				    && ((x.Isin?.Length == 1 && x.Isin?[0] != null && x.Isin?[0] == isin)) 
						|| (x.Isin?.Length == 2 && x.Isin?[1] != null && x.Isin?[1] == isin));

			    if (s == null)
				    throw new Exception($"ReadShareOperations(): Bks, not found stock by {name}, {isin}"); 
	
			    ParseShareOperation(s, rd, account, map);
		    }
	    }

		private void ParseShareOperation(BaseStock s, IExcelDataReader rd, BaseAccount account, BksExcelMapping map)
		{
			var index = 0;
			var cells = map.GetMappingForSharesOpeartions();

			while (rd.Read())
			{
				DateTime d;
				var opDate = ExcelUtil.GetCellValue(cells.Date, rd);
				if (!DateTime.TryParse(opDate, out d)) {
					break;
				}

				TimeSpan time;
				var opTime = ExcelUtil.GetCellValue(cells.Time, rd);
				if (!TimeSpan.TryParse(opTime, out time)) {
					continue;
				}

				DateTime dd;
				var opDDate = ExcelUtil.GetCellValue(cells.DeliveryDate, rd);
				if (!DateTime.TryParse(opDDate, out dd)) {
					continue;
				}

				var opType = OperationType.Buy;
				var opTransId = ExcelUtil.GetCellValue(cells.TransId, rd);
				var opQty = ExcelUtil.GetCellValue(cells.BuyQty, rd);
				var opPrice = ExcelUtil.GetCellValue(cells.BuyPrice, rd);

				if (opQty == null && ExcelUtil.GetCellValue(cells.SellQty, rd) != null)
				{
					opType = OperationType.Sell;
					opQty = ExcelUtil.GetCellValue(cells.SellQty, rd);
					opPrice = ExcelUtil.GetCellValue(cells.SellPrice, rd);
				}
				//var opComment = ExcelUtil.GetCellValue(cells.Comment, rd);
					
				var op = new Operation
				{
					Index = ++index,
					Account = account,
					AccountType = (AccountType)account.BitCode,
					Date = d.Add(time),
					DeliveryDate = dd,
					Qty = int.Parse(opQty),
					Price = decimal.Parse(opPrice, CultureInfo.InvariantCulture),
					Currency = Currency.Rur,
					Stock = s,
					//Comment = opComment
					TransId = opTransId
				};

				op.Summa = op.Price * op.Qty;
				op.Type = opType;

				//todo: calc commision by 0.01 percent
				//if (op.Type == OperationType.Buy)
				{
					op.BankCommission1 = 0;
					op.BankCommission2 = 0;
				}
					
				_builder.AddOperation(op);
			}
		}
	    
		private void ReadBondsOperations(IExcelDataReader rd, BaseAccount account, BksExcelMapping map)
	    {
		    //var index = 0;
		    var cells = map.GetMappingForBondsOpeartions();

		    while (rd.Read())
		    {
			    var name = ExcelUtil.GetCellValue(cells.Name, rd);
			    if (string.IsNullOrEmpty(name))
				    break;  // the end of bond section

			    if (string.IsNullOrEmpty(name) || name.Length > 20 || name.Length <= 8)
				    continue;

			    var s = _builder.Stocks.FirstOrDefault(x => x.Company != null 
				    && (x.RegNum?.ToLower() == name 
				        || (x.Isin?.Length == 1 && x.Isin?[0] != null && x.Isin?[0] == name)) 
						|| (x.Isin?.Length == 2 && x.Isin?[1] != null && x.Isin?[1] == name));

			    if (s == null)
				    throw new Exception($"ReadBondsOperations(): Bks, not found stock by {name}"); 
	
			    ParseBondOperation(s, rd, account, map);
		    }
	    }

		private void ParseBondOperation(BaseStock s, IExcelDataReader rd, BaseAccount account, BksExcelMapping map)
		{
			var index = 0;
			var cells = map.GetMappingForBondsOpeartions();

			while (rd.Read()) //next row
			{
				DateTime d;
				var opDate = ExcelUtil.GetCellValue(cells.Date, rd);
				if (!DateTime.TryParse(opDate, out d)) {
					break;
				}

				DateTime dd;
				var opDDate = ExcelUtil.GetCellValue(cells.DeliveryDate, rd);
				if (!DateTime.TryParse(opDDate, out dd)) {
					continue;
				}

				var opType = OperationType.Buy;
				var opTransId = ExcelUtil.GetCellValue(cells.TransId, rd);
				var opQty = ExcelUtil.GetCellValue(cells.BuyQty, rd);
				var opPrice = ExcelUtil.GetCellValue(cells.BuyPrice, rd);
				var opNkd = ExcelUtil.GetCellValue(cells.BuyNkd, rd);
				var opSumma = ExcelUtil.GetCellValue(cells.BuySumma, rd);
				//var opComment = ExcelUtil.GetCellValue(cells.Comment, rd);

				if (opQty == null && ExcelUtil.GetCellValue(cells.SellQty, rd) != null)
				{
					opType = OperationType.Sell;
					opQty = ExcelUtil.GetCellValue(cells.SellQty, rd);
					opPrice = ExcelUtil.GetCellValue(cells.SellPrice, rd);
				}

				if (opType == OperationType.Sell)
				{
					opNkd = ExcelUtil.GetCellValue(cells.SellNkd, rd);
					if (string.IsNullOrEmpty(opNkd) || !decimal.TryParse(opNkd, out _))
						throw new Exception($"ParseBondOperation(), opNkd == null or empty for sell bond. {dd}");
				}

				if (opType == OperationType.Sell)
					opSumma = ExcelUtil.GetCellValue(cells.SellSumma, rd);

				if (string.IsNullOrEmpty(opSumma) || !decimal.TryParse(opSumma, out _))
					throw new Exception($"ParseBondOperation(), opSumma == null or empty for bond. {dd}");

				var op = new Operation
				{
					Index = ++index,
					Account = account,
					AccountType = (AccountType)account.BitCode,
					Date = d,
					DeliveryDate = dd,
					Qty = int.Parse(opQty),
					Price = decimal.Parse(opPrice, CultureInfo.InvariantCulture),
					//Summa = decimal.Parse(opSumma, CultureInfo.InvariantCulture),
					Nkd = decimal.Parse(opNkd ?? "0", CultureInfo.InvariantCulture),
					Currency = Currency.Rur,
					Stock = s,
					//Comment = opComment
					TransId = opTransId,
					BankCommission1 = 0,
					BankCommission2 = 0
				};

				op.Price *= 10;
				op.Summa = decimal.Parse(opSumma, CultureInfo.InvariantCulture); //op.Price * op.Qty;
				op.Type = opType;
				
				_builder.AddOperation(op);
			}
		}

		private void ReadCurrencyOperations(IExcelDataReader rd, BaseAccount account, BksExcelMapping map)
	    {
			var isDataStarting = false;
		    var cells = map.GetMappingForCurrencyOperations();

		    while (rd.Read())
		    {
			    var value = ExcelUtil.GetCellValue(cells.Name, rd);

				if (string.IsNullOrEmpty(value) && !isDataStarting)
					continue;
				if (!string.IsNullOrEmpty(value))
					isDataStarting = true;
				if (string.IsNullOrEmpty(value) && isDataStarting)
					break;

				if (!value.EndsWith("_TOM"))
					continue;

				var curStr = value.Substring(0,3); // "USDRUB_TOM", "CNYRUB_TOM"
				Currency? opCur = (Currency)Enum.Parse(typeof(Currency), curStr, true);

				ParseCurrencyOperation(opCur.Value, rd, account, map);
		    }
	    }

		private void ParseCurrencyOperation(Currency cur, IExcelDataReader rd, BaseAccount account, BksExcelMapping map)
		{
			var index = 0;
			var cells = map.GetMappingForCurrencyOperations();

			while (rd.Read())
			{
				var opDDate = ExcelUtil.GetCellValue(cells.DeliveryDate, rd);
				if (!DateTime.TryParse(opDDate, out var dd))
					break;
				
				var opDate = ExcelUtil.GetCellValue(cells.Date, rd);
				if (!DateTime.TryParse(opDate, out var d))
					continue;

				var opTime = ExcelUtil.GetCellValue(cells.Time, rd);
				if (!TimeSpan.TryParse(opTime, out var time))
					continue;

				var type = OperationType.CurBuy;
				var opTransId = ExcelUtil.GetCellValue(cells.TransId, rd);
				var opQty = ExcelUtil.GetCellValue(cells.BuyQty, rd);
				var opPrice = ExcelUtil.GetCellValue(cells.BuyPrice, rd);
				var opSumma = ExcelUtil.GetCellValue(cells.BuySumma, rd);

				if (opQty == null && opPrice == null && ExcelUtil.GetCellValue(cells.SellQty, rd) != null)
				{
					type = OperationType.CurSell;
					opQty = ExcelUtil.GetCellValue(cells.SellQty, rd);
					opPrice = ExcelUtil.GetCellValue(cells.SellPrice, rd);
					opSumma = ExcelUtil.GetCellValue(cells.SellSumma, rd);
				}
				    
				var op = new Operation
				{
					Index = ++index,
					Account = account,
					AccountType = (AccountType)account.BitCode,
					Type = type,
					Date = d.Add(time),
					DeliveryDate = dd,
					Qty = int.Parse(opQty),
					Price = decimal.Parse(opPrice, CultureInfo.InvariantCulture),
					Summa = decimal.Parse(opSumma, CultureInfo.InvariantCulture),
					Currency =  cur,
					TransId = opTransId,
					BankCommission1 = 0,
					BankCommission2 = 0
				};

				_builder.AddOperation(op);
			}
		}
    }

    public class BksExcelMapping 
	{
		private readonly int _accountCode;
		private readonly int _year;

		public BksExcelMapping(int accountCode, int year)
		{
			_accountCode = accountCode;
			_year = year;
		}

		public class CurrencyMap
		{
			public string Name;
			public string Date, Time;
			public string Type;
			public string DeliveryDate;		// плановая дата поставки
			public string Currency;			// 
			public string Comment; 
			public string BuySumma, SellSumma;
			public string TransId;
			public string BuyQty, BuyPrice;
			public string SellQty, SellPrice;
		}

		public class CacheMap
		{
			public string Date;
			public string Type;
			public string DeliveryDate;		// плановая дата поставки
			public string Currency;			// 
			public string Comment; 
			public string Summa;
		}

		public class ShareOperationMap
		{
			public string Name, Date, Time;
			public string BuyPrice, SellPrice;
			public string BuyQty, SellQty;
			public string Type;
			public string BankCommission1;  // Комиссия Банка за расчет по сделке
			public string BankCommission2;  // Комиссия Банка за заключение сделки
			public string OrderId;          // № заявки
			public string TransId;          // № сделки
			public string DeliveryDate;		// плановая дата поставки
			public string Nkd = "AF";		// 
			public string Currency;			// 
			public string RegNum, Isin;			// 
		}

		public class BondOperationMap
		{
			public string Name;
			public string Date;
			public string BuyPrice, SellPrice;
			public string BuyQty, SellQty;
			public string BuySumma, SellSumma;
			public string Type;
			public string BankCommission1;  // Комиссия Банка за расчет по сделке
			public string BankCommission2;  // Комиссия Банка за заключение сделки
			public string OrderId;          // № заявки
			public string TransId;          // № сделки
			public string DeliveryDate;		// плановая дата поставки
			public string BuyNkd = "AF";	// 
			public string SellNkd = "";		
			public string Currency;			
		}
		

		public CacheMap GetMappingForCache()
		{
			CacheMap m;

			if (_year == 2022)
				m = new CacheMap {
					Date = "B",
					Type = "C",
					DeliveryDate = "D", 
					Currency = "P",
					Comment = "O",
					Summa = "G"
				};
			else
				throw new Exception($"BksExcelCellsMapping(): wrong account accountCode or year. {_year},{_accountCode}");

			return m;
		}
		
		/// <summary>
		/// mapping cells for each account
		/// </summary>
		/// <returns></returns>
		public ShareOperationMap GetMappingForSharesOpeartions()
		{
			ShareOperationMap m;

			if (_year == 2022)
				m = new ShareOperationMap {
					Name = "B",
					Date = "M",
					Time = "N",
					Type = "C",
					BuyQty = "E",
					BuyPrice = "F",
					SellQty = "H",
					SellPrice = "I",
					BankCommission1 = "P",
					BankCommission2 = "O",
					OrderId = "",
					TransId = "C",
					DeliveryDate = "B", 
					Nkd = "K",
					Currency = "M",
					RegNum = "F",
					Isin = "H",
				};
			else
				throw new Exception($"BksExcelCellsMapping(): wrong account accountCode or year. {_year}, {_accountCode}");

			return m;
		}

		public BondOperationMap GetMappingForBondsOpeartions()
		{
			BondOperationMap m;

			if (_year == 2022)
				m = new BondOperationMap {
					Name = "B",
					Date = "O",
					Type = "",
					BuyQty = "E",
					BuyPrice = "F",
					BuySumma = "G",
					SellQty = "I",
					SellPrice = "J",
					SellSumma = "K",

					BankCommission1 = "P",
					BankCommission2 = "O",
					OrderId = "",
					TransId = "C",
					DeliveryDate = "B", 
					BuyNkd = "H",
					SellNkd = "L",
					Currency = "N"
				};
			else
				throw new Exception($"BksExcelCellsMapping(): wrong account accountCode or year. {_year}, {_accountCode}");

			return m;
		}

		public CurrencyMap GetMappingForCurrencyOperations()
		{
			CurrencyMap m;

			if (_year == 2022)
				m = new CurrencyMap {
					Name = "B",
					Date = "K",
					Time = "L",
					DeliveryDate = "N", 
					Currency = "P",
					Comment = "O",
					BuySumma = "G",
					TransId = "C",
					BuyQty = "F",
					BuyPrice = "E",
					SellQty = "I",
					SellPrice = "H",
					SellSumma = "J",
				};
			else
				throw new Exception($"BksExcelCellsMapping(): wrong account accountCode or year. {_year},{_accountCode}");

			return m;
		}
	}
}