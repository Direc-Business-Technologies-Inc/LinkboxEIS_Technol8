using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer.Models;
using NPOI.OpenXml4Net.Exceptions;
//using SAPbobsCOM;
//using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using static DomainLayer.ViewModels.MapCreateViewModel;

namespace DataAccessLayer.Class
{

    public class QueryAccess
    {
        public static string con(string AddonDBName, string IPAddress, int? Port, string Server, string User, string Password)
        {
            //create database if not exist
            var output = new StringBuilder();
            output.Append($"Data Source={Server};");
            output.Append("Initial Catalog=master;");
            output.Append($"User Id={User};");
            output.Append($"Password={Password};");

            SqlConnection cnn;
            cnn = new SqlConnection(output.ToString());
            string sqlCreateDBQuery = $"SELECT database_id FROM sys.databases WHERE Name = '{AddonDBName}'";
            using (SqlCommand cmd = new SqlCommand(sqlCreateDBQuery, cnn))
            {
                try
                {
                    cnn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            string Createscript = $"CREATE DATABASE {AddonDBName}";
                            using (SqlCommand cmdcreate = new SqlCommand(Createscript, cnn))
                            {
                                reader.Close();
                                cmdcreate.ExecuteNonQuery();
                            }
                        }
                    }
                    cnn.Close();
                }
                catch (Exception ex)
                {

                    throw;
                }

            }
            return output.ToString();
        }

        public static string conHana(string NewDatabase, string Database, string IPAddress, int? Port, string Server, string User, string Password)
        {
            //create database if not exist
            var output = new StringBuilder();
            output.Append("DRIVER={HDBODBC32};");
            output.Append($"SERVERNODE={Server}:30015;");
            output.Append($"UID={User};");
            output.Append($"PWD={Password};");
            output.Append($@"CS=""{Database}"";");

            //HanaConnection cnn;
            //cnn = new HanaConnection(output.ToString());
            //string sqlCreateDBQuery = $"SELECT * FROM SCHEMAS WHERE SCHEMA_NAME = '{NewDatabase}'";
            try
            {
                //using (HanaCommand cmd = new HanaCommand(sqlCreateDBQuery, cnn))
                //{
                //    cnn.Open();
                //    using (HanaDataReader reader = cmd.ExecuteReader())
                //    {
                //        if (!reader.HasRows)
                //        {
                //            string Createscript = $@"CREATE SCHEMA ""{NewDatabase}""";
                //            using (HanaCommand cmdcreate = new HanaCommand(Createscript, cnn))
                //            {
                //                reader.Close();
                //                cmdcreate.ExecuteNonQuery();
                //            }
                //        }
                //    }
                //    cnn.Close();
                //}
                return output.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public static string CreateTable(string TableName, string SAPDBName, string Database, int? Port, string Server, string User, string Password)
        {
            //create database if not exist
            var output = new StringBuilder();
            output.Append($"Data Source={Server};");
            output.Append("Initial Catalog = master;");
            output.Append($"User Id={User};");
            output.Append($"Password={Password};");

            SqlConnection cnn;
            //cnn = new SqlConnection($@"Data Source={Server};Initial Catalog=master;User ID={User};Password={Password}");
            cnn = new SqlConnection(output.ToString());
            string checktable = $"SELECT * FROM {Database}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{SAPDBName}_{TableName}'";
            using (SqlCommand cmd = new SqlCommand(checktable, cnn))
            {
                cnn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        string Createscript = $"USE [{Database}] CREATE TABLE[dbo].[{SAPDBName}_{TableName}]([DocEntry][NVARCHAR](254) NOT NULL CONSTRAINT [PK_dbo.{SAPDBName}_{TableName}] )";
                        using (SqlCommand cmdcreate = new SqlCommand(Createscript, cnn))
                        {
                            reader.Close();
                            cmdcreate.ExecuteNonQuery();
                        }
                    }
                }
                cnn.Close();
            }
            //return $@"Data Source={Server};Initial Catalog={Database};User ID={User};Password={Password}";
            return output.ToString();
        }

        public static string CreateTableHana(string TableName, string SAPDBName, string Database, int? Port, string Server, string User, string Password)
        {
            //create database if not exist
            var output = new StringBuilder();
            output.Append("DRIVER={HDBODBC32};");
            output.Append($"SERVERNODE={Server}:30015;");
            output.Append($"UID={User};");
            output.Append($"PWD={Password};");
            output.Append($@"CS=""{Database}"";");

            //2023/07/28 - I commented this because Error in posting in PCII

            //HanaConnection cnn;
            ////cnn = new HanaConnection("DRIVER={HDBODBC32};SERVERNODE=" + Server + ":30015;UID=" + User + ";PWD=" + Password + ";CS=" + Database);
            //cnn = new HanaConnection(output.ToString());
            //string checktable = $@"SELECT TABLE_NAME FROM M_CS_TABLES WHERE SCHEMA_NAME = '{Database}' AND TABLE_NAME = '{SAPDBName}_{TableName}';";
            //using (HanaCommand cmd = new HanaCommand(checktable, cnn))
            //{
            //    cnn.Open();
            //    using (HanaDataReader reader = cmd.ExecuteReader())
            //    {
            //        if (!reader.HasRows)
            //        {
            //            string Createscript = $@"CREATE COLUMN TABLE ""{Database}"".""{SAPDBName}_{TableName}"" (""DocEntry"" NVARCHAR(254) NOT NULL)";
            //            using (HanaCommand cmdcreate = new HanaCommand(Createscript, cnn))
            //            {
            //                reader.Close();
            //                cmdcreate.ExecuteNonQuery();
            //            }
            //        }
            //    }
            //    cnn.Close();
            //}
            //return "DRIVER={HDBODBC32};SERVERNODE=" + Server + ":30015;UID=" + User + ";PWD=" + Password + ";CS=" + Database;
            return output.ToString();
        }

        public static string con2(string AddonDBName, string IPAddress, int? Port, string Server, string User, string Password)
        { return $@"Data Source={Server};Initial Catalog={AddonDBName};User ID={User};Password={Password}"; }

        public static int getTransId(string constr, string headtbname, string dbname)
        {
            SqlConnection cnn;
            cnn = new SqlConnection(constr);
            int TransId = 0;
            string GetId = $"SELECT ISNULL(MAX(TransactionId), 0) FROM[{dbname}].[dbo].[{headtbname}]";
            using (SqlCommand cmd = new SqlCommand(GetId, cnn))
            {

                cnn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {

                            TransId = reader.GetInt32(0);

                        }
                    }
                }
                cnn.Close();
            }
            return TransId + 1;
        }

        public static void GenerateTable(string AddonDB, string constr, List<string[]> HeaderVal, List<string[]> RowVal, string RowTable, string HeaderTable, int HeadCount, int RowCount)
        {
            var NewHead = HeaderVal;
            var NewRow = RowVal;

            //for (int i = 0; i < HeadCount; i++)
            //{
            //    if (NewHead.Count > 0)
            //    {
            //        NewHead.RemoveAt(0);
            //    }                
            //}

            //for (int i = 0; i < RowCount; i++)
            //{
            //    if (NewRow.Count > 0)
            //    {
            //        NewRow.RemoveAt(0);
            //    }                
            //}

            string query = "", rowquery = "", headerquery = "";
            SqlConnection cnn;
            cnn = new SqlConnection(constr);

            #region OldVersionUpdateTableColumn
            //if (NewHead != null || NewRow != null)
            //{
            //    if (NewHead != null && NewHead.Count != 0 && HeadCount != 0)
            //    {
            //        foreach (var item in NewHead)
            //        {
            //            var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
            //            string length = item[2].Contains("Int") || item[2].Contains("Date") ? "" : $"({len})";
            //            headerquery += $" [{item[1]}] [{item[2]}]{length}";
            //        }
            //        string addheader = $"alter table [Data_{HeaderTable}] add {headerquery} ";
            //        try
            //        {
            //            using (SqlCommand cmd = new SqlCommand(addheader, cnn))
            //            {
            //                cmd.Connection.Open();
            //                cmd.ExecuteNonQuery();
            //                cmd.Connection.Close();
            //            }
            //        }
            //        catch { 

            //        }

            //    }

            //    if (NewRow != null && NewRow.Count != 0 && RowCount != 0)
            //    {
            //        foreach (var item in NewRow)
            //        {
            //            var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
            //            string length = item[2].Contains("Int") || item[2].Contains("Date") ? "" : $"({len})";
            //            rowquery += $" [{item[1]}] [{item[2]}]{length}";
            //        }
            //        string addRow = $"alter table [Data_{RowTable}] add {rowquery} ";
            //        try
            //        {
            //            using (SqlCommand cmd = new SqlCommand(addRow, cnn))
            //            {
            //                cmd.Connection.Open();
            //                cmd.ExecuteNonQuery();
            //                cmd.Connection.Close();
            //            }
            //        }
            //        catch { }

            //    }
            //}
            #endregion

            if (HeaderTable != null && HeaderTable != "")
            {

                string checktable = $"SELECT * FROM {AddonDB}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Data_{HeaderTable}'";
                using (SqlCommand cmd = new SqlCommand(checktable, cnn))
                {
                    cnn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            foreach (var item in HeaderVal)
                            {
                                var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
                                string length = item[2].Contains("INT") || item[2].Contains("DATE") ? "" : $"({len})";
                                if (item[1] != "") query += $", [{item[1]}] [{item[2]}]{length} {item[4]}";
                            }
                            headerquery = $"USE [{AddonDB}] CREATE TABLE[dbo].[Data_{HeaderTable}]([Col_Id] [int] IDENTITY(1,1) NOT NULL,[Col_TransactionId] [int],[Col_UploadDate] [datetime],[Col_Status] [char](1),[Col_Message] [nvarchar](255) {query} CONSTRAINT [PK_dbo.{HeaderTable}] " +
                            $"PRIMARY KEY CLUSTERED([Col_Id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]";

                            try
                            {
                                using (SqlCommand cmd1 = new SqlCommand(headerquery, cnn))
                                {
                                    reader.Close();
                                    cmd1.ExecuteNonQuery();
                                }
                            }
                            catch { }

                        }
                        else
                        {
                            ////DLN : MUST HAVE ALTER TABLE TO UPDATE THE NEW AND DELETED COLUMN MAPPING
                            foreach (var item in HeaderVal)
                            {
                                if (item[1] != "")
                                {
                                    var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
                                    string length = item[2].Contains("INT") || item[2].Contains("DATE") ? "" : $"({len})";
                                    var isCol = CheckTableColumn(constr, $@"Data_{HeaderTable}", item[1]);
                                    if (isCol == false)
                                    {

                                        headerquery = $"USE [{AddonDB}] ALTER TABLE [dbo].[Data_{HeaderTable}] ADD {item[1]} {item[2]} {length}; ";
                                        using (SqlCommand cmd1 = new SqlCommand(headerquery, cnn))
                                        {
                                            reader.Close();
                                            cmd1.ExecuteNonQuery();
                                        }

                                    }
                                    else
                                    {
                                        headerquery = $"USE [{AddonDB}] ALTER TABLE [dbo].[Data_{HeaderTable}] ALTER COLUMN {item[1]} {item[2]} {length}; ";
                                        using (SqlCommand cmd1 = new SqlCommand(headerquery, cnn))
                                        {
                                            reader.Close();
                                            cmd1.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    cnn.Close();
                }


            }
            query = "";
            if (RowTable != null && RowTable != "")
            {


                string checktable = $"SELECT * FROM {AddonDB}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Data_{RowTable}'";
                using (SqlCommand cmd = new SqlCommand(checktable, cnn))
                {
                    cnn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            foreach (var x in RowVal)
                            {

                                foreach (var item in RowVal)
                                {
                                    var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
                                    string length = item[2].Contains("INT") || item[2].Contains("DATE") ? "" : $"({len})";
                                    if (item[1] != "") query += $", [{item[1]}] [{item[2]}]{length} {item[4]}";
                                }
                                rowquery = $"USE [{AddonDB}] CREATE TABLE[dbo].[Data_{RowTable}]([Col_Id] [int] IDENTITY(1,1) NOT NULL,[Col_TransactionId] [int],[Col_Status] [char](1),[Col_Message] [nvarchar](255) {query} CONSTRAINT [PK_dbo.{RowTable}] " +
                                $"PRIMARY KEY CLUSTERED([Col_Id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]";

                                try
                                {
                                    using (SqlCommand cmd1 = new SqlCommand(rowquery, cnn))
                                    {
                                        reader.Close();
                                        cmd1.ExecuteNonQuery();
                                    }
                                }
                                catch { }
                            }

                        }
                        else
                        {
                            ////DLN : MUST HAVE ALTER TABLE TO UPDATE THE NEW AND DELETED COLUMN MAPPING
                            foreach (var item in RowVal)
                            {
                                if (item[1] != "")
                                {
                                    string length = item[2].Contains("INT") || item[2].Contains("DATE") ? "" : $"({item[3]})";
                                    var isCol = CheckTableColumn(constr, $@"Data_{RowTable}", item[1]);
                                    if (isCol == false)
                                    {
                                        try
                                        {
                                            rowquery = $"USE [{AddonDB}] ALTER TABLE [dbo].[Data_{RowTable}] ADD {item[1]} {item[2]} {length}; ";
                                            using (SqlCommand cmd1 = new SqlCommand(rowquery, cnn))
                                            {
                                                reader.Close();
                                                cmd1.ExecuteNonQuery();
                                            }
                                        }
                                        catch { }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            rowquery = $"USE [{AddonDB}] ALTER TABLE [dbo].[Data_{RowTable}] ALTER COLUMN {item[1]} {item[2]} {length}; ";
                                            using (SqlCommand cmd1 = new SqlCommand(rowquery, cnn))
                                            {
                                                reader.Close();
                                                cmd1.ExecuteNonQuery();
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                    cnn.Close();
                }
            }


        }


        public static void GenerateRowTable(string AddonDB, string constr, List<string[]> RowVal, List<DataRow> TableList, string RowTable, int RowCount, List<DataRow> RowsPrimaryKey)
        {
            var NewRow = RowVal;

            string query = "", rowquery = "", headerquery = "";
            SqlConnection cnn;
            cnn = new SqlConnection(constr);

            foreach (var x in TableList)
            {
                query = "";
                if (x["TABLE_NAME"] != null && x["TABLE_NAME"].ToString() != "")
                {
                    string checktable = $"SELECT * FROM {AddonDB}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{x["TABLE_NAME"].ToString()}'";
                    using (SqlCommand cmd = new SqlCommand(checktable, cnn))
                    {
                        cnn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                foreach (var item in RowVal)
                                {
                                    if (x["TABLE_NAME"].ToString() == item[5])
                                    {
                                        var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
                                        if (!len.Contains(","))
                                            len = Convert.ToInt32(len == "" ? "0" : len.ToString()) > 8000 ? "8000" : len;
                                        string length = item[2].Contains("INT") || item[2].Contains("DATE") || item[2].Contains("TIME") || item[2].Contains("NCLOB") ? "" : $"({len})";
                                        string datatype = item[2].Contains("DECIMAL") ? "NUMERIC" : item[2];
                                        if (item[2].Contains("TIME"))
                                            datatype = "DATETIME";
                                        if (item[2].Contains("NCLOB"))
                                            datatype = "NTEXT";
                                        if (item[1] != "") query += $", [{item[1]}] [{datatype}]{length} {item[4]}";
                                    }
                                }

                                string PrimaryKeyQry = "PRIMARY KEY CLUSTERED(#) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]";
                                string keyqry = "";
                                foreach (var item in RowsPrimaryKey)
                                {
                                    if (item["TABLE_NAME"].ToString() == x["TABLE_NAME"].ToString())
                                        keyqry += $" [{item["COLUMN_NAME"].ToString()}] ASC,";
                                }
                                rowquery = $"USE [{AddonDB}] CREATE TABLE[dbo].[{x["TABLE_NAME"].ToString()}]({query.Remove(0, 1)} CONSTRAINT [PK_dbo.{x["TABLE_NAME"].ToString()}] " +
                                $"{(keyqry == "" ? ")" : PrimaryKeyQry.Replace("#", keyqry.Remove(keyqry.Length - 1, 1)))}";
                                //rowquery = $"USE [{AddonDB}] CREATE TABLE[dbo].[{x["TABLE_NAME"].ToString()}]({query.Remove(0, 1)})";
                                try
                                {
                                    using (SqlCommand cmd1 = new SqlCommand(rowquery, cnn))
                                    {
                                        reader.Close();
                                        cmd1.ExecuteNonQuery();
                                    }
                                }
                                catch (Exception ex) { }
                            }

                            else
                            {
                                ////DLN : MUST HAVE ALTER TABLE TO UPDATE THE NEW AND DELETED COLUMN MAPPING
                                foreach (var item in RowVal)
                                {
                                    if (x["TABLE_NAME"].ToString() == item[5])
                                    {
                                        if (item[1] != "")
                                        {
                                            var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
                                            if (!len.Contains(","))
                                                len = Convert.ToInt32(len == "" ? "0" : len.ToString()) > 8000 ? "8000" : len;
                                            string length = item[2].Contains("INT") || item[2].Contains("DATE") || item[2].Contains("TIME") || item[2].Contains("NCLOB") ? "" : $"({len})";
                                            string datatype = item[2].Contains("DECIMAL") ? "NUMERIC" : item[2];
                                            if (item[2].Contains("TIME"))
                                                datatype = "DATETIME";
                                            if (item[2].Contains("NCLOB"))
                                                datatype = "NTEXT";
                                            var isCol = CheckTableColumn(constr, $@"{x["TABLE_NAME"].ToString()}", item[1]);
                                            if (isCol == false)
                                            {
                                                try
                                                {
                                                    rowquery = $"USE [{AddonDB}] ALTER TABLE [dbo].[{x["TABLE_NAME"].ToString()}] ADD {item[1]} {datatype} {length}; ";
                                                    using (SqlCommand cmd1 = new SqlCommand(rowquery, cnn))
                                                    {
                                                        reader.Close();
                                                        cmd1.ExecuteNonQuery();
                                                    }
                                                }
                                                catch (Exception ex) { }
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    rowquery = $"USE [{AddonDB}] ALTER TABLE [dbo].[{x["TABLE_NAME"].ToString()}] ALTER COLUMN {item[1]} {datatype} {length}; ";
                                                    using (SqlCommand cmd1 = new SqlCommand(rowquery, cnn))
                                                    {
                                                        reader.Close();
                                                        cmd1.ExecuteNonQuery();
                                                    }
                                                }
                                                catch (Exception ex) { }
                                            }
                                        }
                                    }

                                }

                            }
                        }
                        cnn.Close();
                    }
                }
            }

        }
        public static void GenerateHeaderTable(string AddonDB, string constr, List<string[]> HeaderVal, string HeaderTable, int HeadCount, List<DataRow> HeaderPrimaryKey)
        {
            try
            {
                var NewHead = HeaderVal;

                string query = "", rowquery = "", headerquery = "";
                SqlConnection cnn;
                cnn = new SqlConnection(constr);

                if (HeaderTable != null && HeaderTable != "")
                {

                    string checktable = $"SELECT * FROM {AddonDB}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{HeaderTable}'";
                    using (SqlCommand cmd = new SqlCommand(checktable, cnn))
                    {
                        cnn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                foreach (var item in HeaderVal)
                                {
                                    var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
                                    if (!len.Contains(","))
                                        len = Convert.ToInt32(len == "" ? "0" : len.ToString()) > 8000 ? "8000" : len;
                                    string length = item[2].Contains("INT") || item[2].Contains("DATE") || item[2].Contains("TIME") || item[2].Contains("NCLOB") ? "" : $"({len})";
                                    string datatype = item[2].Contains("DECIMAL") ? "NUMERIC" : item[2];
                                    if (item[2].Contains("TIME"))
                                        datatype = "DATETIME";
                                    if (item[2].Contains("NCLOB"))
                                        datatype = "NTEXT";
                                    if (item[1] != "") query += $", [{item[1]}] [{datatype}]{length} {item[4]}";
                                }
                                string PrimaryKeyQry = "PRIMARY KEY CLUSTERED(#) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]";
                                string keyqry = "";
                                foreach (var item in HeaderPrimaryKey)
                                {
                                    keyqry += $" [{item["COLUMN_NAME"].ToString()}] ASC,";
                                }
                                headerquery = $"USE [{AddonDB}] CREATE TABLE[dbo].[{HeaderTable}]({query.Remove(0, 1)} CONSTRAINT [PK_dbo.{HeaderTable}] " +
                                $"{(keyqry == "" ? ")" : PrimaryKeyQry.Replace("#", keyqry.Remove(keyqry.Length - 1, 1)))}";
                                //headerquery = $"USE [{AddonDB}] CREATE TABLE[dbo].[{HeaderTable}]({query.Remove(0, 1)})";
                                try
                                {
                                    using (SqlCommand cmd1 = new SqlCommand(headerquery, cnn))
                                    {
                                        reader.Close();
                                        cmd1.ExecuteNonQuery();
                                    }
                                }
                                catch (Exception ex) { }

                            }
                            else
                            {
                                ////DLN : MUST HAVE ALTER TABLE TO UPDATE THE NEW AND DELETED COLUMN MAPPING
                                foreach (var item in HeaderVal)
                                {
                                    if (item[1] != "")
                                    {
                                        var len = string.IsNullOrEmpty(item[3]) ? "250" : item[3];
                                        if (!len.Contains(","))
                                            len = Convert.ToInt32(len == "" ? "0" : len.ToString()) > 8000 ? "8000" : len;
                                        string length = item[2].Contains("INT") || item[2].Contains("DATE") || item[2].Contains("TIME") || item[2].Contains("NCLOB") ? "" : $"({len})";
                                        string datatype = item[2].Contains("DECIMAL") ? "NUMERIC" : item[2];
                                        if (item[2].Contains("TIME"))
                                            datatype = "DATETIME";
                                        if (item[2].Contains("NCLOB"))
                                            datatype = "NTEXT";
                                        var isCol = CheckTableColumn(constr, $@"{HeaderTable}", item[1]);
                                        if (isCol == false)
                                        {

                                            headerquery = $"USE [{AddonDB}] ALTER TABLE [dbo].[{HeaderTable}] ADD {item[1]} {datatype} {length}; ";
                                            try
                                            {
                                                using (SqlCommand cmd1 = new SqlCommand(headerquery, cnn))
                                                {
                                                    reader.Close();
                                                    cmd1.ExecuteNonQuery();
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                            }


                                        }
                                        else
                                        {
                                            headerquery = $"USE [{AddonDB}] ALTER TABLE [dbo].[{HeaderTable}] ALTER COLUMN {item[1]} {datatype} {length}; ";
                                            try
                                            {
                                                using (SqlCommand cmd1 = new SqlCommand(headerquery, cnn))
                                                {
                                                    reader.Close();
                                                    cmd1.ExecuteNonQuery();
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                            }

                                        }
                                    }
                                }
                            }
                        }
                        cnn.Close();
                    }


                }


            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public static string HANA_conString(string Server, string User, string Password, string Database)
        {
            //return "DRIVER={HDBODBC32};SERVERNODE=" + Server + ":30015;UID=" + User + ";PWD=" + Password + ";CS=" + Database; 
            var output = new StringBuilder();
            output.Append("DRIVER={HDBODBC32};");
            output.Append($"SERVERNODE={Server}:30015;");
            output.Append($"UID={User};");
            output.Append($"PWD={Password};");
            output.Append($@"CS=""{Database}"";");
            return output.ToString();
        }


        public static string MSSQL_conString(string Server, string User, string Password, string Database)
        {
            //return "Server=" + Server + ";Database=" + Database + ";User Id=" + User + ";Password=" + Password;
            var output = new StringBuilder();
            output.Append($"Server={Server};");
            output.Append($@"Database=""{Database}"";");
            output.Append($"User Id={User};");
            output.Append($"Password={Password};");
            return output.ToString();
        }


        //public static Tuple<List<string>, List<string>> connHana(string constr, List<string> tables, string dbname, string dbtype)
        //{
        //    string rowquery = "", headerquery = "";
        //    List<string> Headerfields = new List<string>();
        //    List<string> Rowfields = new List<string>();
        //    if (dbtype.Contains("HANA"))
        //    {
        //        //HanaConnection cnn;
        //        //cnn = new HanaConnection(constr);
        //        //headerquery = $@"SELECT COLUMN_NAME FROM SYS.COLUMNS WHERE SCHEMA_NAME = '{dbname}' AND TABLE_NAME = '{tables[0]}' 
        //        //                 UNION ALL 
        //        //                 SELECT COLUMN_NAME FROM SYS.COLUMNS WHERE SCHEMA_NAME = '{dbname}' AND TABLE_NAME = '{tables[1]}'";

        //        ////rowquery = $"SELECT COLUMN_NAME FROM SYS.COLUMNS WHERE SCHEMA_NAME = '{dbname}' AND TABLE_NAME = '{tables[1]}' ORDER BY COLUMN_NAME";
        //        //using (HanaCommand cmd = new HanaCommand(headerquery, cnn))
        //        //{
        //        //    cmd.Connection.Open();
        //        //    HanaDataReader read = cmd.ExecuteReader();
        //        //    while (read.Read())
        //        //    {
        //        //        Headerfields.Add(read.GetString(0));
        //        //    }
        //        //    read.Close();
        //        //    cmd.Connection.Close();
        //        //}
        //        //using (HanaCommand cmd = new HanaCommand(headerquery, cnn))
        //        //{
        //        //    cmd.Connection.Open();
        //        //    HanaDataReader read = cmd.ExecuteReader();
        //        //    while (read.Read())
        //        //    {
        //        //        Rowfields.Add(read.GetString(0));
        //        //    }
        //        //    read.Close();
        //        //    cmd.Connection.Close();
        //        //}
        //    }
        //    else
        //    {
        //        SqlConnection cnn;
        //        cnn = new SqlConnection(constr);
        //        headerquery = $@"SELECT COLUMN_NAME FROM {dbname}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = '{dbname}' AND TABLE_NAME = '{tables[0]}' 
        //                        UNION ALL 
        //                        SELECT COLUMN_NAME FROM {dbname}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = '{dbname}' AND TABLE_NAME = '{tables[1]}' ";

        //        //rowquery = $@"SELECT COLUMN_NAME FROM {dbname}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = '{dbname}' AND TABLE_NAME = '{tables[1]}' ORDER BY COLUMN_NAME";
        //        using (SqlCommand cmd = new SqlCommand(headerquery, cnn))
        //        {
        //            cmd.Connection.Open();
        //            SqlDataReader read = cmd.ExecuteReader();
        //            while (read.Read())
        //            {
        //                Headerfields.Add(read.GetString(0));
        //            }
        //            read.Close();
        //            cmd.Connection.Close();
        //        }
        //        using (SqlCommand cmd = new SqlCommand(headerquery, cnn))
        //        {
        //            cmd.Connection.Open();
        //            SqlDataReader read = cmd.ExecuteReader();
        //            while (read.Read())
        //            {
        //                Rowfields.Add(read.GetString(0));
        //            }
        //            read.Close();
        //            cmd.Connection.Close();
        //        }
        //    }

        //    return Tuple.Create(Headerfields, Rowfields);
        // }


        public static void UploadtoAddon(string con, Tuple<DataTable, DataTable> Data, string HeaderName, string RowName)
        {
            SqlConnection cnn;
            cnn = new SqlConnection(con);
            using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(cnn))
            {
                //Set the database table name
                sqlBulkCopy.DestinationTableName = $"dbo.{HeaderName}";
                foreach (var col in Data.Item1.Columns)
                {
                    sqlBulkCopy.ColumnMappings.Add(col.ToString(), col.ToString());
                }
                cnn.Open();
                sqlBulkCopy.WriteToServer(Data.Item1);
                cnn.Close();
            }

            using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(cnn))
            {
                //Set the database table name
                sqlBulkCopy.DestinationTableName = $"dbo.{RowName}";
                foreach (var col in Data.Item2.Columns)
                {
                    sqlBulkCopy.ColumnMappings.Add(col.ToString(), col.ToString());
                }
                cnn.Open();
                sqlBulkCopy.WriteToServer(Data.Item2);
                cnn.Close();
            }
        }
        public static bool CheckCon(string con, string hanaorsql)
        {
            if (hanaorsql.Contains("HANA"))
            {
                //HanaConnection cnn;
                //cnn = new HanaConnection(con);

                //try
                //{
                //    cnn.Open();
                //    return true;
                //}
                //catch
                //{
                //    cnn.Close();
                return false;
                //}
            }
            else
            {
                SqlConnection cnn;
                cnn = new SqlConnection(con);
                try
                {
                    cnn.Open();
                    return true;
                }
                catch (Exception ex)
                {
                    cnn.Close();
                    return false;
                }
            }
        }

        //public static Tuple<DataTable, DataTable> getDataType(string constr, List<string> tables, string dbname, string dbtype, string Field)
        //{
        //    string rowquery = "", headerquery = "";
        //    //List<string> Headerfields = new List<string>();
        //    //List<string> Rowfields = new List<string>();
        //    DataTable HeaderDataType = new DataTable();
        //    DataTable RowDataType = new DataTable();
        //    if (dbtype.Contains("HANA"))
        //    {
        //        //2023/07/28 - I commented this because Error in posting in PCII
        //        //HanaConnection cnn;
        //        //cnn = new HanaConnection(constr);
        //        //headerquery = $"SELECT DATA_TYPE_NAME,LENGTH,SCALE FROM SYS.COLUMNS WHERE SCHEMA_NAME = '{dbname}' AND TABLE_NAME = '{tables[0]}' AND COLUMN_NAME = '{Field}' " +
        //        //              $"UNION ALL " +
        //        //              $"SELECT DATA_TYPE_NAME,LENGTH,SCALE FROM SYS.COLUMNS WHERE SCHEMA_NAME = '{dbname}' AND TABLE_NAME = '{tables[1]}' AND COLUMN_NAME = '{Field}' ";

        //        ////rowquery = $"SELECT DATA_TYPE_NAME,LENGTH,SCALE FROM SYS.COLUMNS WHERE SCHEMA_NAME = '{dbname}' AND TABLE_NAME = '{tables[1]}' AND COLUMN_NAME = '{Field}'";

        //        //using (HanaCommand cmd = new HanaCommand(headerquery, cnn))
        //        //{
        //        //    HanaDataAdapter da = new HanaDataAdapter(cmd);
        //        //    cmd.Connection.Open();
        //        //    da.Fill(HeaderDataType);
        //        //    cmd.Connection.Close();
        //        //}
        //        //using (HanaCommand cmd = new HanaCommand(/*rowquery*/headerquery, cnn))
        //        //{
        //        //    HanaDataAdapter da = new HanaDataAdapter(cmd);
        //        //    cmd.Connection.Open();
        //        //    da.Fill(RowDataType);
        //        //    cmd.Connection.Close();
        //        //}
        //    }
        //    else
        //    {
        //        SqlConnection cnn;
        //        cnn = new SqlConnection(constr);
        //        headerquery = $"SELECT DATA_TYPE_NAME,LENGTH FROM {dbname}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = '{dbname}' AND TABLE_NAME = '{tables[0]}' " +
        //                      $"UNION ALL" +
        //                      $"SELECT DATA_TYPE_NAME,LENGTH FROM {dbname}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = '{dbname}' AND TABLE_NAME = '{tables[1]}' ";

        //        //rowquery = $"SELECT DATA_TYPE_NAME,LENGTH FROM {dbname}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = '{dbname}' AND TABLE_NAME = '{tables[1]}' ";
        //        using (SqlCommand cmd = new SqlCommand(headerquery, cnn))
        //        {
        //            SqlDataAdapter da = new SqlDataAdapter(cmd);
        //            cmd.Connection.Open();
        //            da.Fill(HeaderDataType);
        //            cmd.Connection.Close();
        //        }
        //        using (SqlCommand cmd = new SqlCommand(/*rowquery*/headerquery, cnn))
        //        {
        //            SqlDataAdapter da = new SqlDataAdapter(cmd);
        //            cmd.Connection.Open();
        //            da.Fill(RowDataType);
        //            cmd.Connection.Close();
        //        }
        //    }

        //    //return Tuple.Create(HeaderDataType, RowDataType);
        //    return Tuple(HeaderDataType, RowDataType);
        //}

        public static string GetItemStockMSSQL()
        {
            return $@"select T1.ItemCode,T1.ItemName, sum(T0.InQty-T0.OutQty) AS Stock,T1.SalUnitMsr , X.DocDate
                        from OINM T0 
	                        inner join OITM T1 on T0.ItemCode=T1.ItemCode
	                        , (select top 1 DocDate from OINV 
				                        order by DocEntry desc) as X 
                        where T1.SellItem='Y'
                        Group by T1.ItemCode,T1.ItemName,T1.SalUnitMsr, X.DocDate";
        }

        public static string GetItemStockHANA()
        {
            return $@"select T1.""ItemCode"",T1.""ItemName"", sum(T0.""InQty""-T0.""OutQty"") AS ""Stock"" ,T1.""SalUnitMsr"",T2.""Price"" 
                        from OINM T0 
	                        inner join OITM T1 on T0.""ItemCode""=T1.""ItemCode"" inner join ITM1 T2 ON T1.""ItemCode"" = T2.""ItemCode"" 
                        where T1.""SellItem""='Y' AND T2.""PriceList"" = 2 
                        Group by T1.""ItemCode"",T1.""ItemName"",T1.""SalUnitMsr"",T2.""Price""";
        }

        public static string UpdateTransaction(string DbName, string Table, string Status, string Message, string Id)
        {
            return $@"UPDATE {DbName}.dbo.{Table} SET UploadDate = GetDate() , Status = '{Status}' ,Message = '{Message}' WHERE TransactionId = '{Id}'";
        }

        public static string CheckItem(string BarCode)
        {
            return $@"SELECT ""ItemCode"" FROM OITM WHERE ""ItemCode"" = '{BarCode}'";
        }

        public static string GetTopOrder()
        {
            return $@"SELECT TOP 1 T0.""DocEntry"" FROM ORDR T0 ORDER BY T0.""DocEntry"" DESC; ";
        }
        public static string GetItemCode(string ProductId)
        {
            string str = $@"SELECT * FROM OITM T0  WHERE T0.""ItemCode"" = '{ProductId}' 
                            /** in 
                            (
                            SELECT ""ItemCode"" FROM OSCN a WHERE a.""Substitute"" = '{ProductId}'
                            UNION ALL
                            SELECT ""Code"" FROM  ""@SPITEMUOMD"" a WHERE a.""Code"" = '{ProductId}'
                            ) **/ ";
            return str;
        }
        public static string GetFreightCode(string FreightName)
        {
            return $@" IF EXISTS (SELECT ""ExpnsCode"" FROM OEXD WHERE ""ExpnsName"" = '{FreightName}')
                            BEGIN
                                SELECT ""ExpnsCode"" FROM OEXD WHERE ""ExpnsName"" = '{FreightName}'
                            END
                        ELSE
                            BEGIN
                                SELECT TOP 1 ""ExpnsCode"" FROM OEXD
                            END ";
        }
        public static string GetDocNum(string CardCode, string OrderNumber)
        {
            return $@"SELECT TOP 1 ""DocNum"" FROM ORDR WHERE ""CardCode"" = '{CardCode}' AND ""NumAtCard"" = '{OrderNumber}' ";
        }

        public static string GetItemProduct(string contype)
        {
            string str = contype.ToLower().Contains("hana") ?
                    $@" select case when ifnull(T0.""U_spParentItem"",'') = '' Then 'Products'
                                    when T0.""U_spParentItem"" <> T0.""ItemCode"" Then 'Variants'
                                    else 'Products'
                                    End as ""ItemType""
	                        , T1.""Currency"", T1.""Price""
                            , case when ifnull(T0.""U_spParentItem"",'') = '' Then T0.""ItemCode""
                                    when T0.""U_spParentItem"" <> T0.""ItemCode"" Then T0.""U_spParentItem""
                                    else T0.""ItemCode""
                                    End as ""Parent""
                            , SC.""Substitute"" as ""IntegrationId""
                            , SC.""U_spImage"" as ""ImageFile""
                            , SC.""U_spImageId"" as ""ImageId""
	                        , C1.""BpCode"" as ""BpCatalog""
                            , C1.""BpName"" as ""BpName""
                            , C0.""CpnNo"" as ""CampaignNumber""
	                        , P0.""ListNum""
                            , T0.*
                        from OCPN C0 INNER JOIN CPN1 C1 ON C0.""CpnNo"" = C1.""CpnNo""
                            inner join CPN2 C2 ON C0.""CpnNo"" = C2.""CpnNo""
                            inner join OITM T0 ON C2.""ItemCode"" = T0.""ItemCode""
                            inner join ITM1 T1 ON T0.""ItemCode"" = T1.""ItemCode"" AND C1.""U_spListNum"" = T1.""PriceList""
                            inner join OPLN P0 ON T1.""PriceList"" = P0.""ListNum""
                            left join OSCN SC ON C2.""ItemCode"" = SC.ItemCode AND C1.BpCode = SC.CardCode AND ISNULL(SC.""U_spParentCode"",'') = ''
                        where C0.""Status"" <> 'C'  AND C0.""U_spUploadStatus""='F'  /**AND SC.ItemCode = SC.""U_spParentCode"" AND IFNULL(SC.""U_spUomCode"",'')= ''**/ "
                    :
                    $@" select case when isnull(T0.""U_spParentItem"",'') = '' Then 'Products'
                                    when T0.""U_spParentItem"" <> T0.""ItemCode"" Then 'Variants'
                                    else 'Products'
                                    End as ""ItemType""
	                        , T1.""Currency"", T1.""Price""
                            , case when isnull(T0.""U_spParentItem"",'') = '' Then T0.""ItemCode""
                                    when T0.""U_spParentItem"" <> T0.""ItemCode"" Then T0.""U_spParentItem""
                                    else T0.""ItemCode""
                                    End as ""Parent""
                            , SC.""Substitute"" as ""IntegrationId""
                            , SC.""U_spImage"" as ""ImageFile""
                            , SC.""U_spImageId"" as ""ImageId""
	                        , C1.""BpCode"" as ""BpCatalog""
                            , C1.""BpName"" as ""BpName""
                            , C0.""CpnNo"" as ""CampaignNumber""
	                        , P0.""ListNum""
                            , T0.*
                        from OCPN C0 INNER JOIN CPN1 C1 ON C0.""CpnNo"" = C1.""CpnNo""
                            inner join CPN2 C2 ON C0.""CpnNo"" = C2.""CpnNo""
                            inner join OITM T0 ON C2.""ItemCode"" = T0.""ItemCode""
                            inner join ITM1 T1 ON T0.""ItemCode"" = T1.""ItemCode"" AND C1.""U_spListNum"" = T1.""PriceList""
                            inner join OPLN P0 ON T1.""PriceList"" = P0.""ListNum""
                            left join OSCN SC ON C2.""ItemCode"" = SC.ItemCode AND C1.BpCode = SC.CardCode AND ISNULL(SC.""U_spParentCode"",'') = ''
                        where C0.""Status"" <> 'C' AND C0.""U_spUploadStatus""='F' /**AND SC.ItemCode = SC.""U_spParentCode"" AND ISNULL(SC.""U_spUomCode"",'')= ''**/ ";
            return str;
        }

        public static string GetItemVariant(string contype, string ItemCode, string ListNum, string CardCode, string CampStatus = "F")
        {
            string str = contype.ToLower().Contains("hana") ?
                    $@" DECLARE @tblItem TABLE (""ItemCode"" nvarchar(100))
                        INSERT INTO @tblItem 
                        SELECT  T2.""ItemCode""
                        from OCPN T0 INNER JOIN CPN1 T1 ON T0.""CpnNo"" = T1.""CpnNo""	
	                        inner join CPN2 T2 on T0.""CpnNo"" = T2.""CpnNo""	
                            /**CHECK FOR OTHER VARIANT**/
                            LEFT JOIN OITM I0 ON T2.""ItemCode"" = I0.""ItemCode""
                        where T0.""U_spUploadStatus"" = '{CampStatus}' AND T1.""BpName"" like '%Shopify%'

                        select case when IFNULL(T0.""U_spParentItem"",'') = '' Then 'Products'
                                  when T0.""U_spParentItem"" <> T0.""ItemCode"" Then 'Variants'
                                  else 'Products'
                                  End as ""ItemType""
	                        , T1.""Currency"", T1.""Price""
                            , (SELECT TOP 1 sq.""Substitute"" FROM OSCN sq WHERE sq.""ItemCode"" = T0.""ItemCode"" and sq.""U_spParentCode"" = '{ItemCode}') as ""IntegrationId""
                            , '{CardCode}' as ""BpCatalog""
	                        , '' as ""spUoMCode"", T0.""ItemName"" as ""spUoMName"", T0.*
                        from OITM T0
                            inner join ITM1 T1 ON T0.""ItemCode"" = T1.""ItemCode""
                            inner join OPLN P0 ON T1.""PriceList"" = P0.""ListNum""
                        where P0.ListNum = '{ListNum}' 
                            and (T0.""U_spParentItem"" <> T0.""ItemCode"" ) 
                            and T0.""U_spParentItem"" = '{ItemCode}' 

                        union all
						 
						select 'Variants' as ""ItemType""
						    , T1.""Currency"", T1.""Price""
						    , a1.""Father"" as ""Parent""
						    , (SELECT TOP 1 sq.""Substitute"" FROM OSCN sq WHERE sq.""ItemCode"" = T0.""ItemCode"" and sq.""U_spParentCode"" = '{ItemCode}') as ""IntegrationId""
	                        , '{CardCode}' as ""BpCatalog""
						    , '' as ""spUoMCode"", T0.""ItemName"" as ""spUoMName"", T0.*
                        from OITM T0
                            inner join ITT1 a1 on T0.""Itemcode"" = a1.""Code""
                            inner join ITM1 T1 on T0.""ItemCode"" = T1.""ItemCode""
                            inner join OPLN P0 ON T1.""PriceList"" = P0.""ListNum""
                        where P0.""ListNum"" = '{ListNum}'
                            and a1.""Father"" = '{ItemCode}'

                        union all
					    select 'Variants' as ""ItemType""
						    , T9.""Currency"", T9.""Price""
						    , T0.""ItemCode"" as ""Parent""
						    , SC.""Substitute"" /**DO1.""U_spSubstitute""**/ as ""IntegrationId""
	                        , '{CardCode}' as ""BpCatalog""
						    , UM.""UomCode"" as spUoMCode, UM.""UomName"" as ""spUoMName"", T0.*
                        from OITM T0 inner join ITM9 T9 ON T0.""ItemCode"" = T9.""ItemCode""
                            inner join OPLN PL ON T9.""PriceList"" = PL.""ListNum""
                            inner join OUOM UM ON T9.""UomEntry"" = UM.""UomEntry""
                            left join OSCN SC ON T0.""ItemCode"" = SC.""ItemCode"" and UM.""UomCode"" = SC.""U_spUomCode""  and SC.""CardCode"" = '{CardCode}'
                            /**left join ""@SPITEMUOMD"" DO1 ON T9.""ItemCode"" = DO1.""Code"" and UM.""UomCode"" = DO1.""U_spUomCode"" **/
                        where T0.ItemCode = '{ItemCode}' AND PL.""ListNum"") = '{ListNum}' "
                    :
                    $@" 
                        /**SMALLEST UoM**/
                        select 'Variants' as ""ItemType""
	                        , T1.""Currency"", (T1.""Price"" /* * ((V0.""Rate"" / 100) + 1 )*/) as ""Price""
	                        , T0.""ItemCode"" as ""Parent""
	                        , SC.""Substitute"" as ""IntegrationId""
                            , SC.""U_spImage"" as ""ImageFile""
                            , SC.""U_spImageId"" as ""ImageId""
	                        , C1.""BpCode"" as ""BpCatalog""
	                        , UM.""UomCode"" as spUoMCode, UM.""UomName"" as ""spUoMName"", C0.""CpnNo"", T0.*
                        from OITM T0 inner join OVTG V0 ON T0.""VatGourpSa"" = V0.""Code""
                            inner join CPN2 C2 ON T0.""ItemCode"" = C2.ItemCode
                            inner join CPN1 C1 ON C2.""CpnNo"" = C1.""CpnNo""
                            inner join OCPN C0 ON C2.""CpnNo"" = C0.""CpnNo""
                            inner join ITM1 T1 ON C2.""ItemCode"" = T1.""ItemCode"" and C1.""U_spListNum"" = T1.""PriceList""
                            inner join OPLN PL ON T1.""PriceList"" = PL.""ListNum""
                            inner join OUOM UM ON T1.""UomEntry"" = UM.""UomEntry""
                            left join OSCN SC ON T0.""ItemCode"" = SC.""ItemCode"" and UM.""UomCode"" = SC.""U_spUomCode""  and SC.""CardCode"" = C1.""BpCode""
                        where C0.""Status"" <> 'C' /**AND isnull(SC.""Substitute"",'') = ''**/
                            AND T1.""UomEntry"" <> '-1' AND T0.""ItemCode"" = '{ItemCode}' and PL.""ListNum"" = '{ListNum}'
                            AND ISNULL(SC.""U_spIncluded"",'NO') = 'YES'

                        union all
                        /**OTHER UoM**/
                        select 'Variants' as ""ItemType""
	                        , T9.""Currency"", (T9.""Price"" /* * ((V0.""Rate"" / 100) + 1 ) */) as ""Price""
	                        , T0.""ItemCode"" as ""Parent""
	                        , SC.""Substitute"" as ""IntegrationId""
                            , SC.""U_spImage"" as ""ImageFile""
                            , SC.""U_spImageId"" as ""ImageId""
	                        , C1.""BpCode"" as ""BpCatalog""
	                        , UM.""UomCode"" as spUoMCode, UM.""UomName"" as ""spUoMName"", C0.""CpnNo"", T0.*
                        from OITM T0 inner join OVTG V0 ON T0.""VatGourpSa"" = V0.""Code""
                            inner join ITM9 T9 ON T0.""ItemCode"" = T9.""ItemCode""
                            inner join OPLN PL ON T9.""PriceList"" = PL.""ListNum""
                            inner join OUOM UM ON T9.""UomEntry"" = UM.""UomEntry""
                            inner join CPN2 C2 ON T0.""ItemCode"" = C2.ItemCode
                            inner join CPN1 C1 ON C2.""CpnNo"" = C1.""CpnNo"" and T9.""PriceList"" = C1.""U_spListNum""
                            inner join OCPN C0 ON C2.""CpnNo"" = C0.""CpnNo""
                            left join OSCN SC ON T0.""ItemCode"" = SC.""ItemCode"" and UM.""UomCode"" = SC.""U_spUomCode""  and SC.""CardCode"" = C1.""BpCode""
                        where C0.""Status"" <> 'C' /**and isnull(SC.""Substitute"",'') = ''**/                               
                            AND T0.ItemCode = '{ItemCode}' and PL.""ListNum"" = '{ListNum}'
                            AND ISNULL(SC.""U_spIncluded"",'NO') = 'YES' ";
            return str;
        }
        public static string GetUdoCollection()
        {
            string str = $@"SELECT ""Code"", ""Name"", ""U_spDescription"", ""U_spSubstitute"" FROM ""@SPCOLLECTION""  WHERE Canceled='N' ";
            return str;
        }
        public static string Search(string ConString, string sqlQuery, int RowNumber, string FieldName)
        {
            DataTable dt;
            string result = "";
            try
            {
                dt = DataAccess.Select(ConString, sqlQuery);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        result = dt.Rows[RowNumber][FieldName].ToString();
                    }
                }
                else
                {
                    result = "error";
                }
            }
            catch (Exception)
            {
                result = "error exception";
            }
            return result;
        }

        public static bool CheckTableColumn(string ConString, string Table, string Column)
        {
            bool result = false;
            #region AlternateQueryVersion1
            string strQuery = $@"IF EXISTS(SELECT 1 FROM sys.columns 
                                          WHERE Name = N'{Column}'
                                          AND Object_ID = Object_ID(N'dbo.{Table}'))
                                    BEGIN SELECT 'TRUE' as RESULT END
                                ELSE
                                    BEGIN SELECT 'FALSE' as RESULT END ";

            #endregion

            #region MSSQLShortQueryVersion
            string sqlQuery = $@"IF COL_LENGTH('dbo.{Table}', '{Column}') IS NOT NULL
                                    BEGIN SELECT 'TRUE' as RESULT END
                                ELSE
                                    BEGIN SELECT 'FALSE' as RESULT END ";
            #endregion

            var dt = DataAccess.Select(ConString, sqlQuery);
            if (dt != null)
            {
                if (dt.Rows[0][0].ToString() == "TRUE")
                {
                    result = true;
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public static string GetItemBatches(string ItemCode, string WhsCode)
        {
            string str = "";
            str = $@"SELECT T1.""ItemCode"", T1.""SysNumber"" as ""SysSerial"", T1.""DistNumber"" as ""IntrSerial"", T0.""Quantity"",  T0.""CommitQty"", (T0.""Quantity"" - T0.""CommitQty"") as ""QtyAvailable""
                        , T1.""MnfSerial"", T1.""LotNumber"", T1.""ExpDate"", T1.""MnfDate"", T1.""InDate""
                    FROM  ""dbo"".""OBTQ"" T0  
                        inner join OBTN T1 ON T0.""MdAbsEntry"" = T1.AbsEntry
                    WHERE T0.""ItemCode"" = ('{ItemCode}')  AND  T0.""WhsCode"" = ('{WhsCode}')  AND  T0.""Quantity"" <> (0) ";
            return str;
        }
        public static string GetWhsCode(string Id)
        {
            string str = $@"SELECT ""WhsCode"" FROM OWHS WHERE ""County"" = '{Id}' ";
            return str;
        }

        public static string GetUoMVariants(string contype)
        {
            string str = contype.ToLower().Contains("hana") ?
                            $@"/**Main Product**/
                                SELECT 'Products' as ""ItemType""
	                                , '' ""Currency"", 0 as ""Price""
	                                , T0.""ItemCode"" as ""Parent""
	                                , SC.""Substitute"" as ""IntegrationId""
	                                , C1.""BpCode"" as ""BpCatalog""
	                                , '' as spUoMCode, '' as ""spUoMName"", C0.""CpnNo"", T0.*
                                FROM OCPN C0
                                    inner Join CPN1 C1 ON C0.""CpnNo"" = C1.""CpnNo""
                                    inner join CPN2 C2 ON C0.""CpnNo"" = C2.""CpnNo""
                                    inner join OITM T0 ON C2.""ItemCode"" = T0.""ItemCode""
                                    left join OSCN SC ON C2.""ItemCode"" = SC.""ItemCode"" and C1.""BpCode"" = SC.""CardCode"" and IfNULL(SC.""U_spUomCode"",'') = ''
                                WHERE C0.""Status"" <> 'C' AND SC.""Substitute"" = ''
                                    AND T0.""ItemCode"" IN (select TOP 2 ""ItemCode"" from OITM order by ""UpdateDate"" DESC, ""UpdateTS"" DESC)

                                union all
                                /**SMALLEST UoM**/
                                select 'Variants' as ""ItemType""
	                                , T1.""Currency"", T1.""Price""
	                                , T0.""ItemCode"" as ""Parent""
	                                , SC.""Substitute"" as ""IntegrationId""
	                                , C1.""BpCode"" as ""BpCatalog""
	                                , UM.""UomCode"" as spUoMCode, UM.""UomName"" as ""spUoMName"", C0.""CpnNo"", T0.*
                                from OITM T0
                                    inner join CPN2 C2 ON T0.""ItemCode"" = C2.ItemCode
                                    inner join CPN1 C1 ON C2.""CpnNo"" = C1.""CpnNo""
                                    inner join OCPN C0 ON C2.""CpnNo"" = C0.""CpnNo""
                                    inner join ITM1 T1 ON C2.""ItemCode"" = T1.""ItemCode"" and C1.""U_spListNum"" = T1.""PriceList""
                                    inner join OPLN PL ON T1.""PriceList"" = PL.""ListNum""
                                    inner join OUOM UM ON T1.""UomEntry"" = UM.""UomEntry""
                                    left join OSCN SC ON T0.""ItemCode"" = SC.""ItemCode"" and UM.""UomCode"" = SC.""U_spUomCode""  and SC.""CardCode"" = C1.""BpCode""
                                where C0.""Status"" <> 'C' AND ifnull(SC.""Substitute"",'') = ''
                                    AND T1.""UomEntry"" <> '-1' AND T0.""ItemCode"" IN (select TOP 2 ""ItemCode"" from OITM order by ""UpdateDate"" DESC, ""UpdateTS"" DESC)

                                union all
                                /**OTHER UoM**/
                                select 'Variants' as ""ItemType""
	                                , T9.""Currency"", T9.""Price""
	                                , T0.""ItemCode"" as ""Parent""
	                                , SC.""Substitute"" as ""IntegrationId""
	                                , C1.""BpCode"" as ""BpCatalog""
	                                , UM.""UomCode"" as spUoMCode, UM.""UomName"" as ""spUoMName"", C0.""CpnNo"", T0.*
                                from OITM T0 inner join ITM9 T9 ON T0.""ItemCode"" = T9.""ItemCode""
                                    inner join OPLN PL ON T9.""PriceList"" = PL.""ListNum""
                                    inner join OUOM UM ON T9.""UomEntry"" = UM.""UomEntry""
                                    inner join CPN2 C2 ON T0.""ItemCode"" = C2.ItemCode
                                    inner join CPN1 C1 ON C2.""CpnNo"" = C1.""CpnNo"" and T9.""PriceList"" = C1.""U_spListNum""
                                    inner join OCPN C0 ON C2.""CpnNo"" = C0.""CpnNo""
                                    left join OSCN SC ON T0.""ItemCode"" = SC.""ItemCode"" and UM.""UomCode"" = SC.""U_spUomCode""  and SC.""CardCode"" = C1.""BpCode""
                                where C0.""Status"" <> 'C' and ifnull(SC.""Substitute"",'') = ''
                                    AND T0.""ItemCode"" IN (select TOP 2 ""ItemCode"" from OITM order by ""UpdateDate"" DESC, ""UpdateTS"" DESC) "
                            :
                            $@"/**Main Product**/
                                    SELECT 'Products' as ""ItemType""
	                                    , '' ""Currency"", 0 as ""Price""
	                                    , T0.""ItemCode"" as ""Parent""
	                                    , SC.""Substitute"" as ""IntegrationId""
	                                    , C1.""BpCode"" as ""BpCatalog""
	                                    , '' as spUoMCode, '' as ""spUoMName"", C0.""CpnNo"", T0.*
                                    FROM OCPN C0
                                        inner Join CPN1 C1 ON C0.""CpnNo"" = C1.""CpnNo""
                                        inner join CPN2 C2 ON C0.""CpnNo"" = C2.""CpnNo""
                                        inner join OITM T0 ON C2.""ItemCode"" = T0.""ItemCode""
                                        left join OSCN SC ON C2.""ItemCode"" = SC.""ItemCode"" and C1.""BpCode"" = SC.""CardCode"" and ISNULL(SC.""U_spUomCode"",'') = ''
                                    WHERE C0.""Status"" <> 'C'  AND isnull(SC.""Substitute"",'') = ''
                                        AND T0.""ItemCode"" IN (select TOP 2 ""ItemCode"" from OITM order by ""UpdateDate"" DESC, ""UpdateTS"" DESC)

                                    union all
                                    /**SMALLEST UoM**/
                                    select 'Variants' as ""ItemType""
	                                    , T1.""Currency"", T1.""Price""
	                                    , T0.""ItemCode"" as ""Parent""
	                                    , SC.""Substitute"" as ""IntegrationId""
	                                    , C1.""BpCode"" as ""BpCatalog""
	                                    , UM.""UomCode"" as spUoMCode, UM.""UomName"" as ""spUoMName"", C0.""CpnNo"", T0.*
                                    from OITM T0
                                        inner join CPN2 C2 ON T0.""ItemCode"" = C2.ItemCode
                                        inner join CPN1 C1 ON C2.""CpnNo"" = C1.""CpnNo""
                                        inner join OCPN C0 ON C2.""CpnNo"" = C0.""CpnNo""
                                        inner join ITM1 T1 ON C2.""ItemCode"" = T1.""ItemCode"" and C1.""U_spListNum"" = T1.""PriceList""
                                        inner join OPLN PL ON T1.""PriceList"" = PL.""ListNum""
                                        inner join OUOM UM ON T1.""UomEntry"" = UM.""UomEntry""
                                        left join OSCN SC ON T0.""ItemCode"" = SC.""ItemCode"" and UM.""UomCode"" = SC.""U_spUomCode""  and SC.""CardCode"" = C1.""BpCode""
                                    where C0.""Status"" <> 'C' AND isnull(SC.""Substitute"",'') = ''
                                        AND T1.""UomEntry"" <> '-1' AND T0.""ItemCode"" IN (select TOP 2 ""ItemCode"" from OITM order by ""UpdateDate"" DESC, ""UpdateTS"" DESC)

                                    union all
                                    /**OTHER UoM**/
                                    select 'Variants' as ""ItemType""
	                                    , T9.""Currency"", T9.""Price""
	                                    , T0.""ItemCode"" as ""Parent""
	                                    , SC.""Substitute"" as ""IntegrationId""
	                                    , C1.""BpCode"" as ""BpCatalog""
	                                    , UM.""UomCode"" as spUoMCode, UM.""UomName"" as ""spUoMName"", C0.""CpnNo"", T0.*
                                    from OITM T0 inner join ITM9 T9 ON T0.""ItemCode"" = T9.""ItemCode""
                                        inner join OPLN PL ON T9.""PriceList"" = PL.""ListNum""
                                        inner join OUOM UM ON T9.""UomEntry"" = UM.""UomEntry""
                                        inner join CPN2 C2 ON T0.""ItemCode"" = C2.ItemCode
                                        inner join CPN1 C1 ON C2.""CpnNo"" = C1.""CpnNo"" and T9.""PriceList"" = C1.""U_spListNum""
                                        inner join OCPN C0 ON C2.""CpnNo"" = C0.""CpnNo""
                                        left join OSCN SC ON T0.""ItemCode"" = SC.""ItemCode"" and UM.""UomCode"" = SC.""U_spUomCode""  and SC.""CardCode"" = C1.""BpCode""
                                    where C0.""Status"" <> 'C' and isnull(SC.""Substitute"",'') = ''
                                        AND T0.""ItemCode"" IN (select TOP 2 ""ItemCode"" from OITM order by ""UpdateDate"" DESC, ""UpdateTS"" DESC) ";
            return str;
        }

        public static string GetInventoryVariant(string contype)
        {
            string str = contype.ToLower().Contains("hana") ?
                        $@"select a.""U_spInventoryId"" as ""InventoryId""
	                            , a.""ItemCode""
	                            , d.""ItemName""
	                            , ifnull(a.""U_spUomCode"", d.""SalUnitMsr"") as ""UomCode""
	                            , cast(round(e.""OnHand"" / ifnull(g.""BaseQty"", D.""NumInSale""), 0) as numeric(18,0)) as ""Available""
                                , IFNULL(B.""U_spLocationID"",W.""County"") as ""LocationId""
                            from OSCN a
                                inner Join CPN1 B on a.""U_spCampNo"" = b.""CpnNo""
	                            inner join OWHS W ON B.""U_spDefWhse"" = W.""WhsCode""
                                inner join OCPN C ON B.""CpnNo"" = C.""CpnNo""
                                inner join OITM D on a.""ItemCode"" = d.""ItemCode""
                                inner join OITW E on a.""ItemCode"" = e.""ItemCode"" and b.""U_spDefWhse"" = e.""WhsCode""
                                left join ouom f on a.""U_spUomCode"" = f.""UomCode""
                                left join UGP1 g on d.""UgpEntry"" = g.""UgpEntry"" and g.""UomEntry"" = f.""UomEntry""
                            where C.""Status"" <> 'C' AND IFNULL(a.""U_spInventoryId"",'') <> '' order by a.""U_spInventoryId"" "
                        :
                        $@"select a.""U_spInventoryId"" as ""InventoryId""
	                            , a.""ItemCode""
	                            , d.""ItemName""
	                            , isnull(a.""U_spUomCode"", d.""SalUnitMsr"") as ""UomCode""
	                            , cast(round(e.""OnHand"" / isnull(g.""BaseQty"", D.""NumInSale""), 0) as numeric(18,0)) as ""Available""
                                , ISNULL(B.""U_spLocationID"",W.""County"") as ""LocationId""
                            from OSCN a
                                inner Join CPN1 B on a.""U_spCampNo"" = b.""CpnNo""
                                inner join OWHS W ON B.""U_spDefWhse"" = W.""WhsCode""
                                inner join OCPN C ON B.""CpnNo"" = C.""CpnNo""
                                inner join OITM D on a.""ItemCode"" = d.""ItemCode""
                                inner join OITW E on a.""ItemCode"" = e.""ItemCode"" and b.""U_spDefWhse"" = e.""WhsCode""
                                left join ouom f on a.""U_spUomCode"" = f.""UomCode""
                                left join UGP1 g on d.""UgpEntry"" = g.""UgpEntry"" and g.""UomEntry"" = f.""UomEntry""
                            where C.""Status"" <> 'C' AND ISNULL(a.""U_spInventoryId"",'') <> '' order by a.""U_spInventoryId"" ";

            return str;
        }

        public static string GetUomCode(string UomName)
        {
            string str = $@"select * from OUOM where UomName = '{UomName.Replace("'", "''")}' ";
            return str;
        }
        public static string GetLocationFromCampaign(string locationid)
        {
            string str = string.IsNullOrEmpty(locationid) ? $@"select TOP 1 T1.""U_spDefWhse"" as WhsCode, T1.""U_spLocationID"" from OCPN T0 inner join CPN1 T1 on T0.""CpnNo"" = T1.""CpnNo""  where T0.""Status"" <> 'C'" :
                            $@"select TOP 1 T1.""U_spDefWhse"" as WhsCode, T1.""U_spLocationID"" from OCPN T0 inner join CPN1 T1 on T0.""CpnNo"" = T1.""CpnNo""
                            where T0.""Status"" <> 'C' and T1.U_spLocationID = '{locationid}' ; ";
            return str;
        }
        public static string GetCampaignData()
        {
            string str = $@"SELECT TOP 1 T1.* FROM OCPN T0 inner join CPN1 T1 on T0.""CpnNo""=T1.""CpnNo"" WHERE  T0.""Status"" <> 'C' ";
            return str;
        }
        public static string GetItemPerUnit(string itemcode, string uomcode)
        {
            string str = $@"SELECT TOP 1 T1.* FROM OITM T0 inner join UGP1 T1 on T0.""UgpEntry""= T1.""UgpEntry"" 
                            inner join OUOM UO on T1.""UomEntry"" = UO.""UomEntry""
                            WHERE  T0.""ItemCode"" = '{itemcode}' and UO.""UomName"" = '{uomcode.Replace("'", "''")}' ";
            return str;
        }
        public static string GetItemExtraDetail(string itemcode)
        {
            string str = $@"SELECT T0.* FROM OITM T0 WHERE T0.ItemCode ='{itemcode}' ; ";
            return str;
        }

        #region FILE TO SAP
        #region XML Comment
        /// <summary>
        /// Creates transaction logs table for field mapping
        /// </summary>
        /// <param name="TableName">Table from SAP.</param>
        /// <param name="con">Connection string.</param>
        /// <param name="Database">Database of addon table.</param>
        /// <param name="PrimaryKey">Primary key of table from SAP. e.g. DocEntry</param>
        /// <param name="MapId">Key of field mapping for reference</param>
        /// <returns>Connection String. haha :)</returns>
        #endregion
        public static string CreateTransactionLogs(string TableName, string con, string Database, string PrimaryKey, int MapId)
        {
            SqlConnection cnn;
            cnn = new SqlConnection(con);
            string checktable = $"SELECT * FROM {Database}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TableName}_{MapId.ToString()}'";
            using (SqlCommand cmd = new SqlCommand(checktable, cnn))
            {
                cnn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        string Createscript = $"USE [{Database}] CREATE TABLE [dbo].[{TableName}_{MapId.ToString()}](" +
                            $"{PrimaryKey} NVARCHAR(255) NOT NULL, " +
                            $"MapId INT NOT NULL, " +
                            $"CreateDate DATETIME, " +
                            $"UploadDate DATETIME, " +
                            $"IsUpload BIT, " +
                            $"CONSTRAINT PK_{TableName}_{MapId.ToString()} PRIMARY KEY ({PrimaryKey}) " +
                            $")";
                        using (SqlCommand cmdcreate = new SqlCommand(Createscript, cnn))
                        {
                            reader.Close();
                            cmdcreate.ExecuteNonQuery();
                        }
                    }
                }
                cnn.Close();
            }
            return con;
        }


        #region XML Comment
        /// <summary>
        /// Upload Transaction Log
        /// </summary>
        /// <param name="TableName">Table from SAP.</param>
        /// <param name="con">Connection string.</param>
        /// <param name="Database">Database of addon table.</param>
        /// <param name="PrimaryKey">Primary key value from uploaded data to sap. e.g. DocEntry</param>
        /// <param name="MapId">Key of field mapping for reference</param>
        /// <param name="IsUpload">To check if the data is uploaded</param>
        /// <returns>Sana all Bumabalik</returns>
        #endregion
        public static string UploadTransactionLog(string PrimaryKey, bool IsUpload, int MapId, string TableName, string con, string Database)
        {
            SqlConnection cnn;
            cnn = new SqlConnection(con);
            string checktable = $"SELECT * FROM {Database}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TableName}_{MapId.ToString()}'";
            using (SqlCommand cmd = new SqlCommand(checktable, cnn))
            {
                cnn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        string Createscript = $"USE [{Database}] CREATE TABLE [dbo].[{TableName}_{MapId.ToString()}](" +
                            $"{PrimaryKey} NVARCHAR(255) NOT NULL, " +
                            $"MapId INT NOT NULL, " +
                            $"CreateDate DATETIME, " +
                            $"UploadDate DATETIME, " +
                            $"IsUpload BIT, " +
                            $"CONSTRAINT PK_{TableName}_{MapId.ToString()} PRIMARY KEY ({PrimaryKey}) " +
                            $")";
                        using (SqlCommand cmdcreate = new SqlCommand(Createscript, cnn))
                        {
                            reader.Close();
                            cmdcreate.ExecuteNonQuery();
                        }
                    }
                }
                cnn.Close();
            }
            return con;
        }

        public static string OPSCreateTable(string PrimaryKey, string TableName, string con, string Database, List<List<DataColumn>> columnList, int MapId)
        {
            try
            {
                SqlConnection cnn;
                cnn = new SqlConnection(con);
                string checktable = $"SELECT * FROM {Database}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TableName}'";
                using (SqlCommand cmd = new SqlCommand(checktable, cnn))
                {
                    cnn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            string CreateScript = $"USE [{Database}] CREATE TABLE [dbo].[{TableName}](";

                            foreach (var list in columnList)
                            {
                                foreach (var xlist in list)
                                {
                                    CreateScript += $"{xlist} NVARCHAR(255), ";
                                }

                                //var datatype = list.DataType;
                                //{
                                //    if (datatype == "Int32")
                                //    {
                                //        datatype = "INT";
                                //    }
                                //    else
                                //    {
                                //        datatype = "NVARCHAR(255)";
                                //    }
                                //}

                                CreateScript += "CreateDate DATETIME, ";
                                CreateScript += "UploadDate DATETIME";

                                // Check if the table name contains "_Header"
                                if (TableName.Equals($"{MapId}_Header"))
                                {
                                    // If it contains "_Header", it's a primary key constraint
                                    CreateScript += $",CONSTRAINT PK_{TableName} PRIMARY KEY ({PrimaryKey}) ";
                                }
                                else
                                {
                                    // Otherwise, it's a foreign key constraint
                                    CreateScript += $",CONSTRAINT FK_{TableName} FOREIGN KEY ({PrimaryKey}) " +
                                                    $"REFERENCES dbo.[{MapId}_Header]({PrimaryKey})";
                                }

                                // Close the table creation part
                                CreateScript += ")";


                                using (SqlCommand cmdcreate = new SqlCommand(CreateScript, cnn))
                                {
                                    reader.Close();
                                    cmdcreate.ExecuteNonQuery();
                                }
                            }


                        }
                    }
                    cnn.Close();
                }
                return con;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public static string OPSInsertData(string TableName, string con, string Database, List<List<DataColumn>> columnList, List<Dictionary<string, object>> columnData, string Primarykey, string Dest, string EntityName, int MapId, string ErrorPath)
        {

            try
            {
                Dictionary<string, List<string>> ErrorListValue = new Dictionary<string, List<string>>();

                string columns = "";
                string checktable = $"SELECT * FROM {Database}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TableName}'";
                foreach (var item in DataAccess.Select(con, checktable).Rows)
                {

                    if (columns == "")
                    {
                        foreach (var list in columnList)
                        {
                            foreach (var coldata in list)
                            {
                                columns += coldata + ",";
                            }
                        }

                    }

                    columns = columns.TrimEnd(',');

                    string Createscript = $"INSERT INTO [{TableName}] ({columns},CreateDate) ";
                    bool updateRule = false;
                    //bool updateReverse = false;
                    //bool dataAlreadyExist = false;
                    foreach (var rowData in columnData)
                    {
                        var cellValue = "";
                        updateRule = false;


                        //dataAlreadyExist = false;
                        //if (dataAlreadyExist)
                        //{
                        //    break;
                        //}
                        foreach (var kvp in rowData)//dapat ung pag kaka susunod ng Column ,, ganun dn pag kaka sunod sunod ng  I Insert na data?
                        {
                            if (EntityName == "BusinessPartners")
                            {

                                var primaryKeyValue = rowData[Primarykey].ToString().Replace("'", "''");

                                string CheckifExist = "";
                                if (TableName.Contains("Header"))
                                {
                                    CheckifExist = $"select {Primarykey} from dbo.[{TableName}] where {Primarykey}='{primaryKeyValue}' and UploadDate is not null";

                                }
                                else if (TableName.Contains("ContactEmployees"))
                                {
                                    var ContactName = rowData["NAME"];
                                    CheckifExist = $"select {Primarykey} from dbo.[{TableName}] where {Primarykey}='{primaryKeyValue}' and NAME='{ContactName}' ";
                                }
                                else if (TableName.Contains("BPAddresses"))
                                {
                                    var AddressName = rowData["ADDRESS"];
                                    CheckifExist = $"select {Primarykey} from dbo.[{TableName}] where {Primarykey}='{primaryKeyValue}' and ADDRESS='{AddressName}' ";
                                }
                                else if (TableName.Contains("BPBankAccounts"))
                                {
                                    var BankCode = rowData["BANKCODE"];
                                    CheckifExist = $"select {Primarykey} from dbo.[{TableName}] where {Primarykey}='{primaryKeyValue}' and BANKCODE='{BankCode}' ";

                                }
                                else if (TableName.Contains("BPWithholdingTaxCollection"))
                                {
                                    var LINENUMM = rowData["LINENUM"];
                                    CheckifExist = $"select {Primarykey} from dbo.[{TableName}] where {Primarykey}='{primaryKeyValue}' and LINENUM='{LINENUMM}' ";

                                }


                                var dtResult = DataAccess.Select(con, CheckifExist);
                                var updateString = "";
                                if (dtResult.Rows.Count > 0 && dtResult.Rows.Count != 0)
                                {
                                    foreach (var exist_value in rowData)
                                    {
                                        var key_Column = exist_value.Key.ToString();
                                        var Key_Value = exist_value.Value.ToString().Replace("'", "''");

                                        if (key_Column != Primarykey)
                                        {
                                            updateString += $"{key_Column}='{Key_Value}',";
                                        }

                                    }
                                    updateString += "UploadDate=null";
                                    var UpdateCommand = $"Update dbo.[{TableName}] Set {updateString} where {Primarykey}='{primaryKeyValue}'";
                                    var confirmTrue = DataAccess.Execute(con, UpdateCommand);

                                    if (confirmTrue == true)
                                    {
                                        updateRule = true;
                                        break;
                                    }

                                }

                                if (updateRule == true)
                                {
                                    break;
                                }

                            }



                                //start of Incoming
                                if (EntityName == "IncomingPayments" && kvp.Key.ToString() == $"{Primarykey.ToUpper()}" && TableName.Contains("Header") || EntityName == "PurchaseInvoices" && kvp.Key.ToString() == $"{Primarykey.ToUpper()}" && TableName.Contains("Header"))
                                 {
                                    var orct_primaryKeyValue = rowData[Primarykey].ToString();
                                    var orct_U_CancelTypeValue = rowData["U_CANCELTYPE"].ToString();

                                    if (orct_U_CancelTypeValue == "REVERSAL" || orct_U_CancelTypeValue=="Reversal")
                                    {
                                        var checkifitExist_cmd = $"select {Primarykey.ToUpper()} from dbo.[{MapId}_Header] where {Primarykey}='{orct_primaryKeyValue}'";

                                        var dt_c = DataAccess.Select(con, checkifitExist_cmd);

                                        if (dt_c != null && dt_c.Rows.Count > 0)
                                        {

                                            var updtcmd = $"update dbo.[{MapId}_Header] set U_CANCELTYPE = 'REVERSAL',UploadDate=null where {Primarykey} = '{orct_primaryKeyValue}'";


                                            using (SqlConnection con_z = new SqlConnection(con))
                                            {
                                                con_z.Open();

                                                using (SqlCommand updateCommand = new SqlCommand(updtcmd, con_z))
                                                {
                                                    int rowsAffected = updateCommand.ExecuteNonQuery();

                                                    // Optionally check the number of rows affected if needed
                                                    if (rowsAffected > 0)
                                                    {
                                                        //updateReverse= true;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        // Update did not affect any rows
                                                    }
                                                }

                                                // Connection will be automatically closed when exiting the using block
                                            }


                                         }
                                        //else //need to Upload pa kahit walang kasamang CounterRef  - Reversal nga pero walang na upload na Payment??
                                        //{
                                        //    break;
                                        //}
                                    }//if Reversal
                                
                            }//last of Incoming Payments




                            //if (TableName.Contains("Header") && EntityName != "BusinessPartners" && EntityName != "IncomingPayments")
                            //{

                            //    var nbp_primaryKeyValue = rowData[Primarykey].ToString().Replace("'", "''");
                            //    var CheckIfUploaded_Cmd = $"select UploadDate from dbo.[{MapId}_Header] where {Primarykey} = '{nbp_primaryKeyValue}'";
                            //    var dt = DataAccess.Select(con, CheckIfUploaded_Cmd);

                            //    if (dt != null && dt.Rows.Count > 0)
                            //    {
                            //        List<string> DataValueList = new List<string>();

                            //        DataValueList.Add(Primarykey);
                            //        DataValueList.Add("This is Already uploaded in the AddOnDatabase");

                            //        AddData(ErrorListValue, DataValueList, nbp_primaryKeyValue);
                            //        //dataAlreadyExist = true;
                            //        //if (dataAlreadyExist)
                            //        //{
                            //        //    break;

                            //        //}
                            //        break;
                            //    }

                            //}

                            if (EntityName != "BusinessPartners" && EntityName != "JournalEntries")
                            {
                          
                                var nbp_primaryKeyValue = rowData[Primarykey].ToString().Replace("'", "''");
                                var CheckIfUploaded_Cmd = "";
                                
                                if (TableName.Contains("DocumentLines") || TableName.Contains("PaymentInvoices") || TableName.Contains("PaymentChecks"))
                                {
                                    var LineNum = "LINE_NUM";
                                    if (EntityName.Equals("PurchaseInvoices"))
                                    {
                                        LineNum = "LINENUM";
                                    }
                                    var check_LineNum = rowData[LineNum].ToString().Replace("'", "''");

                                    CheckIfUploaded_Cmd = $"select {Primarykey} from dbo.[{TableName}] where {Primarykey} = '{nbp_primaryKeyValue}' and {LineNum} =  {check_LineNum}";

                                }
                                else
                                {
                                     CheckIfUploaded_Cmd = $"select {Primarykey} from dbo.[{TableName}] where {Primarykey} = '{nbp_primaryKeyValue}'";
                                }

                                var dt = DataAccess.Select(con, CheckIfUploaded_Cmd);

                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    List<string> DataValueList = new List<string>();

                                    DataValueList.Add(Primarykey);
                                    DataValueList.Add("This is Already uploaded in the AddOnDatabase");

                                    AddData(ErrorListValue, DataValueList, nbp_primaryKeyValue);
                                    //dataAlreadyExist = true;
                                    //if (dataAlreadyExist)
                                    //{
                                    //    break;

                                    //}
                                    break;
                                }

                            }




                            //if (coldata.SourceField == kvp.Key)
                            //{
                            var kValue = kvp.Value.ToString().Replace("'", "''");
                            //string DataisExist = $"SELECT {Primarykey} FROM dbo.[{TableName}] WHERE {Primarykey} = '{kValue}'"; // cant Add Address If its not Exist in the Main Table Header

                            //var dt = DataAccess.Select(con, DataisExist);

                            //if (dt.DataSet != null || dt.Rows.Count > 0)
                            //{
                            //    //nextItem = true;
                            //    break;

                            //}
                            cellValue += $"'{kValue}'" + ",";
                            //break;

                            //}

                        }
                        //}


                        if (cellValue != "" && updateRule != true)
                        {
                            cellValue = cellValue.ToString().TrimEnd(',');

                            var CreateInsertionScript = $"{Createscript} VALUES ({cellValue},'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}')";
                            DataAccess.Execute(con, CreateInsertionScript);
                        }


                    }

                }


                if (ErrorListValue.Count != 0)
                {
                    string DBlocalcon = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";

                    DataAccess.OPSCreateExcelDuplicationChecker(ErrorPath, ErrorListValue, DBlocalcon, MapId,TableName);

                }



                return con;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }



        }

        static Dictionary<string, List<string>> AddData(Dictionary<string, List<string>> ErrorListValue, List<string> DataValueList, string PKey)
        {
            if (!ErrorListValue.ContainsKey(PKey))
            {
                ErrorListValue.Add(PKey, DataValueList);
            }

            return ErrorListValue;
        }
        #endregion

        public static string ReplaceQueryParameter(string query, string strOldValue, string strNewValue, string BankCode)
        {
            if (strOldValue == "#U_OP_BPBANK#")
            {
                return query.Replace(strOldValue, BankCode);
            }
            else
            {
                return query.Replace(strOldValue, strNewValue);
            }


        }

        public static string OriginalReplaceQueryParameter(string query, string strOldValue, string strNewValue,int? amount)
        {
            
            //if(strOldValue.Contains("Billing Statement"))
            //{
            //    query = $"select id from account  where accttype = 'Bank' and LOWER(fullname)  Like '%{strNewValue}'";
            //    return query;
            //}

            //if (strOldValue.Contains("Order Type"))
            //{
            //    query = $"select id from CUSTOMLIST_GVI_PAYMENT_CHANNEL_LIST where LOWER(name) LIKE ('%{strNewValue}%')";
            //    return query;
            //}

            if (amount>0 && amount != null)
            {
                query += $" and custitem_gvi_provider_acc ={amount}";
            }

            return query.Replace(strOldValue, strNewValue);



        }

    }
}