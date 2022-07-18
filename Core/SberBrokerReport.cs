using System;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using Invest.Core.Entities;
using Invest.Core.Enums;

namespace Invest.Core
{
    public class SberBrokerReport : IBrokerReport
    {
	    private readonly string _reportDir;
	    private readonly Builder _builder;

	    // load broker reports by year (since 2022)
	    private readonly DateTime _startOperationDate;

	    public SberBrokerReport(string reportDir, Builder builder)
	    {
		    _startOperationDate = new DateTime(2022, 1, 1);
		    _reportDir = reportDir;
		    _builder = builder;

		    if (_builder.Accounts == null)
			    throw new Exception("SberBrokerReport(): Accounts is null or empty");

		    if (!Directory.Exists(reportDir))
			    throw new Exception($"SberBrokerReport(): Dir '{_reportDir}' is not exist");
	    }

	    public void Process()
	    {
		    //load broker reports by year (since 2022)
		    for (var year = _startOperationDate.Year; year <= DateTime.Today.Year; ++year)
		    {
			    foreach(var a in _builder.Accounts.Where(x => x.Broker == "Sber"))
				    LoadReportFile(a, year);
		    }

		    AddExtendedOperations();
	    }

	    private void AddExtendedOperations()
	    {
		    //transfer
		    var a = _builder.Accounts.FirstOrDefault(x => x.Broker == "Sber");
		    const string comment = "Ввод ДС";

		    _builder.AddOperation(new Operation {
			    Account = a,
			    Date = new DateTime(2022,3,21),
			    Summa = 10000.00m,
			    Currency = Currency.Rur,
			    Type = OperationType.CacheIn,
			    Comment = comment,
			    TransId="sb001"
		    });
		    _builder.AddOperation(new Operation {
			    Account = a,
			    Date = new DateTime(2022,3,21),
			    Summa = 10000.00m,
			    Currency = Currency.Rur,
			    Type = OperationType.CacheIn,
			    Comment = comment,
			    TransId="sb002"
		    });
		    _builder.AddOperation(new Operation {
			    Account = a,
			    Date = new DateTime(2022,3,23),
			    Summa = 10000.00m,
			    Currency = Currency.Rur,
			    Type = OperationType.CacheIn,
			    Comment = comment,
			    TransId="sb003"
		    });
		    _builder.AddOperation(new Operation {
			    Account = a,
			    Date = new DateTime(2022,3,23),
			    Summa = 10000.00m,
			    Currency = Currency.Rur,
			    Type = OperationType.CacheIn,
			    Comment = comment,
			    TransId="sb004"
		    });
	    }

	    private void LoadReportFile(BaseAccount account, int year)
	    {
		    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		    var dir = new DirectoryInfo(_reportDir);

		    var fileMask = $"Сделки_{year}-*.xlsx";
		    var xslFiles = dir.GetFiles(fileMask);
			
		    if (xslFiles.Length == 0)
			    throw new Exception($"There are no xsl files in directory with mask: '{fileMask}'");

		    if (xslFiles.Length > 1)
			    throw new Exception($"There are more than one xsl file found in directory by acc {account.BitCode} and year '{year}' ('{fileMask}')");

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
			var map = new SberExcelMapping(account.BitCode, year);

			using (var reader = ExcelReaderFactory.CreateReader(fs))
			{
				var emptyCellCount = 10;
				const int startRowIndex = 0;
				var rowIndex = 0;

				while (reader.Read())
				{
					if (rowIndex > startRowIndex)
					{
						var titleCell = reader.GetValue(ExcelUtil.ExcelCell("B"));
						if (titleCell != null)
						{
							var b = titleCell.ToString().Trim();

							if (!string.IsNullOrEmpty(b))
								ReadOperations(reader, account, map);
						}

						if (titleCell == null)
							emptyCellCount--;

						if (emptyCellCount == 0)
							break;
					}
					rowIndex++;
				}
			}
		}

	    private void ReadOperations(IExcelDataReader rd, BaseAccount account, SberExcelMapping map)
	    {
		    var index = 0;
		    var cells = map.GetMappingForOperation();

			// "Заключенные в отчетном периоде сделки с ценными бумагами"
			while (rd.Read())
			{
				var name = ExcelUtil.GetCellValue(cells.Name, rd); // RU000A0JTS06, isin
				if (string.IsNullOrEmpty(name))
					continue;
				
				var s = _builder.GetStock(name) ?? _builder.GetStockByIsin(name);
				if (s == null)
					throw new Exception($"ReadOperations(): stock or isin '{name}' is not found.");

				var opDate = ExcelUtil.GetCellValue(cells.Date, rd);
				var opType = ExcelUtil.GetCellValue(cells.Type, rd);
				var opQty = ExcelUtil.GetCellValue(cells.Qty, rd);
				var opPrice = ExcelUtil.GetCellValue(cells.Price, rd);
				var opBankCommission1 = ExcelUtil.GetCellValue(cells.BankCommission1, rd);
				var opBankCommission2 = ExcelUtil.GetCellValue(cells.BankCommission2, rd);
				var deliveryDate = ExcelUtil.GetCellValue(cells.DeliveryDate, rd);
				var nkd = ExcelUtil.GetCellValue(cells.Nkd, rd);

				if (!string.IsNullOrEmpty(opType) && !(opType == "Покупка" || opType == "Продажа"))
					throw new Exception($"ReadOperations(): stock '{name}'. Wrong opType = '{opType}'");

				if (string.IsNullOrEmpty(opDate) || opType == null || opPrice == null)
					continue;

				var op = new Operation
				{
					Index = ++index,
					Account = account,
					AccountType = (AccountType)account.BitCode,
					Date = DateTime.Parse(opDate),
					Stock = s,
					Qty = int.Parse(opQty),
					Price = decimal.Parse(opPrice),
					Type = opType == "Покупка" ? OperationType.Buy : OperationType.Sell,
					QtySaldo = int.Parse(opQty),
					Currency = s.Currency,
					DeliveryDate = DateTime.Parse(deliveryDate),
					//OrderId = ExcelUtil.GetCellValue(cells.OrderId, rd),
					TransId = ExcelUtil.GetCellValue(cells.TransId, rd)
				};

				if (op.TransId == null)
					throw new Exception($"ReadOperations(): op.TransId == null. {account.Id}, {op.Date.Year}, {op}");
				if (op.TransId != null && op.TransId.Length < 5)
					throw new Exception($"ReadOperations(): op.TransId is not correct string, value: {account.Id}, {op.Date.Year}, {op.TransId}");

				// russian bonds
				op.Summa = s.Type == StockType.Bond && s.Currency == Currency.Rur
					? op.Price * 10 * op.Qty
					: op.Price * op.Qty;

				if ((op.Currency == Currency.Usd || op.Currency == Currency.Eur) && op.Date >= _startOperationDate)
				{
					op.PriceInRur = op.Price * Builder.GetCurRate(op.Currency, op.Date);
					op.RurSumma = op.PriceInRur * op.Qty;
				}

				if (s.Type == StockType.Bond && !string.IsNullOrEmpty(nkd))
					op.Nkd = decimal.Parse(nkd);

				if (opBankCommission1 != null)
					op.BankCommission1 = decimal.Parse(opBankCommission1);
				if (opBankCommission2 != null)
					op.BankCommission2 = decimal.Parse(opBankCommission2);

				_builder.AddOperation(op);
			}
	    }
    }

    public class SberExcelMapping 
	{
		private readonly int _accountCode;
		private readonly int _year;

		public SberExcelMapping(int accountCode, int year)
		{
			_accountCode = accountCode;
			_year = year;
		}

		public class OperationMap
		{
			public string Name;
			public string Date;
			public string Price;
			public string Qty;
			public string Type;
			public string BankCommission1;  // Комиссия Банка за расчет по сделке
			public string BankCommission2;  // Комиссия Банка за заключение сделки
			public string OrderId;          // № заявки
			public string TransId;          // № сделки
			public string DeliveryDate;		// плановая дата поставки
			public string Nkd = "AF";		// 
			public string Currency;			// 
		}
		
		
		/// <summary>
		/// mapping cells for each account
		/// </summary>
		/// <returns></returns>
		public OperationMap GetMappingForOperation()
		{
			OperationMap m = null;

			if (_accountCode == (int)AccountType.SBr)
			{ 
				if (_year == 2022)
					m = new OperationMap {
						Name = "E",
						Date = "C",
						Type = "H",
						Qty = "I",
						Price = "J",
						BankCommission1 = "P",
						BankCommission2 = "O",
						OrderId = "",
						TransId = "B",
						DeliveryDate = "D", 
						Nkd = "K",
						Currency = "M"
					};
			}
			else
				throw new Exception($"SberExcelCellsMapping(): wrong account accountCode or year. {_year},{_accountCode}");

			return m;
		}
	}
}
