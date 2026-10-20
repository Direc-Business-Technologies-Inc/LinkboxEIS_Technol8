using DataAccessLayer.Class;
using DomainLayer.ViewModels;
//using Sap.Data.Hana;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace LinkBoxUI.Helpers
{
    public class SqlHelpers
    {
        public string GetConnection(string sAppName)
        {
            var output = ConfigurationManager.ConnectionStrings[sAppName] != null ? ConfigurationManager.ConnectionStrings[sAppName].ToString() : "";
            return output;
        }

        public DataTable SQLGetData(string sQuery, string Connectionstring)
        {
            try
            {
                using (DataTable dt = new DataTable())
                {
                    using (SqlConnection con = new SqlConnection(Connectionstring))
                    {
                        using (SqlCommand cmd = new SqlCommand(sQuery, con))
                        {
                            SqlDataAdapter da = new SqlDataAdapter(cmd);
                            con.Open();
                            da.Fill(dt);
                            con.Close();
                        }
                    }

                    return dt;
                }
            }
            catch (Exception ex)
            { return null; }

            //return output;
        }

        public DataTable HanaGetData(string sQuery, string Connectionstring)
        {
            try
            {
                using (DataTable dt = new DataTable())
                {
                    //using (HanaConnection con = new HanaConnection(Connectionstring))
                    //{
                    //    using (HanaCommand cmd = new HanaCommand(sQuery, con))
                    //    {
                    //        HanaDataAdapter da = new HanaDataAdapter(cmd);
                    //        con.Open();
                    //        da.Fill(dt);
                    //        con.Close();
                    //    }
                    //}

                    return dt;
                }
            }
            catch (Exception ex)
            { return null; }

            //return output;
        }
        public string ExecuteNonQuery(EmailViewModel model, string query)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(model.DatabaseConnectionView.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    int affectedRows = cmd.ExecuteNonQuery();

                    return $"Success: {affectedRows} rows affected.";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }


        public DataTable Fill_DataTable(EmailViewModel model, string Query)
        {
            try
            {
                DataTable dt = new DataTable();

                if (model.DatabaseConnectionView.ConnectionType.Contains("HANA"))
                {
                    dt = DataAccess.SelectHana(model.DatabaseConnectionView.ConnectionString, Query);
                }
                else
                {
                    dt = DataAccess.Select(model.DatabaseConnectionView.ConnectionString, Query);
                }

                return dt;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

    }
}