using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Class
{
    public class ExcelMapper
    {
        private ExcelMapperConfig _config;
        private XLWorkbook _workBook;
        public ExcelMapper()
        {
            _config = new ExcelMapperConfig();
        }
        private ExcelMapperConfig config => _config;
        private XLWorkbook workbook => _workBook;
        public IXLWorksheet WorkSheet => getWorkSheet();
        private IXLWorksheet getWorkSheet()
        {
            var worksheetName = config.Worksheet;
            XLWorkbook wb = workbook;
            IXLWorksheet ws;

            if (worksheetName == null)
                ws = wb.Worksheet(1);
            else
                ws = wb.Worksheet(worksheetName);
            return ws;
        }


        public DataTable asDataTable()
        {
            DataTable dt = new DataTable();
            var worksheetName = config.Worksheet;

            IXLWorksheet ws;
            XLWorkbook wb = workbook;

            if (worksheetName == null)
                ws = wb.Worksheet(1);
            else
                ws = wb.Worksheet(worksheetName);
            IXLAddress col = ws.Cell(config.HeaderRow).Address;
            int colNum = col.ColumnNumber, colRowNum = col.RowNumber,
                colPointer = colNum,
                colCount = config.HeaderColCount;

            List<ExcelMapping> mapping = config.Mapping;
            Dictionary<string, int> xlHeaderDict = new Dictionary<string, int>();

            for (int i = 0; i < colCount; i++)
            {
                int colRef = colPointer + i;
                string colHeaderName = ws.Cell(colRowNum, colRef).GetFormattedString();
                if (xlHeaderDict.ContainsKey(colHeaderName)) continue;
                xlHeaderDict.Add(colHeaderName, colRef);
            }

            foreach (ExcelMapping map in mapping)
                dt.Columns.Add(map.Target);

            string rowStart = config.DataRowStart;
            var rowAddress = ws.Cell(rowStart).Address;
            int rowNum = rowAddress.RowNumber;

            for (int j = rowNum; !ws.Cell(j, colRowNum).IsEmpty(); j++)
            {
                var r = dt.NewRow();
                foreach (ExcelMapping map in mapping)
                {
                    string mappedHeaderName = map.Target;
                    IXLCell cell;
                    if (map.IsSingleCell)
                        cell = ws.Cell(map.Source);
                    else
                    {
                        int headerColNum;
                        if (!xlHeaderDict.TryGetValue(map.Source ?? "", out headerColNum))
                        {
                            r[mappedHeaderName] = "";
                            continue;
                        }
                        cell = ws.Cell(j, headerColNum);
                    }
                    var fs = cell.GetFormattedString();
                    var formattedVal = map.formatValue(fs);
                    r[mappedHeaderName] = formattedVal;
                }
                dt.Rows.Add(r);
            }

            return dt;
        }
        public DataTable asRawDataTable()
        {
            DataTable dt = new DataTable();
            var worksheetName = config.Worksheet;

            IXLWorksheet ws = getWorkSheet();

            IXLAddress col = ws.Cell(config.HeaderRow).Address;
            int colNum = col.ColumnNumber, colRowNum = col.RowNumber,
              colPointer = colNum,
              colCount = config.HeaderColCount;

            Dictionary<string, int> xlHeaderDict = new Dictionary<string, int>();
            for (int i = colPointer; i <= colCount + 1; i++)
            {
                string colHeaderName = ws.Cell(colRowNum, i).GetFormattedString();
                xlHeaderDict.Add(colHeaderName, i);
                dt.Columns.Add(colHeaderName);
            }

            string rowStart = config.DataRowStart;
            var rowAddress = ws.Cell(rowStart).Address;
            int rowNum = rowAddress.RowNumber;


            var dtHeaders = dt.Columns.Cast<DataColumn>().Select(dtCol => dtCol.ColumnName).AsEnumerable();
            for (int i = rowNum; !ws.Cell(i, colPointer).IsEmpty(); i++)
            {
                var r = dt.NewRow();
                foreach (string header in dtHeaders)
                {
                    var xlColNum = xlHeaderDict[header];
                    r[header] = extractData(ws.Cell(i, xlColNum));
                }
                dt.Rows.Add(r);
            }

            return dt;

        }
        private string extractData(IXLCell cell)
        {
            return cell.GetFormattedString();
        }
        public string[] getHeaders()
        {

            DataTable dt = new DataTable();
            var worksheetName = config.Worksheet;

            IXLWorksheet ws = getWorkSheet();

            IXLAddress col = ws.Cell(config.HeaderRow).Address;
            int colNum = col.ColumnNumber, colRowNum = col.RowNumber,
                colPointer = colNum,
                colCount = config.HeaderColCount;

            List<ExcelMapping> mapping = config.Mapping;
            List<string> xlHeaderDict = new List<string>();

            for (int i = 0; i < colCount; i++, colPointer++)
            {
                string colHeaderName = ws.Cell(colRowNum, colPointer).GetFormattedString();
                xlHeaderDict.Add(colHeaderName);
            }

            return xlHeaderDict.ToArray();

        }
        public void setWorkBook(XLWorkbook workbook)
        {
            _workBook = workbook;
        }
        public void setHeader(string rowCell, int columnCount)
        {
            config.HeaderRow = rowCell;
            config.HeaderColCount = columnCount;
        }

        public void setWorkSheet(string worksheet)
        {
            config.Worksheet = worksheet;
        }

        public void setRow(string startRowCell)
        {
            config.DataRowStart = startRowCell;
        }

        public ExcelMapping addMapping(string source, string target, bool IsSingleCell = false)
        {
            ExcelMapping newMapping = new ExcelMapping
            {
                Source = source,
                Target = target,
                IsSingleCell = IsSingleCell
            };
            config.Mapping.Add(newMapping);
            return newMapping;
        }

        public void addMapping(ExcelMapping mapping)
        {
            config.Mapping.Add(mapping);
        }

    }


    public class ExcelMapperConfig
    {
        public string Worksheet { get; set; }
        public string HeaderRow { get; set; }
        public int HeaderColCount { get; set; }
        public string DataRowStart { get; set; }
        public List<ExcelMapping> Mapping { get; set; } = new List<ExcelMapping>();
    }

    public class ExcelMapping
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public bool IsSingleCell { get; set; } = false;
        public string formatValue(string val) => callBack(val);
        public Func<string, string> callBack = val => val;
        public void FormatCallBack(Func<string, string> callback) => this.callBack = callback;
    }

}
