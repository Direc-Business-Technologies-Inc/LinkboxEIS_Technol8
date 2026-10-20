//using CrystalDecisions.CrystalReports.Engine;
using DataCipher;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Web;

namespace LinkBoxUI.Helpers
{
    public class EmailHelpers
    {
        public SqlHelpers sql = new SqlHelpers();
        public string Send(EmailViewModel creds, string Filepath = null)
        {
            try
            {


                if (creds.EmailCreds.EmailTo != null)
                {
                    
                    SmtpClient client = new SmtpClient(creds.EmailCreds.EmailHost);
                    //If you need to authenticate
                    var password = Cryption.Decrypt($"{creds.EmailCreds.EmailPassword}");
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(creds.EmailCreds.EmailFrom, password);
                    client.Port = Convert.ToInt32(creds.EmailCreds.EmailPort);
                    client.EnableSsl = true;
                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(creds.EmailCreds.EmailFrom, creds.EmailCreds.EmailDesc);
                  
                    if (creds.CcTable != null && creds.CcTable.Rows.Count != 0)
                    {
                        try
                        {
                            var cccolumns = creds.CcTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToList();
                          
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    if (!string.IsNullOrEmpty(creds.EmailCreds.EmailTo))
                    {
                        foreach (var cc in creds.EmailCreds.EmailTo.Split(','))
                        {
                            mailMessage.To.Add(new MailAddress(cc.ToString()));
                            //mailMessage.To.Add(new MailAddress("direc.danilo@gmail.com"));
                        }
                    }

                    if (!string.IsNullOrEmpty(creds.EmailCreds.EmailCc))
                    {
                        foreach (var cc in creds.EmailCreds.EmailCc.Split(','))
                        {
                            mailMessage.CC.Add(new MailAddress(cc.ToString()));
                            //mailMessage.To.Add(new MailAddress("direc.danilo@gmail.com"));
                        }
                    }
                    string formattedDateTime = DateTime.Now.ToString("MMddyyyy HH:mm:ss");
                    var subject = $"{creds.EmailCreds.EmailSubject} - [{formattedDateTime}]";
                    var ebody = creds.EmailCreds.Body;
                    mailMessage.Subject = subject;
                  
                    mailMessage.Body = ebody.Replace("#ATTACHMENT#", "");

                    mailMessage.IsBodyHtml = true;

                  

                    mailMessage.Body = mailMessage.Body;
                    try
                    {
                        if (ebody.Contains("QUERY"))
                        {
                            var query = creds.QueryDetails.QueryString;

                            //query = query.Replace("#CARDCODE#", item["CardCode"].ToString());

                            creds.QueryTable = sql.Fill_DataTable(creds, query);

                            StringBuilder fields = new StringBuilder();

                            foreach (DataRow row in creds.QueryTable.Rows)
                            {
                                fields.AppendLine($"<tr>");

                                foreach (DataColumn col in creds.QueryTable.Columns)
                                {
                                    fields.AppendLine($"<td>{row[col.ColumnName].ToString()}</td>");
                                }

                                fields.AppendLine($"</tr>");

                            }
                            var headearFields = new StringBuilder();


                            foreach (DataColumn col in creds.QueryTable.Columns)
                            {
                                headearFields.AppendLine($"<th>{col.ColumnName}</th>");
                            }

                            var body = $@"<table style ='text-align:center;width:100%;height: 50px;' border ='1' >
                                                            <tr>
                                                                {headearFields.ToString()}
                                                            </tr>
                                                            <tbody>
                                                                {fields.ToString()}
                                                            </tbody>
                                                            </table>
                                                            <br/><br/>";

                            mailMessage.Body = mailMessage.Body.Replace("#EMAILQUERY#", body);
                            mailMessage.IsBodyHtml = true;
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    var attachmentPath = "";
                  
                    try
                    {
                        client.Send(mailMessage);
                        string query = @"
                        UPDATE dbo.SystemLogs
                        SET [Table] = [Table] + '-Email Sent'
                        WHERE [Table] IS NOT NULL
                          AND [Table] NOT LIKE '%-Email Sent%';
                    ";
                        string ret_message = sql.ExecuteNonQuery(creds, query);

                        return ret_message;
                    }
                    catch(Exception ex)
                    {
                        return $"Error Email-Sending :  {ex.Message}";
                    }


                  
                }
                else
                {
                    return "error No Email To.";
                }

            }
            catch (Exception ex)
            {
                return $"error - {ex.ToString()}";
            }
        }
        public AlternateView GetEmbeddedImage(String filePath, string body)
        {
            LinkedResource res = new LinkedResource(filePath, MediaTypeNames.Image.Jpeg);
            res.ContentId = Guid.NewGuid().ToString();
            string htmlBody = body.Replace("#LOGO#", res.ContentId);
            AlternateView alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
            alternateView.LinkedResources.Add(res);
            return alternateView;
        }

    }
}