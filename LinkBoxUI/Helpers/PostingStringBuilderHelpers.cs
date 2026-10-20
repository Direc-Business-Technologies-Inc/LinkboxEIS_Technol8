using DataAccessLayer.Class;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace LinkBoxUI.Helpers
{
    public class PostingStringBuilderHelpers
    {
        public static bool ColumnExist(DataTable data, string ColumnName)
        {
            try
            {
                return data.Rows[0][$@"{ColumnName}"].ToString().Any();
            }
            catch
            {
                return false;
            }
        }
        public static bool IsDate(string strDate)
        {
            DateTime fromDateValue;
            var formats = new[] { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy HH:mm:ss", "yyyy'-'MM'-'dd'T'HH':'mm':'ss", "yyyy'-'MM'-'dd'T'HH':'mm':'ssZ" };
            if (DateTime.TryParseExact(strDate, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out fromDateValue))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool ColumnExist(DataRow data, string ColumnName)
        {
            try
            {
                return data[$@"{ColumnName}"].ToString().Any();
            }
            catch
            {
                return false;
            }
        }

        public static string BuildJsonForInvoice(PostingViewModel model, DataRow Header, DataTable Rows, DataColumnCollection column)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@" ""CardCode"" : ""MAIN"",");
            json.AppendLine($@" ""U_Remarks"" : ""Uploaded by LinkBox {DateTime.Now}"",");
            //Header
            foreach (DataColumn col in column)
            {
                var field = model.HeaderFields.Where(x => x.AddonField == col.ColumnName).FirstOrDefault();
                if (field != null && col.ColumnName.ToLower().Contains("date"))
                {
                    var docdate = DateTime.ParseExact(Header[field.AddonField].ToString(), "MM/dd/yyyy", null);
                    json.AppendLine($@" ""{field.SAPFieldId}"" : ""{Convert.ToDateTime(docdate).ToString("yyyy-MM-dd")}"",");
                }
                else if (field != null)
                {
                    json.AppendLine($@" ""{field.SAPFieldId}"" : ""{Header[field.AddonField].ToString()}"",");
                }

            }

            //Rows

            json.AppendLine($@" ""DocumentLines"" : [");
            ;
            foreach (DataRow row in Rows.Rows)
            {
                json.AppendLine("   {");
                foreach (DataColumn col in Rows.Columns)
                {
                    var field = model.RowFields.Where(x => x.AddonField == col.ColumnName).FirstOrDefault();
                    if (field != null && col.ColumnName.ToLower().Contains("price"))
                    {
                        var discount = string.IsNullOrEmpty(row[model.RowFields.Where(x => x.AddonField.ToLower().Contains("discount")).FirstOrDefault().AddonField].ToString()) ? 0 :
                            Convert.ToDouble(Regex.Replace(row[model.RowFields.Where(x => x.AddonField.ToLower().Contains("discount")).FirstOrDefault().AddonField].ToString(), "[^0-9.]", ""));
                        var price = Convert.ToDouble(row[field.AddonField].ToString()) -
                            ((Convert.ToDouble(row[field.AddonField].ToString()) / 100) * discount);

                        json.AppendLine($@" ""PriceAfterVAT"" : ""{price}"",");

                    }
                    else if (field != null && col.ColumnName.ToLower().Contains("discount"))
                    {
                        var discount = string.IsNullOrEmpty(row[field.AddonField].ToString()) ? "0" : row[field.AddonField].ToString().Replace("%", "");
                        json.AppendLine($@" ""DiscountPercent"" : ""{discount}"",");
                        json.AppendLine($@" ""FreeText"" : ""{row[field.AddonField].ToString()}"",");

                    }
                    else if (field != null && !col.ColumnName.ToLower().Contains("return") && !column.Contains(col.ColumnName))
                    {
                        json.AppendLine($@" ""{field.SAPFieldId}"" : ""{row[field.AddonField].ToString()}"",");
                    }

                }
                json.AppendLine("   },");
            }
            json.AppendLine("]");
            json.AppendLine("}");
            return json.ToString();
        }

        public static string BuildJsonForMeMo(PostingViewModel model, DataRow Header, DataTable Rows, DataColumnCollection column, string DocEntry)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@" ""CardCode"" : ""MAIN"",");
            json.AppendLine($@" ""U_Remarks"" : ""Uploaded by LinkBox {DateTime.Now}"",");
            //Header
            foreach (DataColumn col in column)
            {
                var field = model.HeaderFields.Where(x => x.AddonField == col.ColumnName).FirstOrDefault();
                if (field != null && col.ColumnName.ToLower().Contains("date"))
                {
                    var docdate = DateTime.ParseExact(Header[field.AddonField].ToString(), "MM/dd/yyyy", null);
                    json.AppendLine($@" ""{field.SAPFieldId}"" : ""{Convert.ToDateTime(docdate).ToString("yyyy-MM-dd")}"",");
                }
                else if (field != null)
                {
                    json.AppendLine($@" ""{field.SAPFieldId}"" : ""{Header[field.AddonField].ToString()}"",");
                }

            }

            //Rows

            json.AppendLine($@" ""DocumentLines"" : [");

            foreach (DataRow row in Rows.Rows)
            {
                json.AppendLine("   {");
                foreach (DataColumn col in Rows.Columns)
                {


                    var field = model.RowFields.Where(x => x.AddonField == col.ColumnName).FirstOrDefault();
                    if (field != null && col.ColumnName.ToLower().Contains("price"))
                    {
                        //var discount = string.IsNullOrEmpty(row[model.RowFields.Where(x => x.AddonField.ToLower().Contains("discount")).FirstOrDefault().AddonField].ToString()) ? 0 :
                        //    Convert.ToDouble(row[model.RowFields.Where(x => x.AddonField.ToLower().Contains("discount")).FirstOrDefault().AddonField].ToString().Replace("%", ""));
                        //var price = Convert.ToDouble(row[field.AddonField].ToString()) -
                        //    ((Convert.ToDouble(row[field.AddonField].ToString()) / 100) * discount);

                        json.AppendLine($@" ""PriceAfterVAT"" : ""{row[field.AddonField].ToString()}"",");
                    }
                    else if (field != null && !col.ColumnName.ToLower().Contains("sold") && !column.Contains(col.ColumnName))
                    {
                        json.AppendLine($@" ""{field.SAPFieldId}"" : ""{row[field.AddonField].ToString()}"",");
                    }

                }
                //json.AppendLine($@" ""BaseEntry"" : ""{DocEntry}"",");
                //json.AppendLine($@" ""BaseType"" : ""13"",");
                json.AppendLine("   },");
            }
            json.AppendLine("]");
            json.AppendLine("}");
            return json.ToString();
        }

        public static string BuildJsonForPayment(PostingViewModel model, DataRow Header, DataTable Cash, DataTable Bank, DataColumnCollection column, string PaymentRef, string RefundRef, string paymentsum, string refundsum)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@" ""CardCode"" : ""MAIN"",");
            json.AppendLine($@" ""U_Remarks"" : ""Uploaded by LinkBox {DateTime.Now}"",");
            //Header
            foreach (DataColumn col in column)
            {
                var field = model.HeaderFields.Where(x => x.AddonField == col.ColumnName).FirstOrDefault();
                if (field != null && col.ColumnName.ToLower().Contains("date"))
                {
                    var docdate = DateTime.ParseExact(Header[field.AddonField].ToString(), "MM/dd/yyyy", null);
                    json.AppendLine($@" ""{field.SAPFieldId}"" : ""{Convert.ToDateTime(docdate).ToString("yyyy-MM-dd")}"",");
                }
                else if (field != null)
                {
                    //var value = field.SAPField.ToLower().Contains("docdate") ? Convert.ToDateTime(Header[field.AddonField].ToString()).ToString("yyyy-MM-dd") : Header[field.AddonField].ToString();
                    //if(field.SAPField.ToLower().Contains("docdate"))
                    //{
                    //     x = Convert.ToDateTime(Header[field.AddonField].ToString()).ToString("yyyy-MM-dd");
                    //}
                    json.AppendLine($@" ""{field.SAPFieldId}"" : ""{Header[field.AddonField].ToString()}"",");
                }

            }
            if (Cash.Rows.Count != 0)
            {
                json.AppendLine($@" ""CashAccount"" : ""110130"",");
                json.AppendLine($@" ""CashSum"" : ""{Cash.Rows[0].ItemArray[0].ToString()}"",");
            }
            //Rows
            if (!string.IsNullOrEmpty(PaymentRef) || !string.IsNullOrEmpty(RefundRef))
            {
                json.AppendLine($@" ""PaymentInvoices"" : [");

                if (!string.IsNullOrEmpty(PaymentRef))
                {
                    json.AppendLine("   {");
                    json.AppendLine($@" ""DocEntry"" : ""{PaymentRef}"",");
                    json.AppendLine($@" ""InvoiceType"" : ""it_Invoice"",");
                    json.AppendLine($@" ""SumApplied"" : ""{paymentsum}"",");

                    json.AppendLine("   },");
                }
                if (!string.IsNullOrEmpty(RefundRef))
                {
                    json.AppendLine("   {");
                    json.AppendLine($@" ""DocEntry"" : ""{PaymentRef}"",");
                    json.AppendLine($@" ""InvoiceType"" : ""it_CredItnote"",");
                    json.AppendLine($@" ""SumApplied"" : ""{refundsum}"",");
                    json.AppendLine("   },");
                }

                json.AppendLine("],");
            }
            if (Bank.Rows.Count != 0)
            {

                json.AppendLine($@" ""PaymentCreditCards"" : [");

                foreach (DataRow row in Bank.Rows)
                {
                    json.AppendLine("   {");
                    json.AppendLine($@" ""CreditCard"" : ""1"",");
                    json.AppendLine($@" ""CreditAcct"" : ""110250"",");
                    json.AppendLine($@" ""CardValidUntil"" : ""2021-12-31"",");
                    json.AppendLine($@" ""CreditSum"" : ""{row["PaymentAmount"].ToString()}"",");
                    json.AppendLine($@" ""CreditCur"" : ""PHP"",");
                    json.AppendLine($@" ""VoucherNum"" : ""1"",");
                    json.AppendLine("   },");
                }
                json.AppendLine("]");
            }
            json.AppendLine("}");
            return json.ToString();
        }

        public static string BuildJsonSAPOrder(DataTable headerdata, DataTable rowsdata, DataTable shippdata, List<PostingViewModel.Fields> DataHeader, List<PostingViewModel.Fields> DataRows, List<DataRow> batchrowdata, DataTable customerinfo)
        {
            ////Declare a list for allocated Batch
            List<BatchAllocatedViewModels.BatchDetail> LBatch = new List<BatchAllocatedViewModels.BatchDetail>();

            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@" ""DocObjectCode"": ""17"",");
            if (DataHeader.Where(sel => sel.SAPFieldId == "DocDate").Any() == false)
            {
                json.AppendLine($@" ""DocDate"": ""{DateTime.Now.ToString("yyyy-MM-dd")}"",");
            }

            if (DataHeader.Where(sel => sel.SAPFieldId == "DocDueDate").Any() == false)
            {
                json.AppendLine($@" ""DocDueDate"": ""{DateTime.Now.ToString("yyyy-MM-dd")}"",");
            }

            //json.AppendLine($@" ""CardCode"" : ""{headerdata.Rows[0]["CardCode"]}"",");
            //json.AppendLine($@" ""CardName"" : ""DKT Shopify"",");
            //json.AppendLine($@" ""U_Remarks"" : ""Uploaded by LinkBox {DateTime.Now}"",");
            json.AppendLine($@" ""U_spUploadText"" : ""Uploaded by Linkbox {DateTime.Now}"",");
            //Header
            foreach (var head in DataHeader)
            {
                if (!string.IsNullOrEmpty(head.DefaultVal))
                {
                    json.AppendLine($@" ""{head.SAPFieldId}"" : ""{head.DefaultVal}"",");
                }
                else if (ColumnExist(headerdata, head.AddonField) && !string.IsNullOrEmpty(head.SAPFieldId))
                {
                    string dataval = head.SAPFieldId.ToLower().Contains("date") ? Convert.ToDateTime(headerdata.Rows[0][$@"{head.AddonField}"].ToString()).ToString("yyyy-MM-dd") : headerdata.Rows[0][$@"{head.AddonField}"].ToString().Replace($@"""", $@"\""");
                    json.AppendLine($@" ""{head.SAPFieldId}"" : ""{dataval}"", ");
                    //json.AppendLine($@" ""{head.SAPFieldId}"" : ""{headerdata.Rows[0][$@"{head.AddonField}"].ToString().Replace($@"""", $@"\""")}"",");
                }
            }

            //Rows
            json.AppendLine($@" ""DocumentLines"" : [");
            foreach (DataRow dr in rowsdata.Rows)
            {
                json.AppendLine("   {");
                foreach (var rows in DataRows)
                {
                    if (ColumnExist(rowsdata, rows.AddonField) && !string.IsNullOrEmpty(rows.SAPFieldId))
                    {
                        if (rows.AddonField.ToLower() == "sku")
                        {
                            ////Nothing difference, this is just to check the value for SKU;
                            json.AppendLine($@" ""{rows.SAPFieldId}"" : ""{dr[$@"{rows.AddonField}"].ToString()}"","); //ITSH-02483 sample only for dbti
                        }
                        else
                        {
                            json.AppendLine($@" ""{rows.SAPFieldId}"" : ""{dr[$@"{rows.AddonField}"].ToString()}"",");
                        }
                    }
                }

                ////DUE TO ERROR, USE ALTERNATIVE WAS AS VIEWMODEL
                json.AppendLine($@" ""SupplierCatNum"": ""{dr["variant_id"].ToString()}"", ");
                json.AppendLine($@" ""VatGroup"": ""{dr["SAPVatGroup"].ToString()}"", ");
                json.AppendLine($@" ""UoMCode"": ""{dr["SAPUoMCode"].ToString()}"", ");
                json.AppendLine($@" ""DistributionRule"": ""{dr["SAPOcrCode"].ToString()}"", ");
                json.AppendLine($@" ""Project"": ""{dr["SAPProject"].ToString()}"", ");

                ////CUSTOMER INFO
                json.AppendLine($@" ""U_API_Vendor"": ""{customerinfo.Rows[0]["first_name"].ToString()} {customerinfo.Rows[0]["last_name"].ToString()}"", ");
                //json.AppendLine($@" ""U_API_TIN"": ""{customerinfo.Rows[0][""].ToString()}"", ");
                json.AppendLine($@" ""U_API_Address"": ""{customerinfo.Rows[0]["note"].ToString()}"", ");

                ////ADD POSTING OF BATCH
                json.AppendLine($@" ""BatchNumbers"": [");

                string Qty = dr[$@"Quantity"].ToString(); //// This is the Order Qty row
                string PerUnit = dr["SAPItemPerUnit"].ToString(); //// This is the Items Per Unit 
                double.TryParse(Qty, out double _Qty);
                double.TryParse(PerUnit, out double _PerUnit);
                double OrderQtyBalance = _Qty * _PerUnit;  ////Initialize the batchQty from Qty order
                var allocatedbatch = LBatch.ToArray();
                var _batchrowdata = batchrowdata.AsEnumerable().Where(x => x["ItemCode"].ToString() == dr[$@"sku"].ToString()).ToList();
                _batchrowdata.ForEach(osrn =>
                {
                    if (OrderQtyBalance > 0)
                    {
                        var BatchOpenQty = osrn["QtyAvailable"].ToString();
                        double.TryParse(BatchOpenQty, out double _BatchOpenQty);
                        var LineBatchQty = ((OrderQtyBalance - _BatchOpenQty) > 0 ? _BatchOpenQty : OrderQtyBalance);
                        var BatchQtyAvailable = ((OrderQtyBalance - _BatchOpenQty) > 0 ? 0 : (_BatchOpenQty - OrderQtyBalance));
                        OrderQtyBalance = ((_BatchOpenQty > OrderQtyBalance) ? 0 : (OrderQtyBalance - _BatchOpenQty));
                        json.AppendLine("   {");
                        json.AppendLine($@" ""BatchNumber"" : ""{osrn["IntrSerial"].ToString()}"",");
                        json.AppendLine($@" ""Quantity"" : {LineBatchQty}");
                        json.AppendLine("   },");

                        ////Exclude fully allocated Batch
                        osrn["QtyAvailable"] = BatchQtyAvailable;
                        ////LBatch.Add(new BatchAllocatedViewModels.BatchDetail { BatchNum = osrn["IntrSerial"].ToString(), Quantity = BatchQty, WhsCode = osrn["WhsCode"].ToString() });
                    }
                });
                json.AppendLine("]");

                json.AppendLine("   },");
            }
            json.AppendLine("],");
            json.AppendLine($@"""DocumentAdditionalExpenses"": [");
            foreach (DataRow dr in shippdata.Rows)
            {
                if (!string.IsNullOrEmpty(dr["SAPExpnsCode"].ToString()))
                {
                    json.AppendLine("   {");
                    json.AppendLine($@"""ExpenseCode"": {dr["SAPExpnsCode"].ToString().Replace($@"""", $@"\""")},");
                    json.AppendLine($@"""LineGross"": ""{dr["price"].ToString()}""");          
                    json.AppendLine("   },");
                }
            }
            json.AppendLine("]");

            json.AppendLine("}");

            return json.ToString();
        }

        public static string BuildJsonZDPostTicket(DataRow headerdata, DataTable rowsdata, List<PostingViewModel.Fields> DataHeader, List<PostingViewModel.Fields> DataRows)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@"   ""ticket"": ");
            json.AppendLine("       {");
            json.AppendLine($@"       ""tags"": [""linkbox""],");
            //Header FROM Excel FILE      
            foreach (var head in DataHeader)
            {
                if (ColumnExist(headerdata, head.SAPFieldId) && !string.IsNullOrEmpty(head.AddonField))
                {
                    ////THE ADDON IS THE API FIELD & THE SAP FIELD FROM FILE TEMPLATE
                    if (!head.AddonField.Contains("custom_field_")) //if not custom field
                    {
                        if (IsDate(headerdata[$@"{head.SAPFieldId}"].ToString()))
                        {
                            json.AppendLine($@" ""{head.AddonField}"" : ""{Convert.ToDateTime(headerdata[$@"{head.SAPFieldId}"].ToString()).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss")}"",");
                        }
                        else
                        {
                            string fieldval = headerdata[$@"{head.SAPFieldId}"].ToString().Replace($@"""", $@"\""");
                            json.AppendLine($@" ""{head.AddonField}"" : ""{fieldval}"",");
                        }
                    }
                }
            }

            //this is to separate the custom_field for chronological order
            json.AppendLine($@" ""custom_fields"": [");
            foreach (var head in DataHeader)
            {
                if (ColumnExist(headerdata, head.SAPFieldId) && !string.IsNullOrEmpty(head.AddonField))
                {
                    if (head.AddonField.Contains("custom_field_"))
                    {
                        json.AppendLine("{" + $@" ""id"": {head.AddonField.Substring(13, head.AddonField.Length - 13)}, ");

                        string fieldval = headerdata[$@"{head.SAPFieldId}"].ToString().Replace(@"""", @"\""");
                        //json.AppendLine($@" ""value"": ""{headerdata[$@"{head.SAPFieldId}"].ToString()}""" + "},");
                        json.AppendLine($@" ""value"": ""{fieldval}""" + "},");
                    }
                }
            }
            json.AppendLine("]");

            json.AppendLine("       }");
            json.AppendLine("}");
            return json.ToString();
        }

        public static string BuildJsonSAPCatalog(string itemcode, string bpcat, string ntegid, string parentcode, string inventoryid, string uomcode, string uomname, string campno = "", string imagefile="", string imageid="")
        {
            ////IT MUST BE PROPER ORDER START FROM ITEMCODE, CARDCODE, SUBSTITUTE
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@" ""ItemCode"": ""{(itemcode == null ? itemcode: itemcode.Replace($@"""", $@"\"""))}"", ");
            json.AppendLine($@" ""CardCode"": ""{(bpcat == null ? bpcat : bpcat.Replace($@"""", $@"\"""))}"", ");
            json.AppendLine($@" ""DisplayBPCatalogNumber"": ""tNO"", ");
            json.AppendLine($@" ""Substitute"": ""{(ntegid == null ? ntegid : ntegid.Replace($@"""", $@"\"""))}"", ");
            json.AppendLine($@" ""U_spParentCode"": ""{(parentcode == null ? parentcode : parentcode.Replace($@"""", $@"\"""))}"", ");
            if (!string.IsNullOrEmpty(inventoryid)) json.AppendLine($@" ""U_spInventoryId"": ""{(inventoryid == null ? inventoryid : inventoryid.Replace($@"""", $@"\"""))}"", ");
            if (!string.IsNullOrEmpty(uomcode)) json.AppendLine($@" ""U_spUomCode"": ""{(uomcode == null ? uomcode : uomcode.Replace($@"""", $@"\"""))}"", ");
            if (!string.IsNullOrEmpty(uomname)) json.AppendLine($@" ""U_spUomName"": ""{(uomname == null ? uomname : uomname.Replace($@"""", $@"\"""))}"", ");
            if (!string.IsNullOrEmpty(campno)) json.AppendLine($@" ""U_spCampNo"": ""{(campno == null ? campno : campno.Replace($@"""", $@"\"""))}"", ");
            if (!string.IsNullOrEmpty(imagefile)) json.AppendLine($@" ""U_spImage"": ""{(imagefile == null ? imagefile : imagefile.Replace($@"""", $@"\"""))}"", ");
            if (!string.IsNullOrEmpty(imageid)) json.AppendLine($@" ""U_spImageId"": ""{(imageid == null ? imageid : imageid.Replace($@"""", $@"\"""))}"", ");
            json.AppendLine($@"""U_spIncluded"": ""YES"",");
            json.AppendLine("}");
            return json.ToString();
        }
        public static string BuildJsonSAPUdoItemUoM(string Code, string Name, PostingViewModel.SapUdoItemUoM HeaderFields)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@" ""Code"": ""{(Code == null ? Code: Code.Replace($@"""", $@"\"""))}"", ");
            json.AppendLine($@" ""Name"": ""{(Name == null ? Name: Name.Replace($@"""", $@"\"""))}"", ");
            json.AppendLine($@" ""U_spCardCode"": ""{(HeaderFields.U_spCardCode == null ? HeaderFields.U_spCardCode : HeaderFields.U_spCardCode.Replace($@"""", $@"\"""))}"",");
            json.AppendLine($@" ""U_spCardName"": ""{(HeaderFields.U_spCardName == null ? HeaderFields.U_spCardName : HeaderFields.U_spCardName.Replace($@"""", $@"\"""))}"",");
            json.AppendLine($@" ""U_spItemName"": ""{(HeaderFields.U_spItemName == null ? HeaderFields.U_spItemName : HeaderFields.U_spItemName.Replace($@"""", $@"\"""))}"",");
            json.AppendLine($@" ""SPITEMUOMDCollection"": [");
            int cnt = 1;
            foreach (var row in HeaderFields.SPITEMUOMDCollection)
            {
                json.AppendLine("   {");
                json.AppendLine($@" ""Code"": ""{(row.Code == null ? row.Code : row.Code.Replace($@"""", $@"\"""))}"", ");
                json.AppendLine($@" ""LineId"": ""{row.LineId}"", ");
                json.AppendLine($@" ""U_spItemCode"": ""{(row.U_spItemCode == null ? row.U_spItemCode : row.U_spItemCode.Replace($@"""", $@"\"""))}"", ");
                json.AppendLine($@" ""U_spUomCode"": ""{(row.U_spUomCode == null ? row.U_spUomCode : row.U_spUomCode.Replace($@"""", $@"\"""))}"", ");
                json.AppendLine($@" ""U_spUomName"": ""{(row.U_spUomName == null ? row.U_spUomName : row.U_spUomName.Replace($@"""", $@"\"""))}"", ");
                json.AppendLine($@" ""U_spSubstitute"": ""{(row.U_spSubstitute == null ? row.U_spSubstitute : row.U_spSubstitute.Replace($@"""", $@"\"""))}"", ");
                json.AppendLine($@" ""U_spInventoryId"": ""{(row.U_spInventoryId == null ? row.U_spInventoryId : row.U_spInventoryId.Replace($@"""", $@"\"""))}"" ");
                json.AppendLine("   },");
                cnt = row.LineId + 1; //To pass the last value on below RowFields
            }
            //////THE NEW ADDED ROW
            //foreach (var row in RowFields)
            //{
            //    json.AppendLine("   {");
            //    json.AppendLine($@" ""Code"": ""{(row.Code == null ? row.Code : row.Code.Replace($@"""", $@"\"""))}"", ");
            //    json.AppendLine($@" ""LineId"": ""{cnt}"", ");
            //    json.AppendLine($@" ""U_spItemCode"": ""{(row.U_spItemCode == null ? row.U_spItemCode : row.U_spItemCode.Replace($@"""", $@"\"""))}"", ");
            //    json.AppendLine($@" ""U_spUomCode"": ""{(row.U_spUomCode == null ? row.U_spUomCode : row.U_spUomCode.Replace($@"""", $@"\"""))}"", ");
            //    json.AppendLine($@" ""U_spUomName"": ""{(row.U_spUomName == null ? row.U_spUomName : row.U_spUomName.Replace($@"""", $@"\"""))}"", ");
            //    json.AppendLine($@" ""U_spSubstitute"": ""{(row.U_spSubstitute == null ? row.U_spSubstitute: row.U_spSubstitute.Replace($@"""", $@"\"""))}"", ");
            //    json.AppendLine($@" ""U_spInventoryId"": ""{(row.U_spInventoryId == null ? row.U_spInventoryId : row.U_spInventoryId.Replace($@"""", $@"\"""))}"" ");
            //    json.AppendLine("   },");
            //    cnt++;
            //}

            json.AppendLine("   ]");
            json.AppendLine("}");
            return json.ToString();
        }

        public static string BuildJsonShopiPostProduct(string Entity, DataRow Headerdata, List<PostingViewModel.Fields> DataHeader)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@"   ""{Entity.ToLower().Substring(0, Entity.Length - 1)}"": "); //singular
            json.AppendLine("       {");

            //Header FROM Excel FILE      
            foreach (var head in DataHeader)
            {
                if (ColumnExist(Headerdata, head.SAPFieldId) && !string.IsNullOrEmpty(head.AddonField))
                {
                    ////THE ADDON IS THE API FIELD                     
                    json.AppendLine($@" ""{head.AddonField}"" : ""{Headerdata[$@"{head.SAPFieldId}"].ToString().Replace($@"""", $@"\""")}"",");

                }
            }

            json.AppendLine("       }");
            json.AppendLine("}");
            return json.ToString();
        }

        public static string BuildJsonShopiPostVariant(string Entity, DataRow Rowsdata, List<PostingViewModel.Fields> DataRows, bool MultiUom)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@"   ""{Entity.ToLower().Substring(0, Entity.Length - 1)}"": ");
            json.AppendLine("       {");
            if (MultiUom == true) 
            { json.AppendLine($@"     ""option1"" : ""{Rowsdata["spUoMName"].ToString().Replace($@"""", $@"\""")}"", ");  }
            else { json.AppendLine($@"     ""option1"" : ""{Rowsdata["FrgnName"].ToString().Replace($@"""", $@"\""")}"", "); }
            foreach (var head in DataRows)
            {
                if (ColumnExist(Rowsdata, head.SAPFieldId) && !string.IsNullOrEmpty(head.AddonField))
                {                             
                    ////Disregard if option field
                    if (head.AddonField != "option1")
                    {
                        ////THE ADDON IS THE API FIELD   
                        json.AppendLine($@" ""{head.AddonField}"" : ""{Rowsdata[$@"{head.SAPFieldId}"].ToString().Replace($@"""", $@"\""")}"",");                        
                    }
                }
            }
            json.AppendLine($@"       ""price"": ""{Rowsdata["Price"].ToString()}"" ");
            json.AppendLine("       }");
            json.AppendLine("}");
            return json.ToString();
        }
        public static string BuildJsonShopiDefaultVariant(string Entity, DataRow Rowsdata)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@"   ""variant"": "); ////Make it a singular word
            json.AppendLine("       {");
            json.AppendLine($@"       ""sku"": ""{Rowsdata["ItemCode"].ToString().Replace($@"""", $@"\""")}"", ");
            json.AppendLine($@"       ""price"": ""{Rowsdata["Price"].ToString().Replace($@"""", $@"\""")}"", ");
            json.AppendLine($@"       ""inventory_management"": ""shopify"" "); //To enable the Track Quantity on Inventory
            json.AppendLine("       }");
            json.AppendLine("}");
            return json.ToString();
        }
    }
}