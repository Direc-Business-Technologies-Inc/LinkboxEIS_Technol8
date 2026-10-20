using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using LinkBoxUI.Properties;
using System.Configuration;

namespace LinkBoxUI.Helpers
{
    public class TaskSchedulerHelpers
    {
        #region Add Scehdule 
        public static void AddSchedule(string SchedCode,
                                       string Frequency,
                                       string SchedDate,
                                       string SchedTime,
                                       string Type,
                                       string Url,string method)
        {
            try
            {
                //Properties.Settings prop = new Properties.Settings();

                var serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
                var user = ConfigurationManager.AppSettings["User"].ToString();
                var domain = ConfigurationManager.AppSettings["Domain"].ToString();
                var password = ConfigurationManager.AppSettings["Password"].ToString();

                using (TaskService ts = new TaskService(serverName,
                                                        user,
                                                        domain,
                                                        password))
                {
                    // Create a new task definition and assign properties
                    int[] daynum = new int[1] { Convert.ToDateTime(SchedDate).Day };
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Author = "DIREC";
                    td.RegistrationInfo.Description = SchedCode;
                    td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;
                    if (Frequency == "One Time")
                    {
                        td.Triggers.Add(new TimeTrigger
                        {
                            StartBoundary = Convert.ToDateTime($"{Convert.ToDateTime(SchedDate).ToShortDateString()} {Convert.ToDateTime(SchedTime).ToLongTimeString()}"),
                            Enabled = true
                        });
                    }
                    else if (Frequency == "Daily")
                    {
                        td.Triggers.Add(new DailyTrigger
                        {
                            StartBoundary = Convert.ToDateTime($"{Convert.ToDateTime(SchedDate).ToShortDateString()} {Convert.ToDateTime(SchedTime).ToLongTimeString()}"),
                            //Repetition = new RepetitionPattern(TimeSpan.FromMinutes(20), TimeSpan.FromDays(1)),
                            Enabled = true
                        });
                    }
                    else if (Frequency == "Weekly")
                    {
                        td.Triggers.Add(new WeeklyTrigger
                        {
                            StartBoundary = Convert.ToDateTime($"{Convert.ToDateTime(SchedDate).ToShortDateString()} {Convert.ToDateTime(SchedTime).ToLongTimeString()}"),
                            //Repetition = new RepetitionPattern(TimeSpan.FromMinutes(20), TimeSpan.FromDays(1)),
                            DaysOfWeek = days(SchedDate),
                            Enabled = true
                        });
                    }
                    else if (Frequency == "Monthly")
                    {
                        td.Triggers.Add(new MonthlyTrigger
                        {
                            StartBoundary = Convert.ToDateTime($"{Convert.ToDateTime(SchedDate).ToShortDateString()} {Convert.ToDateTime(SchedTime).ToLongTimeString()}"),
                            MonthsOfYear = MonthsOfTheYear.AllMonths,
                            //Repetition = new RepetitionPattern(TimeSpan.FromMinutes(20), TimeSpan.FromDays(1)),
                            DaysOfMonth = daynum,
                            Enabled = true
                        });
                    }
                    td.Settings.AllowDemandStart = true;
                    //td.Settings.RunOnlyIfLoggedOn = true;
                    // Create an action that will launch Notepad whenever the trigger fires need to change path
                    //td.Actions.Add(new ExecAction(@"\\192.168.2.9\d$\FTPUploader\UploadFTP.exe", null));
                    //if (Type == "Upload")
                    //{
                    //td.Actions.Add(new ExecAction($@"{prop.FTPPath}UploadFTP.exe", null));
                    var uri = string.IsNullOrEmpty(Url) ? "http://localhost:8970/Linkbox/Post" : Url; //LIVE
                    //var uri = string.IsNullOrEmpty(Url) ? "http://localhost:8969/Linkbox/Post" : Url; //UAT
                    //var uri = string.IsNullOrEmpty(Url) ? "http://localhost:40710/Linkbox/Post" : Url;
                    td.Actions.Add(new ExecAction($@"powershell.exe", $@"-Command ""Invoke-RestMethod -Method '{method}' -Uri {uri}"""));
                    //}
                    //td.Actions.Add(new ExecAction(@"C:\Users\DIREC_JAMES\Documents\Visual Studio 2017\Projects\LinkBox\UploadFTP\bin\Debug\UploadFTP.exe", null));
                    // Register the task in the root folder

                    ts.RootFolder.RegisterTaskDefinition(SchedCode,
                                                         td,
                                                         TaskCreation.CreateOrUpdate,
                                                         user,
                                                         password);


                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        #endregion


        public static void UpdateSchedule(string SchedCode)
        {
            //Properties.Settings prop = new Properties.Settings();
            Settings prop = new Settings();
            using (TaskService ts = new TaskService(prop.ServerName,
                                                    prop.User,
                                                    prop.Domain,
                                                    prop.Password))
            {
                try
                {
                    foreach (var task in ts.AllTasks)
                    {
                        if (task.ToString() == SchedCode)
                        {
                            ts.RootFolder.DeleteTask(SchedCode);
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {

                }
            }
        }

        public static DaysOfTheWeek days(string SchedDate)
        {
            string day = System.DateTime.Parse(SchedDate).ToString("dddd");
            int i = -1;
            var getDate = DaysOfTheWeek.AllDays;
            Array value = Enum.GetValues(typeof(DaysOfTheWeek));
            foreach (var item in Enum.GetValues(typeof(DaysOfTheWeek)))
            {
                i++;
                if (day == item.ToString())
                {
                    getDate = (DaysOfTheWeek)value.GetValue(i);
                    break;
                }
            }
            return getDate;
        }

        public static List<string> getRunningTask(string ServerName, string User, string Domain, string Password)
        {

            //Properties.Settings prop = new Properties.Settings();
            List<string> Tasks = new List<string>();
            using (TaskService ts = new TaskService(ServerName,
                                                    User,
                                                    Domain,
                                                    Password))
            {
                foreach (var task in ts.GetRunningTasks())
                {
                    Tasks.Add(task.ToString());
                }
            }
            return Tasks;
        }

        public static void RunTask(string Code)
        {
            //Properties.Settings prop = new Properties.Settings();

            var ServerName = ConfigurationManager.AppSettings["ServerName"].ToString();
            var User = ConfigurationManager.AppSettings["User"].ToString();
            var Domain = ConfigurationManager.AppSettings["Domain"].ToString();
            var Password = ConfigurationManager.AppSettings["Password"].ToString();

            using (TaskService ts = new TaskService(ServerName,
                                                   User,
                                                   Domain,
                                                   Password))
            {
                Microsoft.Win32.TaskScheduler.Task task = ts.FindTask(Code);
                if (task != null)
                {               
                    if (task.State == TaskState.Running || task.State == TaskState.Queued)
                    {
                        task.Stop();
                        System.Threading.Thread.Sleep(2000);
                    }
                    task.Run();        
                    var state = task.State;
                }                
            }           
        }

        public static void EnableTask(string Code)
        {
            var ServerName = ConfigurationManager.AppSettings["ServerName"].ToString();
            var User = ConfigurationManager.AppSettings["User"].ToString();
            var Domain = ConfigurationManager.AppSettings["Domain"].ToString();
            var Password = ConfigurationManager.AppSettings["Password"].ToString();

            //Properties.Settings prop = new Properties.Settings();
            using (TaskService ts = new TaskService(ServerName,
                                                   User,
                                                   Domain,
                                                   Password))
            {
                Microsoft.Win32.TaskScheduler.Task task = ts.FindTask(Code);
                if (task != null)
                {
                    if (task.State == TaskState.Running || task.State == TaskState.Queued)
                    {
                        task.Stop();
                        System.Threading.Thread.Sleep(2000);
                    }
                    task.Enabled = true;

                }
            }
        }

        public static void DisableTask(string Code)
        {
            //Properties.Settings prop = new Properties.Settings();

            var ServerName = ConfigurationManager.AppSettings["ServerName"].ToString();
            var User = ConfigurationManager.AppSettings["User"].ToString();
            var Domain = ConfigurationManager.AppSettings["Domain"].ToString();
            var Password = ConfigurationManager.AppSettings["Password"].ToString();
            using (TaskService ts = new TaskService(ServerName,
                                                   User,
                                                   Domain,
                                                   Password))
            {
                Microsoft.Win32.TaskScheduler.Task task = ts.FindTask(Code);
                if (task != null)
                {
                    if (task.State == TaskState.Running || task.State == TaskState.Queued)
                    {
                        task.Stop();
                        System.Threading.Thread.Sleep(2000);
                    }
                    task.Enabled = false;

                }
            }
        }
    }

    public class TaskSchedHelper
    {
        public void RunTask(string Code, string ServerName, string User, string Domain, string Password)
        {
            //Properties.Settings prop = new Properties.Settings();
            using (TaskService ts = new TaskService(ServerName,
                                                   User,
                                                   Domain,
                                                   Password))
            {
                Microsoft.Win32.TaskScheduler.Task task = ts.FindTask(Code);
                if (task != null)
                {
                    if (task.State == TaskState.Running || task.State == TaskState.Queued)
                    {
                        task.Stop();
                        System.Threading.Thread.Sleep(2000);
                    }
             
                    //await System.Threading.Tasks.Task.Run(() => {
                        //task.RunEx(TaskRunFlags.UseSessionId, 0, $@"{Domain}\{User}", "");
                        task.Run();
                        var state = task.State;
                    //});                    
                }
            }
        }
    }
}