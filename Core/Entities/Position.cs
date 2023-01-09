using System;
using System.Collections.Generic;
using System.Linq;
using Invest.Core.Enums;

namespace Invest.Core.Entities
{
	public class Position
	{
		public Position(){}

		public Position(int num, Operation op) 
			: this(num, op.Date, op.Type == OperationType.Sell ? PositionType.Short : PositionType.Long) 
		{}

		public Position(int num, DateTime startDate, PositionType type = PositionType.Long)
		{
			Num = num;
			Type = type;
			StartDate = startDate;
			NotClosedOps = new Queue<Operation>();
			Items = new List<PositionItem>();
		}

		public int Num;
		public PositionType Type;
		public DateTime StartDate;
		public DateTime? CloseDate;
		public bool IsClosed;
		public Queue<Operation> NotClosedOps;
		public List<PositionItem> Items;
		public VirtualAccount VirtualAccount;
		public Currency Currency;
		public Exchange Exchange; 
		public int CurentQty;   // temprary qty for calc procedure and also save actual Qty for last open position
		
		public decimal? FinResult => Items.LastOrDefault(x => x.TotalFinResult != null)?.TotalFinResult;

		public decimal? BuySum {
			get {
				return Items.Where(x => x.Operation.Type == OperationType.Buy).Sum(item => item.Operation.Price * item.Qty);
			}
		}

		public decimal? SellSum {
			get
			{
				var s = Items.Where(x => x.Operation.Type == OperationType.Sell);
				return s.Any() 
					? Items.Where(x => x.Operation.Type == OperationType.Sell).Sum(item => item.Operation.Price * item.Qty) 
					: null;
			}
		}

		public decimal Commission
		{
			get {
				return Items.Sum(item => item.Commission);
			}
		}

		public decimal? TotalFinResult
		{
			get {
				var item = Items.LastOrDefault(x => x.ForCalc);
				return item?.TotalFinResult;
			}
		}

		public decimal? PosPrice
		{
			get {
				var item = Items.LastOrDefault();
				return item?.PosPrice;
			}
		}

		public FifoResult FifoResult
		{
			get {
				var result = new FifoResult();

				var item = Items.LastOrDefault(x => x.ForCalc);
				if (item != null && item.FifoResult != null)
				{
					result.Summa = item.FifoResult.Summa;
					result.Commission  = item.FifoResult.Commission;
					result.RurSumma = item.FifoResult.RurSumma;
					result.RurCommission = item.FifoResult.RurCommission;
					result.TotalSumma = item.FifoResult.TotalSumma;
				}

				return result;
			}
		}
		
		public void AddItem(Operation op, int qty)
		{
			var item = new PositionItem(op, qty);
			Items.Add(item);

			item.ForCalc = (Type == PositionType.Long && item.Operation.Type == OperationType.Sell)
				                || (Type == PositionType.Short && item.Operation.Type == OperationType.Buy);

			// divide commission per item
			if (op.Commission.HasValue && op.LotCount != 0)
				item.Commission = qty == op.Qty
					? op.Commission.Value
					: op.Commission.Value / (op.LotCount * qty);

			CurentQty += op.Type == OperationType.Buy ? qty : -qty;
		}

		public void CalcPosPrice()
		{
			var curentQty = 0;
			var list = new List<PositionItem>();

			for(var i=0; i < Items.Count; i++)
			{
				var item = Items[i];
				list.Add(item);

				curentQty += item.Operation.Type == OperationType.Buy
					? item.Qty
					: -item.Qty;

				if (list.Count == 1 || i == 0) {
					list[i].PosPrice = item.Operation.Price ?? 0;
					continue;
				}

				var recB = list.Where(x => x.Operation.Type == OperationType.Buy).Sum(x => x.Operation.Price * x.Qty) ?? 0;
				var recS = list.Where(x => x.Operation.Type == OperationType.Sell).Sum(x => x.PosPrice * x.Qty);

				if (Type == PositionType.Short)
				{
					recB = list.Where(x => x.Operation.Type == OperationType.Buy).Sum(x => x.PosPrice * x.Qty);
					recS = list.Where(x => x.Operation.Type == OperationType.Sell).Sum(x => x.Operation.Price * x.Qty) ?? 0;
				}

				item.PosPrice = !item.ForCalc 
					? (recB - recS) / curentQty 
					: list[i-1].PosPrice;
			}
		}

		public void CalcBondPosPrice()
		{
			var curentQty = 0;
			var list = new List<PositionItem>();

			for(var i=0; i < Items.Count; i++)
			{
				var item = Items[i];
				list.Add(item);

				curentQty += item.Operation.Type == OperationType.Buy
					? item.Qty
					: -item.Qty;

				if (list.Count == 1 || i == 0) {
					list[i].PosPrice = item.Operation.Price + (item.Operation.Nkd / item.Qty) ?? 0;
					continue;
				}

				var recB = list.Where(x => x.Operation.Type == OperationType.Buy).Sum(x => x.Operation.Price * x.Qty) ?? 0;
				var recS = list.Where(x => x.Operation.Type == OperationType.Sell).Sum(x => x.PosPrice * x.Qty);

				item.PosPrice = !item.ForCalc 
					? (recB - recS) / curentQty 
					: list[i-1].PosPrice;
			}
		}

		public void Close(DateTime closeDate)
		{
			CloseDate = closeDate;
			IsClosed = true;
			CurentQty = 0;
		}

		public void CalcFinResult()
		{
			decimal totalFinResult = 0;
			foreach(var item in Items)
			{
				var op = item.Operation;

				if (op.Commission == null || op.Qty == null)
					throw new Exception("CalcPositionFinResult(): op.Commission == null || op.Qty == null");

				if (item.ForCalc)
				{
					if (item.Operation.Stock.Ticker == "Систем1P21") { var t= 0; }

					item.FinResult = ((op.Price ?? 0) - item.PosPrice) * item.Qty;
					if (Type == PositionType.Short)
						item.FinResult *= -1;

					totalFinResult += item.FinResult.Value;
					item.TotalFinResult = totalFinResult;
				}

				item.Commission = item.Qty == item.Operation.Qty
					? op.Commission.Value
					: op.Commission.Value / (op.LotCount * item.Qty);
			}
		}

		public void CalcBondFinResult()
		{
			decimal totalFinResult = 0;
			foreach(var item in Items)
			{
				var op = item.Operation;

				if (op.Commission == null || op.Qty == null)
					throw new Exception("CalcBondFinResult(): op.Commission == null || op.Qty == null");

				if (item.ForCalc)
				{
					//item.FinResult = ((op.Price ?? 0) - item.PosPrice) * item.Qty;
					item.FinResult = op.Summa + (op.Nkd ?? 0) - (item.PosPrice * item.Qty);

					if (item.Operation.Stock.Ticker == "Систем1P21") { var t= 0; }
					
					totalFinResult += item.FinResult.Value;
					item.TotalFinResult = totalFinResult;
				}

				item.Commission = item.Qty == item.Operation.Qty
					? op.Commission.Value
					: op.Commission.Value / (op.LotCount * item.Qty);
			}
		}
	}

	public class PositionItem
	{
		public PositionItem(Operation op, int qty)
		{
			Operation = op;
			Qty = qty;
		}

		public Operation Operation;
		public int Qty;
		public decimal PosPrice;
		public decimal? FinResult, TotalFinResult;
		public FifoResult FifoResult;
		public decimal Commission;
		public bool ForCalc;
	}
}