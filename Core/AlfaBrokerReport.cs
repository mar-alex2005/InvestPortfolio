using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Invest.Core.Entities;
using Invest.Core.Enums;

namespace Invest.Core
{
    public class AlfaBrokerReport : IBrokerReport
    {
	    private readonly string _reportDir;
	    private readonly Builder _builder;

	    // load broker reports by year (since 2022)
	    private readonly DateTime _startOperationDate;

	    public AlfaBrokerReport(string reportDir, Builder builder)
	    {
		    _startOperationDate = new DateTime(2022, 3, 1);
		    _reportDir = reportDir;
		    _builder = builder;

		    if (_builder.Accounts == null)
			    throw new Exception("AlfaBrokerReport(): Accounts is null or empty");

		    if (!Directory.Exists(reportDir))
			    throw new Exception($"AlfaBrokerReport(): Dir '{_reportDir}' is not exist");
	    }

	    public void Process()
	    {
		    //load broker reports by year (since 2022)
		    for (var year = _startOperationDate.Year; year <= DateTime.Today.Year; ++year)
		    {
			    foreach(var a in _builder.Accounts.Where(x => x.Broker == "Alfa"))
				    LoadReportFile(a, year);
		    }

			AddExtendedOperations();
	    }

	    private void AddExtendedOperations()
	    {
		    //transfer
		    var a = _builder.Accounts.FirstOrDefault(x => x.Broker == "Alfa");
			var d = new DateTime(2022,3,24);
			var comment = "Перевод ИИС (денежных средств)";

		    _builder.AddOperation(new Operation {
			    Account = a,
			    Date = d,
			    Summa = 24118.94m,
			    Currency = Currency.Rur,
			    Type = OperationType.CacheIn,
			    Comment = comment,
				TransId="p001"
		    });
		    _builder.AddOperation(new Operation {
			    Account = a,
			    Date = d,
			    Summa = 0.7m,
			    Currency = Currency.Usd,
			    Type = OperationType.CacheIn,
			    Comment = comment,
			    TransId="p002"
		    });
		    _builder.AddOperation(new Operation {
			    Account = a,
			    Date = d,
			    Summa = 165.74m,
			    Currency = Currency.Eur,
			    Type = OperationType.CacheIn,
			    Comment = comment,
			    TransId="p003"
		    });
	    }

	    private void LoadReportFile(BaseAccount account, int year)
	    {
		    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		    var dir = new DirectoryInfo(_reportDir);
			var yy = year.ToString().Substring(2,2);

		    var fileMask = $"Брокерский+4448836+(01.01.{yy}-*.*.{yy}).xml";
		    var xslFiles = dir.GetFiles(fileMask);

		    if (xslFiles.Length == 0)
			    throw new Exception($"There are no xml files in directory with mask: '{fileMask}'");

		    if (xslFiles.Length > 1)
			    throw new Exception($"There are more than one xsl file found in directory by acc {account.BitCode} and year '{year}' ('{fileMask}')");

		    foreach(var file in xslFiles.OrderBy(x => x.LastWriteTime))
		    {
			    using(var fs = File.Open(file.FullName, FileMode.Open, FileAccess.Read))
			    {
				    Parse(account, fs);
			    }
		    }
	    }

	    private void Parse(BaseAccount account, FileStream fs)
		{
			var doc = new XmlDocument();
			doc.Load(fs);

			if (doc.DocumentElement == null)
				throw new Exception("Parse(): alfa doc.DocumentElement == null");

			// завершенные сделки
			var nodes = doc.DocumentElement.SelectNodes("/report_broker/trades_finished/trade");
			if (nodes != null)
				ReadOperations(nodes, account);
				//throw new Exception("Parse(): alfa trade nodes == null");

			nodes = doc.DocumentElement.SelectNodes("/report_broker/money_moves/money_move");
			if (nodes != null)
				ReadMoneyMoves(nodes, account);
		}

	    private void ReadOperations(XmlNodeList nodes, BaseAccount account)
	    {
			var index = 0;

		    foreach(XmlNode node in nodes)
			{
				if (node == null)
					continue;

				var name = node["isin_reg"]?.InnerText; // RU000A0JTS06, isin
				var placeName = node["place_name"]?.InnerText; // МБ ФР, МБ ВР
				if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(placeName))
					continue;
				
				var s = _builder.GetStock(name) ?? _builder.GetStockByIsin(name);
				if (s == null && placeName == null)
					throw new Exception($"ReadOperations(): alfa, stock or isin '{name}' is not found.");

				var opDate = node["db_time"]?.InnerText;
				var opQty = int.Parse(node["qty"].InnerText);
				var opPrice = decimal.Parse(node["Price"].InnerText, CultureInfo.InvariantCulture);
				var opSumma = decimal.Parse(node["summ_trade"].InnerText, CultureInfo.InvariantCulture);
				var opBankCommission1 = decimal.Parse(node["bank_tax"].InnerText, CultureInfo.InvariantCulture);
				var deliveryDate = node["settlement_time"]?.InnerText;
				var opCur = node["curr_calc"]?.InnerText ?? "RUR";
				var pName = node["p_name"]?.InnerText;

				var cur = Currency.Rur;
				if (opCur.Equals(Currency.Usd.ToString(), StringComparison.OrdinalIgnoreCase))
					cur = Currency.Usd;
				else if (opCur.Equals(Currency.Eur.ToString(), StringComparison.OrdinalIgnoreCase))
					cur = Currency.Eur;

				//<place_name>МБ ВР</place_name><p_name>EUR</p_name>
				if (!string.IsNullOrEmpty(placeName) && placeName == "МБ ВР" && (pName == "EUR" || pName == "USD"))
				{
					var oCur = new Operation
					{
						Index = ++index,
						Account = account,
						//AccountType = (AccountType)account.BitCode,
						Date = DateTime.Parse(opDate),
						Qty = Math.Abs(opQty),
						Price = opPrice,
						Type = opQty > 0 ? OperationType.CurBuy : OperationType.CurSell,
						QtySaldo = opQty,
						Summa = opSumma,
						Currency = pName == "EUR" ? Currency.Eur : Currency.Usd,
						DeliveryDate = DateTime.Parse(deliveryDate),
						TransId = node["trade_no"]?.InnerText,
						BankCommission1 = opBankCommission1,
						BankCommission2 = null
					};
					_builder.AddOperation(oCur);
					continue;
				}

				//if (!string.IsNullOrEmpty(opType) && !(opType == "Покупка" || opType == "Продажа"))
				//	throw new Exception($"ReadOperations(): stock '{name}'. Wrong opType = '{opType}'");

				//if (string.IsNullOrEmpty(opDate) || opType == null || opPrice == null)
				//	continue;

				var op = new Operation
				{
					Index = ++index,
					Account = account,
					AccountType = (AccountType)account.BitCode,
					Date = DateTime.Parse(opDate),
					Stock = s,
					Qty = opQty,
					Price = opPrice,
					Type = opQty > 0 ? OperationType.Buy : OperationType.Sell,
					QtySaldo = opQty,
					Summa = opSumma,
					Currency = cur, //s.Currency,
					DeliveryDate = DateTime.Parse(deliveryDate),
					//OrderId = ExcelUtil.GetCellValue(cells.OrderId, rd),
					TransId = node["trade_no"]?.InnerText,
					BankCommission1 = opBankCommission1,
					BankCommission2 = null
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

				//if (s.Type == StockType.Bond && !string.IsNullOrEmpty(nkd))
				//	op.Nkd = decimal.Parse(nkd);

				_builder.AddOperation(op);
			}
	    }

		private void ReadMoneyMoves(XmlNodeList nodes, BaseAccount account)
	    {
		    foreach(XmlNode node in nodes)
			{
				if (node == null)
					continue;

				var opDate = node["settlement_date"]?.InnerText;
                if (string.IsNullOrEmpty(opDate))
                    break;
                
                if (!DateTime.TryParse(opDate, out var date))
                    continue;

                var opSumma = node["volume"]?.InnerText;
                var opCur = node["p_code"]?.InnerText;
                var opGroup = node["oper_group"]?.InnerText;
                var opComment = node["comment"]?.InnerText;

				if (!string.IsNullOrEmpty(opCur))
					opCur = opCur.Replace("RUB", "Rur", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(opComment))
	                throw new Exception($"ReadMoneyMoves(): Alfa, opComment == null. a: {account.Id}, t: {opGroup}, {opDate}");

                OperationType? type = null;
				var summa = decimal.Parse(opSumma, CultureInfo.InvariantCulture);
				BaseStock stock = null;

                if (opGroup == "Купоны" && opComment.StartsWith("погашение купона", StringComparison.OrdinalIgnoreCase))
                    type = OperationType.Coupon;
                if (opGroup == "" && opComment.StartsWith("полное погашение номинала", StringComparison.OrdinalIgnoreCase))
				{
	                type = OperationType.Sell;	                

	                var s = _builder.Stocks.FirstOrDefault(x => x.Type == StockType.Bond 
	                            && !string.IsNullOrEmpty(x.RegNum) && x.Company != null 
	                            && (opComment.ToLower().Contains(x.RegNum.ToLower()) 
	                                || (x.Isin?[0] != null && opComment.ToLower().Contains(x.Isin[0].ToLower()))
	                            )
	                );

	                stock = s ?? throw new Exception($"ReadMoneyMoves(): Alfa, not found stock by {opComment}, {opDate}");
				}

                //            if (opType == "Вознаграждение Брокера")
                //                type = OperationType.BrokerFee;
                //            if (opType == "Дивиденды" 
                //                || (opType.Equals("Зачисление денежных средств", StringComparison.OrdinalIgnoreCase) 
                //                        && !string.IsNullOrEmpty(opComment) 
                //                        && opComment.ToLower().Contains("дивиденды"))
				//)
			    //                type = OperationType.Dividend;
			    //if (opType == "НДФЛ")
				//{
				//	if (!string.IsNullOrEmpty(opCur) && opCur == "RUR")
				//                    type = OperationType.Ndfl;
				//}

                if (type == null)
                    continue;

				if (opCur == null)
					throw new Exception($"ReadCacheIn(): opCur == null. a: {account.Id}, t: {type}, {date}");

				if (string.IsNullOrEmpty(opComment) && (type != OperationType.UsdExchange && type != OperationType.CacheIn))
					throw new Exception($"ReadCacheIn(): opComment == null. a: {account.Id}, t: {type}, {date}");

				Operation op = null;

				if (type == OperationType.Coupon)
				{
	                op = new Operation {
	                    AccountType = (AccountType)account.BitCode,
	                    Account = account,
	                    Date = date,
	                    Summa = summa,
						Currency = (Currency)Enum.Parse(typeof(Currency), opCur, true),
	                    Type = type.Value,
	                    Comment = opComment
	                };
				}
				else if (type == OperationType.Sell)
				{
					op = new Operation {
						AccountType = (AccountType)account.BitCode,
						Account = account,
						Date = date,
						DeliveryDate = date,
						Summa = summa,
						Currency = (Currency)Enum.Parse(typeof(Currency), opCur, true),
						Type = type.Value,
						Comment = opComment,
						Stock = stock,
						Qty = (int?)(summa / 1000),
						Price = 1000,
						BankCommission1 = 0, BankCommission2 = 0
					};
				}

				if (op == null)
					throw new Exception("ReadCacheIn(): Alfa, attempt add null operation");

                _builder.AddOperation(op);
			}
	    }
	}
}