using System;
using System.Collections.Generic;
using System.Text;
using Invest.Core.Entities;
using Newtonsoft.Json.Converters;

namespace Invest.Core
{
	public class InterfaceConverter<TInterface, TConcrete> : CustomCreationConverter<TInterface>
		where TConcrete : TInterface, new()
	{
		/// <summary></summary>
		/// <param name="objectType"></param>
		/// <returns></returns>
		public override TInterface Create(Type objectType)
		{
			return new TConcrete();
		}
	}

	public class AccountConverter : CustomCreationConverter<BaseAccount>
	{
		/// <inheritdoc />
		public override BaseAccount Create(Type objectType)
		{
			return new Account();
		}
	}

	//public class StockConverter : CustomCreationConverter<BaseStock>
	//{
	//	/// <inheritdoc />
	//	public override BaseStock Create(Type objectType)
	//	{
	//		return new Stock();
	//	}
	//}

	public class CompanyConverter : CustomCreationConverter<BaseCompany>
	{
		/// <inheritdoc />
		public override BaseCompany Create(Type objectType)
		{
			return new Company();
		}
	}
}
