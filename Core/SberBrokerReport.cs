using System;
using System.Collections.Generic;
using System.Text;

namespace Invest.Core
{
    public class SberBrokerReport
    {

	    //      private void ParseSberFile(AccountType accountType, int year, FileStream fs)
	    //      {
	    //       var cellMappingCache = GetExcelCellsMappingForCache(accountType, year);
	    //       var cellMapping = GetExcelCellsMappingForOperation(accountType, year);
	    //       var cellMappingUsdRubOperation = GetExcelCellsMappingForUsdRubOperation(accountType, year);

	    //       using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(fs))
	    //       {
	    //        var emptyCellCount = 200;
	    //        const int startIndex = 8;
	    //        var rowIndex = 0;

	    //        while (reader.Read())
	    //        {
	    //	        if (rowIndex >= startIndex)
	    //	        {
	    //		        var titleCell = reader.GetValue(ExcelCell("B"));
	    //		        if (titleCell != null)
	    //		        {
	    //			        var title = titleCell.ToString().Trim();

	    //			        //if (title.Equals("Движение денежных средств", StringComparison.OrdinalIgnoreCase))
	    //				       // ReadCacheIn(reader, accountType, cellMappingCache);
                                    
	    //			        //if (title.Equals("Заключенные в отчетном периоде сделки с ценными бумагами", StringComparison.OrdinalIgnoreCase))
	    //				    //    ReadOperations(reader, accountType, cellMapping);

	    //			        //if (title.Equals("Завершенные в отчетном периоде сделки с иностранной валютой (обязательства прекращены)", StringComparison.OrdinalIgnoreCase)
	    //			        //    || title.Equals("Заключенные в отчетном периоде сделки с иностранной валютой", StringComparison.OrdinalIgnoreCase))
	    //				       // ReadUsdRubOperations(reader, accountType, cellMappingUsdRubOperation);
	    //		        }

	    //		        if (titleCell == null)
	    //			        emptyCellCount--;

	    //		        if (emptyCellCount == 0)
	    //			        break;
	    //	        }
	    //	        rowIndex++;
	    //        }
	    //       }
	    //}
    }
}
