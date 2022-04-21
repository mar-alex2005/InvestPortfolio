using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ExcelDataReader;
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
	    }

	    private void LoadReportFile(BaseAccount account, int year)
	    {
		    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		    var dir = new DirectoryInfo(_reportDir);
			var yy = year.ToString().Substring(2,2);

		    var fileMask = $"Брокерский+4448836+(*.*.{yy}-*.*.{yy}).xml";
		    var xslFiles = dir.GetFiles(fileMask);

		    if (xslFiles.Length == 0)
			    throw new Exception($"There are no xml files in directory with mask: '{fileMask}'");

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

			var doc = new XmlDocument();
			doc.Load(fs);

			// завершенные сделки
			var nodes = doc.DocumentElement.SelectNodes("/report_broker/trades_finished/trade");

			if (nodes == null)
				throw new Exception("Parse(): alfa trade nodes == null");

			ReadOperations(nodes, account, map);
		}

	    private void ReadOperations(XmlNodeList nodes, BaseAccount account, SberExcelMapping map)
	    {
			var index = 0;

		    foreach(XmlNode node in nodes)
			{
				if (node == null)
					continue;

				var name = node["isin_reg"]?.InnerText; // RU000A0JTS06, isin
				if (string.IsNullOrEmpty(name))
					continue;
				
				var s = _builder.GetStock(name) ?? _builder.GetStockByIsin(name);
				if (s == null)
					throw new Exception($"ReadOperations(): stock or isin '{name}' is not found.");

				var opDate = node["db_time"]?.InnerText;
				//var opType = ExcelUtil.GetCellValue(cells.Type, rd);
				var opQty = int.Parse(node["qty"].InnerText);
				var opPrice = decimal.Parse(node["Price"].InnerText, CultureInfo.InvariantCulture);
				var opSumma = decimal.Parse(node["summ_trade"].InnerText, CultureInfo.InvariantCulture);
				var opBankCommission1 = decimal.Parse(node["bank_tax"].InnerText, CultureInfo.InvariantCulture);
				var deliveryDate = node["settlement_time"]?.InnerText;
				var opCur = node["curr_calc"]?.InnerText ?? "RUR";
				//var nkd = ExcelUtil.GetCellValue(cells.Nkd, rd);

				var cur = Currency.Rur;
				if (opCur.Equals(Currency.Usd.ToString(), StringComparison.OrdinalIgnoreCase))
					cur = Currency.Usd;
				else if (opCur.Equals(Currency.Eur.ToString(), StringComparison.OrdinalIgnoreCase))
					cur = Currency.Eur;

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
	}
}