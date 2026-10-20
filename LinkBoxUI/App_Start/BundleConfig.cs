using System.Web;
using System.Web.Optimization;

namespace LinkBoxUI
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                     "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      //"~/Scripts/admin/js/jquery.min.js",
                      "~/Scripts/jquery.validate.min.js",
                      "~/Scripts/jquery.validate.unobtrusive.min.js",
                      "~/Scripts/admin/js/bootstrap.min.js",
                      "~/Scripts/admin/js/jquery.dataTables.min.js",
                      "~/Scripts/admin/js/dataTables.bootstrap.min.js",
                      "~/Scripts/admin/js/adminlte.min.js",
                      "~/Scripts/admin/js/moment.min.js",
                      "~/Scripts/admin/js/fastclick.js",
                      "~/Scripts/admin/js/jquery.sparkline.min.js",
                      "~/Scripts/admin/js/jquery-jvectormap-1.2.2.min.js",
                      "~/Scripts/admin/js/jquery-jvectormap-world-mill-en.js",
                      "~/Scripts/admin/js/jquery.slimscroll.min.js",
                      "~/Scripts/admin/js/Chart.min.js",
                      "~/Scripts/admin/js/icheck.min.js",
                      "~/Scripts/admin/js/jquery.inputmask.js",
                      "~/Scripts/admin/js/jquery.knob.js",
                      "~/Scripts/admin/js/jquery.inputmask.date.extensions.js",
                      "~/Scripts/admin/js/jquery.inputmask.extensions.js",
                      "~/Scripts/admin/js/bootstrap3-wysihtml5.all.min.js",
                      "~/Scripts/admin/js/bootstrap-toggle.min.js",
                      "~/Scripts/admin/js/dashboard2.js",
                      "~/Scripts/admin/js/select2.full.min.js",
                      "~/Scripts/admin/js/sweetalert2.all.min.js",
                      //"~/Scripts/admin/js/buttons.flash.min.js",
                      "~/Scripts/admin/js/buttons.html5.min.js",
                      "~/Scripts/admin/js/buttons.print.min.js",
                      "~/Scripts/admin/js/dataTables.buttons.min.js",
                      "~/Scripts/admin/js/jszip.min.js",
                      "~/Scripts/admin/js/pdfmake.min.js",
                      "~/Scripts/admin/js/vfs_fonts.js",
                      "~/Scripts/admin/js/demo.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/admin/css/bootstrap-theme.min.css",
                      "~/Content/admin/css/bootstrap.min.css",
                      "~/Content/admin/css/font-awesome.min.css",
                      "~/Content/admin/css/ionicons.min.css",
                      "~/Content/admin/css/jquery-jvectormap.css",
                      "~/Content/admin/css/dataTables.bootstrap.min.css",
                      "~/Content/admin/css/select2.min.css",
                      "~/Content/admin/css/AdminLTE.min.css",
                      "~/Content/admin/css/iCheck/square/_all.css",
                      "~/Content/admin/css/iCheck/square/blue.css",
                      //"~/Content/admin/css/iCheck/square/blue.png",
                      "~/Content/admin/css/_all-skins.min.css",
                      "~/Content/admin/css/bootstrap3-wysihtml5.min.css",
                      "~/Content/admin/css/bootstrap-toggle.min.css",
                      "~/Content/admin/css/sweetalert2.min.css",
                      "~/Content/admin/css/hover-min.css",
                      "~/Content/admin/css/hover.css",
                      "~/Content/admin/css/custom.css",
                      "~/Content/Login.css",
                      "~/Content/custom.css"));

            bundles.Add(new ScriptBundle("~/bundles/adminlogin/bootstrap").Include(
                         "~/Scripts/admin/js/jquery.min.js",
                         "~/Scripts/admin/js/bootstrap.min.js"
                         ));

            bundles.Add(new StyleBundle("~/Content/adminlogin/css").Include(
                      "~/Content/admin/css/bootstrap.min.css",
                      "~/Content/admin/css/login.css"
                      ));
        }
    }
}
