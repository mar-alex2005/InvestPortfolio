using System;
using Invest.Core.Enums;

namespace Invest.Core
{
	public class ExcelMapping 
	{
		public class ExcelCellsMappingOperation
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

		public class ExcelCellsMappingCache
		{
			public string Date;
			public string Summa;
			public string Cur;
			public string Type;
			public string Comment;
		}

		public class ExcelCellsMappingCurrencyOperation
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
			public string FinInstrument = "B";	// Финансовый инструмент ("USDRUB_CNGD, EURRUB_CNGD, CNYRUB_TOM")
		}

		/// <summary>
		/// mapping cells for each account
		/// </summary>
		/// <param name="accountType"></param>
		/// <param name="year"></param>
		/// <returns></returns>
		public ExcelCellsMappingOperation GetExcelCellsMappingForOperation(AccountType accountType, int year)
		{
			ExcelCellsMappingOperation m = null;

			if (accountType == AccountType.Iis) // 7101NME0
			{
				if (year == 2019)
					m = new ExcelCellsMappingOperation {
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
				else if (year == 2020)
					m = new ExcelCellsMappingOperation {
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
				else if (year == 2021)
					m = new ExcelCellsMappingOperation {
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
				else if (year == 2022)
					m = new ExcelCellsMappingOperation {
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
			else if (accountType == AccountType.VBr)
			{ 
				if (year == 2019)
					m = new ExcelCellsMappingOperation {
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
				else if (year == 2020)
					m = new ExcelCellsMappingOperation {
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
				else if (year == 2021)
					m = new ExcelCellsMappingOperation {
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
				else if (year == 2022)
					m = new ExcelCellsMappingOperation {
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
			else if (accountType == AccountType.SBr)
			{ 
				if (year == 2022)
					m = new ExcelCellsMappingOperation {
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

		private ExcelCellsMappingCache GetExcelCellsMappingForCache(AccountType accountType, int year)
		{
			ExcelCellsMappingCache m = null;

			if (accountType == AccountType.Iis)
			{
				if (year == 2019)
					m = new ExcelCellsMappingCache {
						Date = "B",
						Summa = "D",
						Cur = "J",
						Type = "P",
						Comment = "AH"
					};
				else if (year == 2020)
					m = new ExcelCellsMappingCache {
						Date = "B",
						Summa = "D",
						Cur = "I",
						Type = "O",
						Comment = "AD"
					};
				else if (year == 2021)
					m = new ExcelCellsMappingCache {
						Date = "B",
						Summa = "D",
						Cur = "J",
						Type = "Q",
						Comment = "AH"
					};
				else if (year == 2022)
					m = new ExcelCellsMappingCache {
						Date = "B",
						Summa = "D",
						Cur = "I",
						Type = "O",
						Comment = "AD"
					};
			}
			else if (accountType == AccountType.VBr)
			{ 
				if (year == 2019)
					m = new ExcelCellsMappingCache() {
						Date = "B",
						Summa = "D",
						Cur = "H",
						Type = "M",
						Comment = "Y"
					};
				else if (year == 2020 || year == 2021 || year == 2022)
					m = new ExcelCellsMappingCache() {
						Date = "B",
						Summa = "D",
						Cur = "J",
						Type = "Q",
						Comment = "AI"
					};
			}
			else if (accountType == AccountType.SBr)
			{
				if (year == 2022)
					m = new ExcelCellsMappingCache() {
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

		private ExcelCellsMappingCurrencyOperation GetExcelCellsMappingForUsdRubOperation(AccountType accountType, int year)
		{
			ExcelCellsMappingCurrencyOperation m = null;

			if (accountType == AccountType.VBr)
			{
				if (year == 2019)
					m = new ExcelCellsMappingCurrencyOperation
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
				else if (year == 2020)
					m = new ExcelCellsMappingCurrencyOperation
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
				else if (year == 2021)
					m = new ExcelCellsMappingCurrencyOperation
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
				else if (year == 2022)
					m = new ExcelCellsMappingCurrencyOperation
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
			else if (accountType == AccountType.Iis)
			{
				if (year == 2021)
					m = new ExcelCellsMappingCurrencyOperation
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
				if (year == 2022)
					m = new ExcelCellsMappingCurrencyOperation {
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
			else if (accountType == AccountType.SBr)
			{
				if (year == 2022)
					m = new ExcelCellsMappingCurrencyOperation {
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
}