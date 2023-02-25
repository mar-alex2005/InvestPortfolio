using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Invest.Core.Entities;
using Invest.Core.Enums;

namespace Invest.Core
{
	public interface IBrokerImport
	{
		void Process();
	}

	public class VtbBrokerReport : IBrokerImport
	{
		private readonly string _reportDir;
		private readonly Builder _builder;
		
		// load broker reports by year (since 2019)
		private readonly DateTime _startOperationDate;

		public VtbBrokerReport(string reportDir, Builder builder)
		{
			_startOperationDate = new DateTime(2019, 12, 1);
			_reportDir = reportDir;
			_builder = builder;

			if (_builder.Accounts == null)
				throw new Exception("VtbBrokerReport(): Accounts is null or empty");

			if (!Directory.Exists(reportDir))
				throw new Exception($"VtbBrokerReport(): Dir '{_reportDir}' is not exist");
		}

		public void Process()
		{
			//load broker reports by year (since 2019)
			for (var year = _startOperationDate.Year; year <= DateTime.Today.Year; ++year)
			{
				foreach(var a in _builder.Accounts)
				{
					if (a.Broker == "Vtb")
					{
						LoadReportFile(a, year);
						AddExtOperations(a, year);
					}
				}
			}
		}
		
		private void LoadReportFile(BaseAccount account, int year)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			var dir = new DirectoryInfo(_reportDir);
			var fileMask = $"{account.Name}_{year}*.xls";
			var xslFiles = dir.GetFiles(fileMask);

			//if (xslFiles.Length == 0)
			//	throw new Exception($"There are no xsl files in directory with mask: '{fileMask}'");

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
			var map = new VtbExcelMapping(account.BitCode, year);

			using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(fs))
			{
				var emptyCellCount = 200;
				const int startIndex = 8;
				var rowIndex = 0;

				while (reader.Read())
				{
					if (rowIndex >= startIndex)
					{
						var titleCell = reader.GetValue(ExcelUtil.ExcelCell("B"));
						if (titleCell != null)
						{
							var title = titleCell.ToString().Trim();

							if (title.Equals("Движение денежных средств", StringComparison.OrdinalIgnoreCase))
								ReadCacheOperations(reader, account, map);

							if (title.Equals("Заключенные в отчетном периоде сделки с ценными бумагами", StringComparison.OrdinalIgnoreCase))
								ReadOperations(reader, account, map);

							if (title.Equals("Завершенные в отчетном периоде сделки с иностранной валютой (обязательства прекращены)", StringComparison.OrdinalIgnoreCase)
								|| title.Equals("Заключенные в отчетном периоде сделки с иностранной валютой", StringComparison.OrdinalIgnoreCase))
								ReadCurOperations(reader, account, map);
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


		private void ReadCacheOperations(ExcelDataReader.IExcelDataReader rd, BaseAccount account, VtbExcelMapping cellMapping)
        {
            var ops = new List<Operation>();
            var cells = cellMapping.GetMappingForCache();

            while(rd.Read())
            { 
                var opDate = ExcelUtil.GetCellValue(cells.Date, rd);
                if (opDate == null)
                    break;
                
                if (!DateTime.TryParse(opDate, out var date))
                    continue;

                var opSumma = ExcelUtil.GetCellValue(cells.Summa, rd);		//rd.GetValue(3);
                var opCur= ExcelUtil.GetCellValue(cells.Cur, rd);
                var opType = ExcelUtil.GetCellValue(cells.Type, rd);			// 15
                var opComment = ExcelUtil.GetCellValue(cells.Comment, rd);	//rd.GetValue(33);

				if (string.IsNullOrEmpty(opSumma))
					throw new Exception($"ReadCacheOperations(): opSumma == null. a: {account.Name}, '{opDate}'");

				if (string.IsNullOrEmpty(opType))
					throw new Exception($"ReadCacheOperations(): opType == null. a: {account.Name}, '{opDate}'");

                if (!string.IsNullOrEmpty(opType))
                {
	                OperationType? type = null;

                    if (opType == "Зачисление денежных средств")
                        type = OperationType.CacheIn;
                    //if (opType == "Вознаграждение Брокера")
                    //    type = OperationType.BrokerFee;
                    if (opType == "Вознаграждение сторонних организаций") // Возмещение расходов НКО АО НРД за обслуживание выпуска ценных бумаг
	                    type = OperationType.BrokerFee;
                    if (opType == "Дивиденды" 
                        || (opType.Equals("Зачисление денежных средств", StringComparison.OrdinalIgnoreCase) 
                                && !string.IsNullOrEmpty(opComment) 
                                && opComment.ToLower().Contains("дивиденды"))
					)
                        type = OperationType.Dividend;

                    if (opType == "Сальдо расчётов по сделкам с иностранной валютой")
                    {
                        if (!string.IsNullOrEmpty(opCur) && opCur == "USD")
                            type = OperationType.UsdExchange;
                    }

					if (opType == "НДФЛ")
					{
						if (!string.IsNullOrEmpty(opCur) && opCur == "RUR")
                            type = OperationType.Ndfl;
					}

					if (opType == "Купонный доход") {
						type = OperationType.Coupon;
						if (date.Year == 2022 && account.Id == "VBr")
						{

						}
					}

					if (opType == "Списание денежных средств")
						type = OperationType.CacheOut;

					if (opType == "Погашение ценных бумаг")
						type = OperationType.Sell;

                    if (type == null)
                        continue;

					if (opCur == null)
						throw new Exception($"ReadCacheIn(): opCur == null. a: {account.Id}, t: {type}, {date}");

					if (string.IsNullOrEmpty(opComment) && (type != OperationType.UsdExchange && type != OperationType.CacheIn))
						throw new Exception($"ReadCacheIn(): opComment == null. a: {account.Id}, t: {type}, {date}");

                    var op = new Operation {
                        AccountType = (AccountType)account.BitCode,
                        Account = account,
                        Date = date,
                        Summa = decimal.Parse(opSumma),
						Currency = (Currency)Enum.Parse(typeof(Currency), opCur, true),
                        Type = type.Value,
                        Comment = opComment
                    };

                    if (op.Type == OperationType.Sell && string.IsNullOrEmpty(opComment))
	                    throw new Exception($"ReadCacheIn(): opComment == null. a: {account.Id}, t: {op.Type}");
   
                    if (op.Type == OperationType.Sell) {
	                    var s = _builder.Stocks.FirstOrDefault(x => x.Type == StockType.Bond 
                            && !string.IsNullOrEmpty(x.RegNum) && x.Company != null 
                            && (op.Comment.ToLower().Contains(x.RegNum.ToLower()) 
                                || (x.Isin?[0] != null && op.Comment.ToLower().Contains(x.Isin[0].ToLower()))
                            )
	                    );

	                    op.Stock = s ?? throw new Exception($"ReadCacheOperations(): Vtb, not found stock by {opComment}, {opDate}");
						op.Price = 1000;  //todo: use constant for lot size
	                    op.Qty = (int?) ((int)op.Summa / op.Price); 
						op.BankCommission1 = 0;
						op.BankCommission2 = 0;
						op.DeliveryDate = op.Date;
					}

                    if (op.Type == OperationType.Coupon) {
	                    //var s = _builder.Stocks.FirstOrDefault(x => x.Type == StockType.Bond 
	                     //       && !string.IsNullOrEmpty(x.RegNum) && x.Company != null 
	                     //       && (op.Comment.ToLower().Contains(x.RegNum.ToLower()) 
	                     //           || (x.Isin?[0] != null && op.Comment.ToLower().Contains(x.Isin[0].ToLower()))
	                     //       )
	                    //);

	                    //op.Stock = s ?? throw new Exception($"ReadCacheOperations(): Vtb, not found stock by {opComment}, {opDate}");
	                    op.BankCommission1 = 0;
	                    op.BankCommission2 = 0;
					}

                    ops.Add(op); //Instance.Operations.Add(op);
                }                
            }

            // Add to global OperationList
            if (ops.Count > 0)
            {
                var d = _startOperationDate;
                var dEnd = DateTime.Now.AddDays(1);
                while(d <= dEnd)
                {
                    if (_builder.Operations.Count(x => x.Date == d 
                        && x.Account == account
                        && (x.Type == OperationType.CacheIn || x.Type == OperationType.CacheOut 
                            || x.Type == OperationType.BrokerFee || x.Type == OperationType.Dividend)) == 0)
                    {
                        foreach(var o in ops.Where(x => x.Date == d))
                        {
	                        _builder.AddOperation(o);
                        }
                    }

                    d = d.AddDays(1);
                }
            }
        }


		private void ReadOperations(ExcelDataReader.IExcelDataReader rd, BaseAccount account, VtbExcelMapping cellMapping)
		{
			var index = 0;
			var cells = cellMapping.GetMappingForOperation();

			// "Заключенные в отчетном периоде сделки с ценными бумагами"
			while (rd.Read())
			{
				var cellName = ExcelUtil.GetCellValue("B", rd);
				if (cellName == null || cellName.Trim() == "")
					break;

				string name;  // Сбербанк ао, 10301481B, RU0009029540
				string isin;  // RU0009029540

				var cellArr = cellName.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
				if (cellArr.Length == 3)
				{
					name = cellArr[0].Trim();
					isin = cellArr[2].Trim();
				}
				else
					throw new Exception("Name of cell doesn`t contain three count literals");

				if (string.IsNullOrEmpty(name))
					continue;

				if (name.Equals("Наименование ценной бумаги", StringComparison.OrdinalIgnoreCase))
					continue;

				var s = _builder.GetStockByName(name) ?? _builder.GetStockByIsin(isin);
				if (s == null)
					throw new Exception($"ReadOperations(): stock '{name}', '{isin}' not found. Cell = '{cellName}'");

				// add company from ticker
				//AddCompany(s);
				//if (name.StartsWith("Газпрнефть", StringComparison.OrdinalIgnoreCase)) { var d = 0;}
				//if (s.Ticker == "Газпнф1P1R" || isin == "RU000A0JXNF9") { var d = 0;}

				var opDate = ExcelUtil.GetCellValue(cells.Date, rd);
				var opType = ExcelUtil.GetCellValue(cells.Type, rd);
				var opQty = ExcelUtil.GetCellValue(cells.Qty, rd);			//rd.GetValue(10);
				var opPrice = ExcelUtil.GetCellValue(cells.Price, rd);		//rd.GetValue(17);
				var opBankCommission1 = ExcelUtil.GetCellValue(cells.BankCommission1, rd);
				var opBankCommission2 = ExcelUtil.GetCellValue(cells.BankCommission2, rd);
				var deliveryDate = ExcelUtil.GetCellValue(cells.DeliveryDate, rd);
				var nkd = ExcelUtil.GetCellValue(cells.Nkd, rd);

				//if (s.Ticker == "ATVI" && opDate.StartsWith("21.01.2022")) { var rr1 =1;}

				//45 - Плановая дата поставки //48 - Плановая дата оплаты
				//52 - № заявки //55 - № сделки

				if (!string.IsNullOrEmpty(opType) && !(opType == "Покупка" || opType == "Продажа"))
					throw new Exception($"ReadOperations(): stock '{name}'. Wtong opType = '{opType}'");

				if (string.IsNullOrEmpty(opDate) || opType == null || opPrice == null)
					throw new Exception($"ReadOperations(): opDate == null || opType == null || opPrice == null, ({opDate}, {opType}, {opPrice})");

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

					OrderId = ExcelUtil.GetCellValue(cells.OrderId, rd),
					TransId = ExcelUtil.GetCellValue(cells.TransId, rd)
				};

				if (op.TransId == null)
					throw new Exception($"ReadOperations(): op.TransId == null. {account.Id}, {op.Date.Year}, {op}");
				if (op.TransId != null && op.TransId.Length < 5)
					throw new Exception($"ReadOperations(): op.TransId is not correct string, value: {account.Id}, {op.Date.Year}, {op.TransId}");

				// russian bonds
				if (s.Type == StockType.Bond && s.Currency == Currency.Rur)
					op.Price *= 10;

				op.Summa = op.Price * op.Qty;

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


		private void ReadCurOperations(ExcelDataReader.IExcelDataReader rd, BaseAccount account, VtbExcelMapping cellMapping)
		{
			var cells = cellMapping.GetMappingForCurOperation();

			while (rd.Read())
			{
				var opDate = ExcelUtil.GetCellValue(cells.OrderDate, rd); //date
				if (opDate == null)
					break;

				if (!DateTime.TryParse(opDate, out var date))
					continue;

				var opSumma = ExcelUtil.GetCellValue(cells.Summa, rd); //rd.GetValue(3);
				var opType = ExcelUtil.GetCellValue(cells.Type, rd); // 15
				var opFinIns = ExcelUtil.GetCellValue(cells.FinInstrument, rd); // "USDRUB_CNGD, EURRUB_CNGD, USDRUB_TOM, EURRUB_TOM, CNYRUB_TOM"
				var cur = Currency.Usd;
				var opDelDate = ExcelUtil.GetCellValue(cells.Date, rd);
				
				//var opCur = VtbExcelMapping.GetCellValue(cells.Cur, rd);
				//var opComment = GetCellValue(cellMapping.Comment, rd); //rd.GetValue(33);

				if (string.IsNullOrEmpty(opFinIns))
					throw new Exception("ReadCurOperations(): opFinIns is null or empty (for example, USDRUB_CNGD)");

				if (!DateTime.TryParse(opDelDate, out var dDate))
					throw new Exception($"ReadCurOperations(): opDelDate is null or empty ({account.Name})");
				
				OperationType? type = null;

				if (opFinIns.StartsWith("USDRUB", StringComparison.OrdinalIgnoreCase))
					cur = Currency.Usd;
				else if (opFinIns.StartsWith("EURRUB", StringComparison.OrdinalIgnoreCase))
					cur = Currency.Eur;
				else if (opFinIns.StartsWith("CNYRUB", StringComparison.OrdinalIgnoreCase))
					cur = Currency.Cny;

				if (opType == "Покупка")
					type = OperationType.CurBuy;
				else if (opType == "Продажа")
					type = OperationType.CurSell;

				if (opFinIns != null && type == null)
					throw new Exception($"ReadCurOperations(): type is null or empty ({opFinIns})");

				if (type == null)
					continue;

				var bankCommission1Str = ExcelUtil.GetCellValue(cells.BankCommission1, rd);
				var bankCommission2Str = ExcelUtil.GetCellValue(cells.BankCommission2, rd);

				var op = new Operation
				{
					Account = account,
					AccountType = (AccountType)account.BitCode,
					Date = date,
					DeliveryDate = dDate,
					Summa = decimal.Parse(opSumma),
					Currency = cur,
					Qty = int.Parse(ExcelUtil.GetCellValue(cells.Qty, rd)),
					Type = type.Value,
					Price = decimal.Parse(ExcelUtil.GetCellValue(cells.Price, rd)),
					OrderId = ExcelUtil.GetCellValue(cells.OrderId, rd),
					TransId = ExcelUtil.GetCellValue(cells.TransId, rd),
					BankCommission1 = !string.IsNullOrEmpty(bankCommission1Str) 
						? decimal.Parse(ExcelUtil.GetCellValue(cells.BankCommission1, rd)) 
						: (decimal?)null,
					BankCommission2 = !string.IsNullOrEmpty(bankCommission2Str) 
						? decimal.Parse(ExcelUtil.GetCellValue(cells.BankCommission2, rd)) 
						: (decimal?)null,
					
					Comment = "Дата: " + ExcelUtil.GetCellValue(cells.OrderDate, rd) + " (" + ExcelUtil.GetCellValue(cells.FinInstrument, rd) + ")"
				};

				//if (op.TransId == "CB214387288") { var t=0;}
				// check for exist TransId
				if (!_builder.Operations.Exists(x => x.TransId == op.TransId))
					_builder.AddOperation(op);
			}
		}


		private void AddExtOperations(BaseAccount account, int year)
        {
			var index = 1000000;
			if (account.BitCode == (int)AccountType.VBr) {
				if (year == 2021)
				{
					var op = new Operation {
						Index = ++index,
						AccountType = AccountType.VBr,
						Account = _builder.GetAccountByCode((int)AccountType.VBr),
						Date = new DateTime(2021, 8, 2, 2,0,1),
						Stock = _builder.GetStock("SWI"),
						Qty = 1,
						Price = 22,
						Type = OperationType.Buy,
						Currency = Currency.Usd,
						DeliveryDate = new DateTime(2021, 8, 2, 2,0,1),
						TransId = "M036440389",
						Summa = 22,
						PriceInRur = 1635.73m,
						RurSumma = 0,
						BankCommission1 = 2.94m,
						BankCommission2 = 0,
						Comment = "Конвертация"
					};
					_builder.AddOperation(op);

					op = new Operation {
						Index = ++index,
						AccountType = AccountType.VBr,
						Account = _builder.GetAccountByCode((int)AccountType.VBr),
						Date = new DateTime(2021, 8, 4, 21,0,0),
						Stock = _builder.GetStock("SWI"),
						Qty = 2,
						Price = 0,
						Type = OperationType.Sell,
						Currency = Currency.Usd,
						DeliveryDate = new DateTime(2021, 8, 2, 2,0,1),
						TransId = "M036435998",
						Summa = 0,
						PriceInRur = 0,
						RurSumma = 0,
						BankCommission1 = 0,
						BankCommission2 = 0,
						Comment = "Конвертация"
					};
					_builder.AddOperation(op);

					//TSVT
					op = new Operation {
						Index = ++index,
						AccountType = AccountType.VBr,
						Account = _builder.GetAccountByCode((int)AccountType.VBr),
						Date = new DateTime(2021, 11, 10, 21,0,1),
						Stock = _builder.GetStock("TSVT"),
						Qty = 1,
						Price = 0, //25.84m,
						Type = OperationType.Buy,
						Currency = Currency.Usd,
						DeliveryDate = new DateTime(2021, 11, 10, 21,0,1),
						TransId = "M037531193",
						Summa = 0, //51.68m,
						PriceInRur = 0,
						RurSumma = 0,
						BankCommission1 = 0,
						BankCommission2 = 0,
						Comment = "Конвертация"
					};
					_builder.AddOperation(op);

					op = new Operation {
						Index = ++index,
						Account = _builder.GetAccountByCode((int)AccountType.VBr),
						AccountType = AccountType.VBr,
						Date = new DateTime(2021, 11, 10, 21,0,1),
						Stock = _builder.GetStock("TSVT"),
						Qty = 1,
						Price = 0,
						Type = OperationType.Buy,
						Currency = Currency.Usd,
						DeliveryDate = new DateTime(2021, 11, 10, 21,0,1),
						TransId = "M037531195",
						Summa = 0,
						PriceInRur = 0,
						RurSumma = 0,
						BankCommission1 = 0,
						BankCommission2 = 0,
						Comment = "Конвертация"
					};
					_builder.AddOperation(op);
				}

				if (year == 2022)
				{
					var op = new Operation
		            {
						Index = ++index,
						Account = _builder.GetAccountByCode((int)AccountType.VBr),
		                AccountType = AccountType.VBr,
		                Date = new DateTime(2022, 1, 25, 21,0,2),
		                Stock = _builder.GetStock("ZYXI"),
		                Qty = 1,
		                Price = 0,
		                Type = OperationType.Buy,
		                QtySaldo = 1,
						Currency = Currency.Usd,
						DeliveryDate = new DateTime(2022, 1, 25, 21,0,2),
						TransId = "M037906453",
						Summa = 0,
						PriceInRur = 0,
						RurSumma = 0,
						BankCommission1 = 0,
						BankCommission2 = 0,
						Comment = "Зачисение 1 акции в качестве дивидендов"
		            };
			        _builder.AddOperation(op);
				}
			}
        }
	}
}