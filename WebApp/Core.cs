﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using InvApp.Entities;
using log4net;

namespace Invest.WebApp
{
    public partial class Core
    {
        protected readonly ILog Log;

        // xsl files directory
        private readonly string[] _excelFilePath = { @"C:\Users\Alex\Downloads",  @"C:\Users\....\Downloads" };
        
        public static Invest.WebApp.Core Instance;
        private Dictionary<string, int> _excelCells;
        /// <summary>Time of first operftion in portfolio</summary>
        private static DateTime _startOperationDate;
        //public decimal Comission { get { return 0.06M; } }

		public List<string> BlueUsList;
		public List<string> BlueRuList;
		public List<string> BlueEurList;

        /// <summary>Kilo</summary>
        public static string KFormat(int qty) {
            //if (qty >= 100000)
            //    return (qty / 100000) + "M";
            //if (qty >= 10000) {
            //    return (qty / 1000D).ToString("0.#") + "K";
            //}
            if (qty >= 1000)
                return (qty / 1000D).ToString("N0") + "K";
            
            return qty.ToString("N0");
        }

		public static string GetSum(decimal? value, string emptyValue = "")
		{
			if (value != null && value != 0 && value != .0m)
				return $"{value:N2}";

			return emptyValue;
		}

        public static string GetSumWithZnak(decimal? value, string emptyValue = "")
        {
            if (value != null && value != 0) 
	            return value > 0 
		            ? $"+{value:N2}" 
		            : $"{value:N2}";

            return emptyValue;
        }

        public static string GetPercent(decimal? value, string emptyValue = "", bool isZnak = true)
        {
            if (value != null && value != 0) 
	            return value > 0 
		            ? $"+{value:N2}%" 
		            : $"{value:N2}%";

            return emptyValue;
        }

		public static decimal? SumValues(params decimal?[] values)
		{
			var r = values.Where(x => x != null).ToList();
			return r.Any() 
				? r.Sum() 
				: null;
		}

        private Core() {
            Log = LogManager.GetLogger("InvApp.Core");
            _startOperationDate = new DateTime(2019, 12,1);

			BlueUsList = LoadPortfolioData(Portfolio.BlueUs);
			BlueRuList = LoadPortfolioData(Portfolio.BlueRu);
			BlueEurList = LoadPortfolioData(Portfolio.BlueEur);
        }

        private void FillExcelCellsDictionary()
        {
            _excelCells = new Dictionary<string, int>();

            const string letters = "abcdefghijklmnopqrstuvwxyz";
            const int size = 15;
            var offsetCh = "";
            var index = 0;

            for (var offset = 0; offset <= size; offset++)
            {
                for (var i = 0; i < letters.Length; i++)
                {
                    var ch = offsetCh + letters[i];
                    _excelCells.Add(ch.ToUpper(), index++);
                }

                offsetCh = letters[offset].ToString();
            }
        }

        public Dictionary<Analytics, FifoResult> FifoResults;
		public Dictionary<Analytics, FinIndicator> FinIndicators;
        public List<Period> Periods;		
        public List<Account> Accounts;
        public List<Company> Companies;
        public List<Stock> Stocks;
        public List<Operation> Operations;
        public Dictionary<DateTime, decimal> UsdRate;
        public Dictionary<DateTime, decimal> EurRate;
		public List<History> History;

        public static Invest.WebApp.Core Init() {

            if (Instance != null)
                return Instance;

            Instance = new Invest.WebApp.Core
            {				
                Accounts = new List<Account>(),
                Companies = new List<Company>(),
                Stocks = new List<Stock>(),
                Operations = new List<Operation>(),
                UsdRate = new Dictionary<DateTime, decimal>(),
                EurRate = new Dictionary<DateTime, decimal>(),
                FifoResults = new Dictionary<Analytics, FifoResult>(),
				FinIndicators = new Dictionary<Analytics, FinIndicator>(),
				History = new List<History>()
            };

			Instance.FillPeriods();

            // For excel cells
            Instance.FillExcelCellsDictionary();

            // Load rates (usd, eur)
            Instance.LoadCurrencyRates(Currency.Usd);
            Instance.LoadCurrencyRates(Currency.Eur);

            Instance.LoadBaseData(@"C:\\Users\\Alex\\Downloads\\data.json");

			//load broker reports by year (since 2019)
			for(var year = _startOperationDate.Year; year <= DateTime.Today.Year; year++)
			{
				Instance.LoadBrokerReports(AccountType.Iis, $"7101NME0_{year}*.xls", year);
				Instance.LoadBrokerReports(AccountType.VBr, $"7101NMC0_{year}*.xls", year);

				Instance.LoadBrokerReports(AccountType.SBr, $"Сделки_{year}*.xlsx", year);
			}

            // Prices
            var priceMgr = new PriceManager();
            //priceMgr.Load();

			// History
			var hist = new HistoryData();
			hist.Load();

            Instance.Calc();            

            return Instance;
        }

        /// <summary>
        /// Load currency rates from cbr
        /// </summary>
        private void LoadCurrencyRates(Currency cur)
        {
	        var curCode = "1235";

            // https://cbr.ru/development/SXML/
            // http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1=02/03/2019&date_req2=14/03/2021&VAL_NM_RQ=R01235 usd
            // http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1=02/03/2019&date_req2=14/03/2021&VAL_NM_RQ=R01239 eur

			if (cur == Currency.Usd)
				curCode = "1235";
			else if (cur == Currency.Eur)
				curCode = "1239";
			
            var url = string.Format("http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1={0:dd/MM/yyyy}&date_req2={1:dd/MM/yyyy}&VAL_NM_RQ=R0{2}",
                _startOperationDate, // "01/12/2019", 
                DateTime.Today,
                curCode
            );

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UseDefaultCredentials = true;
            req.UserAgent = "Chrome"; // error 403
            //req.Proxy.Credentials = CredentialCache.DefaultCredentials;
            var resp = (HttpWebResponse)req.GetResponse();

            using (var stream = new StreamReader(resp.GetResponseStream()))
            {
                var response = stream.ReadToEnd();
                var doc = new XmlDocument();
                doc.LoadXml(response);

                var nodes = doc.SelectNodes("//Record");
                if (nodes == null) 
                    return;

                foreach (XmlNode d in nodes)
                {
                    if (d.Attributes?["Date"] == null) 
                        continue;

                    var date = DateTime.Parse(d.Attributes?["Date"].Value);
                    var valueNode = d.ChildNodes[1];
                    if (valueNode != null)
                    {
                        var f = new CultureInfo("en-US", false).NumberFormat;
                        f.NumberDecimalSeparator = ",";
                        if (decimal.TryParse(valueNode.InnerText, NumberStyles.AllowDecimalPoint, f, out var v))
                        {
							if (cur == Currency.Usd)
								UsdRate.Add(date, v);
							else if (cur == Currency.Eur)
								EurRate.Add(date, v);
						}
                    }
                }
            }
        }

        private int ExcelCell(string cellName)
        {
            if (!_excelCells.ContainsKey(cellName))
                throw new Exception($"Not found cell by name {cellName}");

            return _excelCells[cellName];
        }

        private string GetCellValue(string cellName, ExcelDataReader.IExcelDataReader reader)
        {
            return reader.GetValue(ExcelCell(cellName))?.ToString().Trim();
        }


        public Stock GetStock(string ticker) {

            for (var i = 0; i < Stocks.Count; i++)
            {
                if (Stocks[i].Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
                    return Stocks[i];
            }

            return null;
        }

        private Stock GetStockByName(string name) {

            for (var i = 0; i < Stocks.Count; i++)
            {
                if (Stocks[i].Name != null && Stocks[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return Stocks[i];
            }

            return null;
        }

		private Stock GetStockByIsin(string isin) {

            for (var i = 0; i < Stocks.Count; i++)
            {
                if (Stocks[i].Isin != null && Stocks[i].Isin.Contains(isin))
                    return Stocks[i];
            }

            return null;
        }

        private Company GetCompanyById(string id) {

            for (var i = 0; i < Companies.Count; i++)
            {
                if (Companies[i].Id != null && Companies[i].Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return Companies[i];
            }

            return null;
        }

        private Operation GetOperation(string transId) {

            for (var i = 0; i < Operations.Count; i++)
            {
                if (Operations[i].TransId != null && Operations[i].TransId.Equals(transId, StringComparison.OrdinalIgnoreCase))
                    return Operations[i];
            }

            return null;
        }


        private static FileInfo[] GetFiles(string[] paths, string patern)
        {
            // xlsx file
            var dir = new DirectoryInfo(paths[0]);
            if (!dir.Exists)
                dir = new DirectoryInfo(paths[1]);

            var files = dir.GetFiles(patern);
            return files.Length == 0 
	            ? null 
	            : files;
        }

        /// <summary>Load all data from json file</summary>
        private void LoadBaseData(string fileName)
        {
			if (!File.Exists(fileName))
				throw new Exception($"LoadBaseData(): file {fileName} no found.");

            using(var fs = File.OpenText(fileName))
            {
                var content = fs.ReadToEnd();
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                
                foreach(var acc in ((Newtonsoft.Json.Linq.JObject)data)["accounts"])
                {
                    Accounts.Add( 
                        new Account{ 
                            Name = acc["name"].ToString(), 
                            Id = acc["id"].ToString(), 
                            BrokerName = acc["brokerName"].ToString(), 
                            Type = (AccountType)Enum.Parse(typeof(AccountType), acc["type"].ToString(), true) 
                            //Currency = (Currency)Enum.Parse(typeof(Currency), acc["cur"].ToString(), true) 
                        });
                }

                //Instance.Companies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Company>>(((Newtonsoft.Json.Linq.JObject)data)["companies"].ToString());
                Instance.Companies = new List<Company>();

				// data
                foreach(var val in ((Newtonsoft.Json.Linq.JObject)data)["data"])
                {
	                var companyId = val["id"].ToString();
	                var company = new Company { Id = companyId, Name = val["name"].ToString(), DivName = val["divName"].ToString() };
					Instance.Companies.Add(company);

					foreach(var s in val["stocks"])
					{
						var stock = new Stock
						{
							Name = s["brokerName"].ToString(),
							Company = company,
							Currency = s["cur"] != null && s["cur"].ToString() != ""
								? (Currency)Enum.Parse(typeof(Currency), s["cur"].ToString(), true)
								: Currency.Rur,
							LotSize = s["lot"] != null && s["lot"].ToString() != "" ? int.Parse(s["lot"].ToString()) : 1,
							Ticker = s["t"].ToString(),
							Type = s["type"] != null && !string.IsNullOrEmpty(s["type"].ToString())
								? (StockType) Enum.ToObject(typeof(StockType), (int)s["type"])
								: StockType.Share
						};

						if (!string.IsNullOrEmpty(s["isin"].ToString()))	
						{
							var isins = s["isin"].ToString().Replace(" ", "");
							stock.Isin = isins.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
						}

						if (s["regNum"] != null && !string.IsNullOrEmpty(s["regNum"].ToString()))	
							stock.RegNum = s["regNum"].ToString();

						if (Stocks.FirstOrDefault(x => x.Ticker == stock.Ticker) != null)
							throw new Exception("Attempt to add a stock with a duplicate ticker.");

						Stocks.Add(stock);
					}
				}
            }

			//
			//AddTestData();
        }

        
        private void LoadBrokerReports(AccountType accountType, string fileMask, int year)
        {
	        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

	        var xslFiles = GetFiles(_excelFilePath, fileMask);
            if (xslFiles == null)
                return;

			if (xslFiles.Length > 1)
				throw new Exception($"There is more one xsl file found in directory by {accountType}, {year}");

            foreach(var file in xslFiles.OrderBy(x => x.LastWriteTime))
            {
                using(var fs = File.Open(file.FullName, FileMode.Open, FileAccess.Read))
                {
	                if (accountType == AccountType.SBr)
		                ParseSberFile(accountType, year, fs);
					else
						ParseVtbFile(accountType, year, fs);
                }
            }
        }

        private void ParseVtbFile(AccountType accountType, int year, FileStream fs)
        {
	        var cellMappingCache = GetExcelCellsMappingForCache(accountType, year);
	        var cellMapping = GetExcelCellsMappingForOperation(accountType, year);
	        var cellMappingUsdRubOperation = GetExcelCellsMappingForUsdRubOperation(accountType, year);

	        using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(fs))
	        {
		        var emptyCellCount = 200;
		        const int startIndex = 8;
		        var rowIndex = 0;

		        while (reader.Read())
		        {
			        if (rowIndex >= startIndex)
			        {
				        var titleCell = reader.GetValue(ExcelCell("B"));
				        if (titleCell != null)
				        {
					        var title = titleCell.ToString().Trim();

					        if (title.Equals("Движение денежных средств", StringComparison.OrdinalIgnoreCase))
						        ReadCacheIn(reader, accountType, cellMappingCache);
                                    
					        if (title.Equals("Заключенные в отчетном периоде сделки с ценными бумагами", StringComparison.OrdinalIgnoreCase))
						        ReadOperations(reader, accountType, cellMapping);                                    

					        if (title.Equals("Завершенные в отчетном периоде сделки с иностранной валютой (обязательства прекращены)", StringComparison.OrdinalIgnoreCase)
					            || title.Equals("Заключенные в отчетном периоде сделки с иностранной валютой", StringComparison.OrdinalIgnoreCase))
						        ReadUsdRubOperations(reader, accountType, cellMappingUsdRubOperation);
				        }

				        if (titleCell == null)
					        emptyCellCount--;

				        if (emptyCellCount == 0)
					        break;
			        }
			        rowIndex++;
		        }
	        }

			AddExtOperations(accountType, year);
        }

        private static void AddExtOperations(AccountType accountType, int year)
        {
			var index = 1000000;

			if (accountType == AccountType.VBr && year == 2021)
			{
				var op = new Operation {
					Index = ++index,
					AccountType = accountType,
					Date = new DateTime(2021, 8, 2, 2,0,1),
					Stock = Instance.GetStock("SWI"),
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
				Instance.Operations.Add(op);

				op = new Operation {
					Index = ++index,
					AccountType = accountType,
					Date = new DateTime(2021, 8, 4, 21,0,0),
					Stock = Instance.GetStock("SWI"),
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
				Instance.Operations.Add(op);


				//TSVT
				op = new Operation {
					Index = ++index,
					AccountType = accountType,
					Date = new DateTime(2021, 11, 10, 21,0,1),
					Stock = Instance.GetStock("TSVT"),
					Qty = 1,
					Price = 0,
					Type = OperationType.Buy,
					Currency = Currency.Usd,
					DeliveryDate = new DateTime(2021, 11, 10, 21,0,1),
					TransId = "M037531193",
					Summa = 0,
					PriceInRur = 0,
					RurSumma = 0,
					BankCommission1 = 0,
					BankCommission2 = 0,
					Comment = "Конвертация"
				};
				Instance.Operations.Add(op);

				op = new Operation {
					Index = ++index,
					AccountType = accountType,
					Date = new DateTime(2021, 11, 10, 21,0,1),
					Stock = Instance.GetStock("TSVT"),
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
				Instance.Operations.Add(op);
			}





	        if (accountType == AccountType.VBr && year == 2022)
	        {
		        var op = new Operation
                {
					Index = ++index,
                    AccountType = accountType,
                    Date = new DateTime(2022, 1, 25, 21,0,2),
                    Stock = Instance.GetStock("ZYXI"),
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
                Instance.Operations.Add(op);
	        }
        }

        private void ParseSberFile(AccountType accountType, int year, FileStream fs)
        {
	        var cellMappingCache = GetExcelCellsMappingForCache(accountType, year);
	        var cellMapping = GetExcelCellsMappingForOperation(accountType, year);
	        var cellMappingUsdRubOperation = GetExcelCellsMappingForUsdRubOperation(accountType, year);

	        using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(fs))
	        {
		        var emptyCellCount = 200;
		        const int startIndex = 8;
		        var rowIndex = 0;

		        while (reader.Read())
		        {
			        if (rowIndex >= startIndex)
			        {
				        var titleCell = reader.GetValue(ExcelCell("B"));
				        if (titleCell != null)
				        {
					        var title = titleCell.ToString().Trim();

					        //if (title.Equals("Движение денежных средств", StringComparison.OrdinalIgnoreCase))
						       // ReadCacheIn(reader, accountType, cellMappingCache);
                                    
					        //if (title.Equals("Заключенные в отчетном периоде сделки с ценными бумагами", StringComparison.OrdinalIgnoreCase))
						    //    ReadOperations(reader, accountType, cellMapping);

					        //if (title.Equals("Завершенные в отчетном периоде сделки с иностранной валютой (обязательства прекращены)", StringComparison.OrdinalIgnoreCase)
					        //    || title.Equals("Заключенные в отчетном периоде сделки с иностранной валютой", StringComparison.OrdinalIgnoreCase))
						       // ReadUsdRubOperations(reader, accountType, cellMappingUsdRubOperation);
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

        /// <summary>Load operations</summary>
        /// <param name="rd"></param>
        /// <param name="accountType"></param>
        /// <param name="cellMapping"></param>
        private void ReadOperations(ExcelDataReader.IExcelDataReader rd, AccountType accountType, ExcelCellsMappingOperation cellMapping)
        {
			var index = 0;

            // "Заключенные в отчетном периоде сделки с ценными бумагами"
            while(rd.Read())
            {
                var cellName = GetCellValue("B", rd);
                if (cellName == null || cellName.Trim() == "")
                    break;

				//if (cellName.Equals("Завершенные в отчетном периоде сделки с ценными бумагами (обязательства прекращены)", StringComparison.OrdinalIgnoreCase))
				//{	var r = 0; }
				//if (cellName == "TSM US, US8740391003, US8740391003") { var rr=0;  }

                string name;  // Сбербанк ао, 10301481B, RU0009029540
				string isin;  // RU0009029540

                var cellArr = cellName.Split(new[]{","}, StringSplitOptions.RemoveEmptyEntries);
                if (cellArr.Length == 3) {
                    name = cellArr[0].Trim();
					isin = cellArr[2].Trim();
				}
				else
					throw new Exception("CellName d`not contain three count literals");

                if (string.IsNullOrEmpty(name))
                    continue;

				if (name.Equals("Наименование ценной бумаги", StringComparison.OrdinalIgnoreCase))
					continue;

                var s = GetStockByName(name) ?? GetStockByIsin(isin);
                if (s == null) {
					throw new Exception($"ReadOperations(): stock '{name}', '{isin}' not found. Cell = '{cellName}'");
				}

				// add company from ticker
				//AddCompany(s);

                var opDate = GetCellValue(cellMapping.Date, rd);
                var opType = GetCellValue(cellMapping.Type, rd);
                var opQty = GetCellValue(cellMapping.Qty, rd);      //rd.GetValue(10);
                var opPrice = GetCellValue(cellMapping.Price, rd);  //rd.GetValue(17);
                var opBankCommission1 = GetCellValue(cellMapping.BankCommission1, rd);
                var opBankCommission2 = GetCellValue(cellMapping.BankCommission2, rd);
				var deliveryDate = GetCellValue(cellMapping.DeliveryDate, rd);
				var nkd = GetCellValue(cellMapping.Nkd, rd);

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
                    AccountType = accountType,
                    Date = DateTime.Parse(opDate),
                    Stock = s,
                    Qty = int.Parse(opQty),
                    Price = decimal.Parse(opPrice),
                    Type = opType == "Покупка" ? OperationType.Buy : OperationType.Sell,
                    QtySaldo = int.Parse(opQty),
					Currency = s.Currency,
					DeliveryDate = DateTime.Parse(deliveryDate),

                    OrderId = GetCellValue(cellMapping.OrderId, rd),
                    TransId = GetCellValue(cellMapping.TransId, rd)					
                };

				//if (op.TransId == null && accountType == AccountType.VBr && op.Date.Year == 2021)
				//	op.TransId = GetCellValue("BG", rd);

                if (op.TransId == null)
                    throw new Exception($"ReadOperations(): op.TransId == null. {accountType}, {op.Date.Year}, {op}");
                if (op.TransId != null && op.TransId.Length < 5)
	                throw new Exception($"ReadOperations(): op.TransId is not correct string, value: {op.TransId}");

				//if (op.Stock.Ticker == "PLZL") { var r = 0; }

				op.Summa = s.Type == StockType.Bond 
					? op.Price * 10 * op.Qty 
					: op.Price * op.Qty;

				if ((op.Currency == Currency.Usd || op.Currency == Currency.Eur) && op.Date >= _startOperationDate)
				{
					op.PriceInRur = op.Price * GetCurRate(op.Currency, op.Date);
                    op.RurSumma = op.PriceInRur * op.Qty;
				}

                if (s.Type == StockType.Bond && !string.IsNullOrEmpty(nkd))
	                op.Nkd = decimal.Parse(nkd);

                if (opBankCommission1 != null)
                    op.BankCommission1 = decimal.Parse(opBankCommission1);
                if (opBankCommission2 != null)
                    op.BankCommission2 = decimal.Parse(opBankCommission2);

                if (!string.IsNullOrEmpty(op.TransId) && GetOperation(op.TransId) == null)
                    Instance.Operations.Add(op);                
            }
        }

		private void ReadCacheIn(ExcelDataReader.IExcelDataReader rd, AccountType accountType, ExcelCellsMappingCache cellMapping)
        {
            var ops = new List<Operation>();
            while(rd.Read())
            { 
                var opDate = GetCellValue(cellMapping.Date, rd);
                if (opDate == null)
                    break;
                
                if (!DateTime.TryParse(opDate, out var date))
                    continue;

                var opSumma = GetCellValue(cellMapping.Summa, rd); //rd.GetValue(3);
                var opCur= GetCellValue(cellMapping.Cur, rd);
                var opType = GetCellValue(cellMapping.Type, rd); // 15
                var opComment = GetCellValue(cellMapping.Comment, rd); //rd.GetValue(33);

                if (!string.IsNullOrEmpty(opType))
                {
                    OperationType? type = null;

                    if (opType == "Зачисление денежных средств")
                        type = OperationType.BrokerCacheIn;
                    if (opType == "Вознаграждение Брокера")
                        type = OperationType.BrokerFee;
                    if (opType == "Дивиденды" 
                        || (opType.Equals("Зачисление денежных средств", StringComparison.OrdinalIgnoreCase) 
                                && !string.IsNullOrEmpty(opComment) && opComment.ToLower().Contains("дивиденды"))
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
						throw new Exception($"ReadCacheIn(): opCur == null. a: {accountType}, t: {type}, {date}");

					if (string.IsNullOrEmpty(opComment) && (type != OperationType.UsdExchange && type != OperationType.BrokerCacheIn))
						throw new Exception($"ReadCacheIn(): opComment == null. a: {accountType}, t: {type}, {date}");

                    var op = new Operation {
                        AccountType = accountType,
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
                    if (Instance.Operations.Count(x => x.Date == d 
                        && x.AccountType == accountType
                        && (x.Type == OperationType.BrokerCacheIn || x.Type == OperationType.BrokerCacheOut || x.Type == OperationType.BrokerFee || x.Type == OperationType.Dividend)) == 0)
                    {
                        foreach(var o in ops.Where(x => x.Date == d))
                        {
                            Instance.Operations.Add(o);
                        }
                    }

                    d = d.AddDays(1);
                }
            }
        }
		
		private void ReadUsdRubOperations(ExcelDataReader.IExcelDataReader rd, AccountType accountType, ExcelCellsMappingCurrencyOperation cellMapping)
        {
            while(rd.Read())
            { 
                var opDate = GetCellValue(cellMapping.OrderDate, rd); //date
                if (opDate == null)
                    break;
                
                if (!DateTime.TryParse(opDate, out var date))
                    continue;

                var opSumma = GetCellValue(cellMapping.Summa, rd); //rd.GetValue(3);
                var opType = GetCellValue(cellMapping.Type, rd); // 15
                var opFinIns = GetCellValue(cellMapping.FinInstrument, rd); // "USDRUB_CNGD, EURRUB_CNGD, USDRUB_TOM, EURRUB_TOM"
                //var opCur = GetCellValue(cellMapping.Cur, rd);
                //var opComment = GetCellValue(cellMapping.Comment, rd); //rd.GetValue(33);

                if (!string.IsNullOrEmpty(opType))
                {
                    OperationType? type = null;

					if (opFinIns.StartsWith("USDRUB", StringComparison.OrdinalIgnoreCase))
					{
	                    if (opType == "Покупка")
	                        type = OperationType.UsdRubBuy;
						if (opType == "Продажа")
	                        type = OperationType.UsdRubSell;
					}

					if (opFinIns.StartsWith("EURRUB", StringComparison.OrdinalIgnoreCase)) {
	                    if (opType == "Покупка")
	                        type = OperationType.EurRubBuy;
	                    if (opType == "Продажа")
							type = OperationType.EurRubSell;
					}

                    if (type == null)
                        continue;

                    var bankCommission1Str = GetCellValue(cellMapping.BankCommission1, rd);
                    var bankCommission2Str = GetCellValue(cellMapping.BankCommission2, rd);

                    var op = new Operation {
                        AccountType = accountType,
                        Date = date,
                        Summa = decimal.Parse(opSumma),
						Qty = int.Parse(GetCellValue(cellMapping.Qty, rd)),
                        Type = type.Value,
						Price = decimal.Parse(GetCellValue(cellMapping.Price, rd)),
						OrderId = GetCellValue(cellMapping.OrderId, rd),
						TransId = GetCellValue(cellMapping.TransId, rd),
						BankCommission1 = !string.IsNullOrEmpty(bankCommission1Str) ? decimal.Parse(GetCellValue(cellMapping.BankCommission1, rd)) : (decimal?) null,
						BankCommission2 = !string.IsNullOrEmpty(bankCommission2Str) ? decimal.Parse(GetCellValue(cellMapping.BankCommission2, rd)) : (decimal?) null,
                        Comment = "Дата: " + GetCellValue(cellMapping.OrderDate, rd) + " (" + GetCellValue(cellMapping.FinInstrument, rd) + ")"
                    };

					if (!string.IsNullOrEmpty(op.TransId) && GetOperation(op.TransId) == null)
						Instance.Operations.Add(op);
                }                
            }            
        }
        



        private void Calc() 
        { 
            foreach(var s in Instance.Stocks)
            {
                s.Data = new Data {
					Stock = s
                };

                s.AccountData = new Dictionary<AccountType, AccountData>();

                foreach(var a in Instance.Accounts) 
                {
                    var accData = new AccountData(a.Type, s);
                    s.AccountData.Add(a.Type, accData);

                    accData.Operations = Instance.Operations
                        .Where(x => x.Stock != null && x.Stock.Ticker.Equals(s.Ticker, StringComparison.OrdinalIgnoreCase) 
                            && (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
                            && x.AccountType == a.Type)
                        .OrderBy(x => x.Date).ThenBy(x => x.TransId)
                        .ToList();

					if (s.Type == StockType.Bond) {
						accData.BuySum = accData.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Summa);
						accData.SellSum = accData.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).Sum(x => x.Summa);
					}
					else {
						accData.BuySum = accData.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Qty * x.Price);
						accData.SellSum = accData.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).Sum(x => x.Qty * x.Price);
					}

					accData.BuyQty = accData.Operations.Where(x => x.Qty != null && x.Type == OperationType.Buy).Sum(x => x.Qty).Value;
                    accData.SellQty = accData.Operations.Where(x => x.Qty != null && x.Type == OperationType.Sell).Sum(x => x.Qty).Value;

                    accData.Positions = CreatePositions(accData.Operations);

                    foreach (var pos in accData.Positions)
                    {
	                    pos.CalcPosPrice();
						pos.CalcFinResult();
						pos.CalcFifoResult(accData);
					}

                    //foreach (var o in ops)
                    {
						//qty += o.Type == OperationType.Buy ? o.Qty.Value : -o.Qty.Value;
						//accData.CurrentQty = qty;

						////if (s.Ticker == "PLZL" && o.AccountType == AccountType.VBr){ var r = 0; }

						////pos price
						//CalcPosPrice(o, prevOper, ops, s, qty, accData);

						////Calc FinResult and close position
						//CalcFinResult(ref ops, o, s, qty, accData);
						//CalcFinResultForShort(ref ops, o, s, qty, accData);

						//accData.AddCommission(o);

						////Fifo
						//CalcFifoResult(ref ops, o, s, accData, a.Type);
						//CalcFifoResultForShort(ref ops, o, s, accData, a.Type);

						////Save commission
						//AddCommissionIndicator(o);
					}
				}

                var operations = Operations.Where(x => x.Stock != null && x.Stock.Ticker.Equals(s.Ticker, StringComparison.OrdinalIgnoreCase))
	                .ToList();

                var buys = operations.Where(x => x.Type == OperationType.Buy).ToList();
                var sells = operations.Where(x => x.Type == OperationType.Sell).ToList();

                s.Data.BuyQty = buys.Where(x => x.Qty != null).Sum(x => x.Qty).Value;
                s.Data.SellQty = sells.Where(x => x.Qty != null).Sum(x => x.Qty).Value;

                decimal buySum = 0;
                decimal sellSum = 0;
                decimal buyComission = 0;
                decimal sellComission = 0;

                s.Data.OpenPosExist = operations.Any(x => !x.IsClosed && x.Type == OperationType.Buy);

                if (buys.Any())
                {
                    s.Data.FirstBuy = buys.Min(x => x.Date);
                    s.Data.LastBuy = buys.Max(x => x.Date);

                    s.Data.BuyMin = buys.Min(x => x.Price);
                    s.Data.BuyMax = buys.Max(x => x.Price);
                }

                var oppSell = operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).ToList();
                if (oppSell.Any()) {
                    s.Data.FirstSell = oppSell.Min(x => x.Date);
                    s.Data.LastSell = oppSell.Max(x => x.Date);

                    s.Data.SellMin = oppSell.Min(x => x.Price);
                    s.Data.SellMax = oppSell.Max(x => x.Price);
                }

                // Средневзвешенная цена покупки
                if (buys.Any())
                {
                    decimal item = 0;
                    var q = 0;
                    foreach(var o in operations.Where(x => x.Stock == s && x.Type == OperationType.Buy && !x.IsClosed))
                    {
                        q += o.Qty.Value;
                        item += (o.Qty.Value * o.Price.Value);
                        buySum += (o.Qty.Value * o.Price.Value);
                        //buyComission += (o.Qty.Value * o.Price.Value) * Instance.Comission / 100;
                        buyComission += o.Commission.Value;
                    }
                    if (q != 0)
                        s.Data.BuyAvg = item / q;
                }

                s.Data.BuySum = operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Qty * x.Price);

                if (oppSell.Any())
                {
                    decimal item = 0;
                    int q = 0;
                    foreach(var o in operations.Where(x => x.Stock == s && x.Type == OperationType.Sell && !x.IsClosed))
                    {
                        q += o.Qty.Value;
                        item += (o.Qty.Value * o.Price.Value);
                        sellSum += (o.Qty.Value * o.Price.Value);
                        sellComission += o.Commission.Value;
                        //sellComission += (o.Qty.Value * o.Price.Value) * Instance.Comission / 100;
                    }
                    if (q != 0)
                        s.Data.SellAvg = item / q;
                }

                s.Data.SellSum = operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).Sum(x => x.Qty * x.Price);


                // calc Estimate result
                if (s.Data.OpenPosExist) //&& sellSum - buySum > 0)
                {
                    var res = s.Data.BuySum * -1;
                    if (sellSum > 0)                    
                        res += s.Data.SellSum; // all sales

                    // add all stocks at currrent price
                    if (s.Data.SellAvg != null)
                        s.Data.StockSum = ((s.CurPrice ?? 0) * s.Data.QtyBalance);

                    if (s.Data.QtyBalance != 0)
                    {
                        //PosPrice take from each account
                        decimal? s1 = 0, s2 = 0;
                        foreach(var a in Accounts)
                        {
							//if (s.Ticker == "CLF") { var r122 =1; }
                            var accData = s.AccountData[a.Type];
                            if (accData.QtyBalance != 0 && accData.PosPrice != null)
							{
								accData.StockSum = accData.PosPrice.Value * accData.QtyBalance;
                                s1 += accData.StockSum;

                                if (accData.QtyBalance != 0)
								{
									//if (s.Ticker == "CLF") { var r12 =1; }
									accData.CurrentSum = ((s.CurPrice ?? 0) * accData.QtyBalance);
                                    s2 += accData.CurrentSum;

									var buySumInAcc = 0m;
									var buyAccComission = 0m;
									foreach(var o in operations.Where(
										x => x.Stock == s && x.Type == OperationType.Buy && x.AccountType == a.Type && x.QtySaldo > 0))
									{
										buySumInAcc += o.QtySaldo * accData.PosPrice.Value;
										buyAccComission += (o.QtySaldo * o.Commission.Value);
									}
								
									accData.Profit = accData.CurrentSum - buySumInAcc;
									accData.ProfitWithComm = accData.Profit - buyAccComission;
									if (buySumInAcc != 0)
										accData.ProfitPercent = accData.Profit / buySumInAcc * 100;		
								}
							}							
                        }

                        s.Data.ProfitWithComm = s.Data.Profit; // - (buyComission + sellComission);      
                        if (s1 != 0)
                            s.Data.ProfitPercent = s.Data.Profit / s1 * 100;

						s.Data.StockSum = s1;
                        s.Data.CurrentSum = s2;

                        if (s.Data.SellQty == 0)
                        {
                            s.Data.ProfitWithComm = s.Data.Profit - (buyComission + sellComission);
                            s.Data.ProfitPercent = s.Data.Profit / buySum * 100;
                        }
                    }
                }
            }

            // FillDivs
            LinkDivsToTickers();
			LinkCouponsToTickers();
        }

        private void ClosePosition(PositionData pos)
        {
	        pos.IsClosed = true;
        }

        private void AddCommissionIndicator(Operation op)
		{
			if (op.Commission != 0)
			{
				var a = new Analytics(op.Stock.Ticker, op.AccountType, op.Currency, op.DeliveryDate.Value);

				if (FinIndicators.ContainsKey(a))						
					FinIndicators[a].Commission = (FinIndicators[a].Commission ?? 0) + op.Commission;						
				else
					FinIndicators.Add(a, new FinIndicator{ Commission = op.Commission });
			}
		}

		private void LinkDivsToTickers()
        {
            var ops = Operations.Where(x => x.Type == OperationType.Dividend);
            foreach(var op in ops)
            {
                //if (op.Summa == .58m) { var r =0; }
                var s = Stocks.FirstOrDefault(x => op.Comment != null 
                    && (
                       x.Company?.Name != null && op.Comment.ToLower().Contains(x.Company.Name.ToLower())
                       || (x.Company != null && !string.IsNullOrEmpty(x.Company.DivName) && op.Comment.ToLower().Contains(x.Company.DivName.ToLower())
                    )
                ));
                
                if (s == null)
                    throw new Exception($"LinkDivsToTickers(): not found stock by {op.Comment}, {op.Date}");

                op.Stock = s;
                var a = new Analytics(op.Stock.Ticker, op.AccountType, op.Currency, op.Date);

				//Divs indicators
				if (FinIndicators.ContainsKey(a))						
					FinIndicators[a].DivSumma = FinIndicators[a].DivSumma ?? 0 + op.Summa;						
				else
					FinIndicators.Add(a, new FinIndicator{ DivSumma = op.Summa });
            }
        }


		private void LinkCouponsToTickers()
		{
			var ops = Operations.Where(x => x.Type == OperationType.Coupon && x.Comment != null);
			foreach(var op in ops)
			{
				//if (op.Summa == .58m) { var r =0; }
				var s = Stocks.FirstOrDefault(x => x.Type == StockType.Bond && !string.IsNullOrEmpty(x.RegNum)
				    && x.Company != null 
					&& op.Comment.ToLower().Contains(x.RegNum.ToLower())
				);
                
				if (s == null)
					throw new Exception($"LinkCouponsToTickers(): not found stock by {op.Comment}, {op.Date}");

				op.Stock = s;
				var a = new Analytics(op.Stock.Ticker, op.AccountType, op.Currency, op.Date);

				//Divs indicators
				if (FinIndicators.ContainsKey(a))						
					FinIndicators[a].CouponSumma = FinIndicators[a].CouponSumma ?? 0 + op.Summa;						
				else
					FinIndicators.Add(a, new FinIndicator{ CouponSumma = op.Summa });
			}
		}


		private void CalcPositionFifoResult(PositionData pos, AccountData accData)
        {
	        var stock = accData.Stock;
	        var notClosedOps = new Queue<PositionItem>();
			PositionItem lastItem = null;

	        foreach(var item in pos.Items)
	        {
		        var lots = item.Qty / stock.LotSize;

		        if ((pos.Type == PositionType.Long && item.Operation.Type == OperationType.Buy)
		            || ( pos.Type == PositionType.Short && item.Operation.Type == OperationType.Sell))
		        {
			        for (var i = 1; i <= lots; i++)
				        notClosedOps.Enqueue(item);
		        }

		        //if (stock.Ticker == "PLZL" && pos.StartDate.Date == new DateTime(2022,2,21) && pos.Type == PositionType.Short) { var q = 0;}

		        if ((pos.Type == PositionType.Long && item.Operation.Type == OperationType.Sell)
			        || (pos.Type == PositionType.Short && item.Operation.Type == OperationType.Buy))
		        {
					lots = item.Qty / stock.LotSize;
					while (lots > 0)
					{
						var opBuy = notClosedOps.Dequeue();
						if (opBuy == null)
							throw new Exception("CalcPositionFifoResult(): opBuy in Queue == null");

						var op = item.Operation;
						var profit = stock.LotSize * (item.Operation.Price - opBuy.Operation.Price).Value;
						if (pos.Type == PositionType.Short)
							profit = stock.LotSize * (opBuy.Operation.Price - item.Operation.Price).Value;

						var commission = (item.Commission / (item.Qty / stock.LotSize)) + (opBuy.Commission / (opBuy.Qty));

						if (stock.Ticker == "PLZL" && pos.StartDate.Date == new DateTime(2022,2,21) && pos.Type == PositionType.Short) { var q = 0;}

						// Fifo result						
						var result = new FifoResult
						{
							Summa = profit,
							Cur = op.Currency,
							// buys comm. + sell comm.
							Commission = Math.Round(commission, 2)
						};

						// доход (и налог) считается с Summa - Commission!! (без учета коммисии)
						if (op.Currency == Currency.Usd || op.Currency == Currency.Eur)
						{
							var buyRate = Instance.GetCurRate(op.Currency, opBuy.Operation.DeliveryDate.Value);
							var sellRate = Instance.GetCurRate(op.Currency, op.DeliveryDate.Value);

							var delta = stock.LotSize * ((op.Price.Value * sellRate) - (opBuy.Operation.Price.Value * buyRate));
							result.RurSumma = Math.Round(delta, 2);
							result.RurCommission =
								Math.Round((opBuy.Commission / (opBuy.Qty / stock.LotSize)) * buyRate + (item.Commission / (item.Qty / stock.LotSize)) * sellRate, 2);
						}
						else
						{
							result.RurSumma = result.Summa;
							result.RurCommission = Math.Round(result.Commission, 2);
						}

						var analitic = new Analytics(op.Stock.Ticker, accData.AccountType, op.Currency, op.DeliveryDate.Value);

						if (!FifoResults.ContainsKey(analitic))
						{
							FifoResults.Add(analitic, result);
						}
						else
						{
							var r = FifoResults[analitic];
							r.Summa += result.Summa;
							r.Commission += result.Commission;
							r.RurSumma += result.RurSumma;
							r.RurCommission += result.RurCommission;
						}

						if (stock.Ticker == "OZONDR" && op.TransId == "S3715404120" /* && op.Date >= new DateTime(2021,3,12)*/)
						{
							var R = 0;
						}

						if (item.FifoResult == null)
						{
							item.FifoResult = new FifoResult
							{
								Summa = profit,
								Cur = op.Currency,
								// buys comm. + sell comm.
								Commission = Math.Round(((item.Commission) / item.Qty) + ((opBuy.Commission) / opBuy.Qty), 2),
								RurSumma = result.RurSumma,
								RurCommission = Math.Round(result.RurCommission, 2),
								TotalSumma = (lastItem?.FifoResult?.TotalSumma ?? 0) + profit
							};
							lastItem = item;
						}
						else
						{
							item.FifoResult.Summa += profit;
							item.FifoResult.Commission += Math.Round(commission, 2);
							item.FifoResult.RurSumma += result.RurSumma;
							item.FifoResult.RurCommission += Math.Round(result.RurCommission, 2);
							item.FifoResult.TotalSumma += profit;
						}

						lots--;
					}
		        }
	        }
        }





        /// <summary>Calc FinResult</summary>
        private void CalcFinResult(ref List<Operation> ops, Operation op, Stock s, int currentQty, AccountData accData)
        {
            if (op.Type != OperationType.Sell)
                return;

			if (accData.CurrentQty < 0)
				return;  // for short position

            if (s.Ticker == "PLZL" && op.AccountType == AccountType.VBr && op.Date.Date == new DateTime(2021,12,1) ){ var r = 0; }

            op.FinResult = (op.Price - op.PosPrice).Value * op.Qty.Value;
            //op.TotalFinResult = accData.FinResult.Value + op.FinResult;

            //accData.FinResult = op.TotalFinResult;
			
            if (s.Ticker == "PLZL" && op.AccountType == AccountType.VBr){ var r = 0; }

            // close position
            if (currentQty == 0)
            {
                foreach (var b in ops.Where(x => x.Date <= op.Date && !x.IsClosed))
                {
                    b.IsClosed = true;
                }

                accData.FinResultForClosedPositions += op.TotalFinResult;
                //accData.FinResult = 0;
            }

			//accData.TotalFinResult = (accData.FinResultForClosedPositions + accData.FinResult);
        }


        private void CalcFinResultForShort(ref List<Operation> ops, Operation op, Stock s, int currentQty, AccountData accData)
        {
            if (op.Type != OperationType.Buy)
                return;

            if (accData.CurrentQty > 0)
                return;

            if (s.Ticker == "PLZL" && op.AccountType == AccountType.VBr && op.Date.Date == new DateTime(2021,12,1) ){ var r = 0; }

            op.FinResult = (op.Price - op.PosPrice).Value * op.Qty.Value * -1;
            //op.TotalFinResult = accData.FinResult.Value + op.FinResult;

            //accData.FinResult = op.TotalFinResult;
			
            // close position
            if (currentQty == 0)
            {
                foreach (var b in ops.Where(x => x.Date <= op.Date && !x.IsClosed))
                {
                    b.IsClosed = true;
                }

                accData.FinResultForClosedPositions += op.TotalFinResult;
                //accData.FinResult = 0;
            }

            //accData.TotalFinResult = accData.FinResultForClosedPositions + accData.FinResult;
        }


        private void CalcFifoResult(ref List<Operation> ops, Operation op, Stock s, AccountData accData, AccountType accountType)
        {
            if (op.Type != OperationType.Sell)
                return;

			if (accData.CurrentQty < 0)
				return;

            var lastSellOp = ops.Where(x => x.Type == OperationType.Sell && x.Date <= op.Date && x.PositionNum == op.PositionNum && x.TransId != op.TransId)
	            .OrderByDescending(x => x.Date)
	            .ThenByDescending(x => x.TransId)
	            .FirstOrDefault();

            // only for sales
            while (op.QtySaldo > 0)
            {
                op.QtySaldo -= s.LotSize;

                if (s.Ticker == "PLZL" && op.AccountType == AccountType.VBr && op.Date.Date == new DateTime(2021,12,1) ){ var r = 0; }

				// find buy operation with saldo value
                var opBuy = ops.Where(x => x.Type == OperationType.Buy && x.QtySaldo > 0 && x.Date < op.Date).OrderBy(x => x.Date).FirstOrDefault();
                if (opBuy == null)
                    break;

                opBuy.QtySaldo -= s.LotSize;
                var profit = s.LotSize * (op.Price - opBuy.Price).Value;
				var commission = ((op.Commission ?? 0) / (op.Qty.Value / s.LotSize)) + ((opBuy.Commission ?? 0) / (opBuy.Qty.Value / s.LotSize));
				                
                // Fifo result						
                var result = new FifoResult {
                    Summa = profit, 
                    Cur = op.Currency, 
                    // buys comm. + sell comm.
                    Commission = Math.Round(commission, 2)
                };

                // доход (и налог) считается с Summa - Commission!! (без учета коммисии)
                if (op.Currency == Currency.Usd || op.Currency == Currency.Eur)
                {
                    var buyRate = Instance.GetCurRate(op.Currency, opBuy.DeliveryDate.Value);
                    var sellRate = Instance.GetCurRate(op.Currency, op.DeliveryDate.Value);
							
                    var delta = s.LotSize * ((op.Price.Value * sellRate) - (opBuy.Price.Value * buyRate));
					result.RurSumma = Math.Round(delta, 2);
                    result.RurCommission = 
                        Math.Round((opBuy.Commission.Value / (opBuy.Qty.Value / s.LotSize )) * buyRate + (op.Commission.Value / (op.Qty.Value / s.LotSize))  * sellRate, 2);
                }
                else 
                {
                    result.RurSumma = result.Summa;
                    result.RurCommission = Math.Round(result.Commission, 2);
                }

                var analitic = new Analytics(op.Stock.Ticker, accountType, op.Currency, op.DeliveryDate.Value);

                if (!FifoResults.ContainsKey(analitic)) {
                    FifoResults.Add(analitic, result);
                }
                else {
                    var r = FifoResults[analitic];
                    r.Summa += result.Summa;
                    r.Commission += result.Commission;
                    r.RurSumma += result.RurSumma;
                    r.RurCommission += result.RurCommission;
                }

                if (s.Ticker == "OZONDR" && op.TransId == "S3715404120" /* && op.Date >= new DateTime(2021,3,12)*/)
                {
	                var R = 0;
                }

                if (op.FifoResult == null) {
                    op.FifoResult = new FifoResult {
                        Summa = profit,
                        Cur = op.Currency,
                        // buys comm. + sell comm.
                        Commission = Math.Round(((op.Commission ?? 0) / op.Qty.Value) + ((opBuy.Commission ?? 0) / opBuy.Qty.Value), 2),
                        RurSumma = result.RurSumma,
                        RurCommission = Math.Round(result.RurCommission, 2),
						TotalSumma = (lastSellOp?.FifoResult?.TotalSumma ?? 0) + profit
                    };
                }
                else {
                    op.FifoResult.Summa += profit;
                    op.FifoResult.Commission += Math.Round(commission, 2);
                    op.FifoResult.RurSumma += result.RurSumma;
                    op.FifoResult.RurCommission += Math.Round(result.RurCommission, 2);
                    op.FifoResult.TotalSumma += profit;
                }                
            }
        }

		private void CalcFifoResultForShort(ref List<Operation> ops, Operation op, Stock s, AccountData accData, AccountType accountType)
        {
            if (op.Type != OperationType.Buy)
                return;

            if (accData.CurrentQty > 0)
                return;

            var lastBuyOp = ops.Where(x => x.Type == OperationType.Buy && x.Date <= op.Date && x.PositionNum == op.PositionNum && x.TransId != op.TransId)
	            .OrderByDescending(x => x.Date)
	            .ThenByDescending(x => x.TransId)
	            .FirstOrDefault();

            if (s.Ticker == "PLZL" && op.AccountType == AccountType.VBr && op.Date.Date == new DateTime(2021,12,1) ){ var r = 0; }

            // only for sales
            while (op.QtySaldo > 0)
            {
                op.QtySaldo -= s.LotSize;

                // find buy operation with saldo value
                var opSell = ops.Where(x => x.Type == OperationType.Sell && x.QtySaldo > 0 && x.Date <= op.Date).OrderBy(x => x.Date).FirstOrDefault();
                if (opSell == null)
                    break;

                opSell.QtySaldo -= s.LotSize;
                var profit = s.LotSize * (op.Price - opSell.Price).Value * -1;
				var commission = ((op.Commission ?? 0) / (op.Qty.Value / s.LotSize)) + ((opSell.Commission ?? 0) / (opSell.Qty.Value / s.LotSize));
				                
                // Fifo result						
                var result = new FifoResult {
                    Summa = profit, 
                    Cur = op.Currency,
                    Commission = Math.Round(commission, 2) // buys comm. + sell comm.
                };

                // доход (и налог) считается с Summa - Commission!! (без учета коммисии)
                if (op.Currency == Currency.Usd || op.Currency == Currency.Eur)
                {
                    var buyRate = Instance.GetCurRate(op.Currency, opSell.DeliveryDate.Value);
                    var sellRate = Instance.GetCurRate(op.Currency, op.DeliveryDate.Value);
							
                    var delta = s.LotSize * ((op.Price.Value * sellRate) - (opSell.Price.Value * buyRate));
					result.RurSumma = Math.Round(delta, 2);
                    result.RurCommission = 
                        Math.Round((opSell.Commission.Value / (opSell.Qty.Value / s.LotSize )) * buyRate + (op.Commission.Value / (op.Qty.Value / s.LotSize))  * sellRate, 2);                        
                }
                else 
                {
                    result.RurSumma = result.Summa;
                    result.RurCommission = Math.Round(result.Commission, 2);
                }

                var analitic = new Analytics(op.Stock.Ticker, accountType, op.Currency, op.DeliveryDate.Value);

                if (!FifoResults.ContainsKey(analitic)) 
                {
                    FifoResults.Add(analitic, result);
                }
                else {
                    var r = FifoResults[analitic];
                    r.Summa += result.Summa;
                    r.Commission += result.Commission;
                    r.RurSumma += result.RurSumma;
                    r.RurCommission += result.RurCommission;
                }

                if (op.FifoResult == null) {
                    op.FifoResult = new FifoResult {
                        Summa = profit,
                        Cur = op.Currency,
                        // buys comm. + sell comm.
                        Commission = Math.Round(((op.Commission ?? 0) / op.Qty.Value) + ((opSell.Commission ?? 0) / opSell.Qty.Value), 2),
                        RurSumma = result.RurSumma,
                        RurCommission = Math.Round(result.RurCommission, 2),
                        TotalSumma = (lastBuyOp?.FifoResult?.TotalSumma ?? 0) + profit
                    };
                }
                else {
                    op.FifoResult.Summa += profit;
                    op.FifoResult.Commission += Math.Round(commission, 2);
                    op.FifoResult.RurSumma += result.RurSumma;
                    op.FifoResult.RurCommission += Math.Round(result.RurCommission, 2);
                    op.FifoResult.TotalSumma += profit;
                }                
            }
        }

		private PositionData CalcPosition(ref List<Operation> ops, AccountData accData, int posNum)
		{
			var pos = new PositionData();
			var posOps = ops.Where(x => x.PositionNum == posNum).ToArray();
			if (posOps.Length == 0)
				return null;

			var lastOp = ops.LastOrDefault(x => x.PositionNum == posNum); 
			//lastOp = ops.Last(x => x.PositionNum == posNum && x.Type == OperationType.Sell);
			if (lastOp == null)
				throw new Exception("lastOp is not exist in the list");
			
			
			pos.Num = posNum;
			pos.StartDate = ops.Where(x => x.PositionNum == posNum).FirstOrDefault().Date;
			//pos.BuySum = posOps.Where(x => x.Type == OperationType.Buy).Sum(x => x.Summa);
			//pos.SellSum = posOps.Where(x => x.Type == OperationType.Sell).Sum(x => x.Summa);
			if (lastOp != null && lastOp.Type == OperationType.Sell) {
				//pos.FinResult = lastOp.TotalFinResult;
				//pos.FifoResult = lastOp.FifoResult;
			}

			if (lastOp != null && lastOp.Type == OperationType.Buy) {
				//pos.FinResult = lastOp.TotalFinResult;
				//pos.FifoResult = lastOp.FifoResult;
			}

			//pos.FifoResult = new FifoResult();
			pos.FifoResult.Summa = 0;
			foreach(var o in posOps)
			{
				if (o.FifoResult != null)
					pos.FifoResult.Summa += o.FifoResult.Summa;
			}

			accData.Positions.Add(pos);

			return pos;
		}

        

        private FifoResult GetFifoResult(string ticker, AccountType accountType, Currency cur, DateTime date)
        {
            var a = new Analytics(ticker, accountType, cur, date);
            return FifoResults.ContainsKey(a) 
                    ? FifoResults[a] 
                    : null;
        }


		public decimal GetCurRate(Currency currency, DateTime? date = null)
        {
            var d = date?.Date ?? DateTime.Today.Date;

			Dictionary<DateTime, decimal> dict = null;

			if (currency == Currency.Usd)
				dict = Instance.UsdRate;
			else if (currency == Currency.Eur)
				dict = Instance.EurRate;

			if (dict == null)
				throw new Exception($"GetCurRate(): not defined currency code {currency}");

			if (dict.ContainsKey(d))
				return dict[d];

            while (!dict.ContainsKey(d))
            {
                d = d.AddDays(-1);
            }

            return dict[d];
        }


        public class CacheView {
            public string Period;
            public decimal? Summa;
        }


        public List<CacheView> GetCacheInData(AccountType? accountType)
        { 
            var ops = Instance.Operations
                .Where(x => x.Type == OperationType.BrokerCacheIn && (accountType == null ||  x.AccountType == AccountType.Iis))
                .OrderBy(x => x.Date)
                .GroupBy(x => x.Date.ToString("MMM, yy"))
                //.Join(periods, g => g.Key, p => p, (g, p) => new { M = p, S = g.Sum(x1 => x1.Summa) });
                .Select( g => new { M = g.Key, S = g.Sum(x1 => x1.Summa)});

            List<CacheView> data = new List<CacheView>();
            var d = DateTime.Now;
            while(d >= _startOperationDate)
            { 
                var op = ops.FirstOrDefault(x => x.M == d.ToString("MMM, yy"));
                data.Add(new CacheView{ Period = d.ToString("MMM, yy"), Summa = op?.S });
                d = d.AddMonths(-1);  
            }    
            
            return data;
        }

        public List<Period> GetPeriods(DateTime? start = null, DateTime? end = null)
        { 
            if (start == null)
                start = _startOperationDate.Date;
            
            if (end == null)
                end = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
			
			return Periods.Where(x => (start == null || x.Start >= start) && (end == null || x.End <= end)).ToList();			
        }

		public Period GetPeriod(DateTime date)
        {             
			return Periods.Single(x => x.Start <= date.Date && x.End >= date.Date);			
        }

		public void FillPeriods(DateTime? start = null, DateTime? end = null)
        { 
            if (start == null)
                start = _startOperationDate;
            
            if (end == null)
                end = DateTime.Now;

            Instance.Periods = new List<Period>();
            while(end >= start)
            {                
                Instance.Periods.Add(new Period(end.Value));
                end = end.Value.AddMonths(-1);
            }
        }

		private List<string> LoadPortfolioData(Portfolio portfolio, string fileName = "portfolio.json")
        {
            const string root = "wwwroot";
            var path = Path.Combine(Directory.GetCurrentDirectory(), root, fileName);

            if (!File.Exists(path))
                return null;

            string jsonResult;

            using (var streamReader = new StreamReader(path))
            {
                jsonResult = streamReader.ReadToEnd();
            }
			
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonResult).ToList();

			foreach(var p in data) 
			{
				if (p["PortfolioId"] == ((int)portfolio).ToString())
				{
					var s = p["Tickers"].Replace(" ", "");
					var tickers = s.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);
					
					return tickers.ToList();
				}
			}

			return null;
        }

		private List<PositionData> CreatePositions(List<Operation> ops)
		{
			var posNum = 0;
			var positions = new List<PositionData>(); 
			PositionData pos = null;

			foreach (var o in ops)
			{
				if (o.Qty == null)
					throw new Exception("CreatePositions(): o.Qty == null");

				//new position (and short position)
				if (pos == null)
				{
					pos = new PositionData(++posNum, o);
					positions.Add(pos);
				}

				var opQty = o.Qty.Value;
				var newQty = pos.CurentQty + o.OffsetQty;

				if ((pos.Type == PositionType.Long && newQty < 0) || (pos.Type == PositionType.Short && newQty > 0))
				{
					pos.AddItem(o, pos.CurentQty); // to zero

					opQty = Math.Abs(o.Qty.Value - pos.CurentQty);
					if (opQty == 0)
						throw new Exception("CreatePositions(): qtySaldo == 0");

					pos.Close(o.Date);

					// new pos item
					pos = new PositionData(++posNum, o);
					positions.Add(pos);
				}
				
				pos.AddItem(o, opQty);

				if (pos.CurentQty == 0)
				{
					pos.Close(o.Date);
					pos = null;
				}
			}

			return positions;
		}


		private void AddTestData()
		{
			var sb = new StringBuilder();
			sb.AppendLine("\"data\": [");

			foreach(var c in Instance.Companies)
			{
				sb.AppendFormat("{{\n \"id\": \"{0}\", \"name\": \"{1}\", \"divName\": \"{2}\", ",
					c.Id, c.Name, c.DivName);

				var stocks = Instance.Stocks.Where(x => x.Company == c).ToList();
				if (!stocks.Any())
					sb.AppendFormat("\"stocks\": []");
				else 
				{
					sb.AppendLine("\"stocks\": [");
					for (var x=0; x < stocks.Count; x++)
					{
						var s = stocks[x];
						var isin = "";
						if (s.Isin != null && s.Isin.Length != 0)
						{
							for(var k=0; k < s.Isin.Length; k++)
							{
								isin += k > 0 
									? ", " + s.Isin[k] 
									: s.Isin[k];
							}
						}

						sb.AppendFormat("{{ \"ticker\": \"{0}\", \"brokerName\": \"{1}\", \"lotSize\": \"{2}\", \"cur\": \"{3}\", \"isin\": \"{4}\" }} {5} \n",
							s.Ticker, s.Name, s.LotSize, s.Currency.ToString().ToLower(), isin, x < stocks.Count-1 ? ", " : "");
					}
					sb.AppendLine("] },\n");
				}

				sb.AppendFormat("");
			}

			sb.AppendLine("]");
		}
    }
}