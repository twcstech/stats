using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StatsGUI.Models;
using StatsGUI.DAL;
using System.Web.Configuration;
using MultiLanMVC;
using System.Threading;
using System.IO;
using MVCImportExcel;
using Excel;
using Microsoft.Office.Interop.Excel;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using Stats_GUI.DAL;

namespace StatsGUI.Controllers
{
    public class QueueController : Controller
    {
        string lang = "";
        FormsRepository repo = new FormsRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
        List<Users> employees = new List<Users>();
        List<Users> reps = new List<Users>();
        List<Users> supervisors = new List<Users>();
        List<Users> managers = new List<Users>();
        List<Users> astdirectors = new List<Users>();
        List<Users> directors = new List<Users>();
        List<Users> accounting = new List<Users>();
        // GET: Queue 
       

        [HttpPost]
        public ActionResult Queue(string Translate="", string sortOrder = "",SearchArgs sargs = null, string searchString = "", string culture = "")
        {
            TempData["Message"] = "";
            TempData["Outdated"] = "";
            string adminLogin = User.Identity.Name;
           
            TempData["Login"] = Login(User.Identity.Name);
            adminLogin = adminLogin.Substring(adminLogin.IndexOf('\\') + 1);
            QueueRepository repo = new QueueRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
            FormsRepository frepo = new FormsRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
            int role = Find(adminLogin);
            StatItem temp = frepo.GetEmployee(User.Identity.Name);
            if (sargs == null)
            {
                sargs = new SearchArgs();
            }
            StatItemList statItemList = new StatItemList
            {
                tcsLogin = User.Identity.Name.Substring(User.Identity.Name.IndexOf('\\') + 1),
                sargs = sargs
            };

         
            ViewBag.Message = temp.role;
            string message = Resources.Resources.alert + " \\n";
            List<StatItem> lstStatItems = repo.GetPendingQueues(adminLogin, sargs);
            List<StatItem> pastDue = new List<StatItem>();
            foreach (var item in lstStatItems)
            {
                StatItem employee = repo.GetEmployee(item.createdBy);
                DateTime alert = Convert.ToDateTime(item.createdOn).AddDays(5);
                if (employee.userId == User.Identity.Name)
                {
                    if (!item.status.Contains("Processing") && item.status != "Ready for Processing" && item.status != "Completed"&& item.status != "Denied")
                    {

                        if (DateTime.Now > alert)
                        {
                            pastDue.Add(item);

                        }
                    }
                }
            }
            if (pastDue.Count != 0)
            {
                ViewBag.Alert = message;
                foreach (var item in pastDue)
                {
                    {
                        ViewBag.Alert += "\\n " + Resources.Resources.Account+ " " + item.accountNumber + " " + Resources.Resources.AccountName + " " + item.accountName + " "+Resources.Resources.Type2+" " + item.itemTypeCode + " \\n";
                    }
                }
            }
            if (temp.role == "accountant")
            {
                sargs.status = "Ready For Processing";
                lstStatItems = repo.GetPendingQueues(adminLogin, sargs);
            }
            else { sargs.status = " "; }
          
            ViewBag.AccountNumberParm = sortOrder == "Account Number" ? "numb_desc" : "Account Number";
            ViewBag.AccountNameParm = sortOrder == "Account Name" ? "name_desc" : "Account Name";
            ViewBag.ItemTypeParm = sortOrder == "Item Type" ? "type_desc" : "Item Type";
            ViewBag.CreatedOnParm = sortOrder == "Created On" ? "con_desc" : "Created On";
            ViewBag.CreatedByParm = sortOrder == "Created By" ? "cby_desc" : "Created By";
            ViewBag.SupvParm = sortOrder == "Super" ? "supv_desc" : "Super";
            ViewBag.MgrParm = sortOrder == "Manager" ? "mgr_desc" : "Manager";
            ViewBag.AdtrParm = sortOrder == "Assitant" ? "ast_desc" : "Assistant";
            ViewBag.AstmParm = sortOrder == "Ast Manager" ? "astm_desc" : "Ast Manager";
            ViewBag.DirParm = sortOrder == "Director" ? "dir_desc" : "Director";
            ViewBag.Status = sortOrder == "Status" ? "stat_desc" : "Status";

            var certs = from s in lstStatItems
                        select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                certs = certs.Where(s => s.accountName.ToLower().Contains(searchString)
                || s.accountName.Contains(searchString)
                || s.accountNumber.ToString().Contains(searchString)
                || s.itemTypeCode.Contains(searchString)
                || s.createdOn.ToString().Contains(searchString)
                || s.createdBy.ToString().Contains(searchString)
                || s.createdBy.ToLower().ToString().Contains(searchString)
                || s.supvLogin.Contains(searchString)
                || s.supvLogin.ToLower().Contains(searchString)
                || s.mgrLogin.ToString().Contains(searchString)
                || s.mgrLogin.ToLower().ToString().Contains(searchString)
                || s.amgrLogin.ToString().Contains(searchString)
                || s.amgrLogin.ToLower().ToString().Contains(searchString)
                || s.adtrLogin.Contains(searchString)
                || s.adtrLogin.ToLower().Contains(searchString)
                || s.dtrLogin.ToString().Contains(searchString)
                || s.dtrLogin.ToLower().ToString().Contains(searchString)
                || s.status.Contains(searchString));
            }

            //culture = CultureHelper.GetImplementedCulture(culture);
            //Save culture in a cookie
            //if (Translate == "Español"||  Translate=="es")
            //{

            //    AsyncCallback callback = null;
                
            //    BeginExecuteCore(callback, "es");
            //    HttpCookie cookie = Request.Cookies["_culture"];
            //    if (cookie != null)
            //        cookie.Value = culture;   // update cookie value
            //    else
            //    {
            //        cookie = new HttpCookie("_culture");
            //        cookie.Value = culture;
            //        cookie.Expires = DateTime.Now.AddYears(1);
            //    }
                
            //    Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            //    Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            //    Response.Cookies.Add(cookie);
            //      statItemList.statItems = certs;         
            //statItemList.statusList = GetStatusSelectList(sargs.status);
            //statItemList.itemTypeList = GetItemTypeSelectList(sargs.itemType);
            //    TempData["lang"]="es";
            //    TempData["Login"] = Login(User.Identity.Name);
            //return View("Queue", statItemList);

            //}
            //if (Translate == "")
            //{

            //    AsyncCallback callback = null;               
            //    BeginExecuteCore(callback, "en-US");
            //    TempData["lang"]= "en-US";
            //    statItemList.statItems = certs;
            //    statItemList.statusList = GetStatusSelectList(sargs.status);
            //    statItemList.itemTypeList = GetItemTypeSelectList(sargs.itemType);
            //    TempData["Login"] = Login(User.Identity.Name);
            //    return View("Queue", statItemList);
            //}
           
            statItemList.statItems = certs;         
            statItemList.statusList = GetStatusSelectList(sargs.status);
            statItemList.itemTypeList = GetItemTypeSelectList(sargs.itemType);
            TempData["Login"] = Login(User.Identity.Name);
            return View("Queue", statItemList);
        }

       
        public ActionResult Queue(SearchArgs sargs = null, string sortOrder = "", string searchString = "", string culture = "")
        {
          
            TempData["Message"] = "";
            TempData["Outdated"] = "";
            string adminLogin = User.Identity.Name;
          
            TempData["Login"] = Login(User.Identity.Name);
            adminLogin = adminLogin.Substring(adminLogin.IndexOf('\\') + 1);
            QueueRepository repo = new QueueRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
            FormsRepository frepo = new FormsRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
            UserRepository urepo = new UserRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
           
            List<Users> users = urepo.GetUsers();
      
          
            int role = Find(adminLogin);
            StatItem temp = frepo.GetEmployee(User.Identity.Name);
            if (sargs == null)
            {
                sargs = new SearchArgs();
            }
            StatItemList statItemList = new StatItemList
            {
                tcsLogin = User.Identity.Name.Substring(User.Identity.Name.IndexOf('\\') + 1),
                sargs = sargs
            };

            if (culture.Contains("es"))
            {
                HttpCookie cookie = Request.Cookies["_culture"];
                if (cookie.Value == "es")
                {
                    TempData["lang"] = "es";
                    QueueController queueMethods = new QueueController();
                    queueMethods.ChangeLanguage("es");
                }
            }

            statItemList.statusList = GetStatusSelectList(sargs.status);
            statItemList.itemTypeList = GetItemTypeSelectList(sargs.itemType);
            TempData["role"] = temp.role;
            string message = Resources.Resources.alert + " \\n";
            List<StatItem> lstStatItems = repo.GetPendingQueues(adminLogin, sargs);
            List<StatItem> pastDue = new List<StatItem>();
            foreach (var item in lstStatItems)
            {
                StatItem employee = repo.GetEmployee(item.createdBy);
                DateTime alert = Convert.ToDateTime(item.createdOn).AddDays(2);
                if (employee.userId == User.Identity.Name)
                {
                    if (item.status != "Ready for Processing" && item.status != "Ready For Processing" && item.status != "Completed" && item.status != "Denied")
                    {

                        if (DateTime.Now > alert)
                        {
                            pastDue.Add(item);

                        }
                    }
                }
            }
            if (pastDue.Count != 0)
            {
                ViewBag.Alert = message;
                foreach (var item in pastDue)
                {
                    {
                        ViewBag.Alert += "\\n " + Resources.Resources.Account + " " + item.accountNumber + " " + Resources.Resources.AccountName + " " + item.accountName + " " + Resources.Resources.Type2 + " " + item.itemTypeCode + " \\n";

                    }
                }
            }
             if (temp.role == "accountant"||temp.role == "supaccountant")
            {
                sargs.status = "Ready For Processing";
                lstStatItems = repo.GetPendingQueues(adminLogin, sargs);
            }
            ViewBag.AccountNumberParm = sortOrder == "Account Number" ? "numb_desc" : "Account Number";
            ViewBag.AccountNameParm = sortOrder == "Account Name" ? "name_desc" : "Account Name";
            ViewBag.ItemTypeParm = sortOrder == "Item Type" ? "type_desc" : "Item Type";
            ViewBag.CreatedOnParm = sortOrder == "Created On" ? "con_desc" : "Created On";
            ViewBag.CreatedByParm = sortOrder == "Created By" ? "cby_desc" : "Created By";
            ViewBag.SupvParm = sortOrder == "Super" ? "supv_desc" : "Super";
            ViewBag.MgrParm = sortOrder == "Manager" ? "mgr_desc" : "Manager";
            ViewBag.AdtrParm = sortOrder == "Assitant" ? "ast_desc" : "Assistant";
            ViewBag.AstmParm = sortOrder == "Ast Manager" ? "astm_desc" : "Ast Manager";
            ViewBag.DirParm = sortOrder == "Director" ? "dir_desc" : "Director";
            ViewBag.Status = sortOrder == "Status" ? "stat_desc" : "Status";

            var certs = from s in lstStatItems
                        select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                certs = certs.Where(s => s.accountName.Contains(searchString)
                || s.accountNumber.ToString().Contains(searchString)
                || s.itemTypeCode.Contains(searchString)
                || s.createdOn.ToString().Contains(searchString)
                || s.createdBy.ToString().Contains(searchString)
                || s.supvLogin.Contains(searchString)
                || s.mgrLogin.ToString().Contains(searchString)
                || s.amgrLogin.ToString().Contains(searchString)
                || s.adtrLogin.Contains(searchString)
                || s.dtrLogin.ToString().Contains(searchString)
                || s.status.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "Account Number":
                    certs = certs.OrderBy(s => s.accountNumber);
                    break;
                case "numb_desc":
                    certs = certs.OrderByDescending(s => s.accountNumber);
                    break;
                case "Account Name":
                    certs = certs.OrderBy(s => s.accountName);
                    break;
                case "name_desc":
                    certs = certs.OrderByDescending(s => s.accountName);
                    break;
                case "Item Type":
                    certs = certs.OrderBy(s => s.itemTypeCode);
                    break;
                case "item_desc":
                    certs = certs.OrderByDescending(s => s.itemTypeCode);
                    break;
                case "Created On":
                    certs = certs.OrderBy(s => s.createdOn).ThenBy(s=>s.createdOn);
                    break;
                case "con_desc":
                    certs = certs.OrderByDescending(s => s.createdOn).ThenBy(s => s.createdOn);
                    break;
                case "Created By":
                    certs = certs.OrderBy(s => s.createdBy);
                    break;
                case "cby_desc":
                    certs = certs.OrderByDescending(s => s.createdBy);
                    break;
                case "Super":
                    certs = certs.OrderBy(s => s.supvLogin);
                    break;
                case "supv_desc":
                    certs = certs.OrderByDescending(s => s.supvLogin);
                    break;
                case "Manager":
                    certs = certs.OrderBy(s => s.mgrLogin);
                    break;
                case "mgr_desc":
                    certs = certs.OrderByDescending(s => s.mgrLogin);
                    break;
                case "Assitant":
                    certs = certs.OrderBy(s => s.adtrLogin);
                    break;
                case "ast_desc":
                    certs = certs.OrderByDescending(s => s.adtrLogin);
                    break;
                case "Ast Manager":
                    certs = certs.OrderBy(s => s.adtrLogin);
                    break;
                case "astm_desc":
                    certs = certs.OrderByDescending(s => s.adtrLogin);
                    break;
                case "Director":
                    certs = certs.OrderBy(s => s.dtrLogin);
                    break;
                case "dir_desc":
                    certs = certs.OrderByDescending(s => s.dtrLogin);
                    break;
                case "Status":
                    certs = certs.OrderBy(s => s.status);
                    break;
                case "stat_desc":
                    certs = certs.OrderByDescending(s => s.status);
                    break;
                default:
                    certs = certs.OrderBy(s => s.accountNumber);
                    break;
            }
      
       

            statItemList.statItems = certs;
            return View("Queue", statItemList);
        }

        public int Find(string tcs)
        {
            Users user = new Users();
         
            employees = repo.GetEmployees();
            user = employees.Find(x => x.tcsNumber==tcs);
            reps.Add(employees.Find(x => x.role == "rep"));
            supervisors.Add(employees.Find(x => x.role == "supervisor"));
            managers.Add(employees.Find(x => x.role == "manager"));
            astdirectors.Add(employees.Find(x => x.role == "assistant"));
            directors.Add(employees.Find(x => x.role == "director"));
            accounting.Add(employees.Find(x => x.role == "accountant"));
 
            var det = (from d in reps
                       where d.tcsNumber == tcs
                       select d).ToList();
            if (det.Count != 0)
            {
                return 1;
            }

            det = (from d in supervisors
                   where d.tcsNumber == tcs
                   select d).ToList();
            if (det.Count != 0)
            {
                return 2;
            }
            det = (from d in managers
                   where d.tcsNumber == tcs
                   select d).ToList();
            if (det.Count != 0)
            {
                return 3;
            }
            det = (from d in astdirectors
                   where d.tcsNumber == tcs
                   select d).ToList();
            if (det.Count != 0)
            {
                return 4;
            }
            det = (from d in directors
                   where d.tcsNumber == tcs
                   select d).ToList();
            if (det.Count != 0)
            {
                return 5;
            }
            det = (from d in accounting
                   where d.tcsNumber == tcs
                   select d).ToList();
            if (det.Count != 0)
            {
                return 6;
            }
            return 0;
        }
        public string Login(string tcs)
        {
            Users user = new Users();
            employees = repo.GetEmployees();
            user = employees.Find(x => x.tcsNumber == tcs);          
            return user.tcsName;
        }
        private IEnumerable<SelectListItem> GetStatusSelectList(string selectedItem)
        {
            List<SelectListItem> statusList = new List<SelectListItem>();
            statusList.Add(new SelectListItem() { Value = " ", Text = Resources.Resources.Show });         
            statusList.Add(new SelectListItem() { Value = "Ready For Processing", Text = Resources.Resources.Ready });
            statusList.Add(new SelectListItem() { Value = "Completed", Text = Resources.Resources.Complete });
            statusList.Add(new SelectListItem() { Value = "Denied", Text = "Denied" });
            statusList.Add(new SelectListItem() { Value = "Awaiting", Text = Resources.Resources.Waiting });
            statusList.Add(new SelectListItem() { Value = "SUP", Text = Resources.Resources.Super});
            statusList.Add(new SelectListItem() { Value = "MAN", Text = Resources.Resources.Manager });
            statusList.Add(new SelectListItem() { Value = "AMAN", Text = Resources.Resources.AMan });
            statusList.Add(new SelectListItem() { Value = "ADIR", Text = Resources.Resources.ADir });
            statusList.Add(new SelectListItem() { Value = "DIR", Text = Resources.Resources.Dir });
            return new SelectList(statusList, "Value", "Text", selectedItem);
        }

        private IEnumerable<SelectListItem> GetItemTypeSelectList(string selectedItem)
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem() { Value = "sa", Text = Resources.Resources.Show });
            typeList.Add(new SelectListItem() { Value = "adv", Text = Resources.Resources.Adv });
            typeList.Add(new SelectListItem() { Value = "wof", Text = Resources.Resources.Wof });
            typeList.Add(new SelectListItem() { Value = "bal", Text = Resources.Resources.Bal });
            typeList.Add(new SelectListItem() { Value = "def", Text = Resources.Resources.Def });
            typeList.Add(new SelectListItem() { Value = "rdf", Text = Resources.Resources.Rdf });
            typeList.Add(new SelectListItem() { Value = "rem", Text = Resources.Resources.Rem });
            typeList.Add(new SelectListItem() { Value = "smt", Text = Resources.Resources.Stm });
            typeList.Add(new SelectListItem() { Value = "rev", Text = Resources.Resources.Rev });
            typeList.Add(new SelectListItem() { Value = "cci", Text = Resources.Resources.Cci });
            typeList.Add(new SelectListItem() { Value = "pif", Text = Resources.Resources.Pif });
            typeList.Add(new SelectListItem() { Value = "ddc", Text = Resources.Resources.Ddc });
            typeList.Add(new SelectListItem() { Value = "sec", Text = Resources.Resources.Sec });
            return new SelectList(typeList, "Value", "Text", selectedItem);
        }

        private IEnumerable<SelectListItem> GetSortSelectList(string selectedItem)
        {
            List<SelectListItem> sortList = new List<SelectListItem>();
            sortList.Add(new SelectListItem() { Value = "ds", Text = "Status" });
            sortList.Add(new SelectListItem() { Value = "adv", Text = "Date Submitted" });
            sortList.Add(new SelectListItem() { Value = "wof", Text = "Type" });
            return new SelectList(sortList, "Value", "Text", selectedItem);
        }
        protected override IAsyncResult BeginExecuteCore(AsyncCallback callback, object state)
      {
            string cultureName = null;

            // Attempt to read the culture cookie from Request
            HttpCookie cultureCookie = Request.Cookies["_culture"];
          
            if (cultureCookie != null)
                cultureName =Convert.ToString(TempData["lang"]);
         
            else
                cultureName = Request.UserLanguages != null && Request.UserLanguages.Length > 0 ?
                        Request.UserLanguages[0] :  // obtain it from HTTP header AcceptLanguages
                        null;
            // Validate culture name
            //cultureName = CultureHelper.GetImplementedCulture(cultureName); // This is safe

            // Modify current thread's cultures            
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cultureName);
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
            return base.BeginExecuteCore(callback, state);
        }
        public ActionResult ChangeLanguage(string lang)
        {
            //new LanguageMang().SetLanguage(lang);
            

            return RedirectToAction("Queue", "Queue");
        }
    }
}