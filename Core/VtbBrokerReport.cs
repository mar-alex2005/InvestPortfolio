﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Invest.Core.Entities;
using Invest.Core.Enums;

namespace Invest.Core
{
	public interface IBrokerReport
	{
		void Process();
	}

	public class VtbBrokerReport : IBrokerReport
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
						LoadReportFile(a, year);
				}
			}
		}
		
		private void LoadReportFile(BaseAccount account, int year)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			var dir = new DirectoryInfo(_reportDir);

			var fileMask = $"{account.Name}_{year}*.xls";
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
						var titleCell = reader.GetValue(VtbExcelMapping.ExcelCell("B"));
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

			//AddExtOperations(accountType, year);
		}


		private void ReadCacheOperations(ExcelDataReader.IExcelDataReader rd, BaseAccount account, VtbExcelMapping cellMapping)
        {
            var ops = new List<Operation>();

            var cells = cellMapping.GetMappingForCache();

            while(rd.Read())
            { 
                var opDate = VtbExcelMapping.GetCellValue(cells.Date, rd);
                if (opDate == null)
                    break;
                
                if (!DateTime.TryParse(opDate, out var date))
                    continue;

                var opSumma = VtbExcelMapping.GetCellValue(cells.Summa, rd);		//rd.GetValue(3);
                var opCur= VtbExcelMapping.GetCellValue(cells.Cur, rd);
                var opType = VtbExcelMapping.GetCellValue(cells.Type, rd);			// 15
                var opComment = VtbExcelMapping.GetCellValue(cells.Comment, rd);	//rd.GetValue(33);

                if (!string.IsNullOrEmpty(opType))
                {
                    OperationType? type = null;

                    if (opType == "Зачисление денежных средств")
                        type = OperationType.BrokerCacheIn;
                    if (opType == "Вознаграждение Брокера")
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

					if (opType == "Купонный доход")
						type = OperationType.Coupon;

                    if (type == null)
                        continue;

					if (opCur == null)
						throw new Exception($"ReadCacheIn(): opCur == null. a: {account.Id}, t: {type}, {date}");

					if (string.IsNullOrEmpty(opComment) && (type != OperationType.UsdExchange && type != OperationType.BrokerCacheIn))
						throw new Exception($"ReadCacheIn(): opComment == null. a: {account.Id}, t: {type}, {date}");

                    var op = new Operation {
                        AccountType = (AccountType)account.BitCode,
                        Date = date,
                        Summa = decimal.Parse(opSumma),
						Currency = (Currency)Enum.Parse(typeof(Currency), opCur, true),
                        Type = type.Value,
                        Comment = opComment
                    };

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
                        && (int)x.AccountType == account.BitCode
                        && (x.Type == OperationType.BrokerCacheIn || x.Type == OperationType.BrokerCacheOut || x.Type == OperationType.BrokerFee || x.Type == OperationType.Dividend)) == 0)
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


		/// <summary>Load operations</summary>
		/// <param name="rd"></param>
		/// <param name="account"></param>
		/// <param name="cellMapping"></param>
		private void ReadOperations(ExcelDataReader.IExcelDataReader rd, BaseAccount account, VtbExcelMapping cellMapping)
		{
			var index = 0;

			var cells = cellMapping.GetMappingForOperation();

			// "Заключенные в отчетном периоде сделки с ценными бумагами"
			while (rd.Read())
			{
				var cellName = VtbExcelMapping.GetCellValue("B", rd);
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
					throw new Exception("Name of cell don`t contains three count literals");

				if (string.IsNullOrEmpty(name))
					continue;

				if (name.Equals("Наименование ценной бумаги", StringComparison.OrdinalIgnoreCase))
					continue;

				var s = _builder.GetStockByName(name) ?? _builder.GetStockByIsin(isin);
				if (s == null)
					throw new Exception($"ReadOperations(): stock '{name}', '{isin}' not found. Cell = '{cellName}'");

				// add company from ticker
				//AddCompany(s);

				var opDate = VtbExcelMapping.GetCellValue(cells.Date, rd);
				var opType = VtbExcelMapping.GetCellValue(cells.Type, rd);
				var opQty = VtbExcelMapping.GetCellValue(cells.Qty, rd);			//rd.GetValue(10);
				var opPrice = VtbExcelMapping.GetCellValue(cells.Price, rd);		//rd.GetValue(17);
				var opBankCommission1 = VtbExcelMapping.GetCellValue(cells.BankCommission1, rd);
				var opBankCommission2 = VtbExcelMapping.GetCellValue(cells.BankCommission2, rd);
				var deliveryDate = VtbExcelMapping.GetCellValue(cells.DeliveryDate, rd);
				var nkd = VtbExcelMapping.GetCellValue(cells.Nkd, rd);

				//if (s.Ticker == "ATVI" && opDate.StartsWith("21.01.2022")) { var rr1 =1;}

				//45 - Плановая дата поставки //48 - Плановая дата оплаты
				//52 - № заявки //55 - № сделки

				if (!string.IsNullOrEmpty(opType) && !(opType == "Покупка" || opType == "Продажа"))
					throw new Exception($"ReadOperations(): stock '{name}'. Wtong opType = '{opType}'");

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

					OrderId = VtbExcelMapping.GetCellValue(cells.OrderId, rd),
					TransId = VtbExcelMapping.GetCellValue(cells.TransId, rd)
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
					op.PriceInRur = op.Price * _builder.GetCurRate(op.Currency, op.Date);
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
				var opDate = VtbExcelMapping.GetCellValue(cells.OrderDate, rd); //date
				if (opDate == null)
					break;

				if (!DateTime.TryParse(opDate, out var date))
					continue;

				var opSumma = VtbExcelMapping.GetCellValue(cells.Summa, rd); //rd.GetValue(3);
				var opType = VtbExcelMapping.GetCellValue(cells.Type, rd); // 15
				var opFinIns = VtbExcelMapping.GetCellValue(cells.FinInstrument, rd); // "USDRUB_CNGD, EURRUB_CNGD, USDRUB_TOM, EURRUB_TOM"
				var cur = Currency.Usd;
				//var opCur = VtbExcelMapping.GetCellValue(cells.Cur, rd);
				//var opComment = GetCellValue(cellMapping.Comment, rd); //rd.GetValue(33);

				if (!string.IsNullOrEmpty(opType))
				{
					OperationType? type = null;

					if (opFinIns.StartsWith("USDRUB", StringComparison.OrdinalIgnoreCase))
					{
						cur = Currency.Usd;

						if (opType == "Покупка")
							type = OperationType.CurBuy;
						if (opType == "Продажа")
							type = OperationType.CurSell;
					}

					if (opFinIns.StartsWith("EURRUB", StringComparison.OrdinalIgnoreCase))
					{
						cur = Currency.Eur;

						if (opType == "Покупка")
							type = OperationType.CurBuy;
						if (opType == "Продажа")
							type = OperationType.CurSell;
					}

					if (type == null)
						continue;

					var bankCommission1Str = VtbExcelMapping.GetCellValue(cells.BankCommission1, rd);
					var bankCommission2Str = VtbExcelMapping.GetCellValue(cells.BankCommission2, rd);

					var op = new Operation
					{
						Account = account,
						AccountType = (AccountType)account.BitCode,
						Date = date,
						Summa = decimal.Parse(opSumma),
						Currency = cur,
						Qty = int.Parse(VtbExcelMapping.GetCellValue(cells.Qty, rd)),
						Type = type.Value,
						Price = decimal.Parse(VtbExcelMapping.GetCellValue(cells.Price, rd)),
						OrderId = VtbExcelMapping.GetCellValue(cells.OrderId, rd),
						TransId = VtbExcelMapping.GetCellValue(cells.TransId, rd),
						BankCommission1 = !string.IsNullOrEmpty(bankCommission1Str) 
							? decimal.Parse(VtbExcelMapping.GetCellValue(cells.BankCommission1, rd)) 
							: (decimal?)null,
						BankCommission2 = !string.IsNullOrEmpty(bankCommission2Str) 
							? decimal.Parse(VtbExcelMapping.GetCellValue(cells.BankCommission2, rd)) 
							: (decimal?)null,
						
						Comment = "Дата: " + VtbExcelMapping.GetCellValue(cells.OrderDate, rd) + " (" + VtbExcelMapping.GetCellValue(cells.FinInstrument, rd) + ")"
					};

					_builder.AddOperation(op);
				}
			}
		}
	}
}