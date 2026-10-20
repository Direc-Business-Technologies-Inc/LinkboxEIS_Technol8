using DomainLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Filters;
using System.Web.Mvc;
using System.Web.Routing;
using LinkBoxUI.Context;
namespace LinkBoxUI.SessionChecker
{
    public class SessionCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpSessionStateBase session = filterContext.HttpContext.Session;
            if (session != null && session["Id"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary {
                                { "Controller", "Home" },
                                { "Action", "Login" }
                    });
            }
        }
    }

    public class UserDetailsCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 1)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class AuthorizationCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 2)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class SetupCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 3)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class FieldMappingCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 4)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class DepositCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 5)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class EmailSetupCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 6)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class UploadSchedCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 8)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class ProcessSetupCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 7)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class SyncCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 9)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

    public class UploaderCheck : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (LinkboxDb db = new LinkboxDb())
            {
                HttpSessionStateBase session = filterContext.HttpContext.Session;
                bool authorize = true;
                int Id = (int)session["Id"];
                var mod = from um in db.UserModules.Where(a => a.UserId == Id && a.IsActive == true)
                          select um.ModId;
                mod.ToList();

                foreach (var x in mod)
                {
                    if (x == 10)
                    {
                        authorize = true;
                    }
                }
                //if (!authorize)
                //{
                //    filterContext.Result = new RedirectToRouteResult(
                //        new RouteValueDictionary {
                //                { "Controller", "Home" },
                //                { "Action", "Index" }
                //        });
                //}
            }
        }
    }

}