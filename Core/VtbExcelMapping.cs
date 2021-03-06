using System;
using System.Collections.Generic;
using Invest.Core.Enums;

namespace Invest.Core
{
	public class VtbExcelMapping 
	{
		private readonly int _accountCode;
		private readonly int _year;
		
		public VtbExcelMapping(int accountCode, int year)
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

		public class CacheMap
		{
			public string Date;
			public string Summa;
			public string Cur;
			public string Type;
			public string Comment;
		}

		public class CurrencyOperationMap
		{
			public string OrderDate;	// order date
			public string Date;			// execution date
			public string Summa;
			public string Cur;
			public string Type;
			public string Qty;
			public string Price;
			public string BankCommission1;  // Комиссия Банка за расчет по сделке
			public string BankCommission2;  // Комиссия Банка за заключение сделки
			public string OrderId;			// № заявки
			public string TransId;				// № сделки
			public string FinInstrument = "B";	// Финансовый инструмент ("USDRUB_CNGD, EURRUB_CNGD")
		}

		/// <summary>
		/// mapping cells for each account
		/// </summary>
		/// <returns></returns>
		public OperationMap GetMappingForOperation()
		{
			OperationMap m = null;

			if (_accountCode == (int)AccountType.Iis) // 7101NME0
			{
				if (_year == 2019)
					m = new OperationMap {
						Name = "B",
						Date = "D",
						Type = "G",
						Qty = "K",
						Price = "R",
						BankCommission1 = "AK",
						BankCommission2 = "AO",
						OrderId = "BA",
						TransId = "BD",
						DeliveryDate = "AT"
					};
				else if (_year == 2020)
					m = new OperationMap {
						Name = "B",
						Date = "D",
						Type = "F",
						Qty = "J",
						Price = "Q",
						BankCommission1 = "AG",
						BankCommission2 = "AJ",
						OrderId = "AV",
						TransId = "AY",
						DeliveryDate = "AO"
					};
				else if (_year == 2021)
					m = new OperationMap {
						Name = "B",
						Date = "D",
						Type = "G",
						Qty = "K",
						Price = "S",
						BankCommission1 = "AK",
						BankCommission2 = "AO",
						OrderId = "BB",
						TransId = "BF",
						DeliveryDate = "AT",
						Nkd = "AF"
					};
				else if (_year == 2022)
					m = new OperationMap {
						Name = "B",
						Date = "D",
						Type = "F",
						Qty = "J",
						Price = "Q",
						BankCommission1 = "AG",
						BankCommission2 = "AJ",
						OrderId = "AV",
						TransId = "AY",
						DeliveryDate = "AO",
						Nkd = "AB",
						Currency = "U"
					};
			}
			else if (_accountCode == (int)AccountType.VBr)
			{ 
				if (_year == 2019)
					m = new OperationMap {
						Name = "B",
						Date = "D",
						Type = "G",
						Qty = "K",
						Price = "S",
						BankCommission1 = "AL",
						BankCommission2 = "AP",
						OrderId = "BC",
						TransId = "BG",
						DeliveryDate = "AU"
					};
				else if (_year == 2020)
					m = new OperationMap {
						Name = "B",
						Date = "D",
						Type = "G",
						Qty = "K",
						Price = "S",
						BankCommission1 = "AL",
						BankCommission2 = "AP",
						OrderId = "BC",
						TransId = "BH",
						DeliveryDate = "AU",
						Currency = "O"
					};
				else if (_year == 2021)
					m = new OperationMap {
						Name = "B",
						Date = "D",
						Type = "G",
						Qty = "K",
						Price = "S",
						BankCommission1 = "AL",
						BankCommission2 = "AP",
						OrderId = "BC",
						TransId = "BH",
						DeliveryDate = "AU",
						Nkd = "AF"
					};
				else if (_year == 2022)
					m = new OperationMap {
						Name = "B",
						Date = "D",
						Type = "G",
						Qty = "K",
						Price = "S",
						BankCommission1 = "AL",
						BankCommission2 = "AP",
						OrderId = "BC",
						TransId = "BH",
						DeliveryDate = "AU",
						Nkd = "AF"
					};
			}
			else if (_accountCode == (int)AccountType.SBr)
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
				throw new Exception("ExcelCellsMapping(): wrong account id");

			return m;
		}

		public CacheMap GetMappingForCache()
		{
			CacheMap m = null;

			if (_accountCode == (int)AccountType.Iis)
			{
				if (_year == 2019)
					m = new CacheMap {
						Date = "B",
						Summa = "D",
						Cur = "J",
						Type = "P",
						Comment = "AH"
					};
				else if (_year == 2020)
					m = new CacheMap {
						Date = "B",
						Summa = "D",
						Cur = "I",
						Type = "O",
						Comment = "AD"
					};
				else if (_year == 2021)
					m = new CacheMap {
						Date = "B",
						Summa = "D",
						Cur = "J",
						Type = "Q",
						Comment = "AH"
					};
				else if (_year == 2022)
					m = new CacheMap {
						Date = "B",
						Summa = "D",
						Cur = "I",
						Type = "O",
						Comment = "AD"
					};
			}
			else if (_accountCode == (int)AccountType.VBr)
			{ 
				if (_year == 2019)
					m = new CacheMap() {
						Date = "B",
						Summa = "D",
						Cur = "H",
						Type = "M",
						Comment = "Y"
					};
				else if (_year == 2020 || _year == 2021 || _year == 2022)
					m = new CacheMap() {
						Date = "B",
						Summa = "D",
						Cur = "J",
						Type = "Q",
						Comment = "AI"
					};
			}
			else if (_accountCode == (int)AccountType.SBr)
			{
				if (_year == 2022)
					m = new CacheMap() {
						Date = "B",
						Summa = "D",
						Cur = "H",
						Type = "M",
						Comment = "Y"
					};
			}
			else
				throw new Exception("ExcelCellsMappingCache(): wrong account id");

			return m;
		}

		public CurrencyOperationMap GetMappingForCurOperation()
		{
			CurrencyOperationMap m = null;

			if (_accountCode == (int)AccountType.VBr)
			{
				if (_year == 2019)
					m = new CurrencyOperationMap
					{
						OrderDate = "D",
						Date = "AA",
						Summa = "R",
						Qty = "I",
						Cur = "O",
						Type = "E",
						Price = "K",
						BankCommission1 = "U",
						BankCommission2 = "W",
						OrderId = "AB",
						TransId = "AF",
						FinInstrument = "B"
					};
				else if (_year == 2020)
					m = new CurrencyOperationMap
					{
						OrderDate = "D",
						Date = "AL",
						Summa = "X",
						Qty = "K",
						Cur = "S",
						Type = "G",
						Price = "O",
						BankCommission1 = "AB",
						BankCommission2 = "AG",
						OrderId = "AP",
						TransId = "AU",
						FinInstrument = "B"
					};
				else if (_year == 2021)
					m = new CurrencyOperationMap
					{
						OrderDate = "D",
						Date = "AL",
						Summa = "X",
						Qty = "K",
						Cur = "S",
						Type = "G",
						Price = "O",
						BankCommission1 = "AB",
						BankCommission2 = "AG",
						OrderId = "AP",
						TransId = "AU",
						FinInstrument = "B"
					};
				else if (_year == 2022)
					m = new CurrencyOperationMap
					{
						OrderDate = "D",
						Date = "AL",
						Summa = "X",
						Qty = "K",
						Cur = "S",
						Type = "G",
						Price = "O",
						BankCommission1 = "AB",
						BankCommission2 = "AG",
						OrderId = "AP",
						TransId = "AU",
						FinInstrument = "B"
					};
			}
			else if (_accountCode == (int)AccountType.Iis)
			{
				if (_year == 2021)
					m = new CurrencyOperationMap
					{
						OrderDate = "D",
						Date = "AL",
						Summa = "X",
						Qty = "K",
						Cur = "S",
						Type = "G",
						Price = "O",
						BankCommission1 = "AA",
						BankCommission2 = "AF",
						OrderId = "AO",
						TransId = "AT",
						FinInstrument = "B"
					};
				if (_year == 2022)
					m = new CurrencyOperationMap {
						OrderDate = "D",
						Date = "AG",
						Summa = "U",
						Qty = "J",
						Cur = "Q",
						Type = "F",
						Price = "M",
						BankCommission1 = "X",
						BankCommission2 = "AB",
						OrderId = "AJ",
						TransId = "AO",
						FinInstrument = "B"
					};
			}
			else if (_accountCode == (int)AccountType.SBr)
			{
				if (_year == 2022)
					m = new CurrencyOperationMap {
						OrderDate = "D",
						Date = "AL",
						Summa = "X",
						Qty = "K",
						Cur = "S",
						Type = "G",
						Price = "O",
						BankCommission1 = "AA",
						BankCommission2 = "AF",
						OrderId = "AO",
						TransId = "AT",
						FinInstrument = "B"
					};
			}
			else
				throw new Exception("ExcelCellsMappingUsdRubOperation(): wrong account id");

			return m;
		}
	}

	public class ExcelUtil 
	{
		private static Dictionary<string, int> _excelCells;

		static ExcelUtil()
		{
			_excelCells = new Dictionary<string, int>();
			FillExcelCellsDictionary();
		}

		public static int ExcelCell(string cellName)
		{
			if (!_excelCells.ContainsKey(cellName))
				throw new Exception($"Cell is not found by name '{cellName}'");

			return _excelCells[cellName];
		}

		public static string GetCellValue(string cellName, ExcelDataReader.IExcelDataReader reader)
		{
			return reader.GetValue(ExcelCell(cellName))?.ToString().Trim();
		}

		private static void FillExcelCellsDictionary()
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
	}
}