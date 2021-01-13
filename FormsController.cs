using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StatsGUI.Models;
using StatsGUI.DAL;
using System.Web.Configuration;
using System.Globalization;
using Stats_GUI.DAL;
using System.IO;

using MultiLanMVC;
using System.Threading;
using System.Data;
using System.Data.OleDb;
using MVCImportExcel.Models;
using System.Text.RegularExpressions;

namespace StatsGUI.Controllers
{
    public class FormsController : Controller
    {
        private static bool newForm = false;//check for old forms
        private static bool oldForm = false;//check for old forms
        private static bool Saved = false;//check for old forms
        private static string url;//capture url
        private static string state = "";//for saving statuses on postbackk
        private static string lang = "";//for saving lanugauge statuses
        private static string createdOn = "";//saved for post back
        private static int Id = 0;    //saved for post back
        private static int numFees = 0;//saved for post back
        private static int nsfFees = 0;//saved for post back
        private static int type = 0;//saved for post back
        private static decimal? amount = 0;    //saved for post back   
        GeneralMethods gen = new GeneralMethods();
        FormsRepository repo = new FormsRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
        QueueRepository quesrepo = new QueueRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
         UserRepository urepo = new UserRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
        //list for saving employees
        string number ="";
        List<Users> tcsMan = new List<Users>();
        List<Users> custMan = new List<Users>();
        List<Users> tcsSup = new List<Users>();
        List<Users> custSup = new List<Users>();
        List<Users> tmcSup = new List<Users>();
        List<Users> tmcMan = new List<Users>();
        List<Users> acct = new List<Users>();
        List<Users> adir = new List<Users>();
        List<Users> dir = new List<Users>();
        List<Users> kmSup = new List<Users>();
        List<Users> employees = new List<Users>();
        List<Users> reps = new List<Users>();
        List<Users> supervisors = new List<Users>();
        List<Users> managers = new List<Users>();
        List<Users> astdirectors = new List<Users>();
        List<Users> directors = new List<Users>();
        List<Users> accounting = new List<Users>();
        private object adv;

        [OutputCache(Duration = 10, VaryByParam = "none")]
        //login searches employeee table on start up
        public string Login(string tcsNumber)
        {
            Users user = new Users();
            employees = repo.GetEmployees();
            user = employees.Find(x => x.tcsNumber == tcsNumber);
            return user.tcsName;
        }
        

        public ActionResult FormShow(int id = -1, string itemType = "")
        {
            string methodName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(itemType);
            if (methodName == "Smt") {
                methodName = "stm";
            }
            return RedirectToAction(methodName, new { id });

        }

        [HttpPost]
        public ActionResult OnSubmitForm(Object objModel)
        {
            string adminLogin = User.Identity.Name;            
            return RedirectToAction("Queue", "Queue", null);
          
        }
        //adv form
  
        public ActionResult Adv(Adv adv, int id = -1)

        {
            if (id == 0)
            {
                id = Id;
            }
            Saved = false;
            oldForm = false;
            Session[SessionKeys.folderID] = 0;
            lang =Convert.ToString(Session[SessionKeys.lang]);
            //establish language

            //    HttpCookie cookie = Request.Cookies["_culture"];
            //    if (cookie.Value == "es")
            //    {
            //        TempData["lang"] = "es";
            //        QueueController queueMethods = new QueueController();
            //        queueMethods.ChangeLanguage("es");
            //    }

            //get url for notifications
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            // get administartors for notifications
           
            //set error handling for 9-digit account
           
            string login = User.Identity.Name;              
           //build form
            if (id > 0)    // edit
            {   adv = repo.Adv(id);//insert
              
               /* adv.admins = adminList;  */    //get admin list         
                adv.callbackName = "OnSubmitAdv";
                oldForm = true;
                Saved = true;
                if (adv.folderId == 0)
                {
                    newForm = true;
                }
                
                Session[SessionKeys.folderID] = adv.folderId;
                state = adv.status;//save for postback
                Id = adv.Id;//save for postback
                adv.pageHeading = Resources.Resources.Adv+" #" + id.ToString() + "";
                StatItem tempUser = repo.GetEmployee(login);//build employee
                adv.role = tempUser.role;
                TempData["number"] = urepo.GetEmployee(adv.createdBy).tcsNumber;
                TempData["role"] = tempUser.role;
                adv.userId = tempUser.userId;
                if (adv.advancedDate != "")
                {
                    if (Convert.ToDateTime(adv.advancedDate) < DateTime.Now) {

                        TempData["Outdated"] = "This Stat item is outdated. Update the CPS Advanced Date before approving or requesting approval";
                    }
                }
                if (adv.advancedInvoice != "")
                {
                    if (Convert.ToDateTime(adv.advancedInvoice) < DateTime.Now)
                    {

                        TempData["Outdated"] = "This Stat item is outdated. Update the New Invoice Date before approving or requesting approval";
                    }
                }
                if (adv.advancedMultiple != "")
                {
                    if (Convert.ToDateTime(adv.advancedMultiple) < DateTime.Now)
                    {

                        TempData["Outdated"] = "This Stat item is outdated. Update the Multiple Payments Date before approving or requesting approval";
                    }
                }


                return View("adv", adv);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length-1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                adv = new Adv();
                //adv.admins = adminList;
                adv.Id = -1;// set id to represent new form
                adv.status = "Waiting Approval";
                adv.pageHeading = Resources.Resources.Adv;
                adv.callbackName = "OnSubmitAdv";
                return View("adv", adv);
            }
        }

        [HttpPost]
        public ActionResult OnSubmitAdv(Adv adv, string Approved,string submitForm)
        {
         
            int result;
            object newId = Id;
            DateTime date = DateTime.Now; 
            //variables for date parameters
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }

            }
            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                adv.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            //declare account number cannot establish always starts off null

            if (adv.accountNumber == null) { adv.accountNumber = ""; }
            
           //does not catch if account number.length=0 will fix soon
            if (adv.accountNumber.Length != 9 && adv.accountNumber != ""|| Regex.Matches(adv.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == true)
                {

                    adv.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("Adv", new
                    {
                        adv = adv,
                        id = adv.Id
                    }
             );
                }
                if (Saved == false)
                {

                    adv.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("Adv",
                          adv = adv
                  );
                }
            }
       

            switch (adv.advType)
            {
                case 0:
                    if ((adv.feeAmount == 0 || adv.feeAmount == null) || adv.feeAmount > Convert.ToDecimal(5.99))
                    {
                        if (Saved == true)
                        {
                            
                        adv.Id = Id;
                            TempData["Message"] = Resources.Resources.valid;
                            return RedirectToAction("Adv", new
                        {
                            adv = adv,
                            id = adv.Id
                        }
                     );
                    }
                        if (Saved == false) { 

                        adv.Id = -1;
                        TempData["Message"] = Resources.Resources.valid;
                        return RedirectToAction("Adv",
                              adv = adv
                      );
                    }
                     }
                   
                   

                      if (adv.advancedDate == null)
                    {//date cant be null

                        if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.validdate;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.validdate;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        }
                    }
               
                    if (DateTime.Now > Convert.ToDateTime(adv.advancedDate))
                    {//date cannot be in the past
                        if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.past;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.past;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        }
                    }
                    break;                   

                case 1:
                    if (adv.advancedInvoice == null)
                    {//invoice cant be null
                          if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.validdate;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.validdate;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        }
                    }
                    if (DateTime.Now > Convert.ToDateTime(adv.advancedInvoice))
                    {//date cant be in the past
                        if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.past;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.past;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        }
                    }
                    break;

                case 2:

                    if (DateTime.Now > Convert.ToDateTime(adv.advancedMultiple))
                    {//date cant be in the past
                        if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.past;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.past;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        };
                    }
                    if (Convert.ToDateTime(adv.paymentDate) > Convert.ToDateTime(adv.advancedMultiple))
                    {//payment date must be before new date
                        if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.payad;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.payad;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        }
                    }

                    if (adv.advancedMultiple == null)
                    {//must have date
                       if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.validdate;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.validdate;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        }
                    }
                    if (adv.multipleAmount == 0)
                    {//must have amount
                       if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.valid;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.valid;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        }
                    }
                    if (adv.paymentDate == null)
                    {//must have date
                        if (Saved == true)
                        {
                            adv.Id = Id;
                            TempData["Message"] = Resources.Resources.validdate;
                            return RedirectToAction("Adv", new
                            {
                                adv = adv,
                                id = adv.Id
                            }
                     );
                        }
                        if (Saved == false)
                        {

                            adv.Id = -1;
                            TempData["Message"] = Resources.Resources.validdate;
                            return RedirectToAction("Adv",
                                  adv = adv
                          );
                        }
                    }
                   break;
            }
      if (adv.multipleAmount > 500)
            {
                adv.disable = false;
            }
                    adv.status = state;
            //status change
            if (adv.status == "Awaiting Approval")
            {
                adv.status = "";
            }

            switch (Approved)
            {
                case "Update":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = state;
                    adv.userId = Login(User.Identity.Name);
                    if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                    {
                        adv.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                    }
                   
                    result = repo.AdvUpdate(adv, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "deny":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "Denied";
                    string tcs = Convert.ToString(TempData["number"]);
                    adv.createdOn = Convert.ToString(DateTime.Now);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", adv.accountNumber + " " + adv.accountName + ": Stat Form Denial", adv);
                    result = repo.AdvUpdate(adv, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "supacct":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    string savedUrl = url;
                    //gen.SendEmail(savedUrl, acct, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "supkms":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "SUP Approved ";
                    adv.supvLogin = Login(User.Identity.Name);
                    adv.supvApproveDate = DateTime.Now.ToString();
                    adv.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    //gen.SendEmail(savedUrl, acct, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtmm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcs":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcss":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amadir":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AMAN Approved ";
                    adv.amgrLogin = Login(User.Identity.Name);
                    adv.amgrApproveDate = DateTime.Now.ToString();
                    adv.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    //gen.SendEmail(savedUrl, acct, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;                   
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcs":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mandir":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "MAN Approved ";
                    adv.mgrLogin = Login(User.Identity.Name);
                    adv.mgrApproveDate = DateTime.Now.ToString();
                    adv.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adacct":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    //gen.SendEmail(savedUrl, acct, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcsm":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcss":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "AD Approved ";
                    adv.adtrLogin = Login(User.Identity.Name);
                    adv.adtrApproveDate = DateTime.Now.ToString();
                    adv.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, adv);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);                
                  

                case "acct":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "Ready For Processing ";
                    adv.dtrLogin = Login(User.Identity.Name);
                    adv.dtrApproveDate = DateTime.Now.ToString();
                    adv.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    result = repo.AdvUpdate(adv, Approved);
                    savedUrl = url ;
                    //gen.SendEmail(savedUrl, acct, adv);                  
                    
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "Complete":
                    adv.Id = Convert.ToInt32(newId);
                    adv.status = "Completed";
                    adv.accountingLogin = Login(User.Identity.Name);
                    adv.dispositionDate = DateTime.Now.ToString();              
                    StatItem temp = new StatItem();
                    //build notification list
                    temp = quesrepo.GetEmployee(adv.createdBy);
                    List<string> superiors = new List<string>();

                    if (temp.userId != null)
                    {
                        superiors.Add(temp.userId);
                    }
                    if (adv.dtrLogin != null)
                    {
                        superiors.ToList().Add(adv.dtrLogin);
                    }
                    if (adv.adtrLogin != null)
                    {
                        superiors.ToList().Add(adv.adtrLogin);
                    }
                    if (adv.supvLogin != null)
                    {
                        superiors.ToList().Add(adv.supvLogin);
                    }
                    if (adv.amgrLogin != null)
                    {
                        superiors.ToList().Add(adv.amgrLogin);
                    }
                    if (adv.amgrLogin != null)
                    {
                        superiors.ToList().Add(adv.mgrLogin);
                    }
                    //notify creator, and all suoeriors
                    gen.SendAlert(url, superiors, "", adv.accountNumber + " " + adv.accountName + ": Stat Form Request");
                    //update form, save id
                    result = repo.AdvUpdate(adv, Approved);
                    return RedirectToAction("Queue", "Queue", null);
            }
           
            if (adv.Id <= 0)
            {//new form
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    adv.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                string adminLogin = User.Identity.Name;
                adv.userId = Login(adminLogin);
                adv.status = "Awaiting Approval";
                adv.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                adv.createdOn = Convert.ToString(DateTime.Now);
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }
                switch (submitForm)
                {
                    case "tcsMan":
                        adv.Id = Convert.ToInt32(newId);                                        
                        result = repo.AdvModify(adv);
                        string savedUrl = url+result;
                        gen.SendEmail(savedUrl, tcsMan, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        gen.SendEmail(savedUrl, tcsSup, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        gen.SendEmail(savedUrl, tmcMan, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        gen.SendEmail(savedUrl, tmcSup, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        gen.SendEmail(savedUrl, custSup, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        gen.SendEmail(savedUrl, custMan, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsadir":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        gen.SendEmail(savedUrl, adir, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        gen.SendEmail(savedUrl, dir, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsacct":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        //gen.SendEmail(savedUrl, acct, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":
                        adv.Id = Convert.ToInt32(newId);
                        result = repo.AdvModify(adv);
                        savedUrl = url + result;
                        gen.SendEmail(savedUrl, kmSup, adv);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }           
                
            }
                        
            return RedirectToAction("Queue", "Queue", null);
        }

        public ActionResult def(Def def, int id = -1, bool wasSaved = false)
        { 
            if (id == 0)
            {
                id = Id;
            }
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            Saved = false;
           oldForm = false;
            //set folder id
            Session[SessionKeys.folderID] = 0;
            //capture url
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            // build admin list
            //List<Users> adminList = new List<Users>();
            //adminList = repo.GetAdmins();
            //set error handling for 9-digit account
            if (def == null)
            {

                TempData["Message"] = Resources.Resources.digit;
            }
            ViewBag.Message = Resources.Resources.attreq;
            //build user
            string login = User.Identity.Name;
           

            if (id > 0)    // edit

            {
               
                def = repo.Def(id);
                if (def.folderId == 0)
                {
                    newForm = true;
                }

                state = def.status;
                //def.admins = adminList;
                def.callbackName = "OnSubmitDef";
                def.wasSaved = wasSaved;
                ViewBag.WasSaved = wasSaved;
                type = def.defType;
                Saved = true;
                oldForm = true;
                Session[SessionKeys.folderID] = def.folderId ;
                Id = def.Id;
                def.pageHeading = Resources.Resources.Def+ " #" + id.ToString() + "";
                TempData["number"] = urepo.GetEmployee(def.createdBy).tcsNumber;
                StatItem temp = repo.GetEmployee(login);
                def.role = temp.role;
                TempData["role"] = temp.role;
                def.userId = temp.userId;

                return View("def", def);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                def = new Def();
                def.Id = -1;                
                //def.admins = adminList;
                def.status = "Awaiting Approval";
                def.pageHeading = Resources.Resources.Def;
                def.callbackName = "OnSubmitDef";
                return View("def", def);
            }
        }

        [HttpPost]
        public ActionResult OnSubmitDef(Def def, IEnumerable<HttpPostedFileBase> files, string Approved, string submitForm)

        {
     

            int Rsl;
            object newId = Id;
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            if (def.accountNumber == null) { def.accountNumber = ""; }
            def.status = state;
            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                def.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }

            if (def.accountNumber.Length != 9 && def.accountNumber != ""||Regex.Matches(def.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == true)
                {

                    def.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("def", new
                    {
                        def = def,
                        id = def.Id
                    }
             );
                }
                if (Saved == false)
                {

                    def.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("def",
                          def = def
                  );
                }
            }

            if (def.isDouble == true)
            {

                switch (Approved)
                {
                    
                    
                    case "supacct":

                        //def.Id = Convert.ToInt32(newId);
                        //def.status = "SUP Approved";
                        //def.supvLogin = Login(User.Identity.Name);
                        //def.supvApproveDate = DateTime.Now.ToString();
                        //def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.DefUpdate(def, Approved);
                        //string savedUrl = url;
                        //gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                       string savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptmm":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supdir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcss":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supkms":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amacct":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtcm":
                        
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amtmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amcsm":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amtcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amcss":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amdir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amkms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "manacct":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "manadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "mandir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "mantcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancss":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mankms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adacct":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adtcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adcsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);
                    case "adcss":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "addir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adkms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "acct":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready for Processing ";
                        def.dtrLogin = Login(User.Identity.Name);
                        def.dtrApproveDate = DateTime.Now.ToString();
                        def.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);



                }

            }
            if (def.isException == true)
            {

                switch (Approved)
                {


                    case "supacct":

                        //def.Id = Convert.ToInt32(newId);
                        //def.status = "SUP Approved";
                        //def.supvLogin = Login(User.Identity.Name);
                        //def.supvApproveDate = DateTime.Now.ToString();
                        //def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.DefUpdate(def, Approved);
                        //string savedUrl = url;
                        //gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                       string savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptmm":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supdir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcss":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supkms":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amacct":
                        //def.Id = Convert.ToInt32(newId);
                        //def.status = "AMAN Approved";
                        //def.amgrLogin = Login(User.Identity.Name);
                        //def.amgrApproveDate = DateTime.Now.ToString();
                        //def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.DefUpdate(def, Approved);
                        //savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtcm":
                        ;
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amtmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amcsm":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amtcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amcss":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amdir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amkms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "manacct":
                        //def.Id = Convert.ToInt32(newId);
                        //def.status = "MAN Approved";
                        //def.mgrLogin = Login(User.Identity.Name);
                        //def.mgrApproveDate = DateTime.Now.ToString();
                        //def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.DefUpdate(def, Approved);
                        //savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "manadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "mandir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "mantcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancss":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mankms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adacct":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adtcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adcsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);
                    case "adcss":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "addir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adkms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "acct":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing ";
                        def.dtrLogin = Login(User.Identity.Name);
                        def.dtrApproveDate = DateTime.Now.ToString();
                        def.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DefUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);



                }

            }
            switch (Approved)
            {
                case "Update":

                    def.Id = Convert.ToInt32(newId);
                    def.status = state;
                    def.userId = Login(User.Identity.Name);
                
                    //if (def.user != null)
                    //{
                    //    string savedUrl = url;

                    //    gen.SendEmail(savedUrl, def.user, def);
                    //    foreach (var user in def.user)
                    //    {
                    //        def.userList += " " + repo.GetEmployee(user).userId;

                    //    }
                    //}
                    Rsl = repo.DefUpdate(def, Approved);
                    return RedirectToAction("Queue", "Queue", null);


                case "deny":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Denied";
                    string tcs = Convert.ToString(TempData["number"]);
                    def.createdOn = Convert.ToString(DateTime.Now);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", def.accountNumber + " " + def.accountName + ": Stat Form Denial", def);
                  Rsl=repo.DefUpdate(def, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "supacct":
                   
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                   string savedUrl = url;
                    //gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                    
                case "supcsm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);                   

                case "supadir":
                   def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supkms":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":                  
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    //gen.SendEmail(savedUrl, acct, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                   ;
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amtmm":                  
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                    
                case "amcsm":                   
         
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amtcs":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amcss":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amadir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amdir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    //gen.SendEmail(savedUrl, acct, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "mandir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "mantcs":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adacct":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    //gen.SendEmail(savedUrl, acct, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adtcm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adcsm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                case "adcss":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "acct":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready for Processing ";
                    def.dtrLogin = Login(User.Identity.Name);
                    def.dtrApproveDate = DateTime.Now.ToString();
                    def.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DefUpdate(def, Approved);
                    savedUrl = url;
                    //gen.SendEmail(savedUrl, acct, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "Complete":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Completed";
                    def.accountingLogin = Login(User.Identity.Name);
                    def.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.DefUpdate(def, Approved);
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(def.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (def.dtrLogin != null)
                    {
                        supervisors.ToList().Add(def.dtrLogin);
                    }
                    if (def.adtrLogin != null)
                    {
                        supervisors.ToList().Add(def.adtrLogin);
                    }
                    if (def.supvLogin != null)
                    {
                        supervisors.ToList().Add(def.supvLogin);
                    }
                    if (def.amgrLogin != null)
                    {
                        supervisors.ToList().Add(def.amgrLogin);
                    }
                    if (def.amgrLogin != null)
                    {
                        supervisors.ToList().Add(def.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", def.accountNumber + " " + def.accountName + ": Stat Form Request");
                    return RedirectToAction("Queue", "Queue", null);
            }
          
            def.status = state;
          
            if (def.Id <= 0)
            {
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    def.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                if ( Convert.ToInt32(Session[SessionKeys.folderID]) == 0)
                {
                    TempData["Message"] = Resources.Resources.attreq;
                    def.Id = -1;
                    return RedirectToAction("def", def);
                }
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }
                def.createdOn = Convert.ToString(DateTime.Now);
                switch (submitForm)
                {
                    case "tcsMan":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";                     
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                       string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;                       
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":

                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, def);
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsadir":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":

                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, def);
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsacct":

                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        //gen.SendEmail(savedUrl, custMan, def);
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":
                       
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;                      
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }

                //string adminLogin = User.Identity.Name;
                //def.userId = Login(adminLogin);
                //def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                //def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                //def.status = "Ready for Review";
                //TempData["Success"] = Resources.Resources.submitted;
               
                //newId = repo.DefModify(def, Request.Files);                
              
            }
            return RedirectToAction("Queue", "Queue", null);

        }
  
        public ActionResult rdf(Def def, int id = -1, bool wasSaved = false)
        {

            if (id == 0)
            {
                id = Id;
            }
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            Saved = false;
            oldForm = false;
          Session[SessionKeys.folderID] = 0;
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();
            if (def == null)
            {

                TempData["Message"] = Resources.Resources.submitted;
            }
            ViewBag.Message = Resources.Resources.attreq;
            string login = User.Identity.Name;
          

            if (id > 0)    // edit

            {
           
                def = repo.Rdf(id);
                if (def.folderId == 0)
                {
                    newForm = true;
                }
                state = def.status;
                //def.admins = statusList;
                def.callbackName = "OnSubmitRdf";
                def.wasSaved = wasSaved;
                ViewBag.WasSaved = wasSaved;
                type = def.defType;
                Saved = true;
                oldForm = true;
                Session[SessionKeys.folderID] = def.folderId;
                Id = def.Id;
                def.pageHeading = Resources.Resources.Rdf+" #" + id.ToString() + "";
                TempData["number"] = urepo.GetEmployee(def.createdBy).tcsNumber;
                StatItem temp = repo.GetEmployee(login);
                def.role = temp.role;
                TempData["role"] = temp.role;
                def.userId = temp.userId;
                return View("rdf", def);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                def = new Def();
                def.Id = -1;
                //def.admins = statusList;
                def.status = "Awaiting Approval";
                def.pageHeading = Resources.Resources.Rdf;
                def.callbackName = "OnSubmitRdf";
                return View("rdf", def);
            }
        }

        [HttpPost]
        public ActionResult OnSubmitRDf(Def def, IEnumerable<HttpPostedFileBase> files, string Approved, string submitForm)

        {

        
            int Rsl;
            object newId = Id;
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            if (def.accountNumber == null) { def.accountNumber = ""; }
            def.status = state;

            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                def.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            if (def.accountNumber.Length != 9 && def.accountNumber != ""||Regex.Matches(def.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == true)
                {

                    def.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("def", new
                    {
                        def = def,
                        id = def.Id
                    }
             );
                }
                if (Saved == false)
                {

                    def.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("def",
                          def = def
                  );
                }
            }

            if (def.isDouble == true)
            {

                switch (Approved)
                {


                    case "supacct":

                        //def.Id = Convert.ToInt32(newId);
                        //def.status = "SUP Approved";
                        //def.supvLogin = Login(User.Identity.Name);
                        //def.supvApproveDate = DateTime.Now.ToString();
                        //def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.RdfUpdate(def, Approved);
                        //string savedUrl = url;
                        //gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        string savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptmm":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supdir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcss":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supkms":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amacct":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtcm":
                        
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amtmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amcsm":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amtcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amcss":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amdir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amkms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "manacct":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "manadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "mandir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "mantcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancss":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mankms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adacct":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adtcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adcsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);
                    case "adcss":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "addir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adkms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "acct":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready for Processing ";
                        def.dtrLogin = Login(User.Identity.Name);
                        def.dtrApproveDate = DateTime.Now.ToString();
                        def.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);



                }

            }

            if (def.isException == true)
            {

                switch (Approved)
                {


                    case "supacct":

                        //def.Id = Convert.ToInt32(newId);
                        //def.status = "SUP Approved";
                        //def.supvLogin = Login(User.Identity.Name);
                        //def.supvApproveDate = DateTime.Now.ToString();
                        //def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.RdfUpdate(def, Approved);
                        //string savedUrl = url;
                        //gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                       string savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptmm":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supdir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcss":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supkms":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "SUP Approved";
                        def.supvLogin = Login(User.Identity.Name);
                        def.supvApproveDate = DateTime.Now.ToString();
                        def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amacct":
                        //def.Id = Convert.ToInt32(newId);
                        //def.status = "AMAN Approved";
                        //def.amgrLogin = Login(User.Identity.Name);
                        //def.amgrApproveDate = DateTime.Now.ToString();
                        //def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.RdfUpdate(def, Approved);
                        //savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtcm":
                        
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amtmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amcsm":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amtcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amcss":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amdir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amkms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "AMAN Approved";
                        def.amgrLogin = Login(User.Identity.Name);
                        def.amgrApproveDate = DateTime.Now.ToString();
                        def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "manacct":
                        //def.Id = Convert.ToInt32(newId);
                        //def.status = "MAN Approved";
                        //def.mgrLogin = Login(User.Identity.Name);
                        //def.mgrApproveDate = DateTime.Now.ToString();
                        //def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.RdfUpdate(def, Approved);
                        //savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "manadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "mandir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "mantcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancss":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mankms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "MAN Approved";
                        def.mgrLogin = Login(User.Identity.Name);
                        def.mgrApproveDate = DateTime.Now.ToString();
                        def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adacct":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adtcm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtmm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adcsm":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtcs":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);
                    case "adcss":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adadir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "addir":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adkms":
                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready For Processing";
                        def.adtrLogin = Login(User.Identity.Name);
                        def.adtrApproveDate = DateTime.Now.ToString();
                        def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "acct":

                        def.Id = Convert.ToInt32(newId);
                        def.status = "Ready for Processing ";
                        def.dtrLogin = Login(User.Identity.Name);
                        def.dtrApproveDate = DateTime.Now.ToString();
                        def.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.RdfUpdate(def, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, def);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);



                }

            }
            switch (Approved)
            {
                case "Update":

                    def.Id = Convert.ToInt32(newId);
                    def.status = state;
                    def.userId = Login(User.Identity.Name);

                    //if (def.user != null)
                    //{
                    //    string savedUrl = url;

                    //    gen.SendEmail(savedUrl, def.user, def);
                    //    foreach (var user in def.user)
                    //    {
                    //        def.userList += " " + repo.GetEmployee(user).userId;

                    //    }
                    //}
                    Rsl = repo.RdfUpdate(def, Approved);
                    return RedirectToAction("Queue", "Queue", null);


                case "deny":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Denied";
                    string tcs = Convert.ToString(TempData["number"]);
                    def.createdOn = Convert.ToString(DateTime.Now);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", def.accountNumber + " " + def.accountName + ": Stat Form Denial", def);
                    Rsl = repo.RdfUpdate(def, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "supacct":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    string savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supkms":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.supvLogin = Login(User.Identity.Name);
                    def.supvApproveDate = DateTime.Now.ToString();
                    def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    ;
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amtmm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amtcs":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amcss":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amadir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amdir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.amgrLogin = Login(User.Identity.Name);
                    def.amgrApproveDate = DateTime.Now.ToString();
                    def.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "mandir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "mantcs":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.mgrLogin = Login(User.Identity.Name);
                    def.mgrApproveDate = DateTime.Now.ToString();
                    def.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adacct":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adtcm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adcsm":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                case "adcss":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing";
                    def.adtrLogin = Login(User.Identity.Name);
                    def.adtrApproveDate = DateTime.Now.ToString();
                    def.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "acct":

                    def.Id = Convert.ToInt32(newId);
                    def.status = "Ready For Processing ";
                    def.dtrLogin = Login(User.Identity.Name);
                    def.dtrApproveDate = DateTime.Now.ToString();
                    def.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RdfUpdate(def, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, def);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "Complete":
                    def.Id = Convert.ToInt32(newId);
                    def.status = "Completed";
                    def.accountingLogin = Login(User.Identity.Name);
                    def.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.RdfUpdate(def, Approved);
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(def.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (def.dtrLogin != null)
                    {
                        supervisors.ToList().Add(def.dtrLogin);
                    }
                    if (def.adtrLogin != null)
                    {
                        supervisors.ToList().Add(def.adtrLogin);
                    }
                    if (def.supvLogin != null)
                    {
                        supervisors.ToList().Add(def.supvLogin);
                    }
                    if (def.amgrLogin != null)
                    {
                        supervisors.ToList().Add(def.amgrLogin);
                    }
                    if (def.amgrLogin != null)
                    {
                        supervisors.ToList().Add(def.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", def.accountNumber + " " + def.accountName + ": Stat Form Request");
                    return RedirectToAction("Queue", "Queue", null);
            }


            if (def.Id <= 0)
            {
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    def.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                if (def.Id == 0 && Convert.ToInt32(Session[SessionKeys.folderID]) == 0)
                {
                    def.Id = -1;
                    TempData["Message"] = Resources.Resources.attreq;
                    return RedirectToAction("rdf", def);
                }
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }
                def.createdOn = Convert.ToString(DateTime.Now);
                switch (submitForm)
                {
                    case "tcsMan":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.RdfModify(def, Request.Files);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.RdfModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.RdfModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.RdfModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.RdfModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":

                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.RdfModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, def);
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":

                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.RdfModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsadir":
                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, def);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":

                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, def);
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsacct":

                        def.Id = Convert.ToInt32(newId);
                        def.userId = Login(User.Identity.Name);
                        def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        def.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.DefModify(def, Request.Files);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, def);
                        return RedirectToAction("Queue", "Queue", null);

                }

                //string adminLogin = User.Identity.Name;
                //def.userId = Login(adminLogin);
                //def.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                //def.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                //def.status = "Ready for Review";
                //TempData["Success"] = Resources.Resources.submitted;              
                //newId = repo.RdfModify(def, Request.Files);
                //if (def.user != null)
                //{
                //    if (url.Contains("?"))
                //    {
                //        url = url.Substring(0, 31);
                //    }
                //    def.createdOn = Convert.ToString(DateTime.Now);
                //    string savedUrl = url;
                //    savedUrl += newId;
                //    //gen.SendEmail(savedUrl, def.user, def);
                //}
            }
            return RedirectToAction("Queue", "Queue", null);

        }
       
        public ActionResult rem(Rem rem, int id = -1, bool wasSaved = false)
        {
            if (id == 0)
            {
                id = Id;
            }
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            Saved = false;
            oldForm = false;
            Session[SessionKeys.folderID] = 0;


            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();


          
            string login = User.Identity.Name;
   
           
            if (id > 0)    // edit
            {
               
                rem = repo.rem(id);
                if (rem.folderId == 0)
                {
                    newForm = true;
                }

                state = rem.status;
                Session[SessionKeys.folderID] = rem.folderId;
                //rem.admins = statusList;
                numFees = rem.lateNumberFees;
                rem.callbackName = "OnSubmitRem";
                rem.wasSaved = wasSaved;                
                amount = rem.lateAmount;
                Saved = true;
                oldForm = true;
                ViewBag.WasSaved = wasSaved;
                Id = rem.Id;
                rem.pageHeading = Resources.Resources.Rem+" #" + id.ToString() + "";
                StatItem temp = repo.GetEmployee(login);
                rem.role = temp.role;
                TempData["number"] = urepo.GetEmployee(rem.createdBy).tcsNumber;
                TempData["role"] = temp.role;
                rem.userId = temp.userId;
                return View("rem", rem);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                rem = new Rem();
                //rem.admins = statusList;
                rem.Id = -1;
                rem.status = "Awaiting Approval";
                rem.pageHeading = Resources.Resources.Rem;
                rem.callbackName = "OnSubmitRem";
                return View("rem", rem);
            }
        }
        [HttpPost]
        public ActionResult OnSubmitRem(Rem rem, string Approved, string submitForm)
        {
         
            int Rsl;
            object newId = Id;
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            //Error Handling
            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                rem.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            if (rem.isLate == true)
            {

                if (rem.lateAmount == 0 || rem.lateAmount == null || rem.lateNumberFees == 0 || rem.lateReason == null || rem.lateReason == "")
                {
               
                    if (Saved == true)
                    {
                        rem.Id = Id;
                        TempData["Message"] = Resources.Resources.missinglate;
                        return RedirectToAction("rem", new
                        {
                            rem = rem,
                            id = rem.Id
                        }
         );
                    }
                    if (Saved == false)
                    {

                        rem.Id = -1;
                        TempData["Message"] = Resources.Resources.missinglate;
                        return RedirectToAction("rem",
                               rem = rem
                       );
                    }

                }

            }
            if (rem.isNSF == true)
            {

                if (rem.NSFAmount == 0 || rem.NSFAmount == null || rem.NSFnumberFees == 0 || rem.NSFreason == null || rem.NSFreason == "")
                {
                   

                    if (Saved == true)
                    {
                        rem.Id = Id;
                        TempData["Message"] = Resources.Resources.missingnsf;
                        return RedirectToAction("rem", new
                        {
                            rem = rem,
                            id = rem.Id
                        }
         );
                    }
                    if (Saved == false)
                    {
                        TempData["Message"] = Resources.Resources.missingnsf;
                        rem.Id = -1;
                        return RedirectToAction("rem",
                              rem = rem
                      );
                    }

                }
            }

            if (rem.isRepo == true)
            {

                if (rem.repoAmount == 0 || rem.repoAmount == null || rem.repoNumberFees == 0 || rem.repoReason == null || rem.repoReason == "")
                {
                   

                    if (Saved == true)
                    {
                        rem.Id = Id;
                        TempData["Message"] = Resources.Resources.missingrepo;
                        return RedirectToAction("rem", new
                        {
                            rem = rem,
                            id = rem.Id
                        }
         );
                    }
                    if (Saved == false)
                    {
                        TempData["Message"] = Resources.Resources.missingrepo;
                        rem.Id = -1;
                        return RedirectToAction("rem",
                              rem = rem
                      );
                    }
                }
            }
            if (rem.isCourt == true)
            {

                if (rem.courtAmount == 0 || rem.courtAmount == null || rem.courtNumberFees == 0 || rem.courtReason == null || rem.courtReason == "")
                {
                   

                    if (Saved == true)
                    {
                        rem.Id = Id;
                        TempData["Message"] = "Missing information for Court Fees";
                        return RedirectToAction("rem", new
                        {
                            rem = rem,
                            id = rem.Id
                        }
         );
                    }
                    if (Saved == false)
                    {
                        TempData["Message"] = "Missing information for Court Fees";
                        rem.Id = -1;
                        return RedirectToAction("rem",
                             rem = rem
                     );
                    }
                }
            }
            if (rem.isAttorney == true)
            {

                if (rem.attorneyAmount == 0 || rem.attorneyAmount == null || rem.attorneyNumberFees == 0 || rem.attorneyReason == null || rem.attorneyReason == "")
                {
                   
                    if (Saved == true)
                    {
                        rem.Id = Id;
                        TempData["Message"] = Resources.Resources.missingattorney;
                        return RedirectToAction("rem", new
                        {
                            rem = rem,
                            id = rem.Id
                        }
         );
                    }
                    if (Saved == false)
                    {
                        TempData["Message"] = Resources.Resources.missingattorney;
                        rem.Id = -1;
                        return RedirectToAction("rem",
                             rem = rem
                     );
                    }

                }
            }
            if (rem.isBank == true)
            {
                if (Convert.ToInt32(Session[SessionKeys.folderID]) == 0)
                {
                    if (Saved == true)
                    {
                        rem.Id = Id;
                        TempData["Message"] = Resources.Resources.attreq;
                        return RedirectToAction("rem", new
                        {
                            rem = rem,
                            id = rem.Id
                        }
         );
                    }
                        if (Saved == false)
                    {

                        TempData["Message"] = Resources.Resources.attreq;
                        rem.Id = -1;
                        return RedirectToAction("rem", rem);
                    }
                }
                if (rem.bankAmount == 0 || rem.bankAmount == null || rem.bankNumberFees == 0 || rem.bankReason == null || rem.bankReason == "")
                {

                    if (Saved == true)
                    {
                        rem.Id = Id;
                        TempData["Message"] = "Missing information for Bank fees";
                        return RedirectToAction("rem", new
                        {
                            rem = rem,
                            id = rem.Id
                        }
         );
                    }
                    if (Saved == false)
                    {
                        TempData["Message"] = "Missing information for Bank fees";
                        rem.Id = -1;
                        return RedirectToAction("rem",
                             rem = rem
                     );
                    }

                }
            }
            if (rem.isInterest == true)
            {

                if (rem.interestAmount == null || rem.interestAmount == 0)
                {
                   
                    if (Saved == true)
                    {
                        rem.Id = Id;
                        TempData["Message"] = Resources.Resources.validint;
                        return RedirectToAction("rem", new
                        {
                            rem = rem,
                            id = rem.Id
                        }
         );
                    }
                    if (Saved == false)
                    {
                        TempData["Message"] = Resources.Resources.validint;
                        rem.Id = -1;
                        return RedirectToAction("rem",
                              rem = rem
                      );
                    }
                }
            }

            if (rem.isFinance == true)
            {

                if (rem.financeAmount == null || rem.financeAmount == 0)
                {
                    TempData["Message"] = Resources.Resources.validfin;
                    rem.Id = -1;
                    return RedirectToAction("rem",
                          rem = rem
                  );
                }
            }

            if (rem.accountNumber == null) { rem.accountNumber = ""; }

            if (rem.accountNumber.Length != 9 && rem.accountNumber != "" || Regex.Matches(rem.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == false)
                {
                    TempData["Message"] = Resources.Resources.digit;
                    rem.Id = -1;
                    return RedirectToAction("rem",
                          rem = rem
                  );
                }
                if (Saved == true)
                {
                    rem.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("rem", new
                    {
                        rem = rem,
                        id = rem.Id
                    }
     );
                }
            }
        
            //Response to update
           
            //status change
          
            switch (Approved)
            {
                case "Update":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = state;
                    rem.userId = Login(User.Identity.Name);             
                    
                    Rsl = repo.RemUpdate(rem, Approved);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                 
            }
        
            rem.status = state;

            if (rem.status == "Awaiting Approval")
            {
                rem.status = "";
            }
            if (Approved != null)
            {
                rem.lateNumberFees = numFees;
              
                rem.lateAmount = amount;
            }

            //approvals

            if (rem.isNSF == true || rem.isLate == true)
            {
                if (rem.NSFAmount >= 3 || rem.lateAmount >= 3)
                {
                    switch (Approved)
                    {

                        case "supacct":
                            //rem.Id = Convert.ToInt32(newId);
                            //rem.status = "SUP Approved";
                            //rem.supvLogin = Login(User.Identity.Name);
                            //rem.supvApproveDate = DateTime.Now.ToString();
                            //rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.RemUpdate(rem, Approved);
                            //string savedUrl = url;
                            //gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            string savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supdir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcss":

                           rem.Id = Convert.ToInt32(newId);
                           rem.status = "SUP Approved";
                           rem.supvLogin = Login(User.Identity.Name);
                           rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supkms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "manacct":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "manadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mandir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mankms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amcsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amdir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amcss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amkms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amacct":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "adtcm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtmm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcsm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtcs":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);



                        case "adadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "addir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adkms":


                           rem.Id = Convert.ToInt32(newId);
                           rem.status = "Ready For Processing";
                           rem.adtrLogin = Login(User.Identity.Name);
                           rem.adtrApproveDate = DateTime.Now.ToString();
                           rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";

                            return RedirectToAction("Queue", "Queue", null);

                        case "adacct":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "acct":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = " Ready For Processing";
                            rem.dtrLogin = Login(User.Identity.Name);
                            rem.dtrApproveDate = DateTime.Now.ToString();
                            rem.createdOn = rem.dtrApproveDate;
                            rem.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                    }
                }
            }

            if (rem.isBank==true)
            {
                if (rem.bankAmount<=100)
                {
                    switch (Approved)
                    {

                        case "supacct":
                            //rem.Id = Convert.ToInt32(newId);
                            //rem.status = "SUP Approved";
                            //rem.supvLogin = Login(User.Identity.Name);
                            //rem.supvApproveDate = DateTime.Now.ToString();
                            //rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.RemUpdate(rem, Approved);
                            //string savedUrl = url;
                            //gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                           string savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supdir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supkms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "manacct":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "manadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mandir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mankms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amcsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amdir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amcss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amkms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amacct":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "adtcm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtmm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcsm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtcs":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);



                        case "adadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "addir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adkms":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";

                            return RedirectToAction("Queue", "Queue", null);

                        case "adacct":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "acct":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = " Ready For Processing";
                            rem.dtrLogin = Login(User.Identity.Name);
                            rem.dtrApproveDate = DateTime.Now.ToString();
                            rem.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                    }
                }
                if (rem.bankAmount >= 100)
                {
                    switch (Approved)
                    {

                        case "supacct":
                            //rem.Id = Convert.ToInt32(newId);
                            //rem.status = "SUP Approved";
                            //rem.supvLogin = Login(User.Identity.Name);
                            //rem.supvApproveDate = DateTime.Now.ToString();
                            //rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.RemUpdate(rem, Approved);
                            //string savedUrl = url;
                            //gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            string savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supdir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supkms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "SUP Approved";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "manacct":
                            //rem.Id = Convert.ToInt32(newId);
                            //rem.status = "MAN Approved";
                            //rem.mgrLogin = Login(User.Identity.Name);
                            //rem.mgrApproveDate = DateTime.Now.ToString();
                            //rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.RemUpdate(rem, Approved);
                            //savedUrl = url;
                            //gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "manadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mandir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mankms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "MAN Approved";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtmm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amcsm":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcs":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amdir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amcss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amkms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "AMAN Approved";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amacct":

                            //rem.Id = Convert.ToInt32(newId);
                            //rem.status = "AMAN Approved";
                            //rem.amgrLogin = Login(User.Identity.Name);
                            //rem.amgrApproveDate = DateTime.Now.ToString();
                            //rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.RemUpdate(rem, Approved);
                            //savedUrl = url;
                            //gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);


                        case "adtcm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtmm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcsm":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtcs":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);



                        case "adadir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "addir":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtms":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcss":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adkms":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";

                            return RedirectToAction("Queue", "Queue", null);

                        case "adacct":


                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "acct":

                            rem.Id = Convert.ToInt32(newId);
                            rem.status = " Ready For Processing";
                            rem.dtrLogin = Login(User.Identity.Name);
                            rem.dtrApproveDate = DateTime.Now.ToString();
                            rem.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                    }
                }
            }
            switch (Approved)
            {
                case "deny":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Denied";
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", rem.accountNumber + " " + rem.accountName + ": Stat Form Denial", rem);
                    Rsl = repo.RemUpdate(rem, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                 case "supacct":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            string savedUrl = url ;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptmm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcsm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcs":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptms":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcss":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Ready For Processing ";
                    rem.supvLogin = Login(User.Identity.Name);
                    rem.supvApproveDate = DateTime.Now.ToString();
                    rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RemUpdate(rem, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, rem);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Ready For Processing ";
                    rem.supvLogin = Login(User.Identity.Name);
                    rem.supvApproveDate = DateTime.Now.ToString();
                    rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RemUpdate(rem, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, rem);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supkms":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.supvLogin = Login(User.Identity.Name);
                            rem.supvApproveDate = DateTime.Now.ToString();
                            rem.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amacct":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtmm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amcsm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcs":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtms":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amcss":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                case "amaadir":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Ready For Processing";
                    rem.amgrLogin = Login(User.Identity.Name);
                    rem.amgrApproveDate = DateTime.Now.ToString();
                    rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RemUpdate(rem, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, rem);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Ready For Processing ";
                    rem.amgrLogin = Login(User.Identity.Name);
                    rem.amgrApproveDate = DateTime.Now.ToString();
                    rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RemUpdate(rem, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, rem);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.amgrLogin = Login(User.Identity.Name);
                            rem.amgrApproveDate = DateTime.Now.ToString();
                            rem.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "manacct":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantmm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancsm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcs":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantms":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancss":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                case "manadir":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Ready For Processing ";
                    rem.mgrLogin = Login(User.Identity.Name);
                    rem.mgrApproveDate = DateTime.Now.ToString();
                    rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RemUpdate(rem, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, rem);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mandir":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Ready For Processing ";
                    rem.mgrLogin = Login(User.Identity.Name);
                    rem.mgrApproveDate = DateTime.Now.ToString();
                    rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RemUpdate(rem, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, rem);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.mgrLogin = Login(User.Identity.Name);
                            rem.mgrApproveDate = DateTime.Now.ToString();
                            rem.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adacct":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, acct, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtcm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tcsMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtmm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tmcMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcsm":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtcs":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtms":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing ";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, tmcSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcss":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing  ";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, custSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                case "adadir":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Ready For Processing ";
                    rem.adtrLogin = Login(User.Identity.Name);
                    rem.adtrApproveDate = DateTime.Now.ToString();
                    rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RemUpdate(rem, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, rem);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Ready For Processing ";
                    rem.adtrLogin = Login(User.Identity.Name);
                    rem.adtrApproveDate = DateTime.Now.ToString();
                    rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RemUpdate(rem, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, rem);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready For Processing  ";
                            rem.adtrLogin = Login(User.Identity.Name);
                            rem.adtrApproveDate = DateTime.Now.ToString();
                            rem.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, kmSup, rem);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "acct":
                            rem.Id = Convert.ToInt32(newId);
                            rem.status = "Ready for Processing ";
                            rem.dtrLogin = Login(User.Identity.Name);
                            rem.dtrApproveDate = DateTime.Now.ToString();
                         
                            rem.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.RemUpdate(rem, Approved);
                            rem.createdOn = rem.dtrApproveDate;
                            savedUrl = url ;
                            gen.SendEmail(savedUrl, acct, rem);

                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                case "Complete":
                    rem.Id = Convert.ToInt32(newId);
                    rem.status = "Completed";
                    rem.accountingLogin = Login(User.Identity.Name);
                    rem.dispositionDate = DateTime.Now.ToString();
                    rem.createdOn = rem.dtrApproveDate;
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(rem.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (rem.dtrLogin != null)
                    {
                        supervisors.ToList().Add(rem.dtrLogin);
                    }
                    if (rem.adtrLogin != null)
                    {
                        supervisors.ToList().Add(rem.adtrLogin);
                    }
                    if (rem.supvLogin != null)
                    {
                        supervisors.ToList().Add(rem.supvLogin);
                    }
                    if (rem.amgrLogin != null)
                    {
                        supervisors.ToList().Add(rem.amgrLogin);
                    }
                    if (rem.amgrLogin != null)
                    {
                        supervisors.ToList().Add(rem.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", rem.accountNumber + " " + rem.accountName + ": Stat Form Request");

                    Rsl = repo.RemUpdate(rem, Approved);
                    return RedirectToAction("Queue", "Queue", null);
            }
          
            if (rem.Id <= 0)
            {
                string adminLogin = User.Identity.Name;
                rem.userId = Login(adminLogin);
                rem.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                rem.createdOn = Convert.ToString(DateTime.Now);
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    rem.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                rem.status = "Awaiting Approval";
                //if (rem.user != null)
                //{
                //    foreach (var user in rem.user)
                //    {
                //        rem.userList += " " + repo.GetEmployee(user).userId;
                //    }
                //}

                if (url.Contains("?"))
                    {
                        url = url.Substring(0, 11);
                    }
                switch (submitForm)
                {
                    case "tcsMan":

                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":
                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsadir":
                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":
                        rem.Id = Convert.ToInt32(newId);
                        Rsl = repo.RemModify(rem);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, rem);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }
            }
         
            return RedirectToAction("Queue", "Queue", null);
        }
     
        public ActionResult rev(Rev rev, int id = -1, bool wasSaved = false)
        {
            if (id == 0)
            {
                id = Id;
            }
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            Saved = false;
            oldForm = false;
            Session[SessionKeys.folderID] = 0;
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();
           
            string login = User.Identity.Name;
           
            
            if (id > 0)    // edit
            {
                rev = repo.rev(id);
                if (rev.folderId == 0)
                {
                    newForm = true;
                }

                state = rev.status;
                //rev.admins = statusList;
                Saved = true;
                oldForm = true;
                rev.callbackName = "OnSubmitRev";
                rev.wasSaved = wasSaved;
                Session[SessionKeys.folderID] = rev.folderId;
                amount = rev.amount;
                ViewBag.WasSaved = wasSaved;
                Id = rev.Id;

                rev.pageHeading =Resources.Resources.Rev+" #" + id.ToString() + "";
                TempData["number"] = urepo.GetEmployee(rev.createdBy).tcsNumber;
                StatItem temp = repo.GetEmployee(login);
                rev.role = temp.role;
                TempData["role"] = temp.role;
                rev.userId = temp.userId;
                return View("rev", rev);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                rev = new Rev();
                rev.Id = -1;
                //rev.admins = statusList;
                rev.status = "Awaiting Approval";
                rev.pageHeading = Resources.Resources.Rev;
                rev.callbackName = "OnSubmitRev";
                return View("rev", rev);
            }

        }
        [HttpPost]
        public ActionResult OnSubmitRev(Rev rev, string Approved, string submitForm)
        {
            int Rsl;
            object newId = Id;
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                rev.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            if (rev.accountNumber == null) { rev.accountNumber = ""; }
         
                if (rev.accountNumber.Length != 9 && rev.accountNumber != "" || Regex.Matches(rev.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == true)
                {

                    rev.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("rev", new
                    {
                        rev = rev,
                        id = rev.Id
                    }
             );
                }
                if (Saved == false)
                {

                    rev.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("rev",
                          rev = rev
                  );
                }
            }
            if (rev.dateFunded == null||rev.dateFunded=="")
            {
                if (Saved == false)
                {
                    TempData["Message"] = Resources.Resources.validdate;
                    rev.Id = -1;
                    return RedirectToAction("rev",
                   rev = rev
             );
                }

                if (Saved == true)
                {
                    rev.Id = Id;
                    TempData["Message"] = Resources.Resources.validdate;
                    return RedirectToAction("rev", new
                    {
                        rev = rev,
                        id = rev.Id
                    }
     );
                }
            }
          
            if (rev.otherAmount == null||rev.otherAmount==0)
            {
                if (Saved == false)
                {
                    TempData["Message"] = Resources.Resources.valid;
                    rev.Id = -1;
                    return RedirectToAction("rev",
                   rev = rev
             );
                }

                if (Saved == true)
                {
                    rev.Id = Id;
                    TempData["Message"] = Resources.Resources.valid;
                    return RedirectToAction("rev", new
                    {
                        rev = rev,
                        id = rev.Id
                    }
     );
                }
            }
                switch (rev.revType)
            {
                case 0:

                    if (rev.feeType == null||rev.feeType=="")
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.enttype;
                            rev.Id = -1;
                            return RedirectToAction("rev",
                           rev = rev
                     );
                        }

                        if (Saved == true)
                        {
                            rev.Id = Id;
                            TempData["Message"] = Resources.Resources.enttype;
                            return RedirectToAction("rev", new
                            {
                                rev = rev,
                                id = rev.Id
                            }
             );
                        }
                    }
                    if (rev.amount == null||rev.amount==0)
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.valid;
                            rev.Id = -1;
                            return RedirectToAction("rev",
                           rev = rev
                     );
                        }

                        if (Saved == true)
                        {
                            rev.Id = Id;
                            TempData["Message"] = Resources.Resources.valid;
                            return RedirectToAction("rev", new
                            {
                                rev = rev,
                                id = rev.Id
                            }
             );
                        }
                    }
                    break;
                case 2:
                
                    
                    if (rev.reveresedFrom == 0)
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.entrevfrom;
                            rev.Id = -1;
                            return RedirectToAction("rev",
                           rev = rev
                     );
                        }

                        if (Saved == true)
                        {
                            rev.Id = Id;
                            TempData["Message"] = Resources.Resources.entrevfrom;
                            return RedirectToAction("rev", new
                            {
                                rev = rev,
                                id = rev.Id
                            }
             );
                        }
                    }
                    if (rev.reveresedTo == 0)
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.entrevto;
                            rev.Id = -1;
                            return RedirectToAction("rev",
                           rev = rev
                     );
                        }

                        if (Saved == true)
                        {
                            rev.Id = Id;
                            TempData["Message"] = Resources.Resources.entrevto;
                            return RedirectToAction("rev", new
                            {
                                rev = rev,
                                id = rev.Id
                            }
             );
                        }
                    }
                    if (rev.name == null||rev.name=="")
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.entname;
                            rev.Id = -1;
                            return RedirectToAction("rev",
                           rev = rev
                     );
                        }

                        if (Saved == true)
                        {
                            rev.Id = Id;
                            TempData["Message"] = Resources.Resources.entname;
                            return RedirectToAction("rev", new
                            {
                                rev = rev,
                                id = rev.Id
                            }
             );
                        }
                    }
                  
                    break;
            }
            switch (Approved)
            {
                case "Update":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = state;
                    rev.userId = Login(User.Identity.Name);
                    //if (rev.user != null)
                    //{
                    //    string savedUrl = url;

                    //    gen.SendEmail(savedUrl, rev.user, rev);
                    //    foreach (var user in rev.user)
                    //    {
                    //        rev.userList += " " + repo.GetEmployee(user).userId;

                    //    }
                    //}
                    Rsl = repo.RevUpdate(rev, Approved);
                    return RedirectToAction("Queue", "Queue", null);
            }
             rev.status = state;

            if (rev.status == "Awaiting Approval")
            {
                rev.status = "";
            }

            switch (Approved)
            {
                case "supacct":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.supvLogin = Login(User.Identity.Name);
                    rev.supvApproveDate = DateTime.Now.ToString();
                    rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    string savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.supvLogin = Login(User.Identity.Name);
                    rev.supvApproveDate = DateTime.Now.ToString();
                    rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.supvLogin = Login(User.Identity.Name);
                    rev.supvApproveDate = DateTime.Now.ToString();
                    rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                   rev.Id = Convert.ToInt32(newId);
                   rev.status = "Ready For Processing";
                   rev.supvLogin = Login(User.Identity.Name);
                   rev.supvApproveDate = DateTime.Now.ToString();
                   rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.supvLogin = Login(User.Identity.Name);
                    rev.supvApproveDate = DateTime.Now.ToString();
                    rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.supvLogin = Login(User.Identity.Name);
                    rev.supvApproveDate = DateTime.Now.ToString();
                    rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":
                   rev.Id = Convert.ToInt32(newId);
                   rev.status = "Ready For Processing";
                   rev.supvLogin = Login(User.Identity.Name);
                   rev.supvApproveDate = DateTime.Now.ToString();
                   rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "supadir":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.supvLogin = Login(User.Identity.Name);
                    rev.supvApproveDate = DateTime.Now.ToString();
                    rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.supvLogin = Login(User.Identity.Name);
                    rev.supvApproveDate = DateTime.Now.ToString();
                    rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supkms":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.supvLogin = Login(User.Identity.Name);
                    rev.supvApproveDate = DateTime.Now.ToString();
                    rev.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtmm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcs":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcss":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amadir":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amkms":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.amgrLogin = Login(User.Identity.Name);
                    rev.amgrApproveDate = DateTime.Now.ToString();
                    rev.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                   rev.Id = Convert.ToInt32(newId);
                   rev.status = "Ready For Processing";
                   rev.mgrLogin = Login(User.Identity.Name);
                   rev.mgrApproveDate = DateTime.Now.ToString();
                   rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "mandir":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcs":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.mgrLogin = Login(User.Identity.Name);
                    rev.mgrApproveDate = DateTime.Now.ToString();
                    rev.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adacct":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcsm":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;                 
                    gen.SendEmail(savedUrl, custMan, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcss":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready For Processing";
                    rev.adtrLogin = Login(User.Identity.Name);
                    rev.adtrApproveDate = DateTime.Now.ToString();
                    rev.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, rev);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "acct":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Ready for Processing ";
                    rev.dtrLogin = Login(User.Identity.Name);
                    rev.dtrApproveDate = DateTime.Now.ToString();
                    rev.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.RevUpdate(rev, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, rev);

                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "deny":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Denied";
                    rev.createdOn = Convert.ToString(DateTime.Now);
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", rev.accountNumber + " " + rev.accountName + ": Stat Form Denial", rev);
                    Rsl = repo.RevUpdate(rev, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "Complete":
                    rev.Id = Convert.ToInt32(newId);
                    rev.status = "Completed";
                    rev.accountingLogin = Login(User.Identity.Name);
                    rev.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.RevUpdate(rev, Approved);
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(rev.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (rev.dtrLogin != null)
                    {
                        supervisors.ToList().Add(rev.dtrLogin);
                    }
                    if (rev.adtrLogin != null)
                    {
                        supervisors.ToList().Add(rev.adtrLogin);
                    }
                    if (rev.supvLogin != null)
                    {
                        supervisors.ToList().Add(rev.supvLogin);
                    }
                    if (rev.amgrLogin != null)
                    {
                        supervisors.ToList().Add(rev.amgrLogin);
                    }
                    if (rev.amgrLogin != null)
                    {
                        supervisors.ToList().Add(rev.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", rev.accountNumber + " " + rev.accountName + ": Stat Form Request");

                    return RedirectToAction("Queue", "Queue", null);
            }
           
            if (rev.Id <= 0)
            {
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    rev.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                string adminLogin = User.Identity.Name;
                rev.userId = Login(adminLogin);
                rev.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                rev.status = "Awaiting Approval";
                rev.createdOn = Convert.ToString(DateTime.Now);
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }

                switch (submitForm)
                {
                    case "tcsMan":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsadir":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":
                        rev.Id = Convert.ToInt32(newId);
                        Rsl = repo.RevModify(rev);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, rev);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                
                //gen.SendEmail(savedUrl, rev.user, rev);
            }
            }
          
            return RedirectToAction("Queue", "Queue", null);

        }
        public ActionResult pif(Pif pif, int id = -1, bool wasSaved = false)
        {
            
                if (id == 0)
                {
                    id = Id;
                }
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            Saved = false;
        oldForm = false;
            Session[SessionKeys.folderID] = 0;
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();
            
            string login = User.Identity.Name;
           
            
            if (id > 0)    // edit
            {

                
                pif = repo.pif(id);
                if (pif.folderId == 0)
                {
                    newForm = true;
                }

                state = pif.status;
                //pif.admins = statusList;
                pif.callbackName = "OnSubmitPif";
                pif.wasSaved = wasSaved;
                Saved = true;
                oldForm = true;
                ViewBag.WasSaved = wasSaved;
                Session[SessionKeys.folderID] = pif.folderId;
                TempData["Outdated"] = "";
                TempData["number"] = urepo.GetEmployee(pif.createdBy).tcsNumber;
                Id = pif.Id;
                pif.pageHeading =Resources.Resources.Pif+" #" + id.ToString() + "";            
                StatItem temp = repo.GetEmployee(login);
                pif.role = temp.role;
                TempData["role"] = temp.role;
                pif.userId = temp.userId;
                return View("pif", pif);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                pif = new Pif();
                pif.Id = -1;


                pif.status = "Ready For Processing";
                pif.pageHeading = Resources.Resources.Pif;
                pif.callbackName = "OnSubmitPif";
                return View("pif", pif);
            }


        }
        [HttpPost]
        public ActionResult OnSubmitPif(Pif pif, string Approved, string submitForm)
        {
          
            int Rsl;
            object newId = Id;
            DateTime cutOffdate = DateTime.Now.AddDays(-30);
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                pif.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            if (pif.accountNumber == null) { pif.accountNumber = ""; }
          
                if (Convert.ToDateTime(pif.datePaid) > cutOffdate)
            {

                if (Saved == false)
                {
                    TempData["Error"] = "Cannot be proccessed due to last payment being less than 30 days";
                    pif.Id = -1;
                    return RedirectToAction("pif",
                     pif = pif
               );
                }

                if (Saved == true)
                {
                    pif.Id = Id;
                    TempData["Error"] = "Cannot be proccessed due to last payment being less than 30 days";
                    return RedirectToAction("pif", new
                    {
                        pif = pif,
                        id = pif.Id
                    }
     );
                }
            }
            if (pif.accountNumber.Length != 9 && pif.accountNumber != "" || Regex.Matches(pif.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == true)
                {

                    pif.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("pif", new
                    {
                        pif = pif,
                        id = pif.Id
                    }
             );
                }
                if (Saved == false)
                {

                    pif.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("pif",
                          pif = pif
                  );
                }
         
            }
            if (pif.datePaid == null || pif.datePaid == "")
            {

                if (Saved == false)
                {
                    TempData["Message"] = "Enter Date Paid";
                    pif.Id = -1;
                    return RedirectToAction("pif",
                   pif = pif
             );
                }

                if (Saved == true)
                {
                    pif.Id = Id;
                    TempData["Message"] = "Enter Date Paid";
                    return RedirectToAction("pif", new
                    {
                        pif = pif,
                        id = pif.Id
                    }
     );
                }
            }

            if (pif.isCoMaker == false && pif.isMaker == false)
            {

                if (Saved == false)
                {
                    TempData["Message"] = "Must be Maker or Co-Maker";
                    pif.Id = -1;
                    return RedirectToAction("pif",
                    pif = pif
              );
                }

                if (Saved == true)
                {
                    pif.Id = Id;
                    TempData["Message"] = "Must be Maker or Co-Maker";
                    return RedirectToAction("pif", new
                    {
                        pif = pif,
                        id = pif.Id
                    }
     );
                }
            }
            switch (Approved)
            {
                case "Update":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = state;
                    pif.userId = Login(User.Identity.Name);
                  
                   
                    Rsl = repo.PifUpdate(pif, Approved);
                    return RedirectToAction("Queue", "Queue", null);
            }
            pif.status = state;

            if (pif.status == "Awaiting Approval")
            {
                pif.status = "";
            }


            switch (Approved)
            {
                case "supacct":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    string savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "supkms":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.supvLogin = Login(User.Identity.Name);
                    pif.supvApproveDate = DateTime.Now.ToString();
                    pif.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);                  
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtmm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amadir":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcs":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcss":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.amgrLogin = Login(User.Identity.Name);
                    pif.amgrApproveDate = DateTime.Now.ToString();
                    pif.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manadir":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mandir":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcs":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved); 
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.mgrLogin = Login(User.Identity.Name);
                    pif.mgrApproveDate = DateTime.Now.ToString();
                    pif.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adacct":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcsm":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcss":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready For Processing";
                    pif.adtrLogin = Login(User.Identity.Name);
                    pif.adtrApproveDate = DateTime.Now.ToString();
                    pif.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "acct":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Ready for Processing ";
                    pif.dtrLogin = Login(User.Identity.Name);
                    pif.dtrApproveDate = DateTime.Now.ToString();
                    pif.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.PifUpdate(pif, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, pif);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "deny":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Denied";
                    pif.createdOn = Convert.ToString(DateTime.Now);
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", pif.accountNumber + " " + pif.accountName + ": Stat Form Denial", pif);
                    Rsl = repo.PifUpdate(pif, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "Complete":
                    pif.Id = Convert.ToInt32(newId);
                    pif.status = "Completed";
                    pif.accountingLogin = Login(User.Identity.Name);
                    pif.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.PifUpdate(pif, Approved);
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(pif.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (pif.dtrLogin != null)
                    {
                        supervisors.ToList().Add(pif.dtrLogin);
                    }
                    if (pif.adtrLogin != null)
                    {
                        supervisors.ToList().Add(pif.adtrLogin);
                    }
                    if (pif.supvLogin != null)
                    {
                        supervisors.ToList().Add(pif.supvLogin);
                    }
                    if (pif.amgrLogin != null)
                    {
                        supervisors.ToList().Add(pif.amgrLogin);
                    }
                    if (pif.amgrLogin != null)
                    {
                        supervisors.ToList().Add(pif.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", pif.accountNumber + " " + pif.accountName + ": Stat Form Request");

                    return RedirectToAction("Queue", "Queue", null);            }

         
            if (pif.Id <= 0)
            {
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    pif.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                string adminLogin = User.Identity.Name;
                pif.userId = Login(adminLogin);
                pif.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                pif.status = "Ready For Processing";
                pif.createdOn = Convert.ToString(DateTime.Now);
                TempData["Success"] = Resources.Resources.submitted;
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }
                switch (submitForm)
                {
                    case "tcsMan":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsadir":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":
                        pif.Id = Convert.ToInt32(newId);
                        Rsl = repo.PifModify(pif);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, pif);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }
            }

            return RedirectToAction("Queue", "Queue", null);
            
        }
  
        public ActionResult cci(Cci cci, int id = -1)
        {
            if (id == 0)
            {
                id = Id;
            }
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            Saved = false;
            oldForm = false;
           Session[SessionKeys.folderID] = 0;
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();

         
            string login = User.Identity.Name;      
           
            if (id > 0)    // edit
            {
                
                cci = repo.cci(id);
                if (cci.folderId == 0)
                {
                    newForm = true;
                }

                Id = cci.Id;
                Saved = true;
                oldForm = true;
                Session[SessionKeys.folderID] = cci.folderId;
                //cci.admins = statusList;
                cci.callbackName = "OnSubmitCci";
                state = cci.status;
                TempData["number"] = urepo.GetEmployee(cci.createdBy).tcsNumber;
                cci.pageHeading =Resources.Resources.Cci+" #" + id.ToString() + "";
                StatItem temp = repo.GetEmployee(login);
                cci.role = temp.role;
                TempData["role"] = temp.role;
                cci.userId = temp.userId;
              
                return View("cci", cci);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                cci = new Cci();
                cci.Id = -1;
                //cci.admins = statusList;
                cci.status = "Waiting Approval";
                cci.pageHeading = Resources.Resources.Cci;
                cci.callbackName = "OnSubmitCci";
                return View("cci", cci);
            }
        }
        
        [HttpPost]
        public ActionResult OnSubmitCci(Cci cci, IEnumerable<HttpPostedFileBase> files, string Approved, string submitForm)
        {
       
            int Rsl;
            object newId =Id;
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                cci.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            if (cci.accountNumber == null) { cci.accountNumber = ""; }
          
                if (cci.accountNumber.Length != 9 && cci.accountNumber != "" || Regex.Matches(cci.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == false)
                {
                    TempData["Message"] = Resources.Resources.digit;
                    cci.Id = -1;
                    return RedirectToAction("cci",
                   cci = cci
             );
                }

                if (Saved == true)
                {
                    cci.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("cci", new
                    {
                        cci = cci,
                        id = cci.Id
                    }
     );
                }
            }
            if (cci.isMaker == false && cci.isCoMaker == false)
            {
                if (Saved == false)
                {
                    TempData["Message"] = Resources.Resources.mocm;
                    cci.Id = -1;
                    return RedirectToAction("cci",
                   cci = cci
             );
                }

                if (Saved == true)
                {
                    cci.Id = Id;
                    TempData["Message"] = Resources.Resources.mocm;
                    return RedirectToAction("cci", new
                    {
                        cci = cci,
                        id = cci.Id
                    }
     );
                }

            }

            if (cci.isNameChange == true || cci.isSSNChange == true)
            {
                if (cci.toChange == null || cci.toChange == "")
                {
                    if (Saved == false)
                    {
                        TempData["Message"] = Resources.Resources.enossn;
                        cci.Id = -1;
                        return RedirectToAction("cci",
                       cci = cci
                 );
                    }

                    if (Saved == true)
                    {
                        cci.Id = Id;
                        TempData["Message"] = Resources.Resources.enossn;
                        return RedirectToAction("cci", new
                        {
                            cci = cci,
                            id = cci.Id
                        }
         );
                    }

                }
            }
            if (cci.isAKA == true )
            {
                if (cci.AKA == null || cci.AKA == "")
                {
                    if (Saved == false)
                    {
                        TempData["Message"] = Resources.Resources.enaka;
                        cci.Id = -1;
                        return RedirectToAction("cci",
                       cci = cci
                 );
                    }

                    if (Saved == true)
                    {
                        cci.Id = Id;
                        TempData["Message"] = Resources.Resources.enaka;
                        return RedirectToAction("cci", new
                        {
                            cci = cci,
                            id = cci.Id
                        }
         );
                    }
                }            
                if (cci.reason == null||cci.reason=="")
                {
                    if (Saved == false)
                    {
                        TempData["Message"] = Resources.Resources.enr;
                        cci.Id = -1;
                        return RedirectToAction("cci",
                       cci = cci
                 );
                    }

                    if (Saved == true)
                    {
                        cci.Id = Id;
                        TempData["Message"] = Resources.Resources.enr;
                        return RedirectToAction("cci", new
                        {
                            cci = cci,
                            id = cci.Id
                        }
         );
                    }
                }
            }
            switch (Approved)
            {
                case "Update":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = state;
                    cci.userId = Login(User.Identity.Name);
                    //if (cci.user != null)
                    //{
                    //    string savedUrl = url;

                    //    gen.SendEmail(savedUrl, cci.user, cci);
                    //    foreach (var user in cci.user)
                    //    {
                    //        cci.userList += " " + repo.GetEmployee(user).userId;
                    //    }
                    //}
                    Rsl = repo.CciUpdate(cci, Approved);
                    return RedirectToAction("Queue", "Queue", null);
            }
          
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase file = Request.Files[i];
                if (file.ContentLength > 0)
                {
                    //And we have a directory for the Vendor already

                    var filename = cci.folderId + "\\" + Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]), filename);

                    if (Directory.Exists(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]) + "\\" + cci.folderId))
                    {
                        file.SaveAs(path);
                    }
                    else
                    {
                        Directory.CreateDirectory(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]) + "\\" + cci.folderId);
                        file.SaveAs(path);
                    }
                }
            }
        
            
            cci.status = state;
            if (cci.status == "Awaiting Approval")
            {
                cci.status = "";
            }

            switch (Approved)
            {
                case "supacct":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    string savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                     savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "supadir":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "supkms":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.supvLogin = Login(User.Identity.Name);
                    cci.supvApproveDate = DateTime.Now.ToString();
                    cci.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtmm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcs":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcss":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amadir":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amkms":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.amgrLogin = Login(User.Identity.Name);
                    cci.amgrApproveDate = DateTime.Now.ToString();
                    cci.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcs":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mandir":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.mgrLogin = Login(User.Identity.Name);
                    cci.mgrApproveDate = DateTime.Now.ToString();
                    cci.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adacct":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcsm":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custMan, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tcsSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, tmcSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcss":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, custSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adadir":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready For Processing";
                    cci.adtrLogin = Login(User.Identity.Name);
                    cci.adtrApproveDate = DateTime.Now.ToString();
                    cci.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, kmSup, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "acct":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Ready for Processing ";
                    cci.dtrLogin = Login(User.Identity.Name);
                    cci.dtrApproveDate = DateTime.Now.ToString();
                    cci.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.CciUpdate(cci, Approved);
                    savedUrl = url ;
                    gen.SendEmail(savedUrl, acct, cci);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "deny":
                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Denied";
                    cci.createdOn = Convert.ToString(DateTime.Now);
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", cci.accountNumber + " " + cci.accountName + ": Stat Form Denial", cci);
                    Rsl = repo.CciUpdate(cci, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "Complete":

                    cci.Id = Convert.ToInt32(newId);
                    cci.status = "Completed";
                    cci.accountingLogin = Login(User.Identity.Name);
                    cci.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.CciUpdate(cci, Approved);

                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(cci.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (cci.dtrLogin != null)
                    {
                        supervisors.ToList().Add(cci.dtrLogin);
                    }
                    if (cci.adtrLogin != null)
                    {
                        supervisors.ToList().Add(cci.adtrLogin);
                    }
                    if (cci.supvLogin != null)
                    {
                        supervisors.ToList().Add(cci.supvLogin);
                    }
                    if (cci.amgrLogin != null)
                    {
                        supervisors.ToList().Add(cci.amgrLogin);
                    }
                    if (cci.amgrLogin != null)
                    {
                        supervisors.ToList().Add(cci.mgrLogin);
                    }
                    gen.SendAlert(url, supervisors, "", cci.accountNumber + " " + cci.accountName + ": Stat Form Request");

                    return RedirectToAction("Queue", "Queue", null);
            }
            
            if (cci.Id <= 0)
            {
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    cci.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                string adminLogin = User.Identity.Name;
                cci.userId = Login(adminLogin);
                cci.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                cci.status = "Awaiting Approval";
                cci.createdOn = Convert.ToString(DateTime.Now);

                if (url.Contains("?"))
                    {
                        url = url.Substring(0, 11);
                    }
                   switch (submitForm)
                {
                    case "tcsMan":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsadir":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);


                    case "kmSup":
                        cci.Id = Convert.ToInt32(newId);
                        Rsl = repo.CciModify(cci);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, cci);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }
                    //gen.SendEmail(savedUrl, cci.user, cci);
                
            }

      
            return RedirectToAction("Queue", "Queue", null);
        }
        public ActionResult sec(sec sec,int id = -1, bool wasSaved = false)
        {
            
                if (id == 0)
                {
                    id = Id;
                }
            Saved = false;
        oldForm = false;
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            Session[SessionKeys.folderID] = 0;

            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();

            if (sec == null)
            {

                TempData["Message"] = Resources.Resources.digit; 
            }
            string login = User.Identity.Name;
            
           
            if (id > 0)    // edit
            {
               
                sec = repo.sec(id);
                if (sec.folderId == 0)
                {
                    newForm = true;
                }
                Saved = true;
                oldForm = true;
                //sec.admins = statusList;
                sec.callbackName = "OnSubmitSec";
                sec.wasSaved = wasSaved;
                ViewBag.WasSaved = wasSaved;
                state = sec.status;
                Session[SessionKeys.folderID] = sec.folderId;
                TempData["number"] = urepo.GetEmployee(sec.createdBy).tcsNumber;
                Id = sec.Id;
                sec.pageHeading = Resources.Resources.Sec+" #" + id.ToString() + "";
                StatItem temp = repo.GetEmployee(login);
                sec.role = temp.role;
                TempData["role"] = temp.role;
                sec.userId = temp.userId;
                
                return View("sec", sec);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                sec = new sec();
                sec.Id = -1;
                //sec.admins = statusList;
                sec.status = "Awaiting Approval";
                sec.pageHeading = Resources.Resources.Sec;
                sec.callbackName = "OnSubmitSec";
                return View("sec", sec);
            }
        }

        [HttpPost]
        public ActionResult OnSubmitSec(sec sec, IEnumerable<HttpPostedFileBase> files, string Approved, string submitForm)
        {

            int Rsl;
            object newId = Id;
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }

            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                sec.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }

            if (sec.accountNumber == null) { sec.accountNumber = ""; }

            if (sec.accountNumber.Length != 9 && sec.accountNumber != "" || Regex.Matches(sec.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == true)
                {

                    sec.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("sec", new
                    {
                        sec = sec,
                        id = sec.Id
                    }
             );
                }
                if (Saved == false)
                {

                    sec.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("sec",
                          sec = sec
                  );
                }
            }
            if (sec.isException == true)
            {
                switch (Approved)
                {

                    case "supacct":

                        //    sec.Id = Convert.ToInt32(newId);
                        //sec.status = "SUP Approved ";
                        //sec.supvLogin = Login(User.Identity.Name);
                        //sec.supvApproveDate = DateTime.Now.ToString();
                        //sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.SecUpdate(sec, Approved);
                        //string savedUrl = url;
                        //gen.SendEmail(savedUrl, tmcMan, sec);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
               string savedUrl = url;
                gen.SendEmail(savedUrl, tcsMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "suptmm":

                    sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tmcMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, custMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, adir, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, dir, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tcsSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tcsMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "supcss":

                   sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, custSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "supkms":

                    sec.Id = Convert.ToInt32(newId);
                sec.status = "SUP Approved ";
                sec.supvLogin = Login(User.Identity.Name);
                sec.supvApproveDate = DateTime.Now.ToString();
                sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, kmSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                        //    sec.Id = Convert.ToInt32(newId);
                        //sec.status = "AMAN Approved";
                        //sec.amgrLogin = Login(User.Identity.Name);
                        //sec.amgrApproveDate = DateTime.Now.ToString();
                        //sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.SecUpdate(sec, Approved);
                        //savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, sec);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    
                 sec.Id = Convert.ToInt32(newId);
                 sec.status = "AMAN Approved";
                sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tcsMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "amtmm":
                    sec.Id = Convert.ToInt32(newId);
                 sec.status = "AMAN Approved";
              sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tmcMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "amcsm":

                    sec.Id = Convert.ToInt32(newId);
                sec.status = "AMAN Approved";
               sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tmcMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "amtcs":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "AMAN Approved";
                sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tcsSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "AMAN Approved";
                sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tmcSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "amcss":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "AMAN Approved";
                sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, custSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "amadir":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "AMAN Approved";
                sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, adir, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "amdir":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "AMAN Approved";
                sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, dir, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "AMAN Approved";
                sec.amgrLogin = Login(User.Identity.Name);
                sec.amgrApproveDate = DateTime.Now.ToString();
                sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, kmSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                        //    sec.Id = Convert.ToInt32(newId);
                        //sec.status = "MAN Approved";
                        //sec.mgrLogin = Login(User.Identity.Name);
                        //sec.mgrApproveDate = DateTime.Now.ToString();
                        //sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.SecUpdate(sec, Approved);
                        //savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, sec);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tcsMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, adir, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "mandir":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, dir, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tmcMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, custMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "mantcs":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tcsSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tmcSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, custSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "MAN Approved";
                sec.mgrLogin = Login(User.Identity.Name);
                sec.mgrApproveDate = DateTime.Now.ToString();
                sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, kmSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "adacct":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, acct, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "adtcm":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tcsMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tmcMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);


                case "adcsm":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, custMan, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tcsSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, tmcSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);
                case "adcss":

                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, custSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, adir, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, dir, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.adtrLogin = Login(User.Identity.Name);
                sec.adtrApproveDate = DateTime.Now.ToString();
                sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, kmSup, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);
                case "acct":

                    sec.Id = Convert.ToInt32(newId);
                sec.status = "Ready For Processing";
                sec.dtrLogin = Login(User.Identity.Name);
                sec.dtrApproveDate = DateTime.Now.ToString();
                sec.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                Rsl = repo.SecUpdate(sec, Approved);
                savedUrl = url;
                gen.SendEmail(savedUrl, acct, sec);
                TempData["Success"] = "Stat Item was succesfully Approved";
                return RedirectToAction("Queue", "Queue", null);
            } }
            switch (Approved)
            {
                case "Update":

                    sec.Id = Convert.ToInt32(newId);
                    sec.status = state;
                    sec.userId = Login(User.Identity.Name);

                   
                    Rsl = repo.SecUpdate(sec, Approved);
                    return RedirectToAction("Queue", "Queue", null);


                case "deny":
                   sec.Id = Convert.ToInt32(newId);
                   sec.status = "Denied";
                    sec.createdOn = Convert.ToString(DateTime.Now);
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", sec.accountNumber + " " + sec.accountName + ": Stat Form Denial", sec);
                    Rsl = repo.SecUpdate(sec, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "supacct":

                    //sec.Id = Convert.ToInt32(newId);
                    //sec.status = "SUP Approved ";
                    //sec.supvLogin = Login(User.Identity.Name);
                    //sec.supvApproveDate = DateTime.Now.ToString();
                    //sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    //Rsl = repo.SecUpdate(sec, Approved);
                    //string savedUrl = url;
                    //gen.SendEmail(savedUrl, tmcMan, sec);
                    TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                    sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                   string savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":

                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                    sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                    sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                    sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                    sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                    sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                    sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":

                   sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                   sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supkms":

                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "SUP Approved ";
                    sec.supvLogin = Login(User.Identity.Name);
                    sec.supvApproveDate = DateTime.Now.ToString();
                    sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amtmm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":

                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amtcs":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amcss":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amadir":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amdir":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.amgrLogin = Login(User.Identity.Name);
                    sec.amgrApproveDate = DateTime.Now.ToString();
                    sec.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "mandir":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "mantcs":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.mgrLogin = Login(User.Identity.Name);
                    sec.mgrApproveDate = DateTime.Now.ToString();
                    sec.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adacct":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adtcm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adcsm":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                case "adcss":

                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Ready For Processing";
                    sec.adtrLogin = Login(User.Identity.Name);
                    sec.adtrApproveDate = DateTime.Now.ToString();
                    sec.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                case "acct":

                    sec.Id = Convert.ToInt32(newId);                 
                    sec.status = "Ready For Processing";
                    sec.dtrLogin = Login(User.Identity.Name);
                    sec.dtrApproveDate = DateTime.Now.ToString();
                    sec.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.SecUpdate(sec, Approved);
                    savedUrl = url;
                    //gen.SendEmail(savedUrl, acct, sec);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "Complete":
                    sec.Id = Convert.ToInt32(newId);
                    sec.status = "Completed";
                    sec.accountingLogin = Login(User.Identity.Name);
                    sec.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.SecUpdate(sec, Approved);
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(sec.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (sec.dtrLogin != null)
                    {
                        supervisors.ToList().Add(sec.dtrLogin);
                    }
                    if (sec.adtrLogin != null)
                    {
                        supervisors.ToList().Add(sec.adtrLogin);
                    }
                    if (sec.supvLogin != null)
                    {
                        supervisors.ToList().Add(sec.supvLogin);
                    }
                    if (sec.amgrLogin != null)
                    {
                        supervisors.ToList().Add(sec.amgrLogin);
                    }
                    if (sec.amgrLogin != null)
                    {
                        supervisors.ToList().Add(sec.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", sec.accountNumber + " " + sec.accountName + ": Stat Form Request");
                    return RedirectToAction("Queue", "Queue", null);
            }

            sec.status = state;
            
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase file = Request.Files[i];
                if (file.ContentLength > 0)
                {
                    //And we have a directory for the settlement already

                    var filename = sec.folderId + "\\" + Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]), filename);

                    if (Directory.Exists(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]) + "\\" + sec.folderId))
                    {
                        file.SaveAs(path);
                    }
                    else
                    {
                        Directory.CreateDirectory(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]) + "\\" + sec.folderId);
                        file.SaveAs(path);
                    }
                }
            }
            if (sec.Id <= 0)
            {
                if ( Convert.ToInt32(Session[SessionKeys.folderID]) == 0)
                {
                        TempData["Message"] = "An attachment is required";
                    sec.Id = -1;
                    return RedirectToAction("sec", sec);
                    
                }
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    sec.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }

                sec.createdOn = Convert.ToString(DateTime.Now);
                switch (submitForm)
                {
                    case "tcsMan":
                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, sec);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, sec);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, sec);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, sec);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, sec);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":

                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, sec);
                        return RedirectToAction("Queue", "Queue", null);


                    case "tcsadir":
                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, sec);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":

                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, sec);
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsacct":

                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, acct, sec);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);


                    case "kmSup":

                        sec.Id = Convert.ToInt32(newId);
                        sec.userId = Login(User.Identity.Name);
                        sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        sec.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.SecModify(sec);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, sec);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }
                //string adminLogin = User.Identity.Name;
                //sec.userId = Login(adminLogin);
                //sec.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                //sec.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                //sec.status = "Ready for Review";
                //TempData["Success"] = "Stat Item was succesfully submitted";
               
              
            }


            return RedirectToAction("Queue", "Queue", null);
        }
      
        public ActionResult bal(Bal bal,int id = -1)
        {
            if (id == 0)
            {
                id = Id;
            }
            Saved = false;
            oldForm = false;
            Session[SessionKeys.folderID] = 0;

            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();
            string login = User.Identity.Name;
           
           
            if (id > 0)    // edit
            {
                
                bal = repo.bal(id);
                if (bal.folderId == 0)
                {
                    newForm = true;
                }

                //bal.admins = statusList;
                Saved = true;
                oldForm = true;
                type = bal.balType;
                bal.pageHeading =Resources.Resources.Bal+ " #" + id.ToString() + "";
                bal.callbackName = "OnSubmitBal";
                TempData["number"] = urepo.GetEmployee(bal.createdBy).tcsNumber;
                state = bal.status;
                 Id = bal.Id;
                Session[SessionKeys.folderID] = bal.folderId;
                StatItem temp = repo.GetEmployee(login);
                bal.role = temp.role;
                TempData["role"] = temp.role;
                bal.userId = temp.userId;
                return View("bal", bal);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l'&& url.Length>=33)
                {
                    url = url.Substring(0, 33);
                }
                bal = new Bal();
                //bal.admins = statusList;
                bal.Id = -1;
                bal.status = "Waiting Approval";
                bal.callbackName = "OnSubmitBal";
                bal.pageHeading = Resources.Resources.Bal;
                return View("bal", bal);
            }
        }
        [HttpPost]
        public ActionResult OnSubmitBal(Bal bal, string Approved, string submitForm)
        {
            
            int Rsl;
            object newId =Id;
            string TBOM = bal.accountNumber.Remove(3, 6);
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();

            foreach (var user in adminList)
            {
                ////if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                bal.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            if (Approved != null&& Approved !="Update")
            {
                bal.balType = type;
            }
            if (bal.accountNumber == null) { bal.accountNumber = ""; }
         
            if (bal.accountNumber.Length != 9 && bal.accountNumber != "" || Regex.Matches(bal.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == true)
                {

                    bal.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("bal", new
                    {
                        bal = bal,
                        id = bal.Id
                    }
             );
                }
                if (Saved == false)
                {

                    bal.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("bal",
                          bal = bal
                  );
                }
            }
            switch (bal.balType)
            {
                case 0:

                    if (bal.amount == 0 || bal.amount > 3 || bal.amount == null)
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.valid;
                            bal.Id = -1;
                            return RedirectToAction("bal",
                        bal = bal
                  );
                        }
                        if (Saved == true)
                        {
                            bal.Id = Id;
                            TempData["Message"] = Resources.Resources.valid;
                            return RedirectToAction("bal", new
                            {
                                bal = bal,
                                id = bal.Id
                            }
             );
                        }
                    }
                    break;
                case 1:
                    if (TBOM == "777" || TBOM == "887")
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = "Do not apply TBOM account";

                            bal.Id = -1;
                            Id = -1;
                            return RedirectToAction("bal",
                        bal = bal
                  );
                        }

                        if (Saved == true)
                        {
                            bal.Id = Id;
                            TempData["Message"] = "Do not apply TBOM account";
                            return RedirectToAction("bal", new
                            {
                                bal = bal,
                                id = bal.Id
                            }
             );
                        }
                    }
                    if (bal.amount == 0 || bal.amount > 20 || bal.amount<3 || bal.amount == null)
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.valid;

                            bal.Id = -1;
                            Id = -1;
                            return RedirectToAction("bal",
                        bal = bal
                  );
                        }

                        if (Saved == true)
                        {
                            bal.Id = Id;
                            TempData["Message"] = Resources.Resources.valid;
                            return RedirectToAction("bal", new
                            {
                                bal = bal,
                                id = bal.Id
                            }
             );
                        }
                    }
                  
                    break;
                case 2:
                
                    if (bal.amount == 0 || bal.amount >=2 || bal.amount == null)
                    {

                        TempData["Message"] = Resources.Resources.valid;
                        if (Saved == false)
                        {
                            bal.Id = -1;
                            return RedirectToAction("bal", bal = bal);
                        }
                        if (Saved == true)
                        {
                            bal.Id = Id;
                            TempData["Message"] = Resources.Resources.valid;
                            return RedirectToAction("bal",new{
                                bal = bal,
                            id=bal.Id}
             );
                        }
                   
                    }
                    break;
                case 3:
                    if (bal.exceptionAmount == 0 || bal.exceptionAmount == null)
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.valid;
                            bal.Id = -1;
                            return RedirectToAction("bal",
                       bal = bal
                 );
                        }
                        if (Saved == true)
                        {
                            bal.Id = Id;
                            TempData["Message"] = Resources.Resources.valid;
                            return RedirectToAction("bal", new
                            {
                                bal = bal,
                                id = bal.Id
                            }
             );
                        }
                    }
                    if (bal.reason == null || bal.reason == "")
                    {
                        if (Saved == false)
                        {
                            TempData["Message"] = Resources.Resources.ear;
                            bal.Id = -1;
                            return RedirectToAction("bal", new
                            {
                                bal = bal,
                                id = bal.Id
                            }

                 );
                        }
                        if (Saved == true)
                        {
                            bal.Id = Id;
                            TempData["Message"] = Resources.Resources.ear;
                            return RedirectToAction("bal", new
                            {
                                bal = bal,
                                id = bal.Id
                            }
             );
                        }
                    }
                    break;
            }
            switch (Approved)
            {
                case "deny":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Denied";
                    bal.createdOn = Convert.ToString(DateTime.Now);
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", bal.accountNumber + " " + bal.accountName + ": Stat Form Denial", bal);
                    Rsl = repo.BalUpdate(bal, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "Update":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = state;
                    bal.userId = Login(User.Identity.Name);                   
                   
                  
                    Rsl = repo.BalUpdate(bal, Approved);
                    return RedirectToAction("Queue", "Queue", null);
            }
          
            bal.status = state;
            if (bal.status == "Awaiting Approval")
            {
                bal.status = "";
            }
           
            if (bal.amount > 3 && bal.amount <= 20)
            {

                switch (Approved)
                {
                    case "supacct":
                        //bal.Id = Convert.ToInt32(newId);
                        //bal.status = "SUP Approved";
                        //bal.supvLogin = Login(User.Identity.Name);
                        //bal.supvApproveDate = DateTime.Now.ToString();
                        //bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.BalUpdate(bal, Approved);
                        //string savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, bal);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                      string  savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptmm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcsm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcs":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supadir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supdir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptms":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcss":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "supkms":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "SUP Approved";
                        bal.supvLogin = Login(User.Identity.Name);
                        bal.supvApproveDate = DateTime.Now.ToString();
                        bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "manacct":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantcm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantmm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancsm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "manadir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mandir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantcs":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantms":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancss":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mankms":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.mgrLogin = Login(User.Identity.Name);
                        bal.mgrApproveDate = DateTime.Now.ToString();
                        bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "deny":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Denied";
                        string tcs = Convert.ToString(TempData["number"]);
                        tcs = tcs.Remove(0, 5);
                        gen.SendDenial(url, tcs, "", bal.accountNumber + " " + bal.accountName + ": Stat Form Denial", bal);
                        Rsl = repo.BalUpdate(bal, Approved);
                        return RedirectToAction("Queue", "Queue", null);

                    case "amacct":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtcm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtmm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amcsm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amadir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amdir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtcs":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtms":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amcss":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amkms":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.amgrLogin = Login(User.Identity.Name);
                        bal.amgrApproveDate = DateTime.Now.ToString();
                        bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adacct":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtcm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtmm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adcsm":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adadir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "addir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtcs":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtms":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adcss":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adkms":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready For Processing";
                        bal.adtrLogin = Login(User.Identity.Name);
                        bal.adtrApproveDate = DateTime.Now.ToString();
                        bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, bal);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);
                    case "acct":
                        bal.Id = Convert.ToInt32(newId);
                        bal.status = "Ready for Processing ";
                        bal.dtrLogin = Login(User.Identity.Name);
                        bal.dtrApproveDate = DateTime.Now.ToString();
                        bal.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.BalUpdate(bal, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, bal);

                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                }
            }
                switch (Approved)
            {
                case "supacct":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    string savedUrl = url;
                    gen.SendEmail(savedUrl, acct, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "supkms":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.supvLogin = Login(User.Identity.Name);
                    bal.supvApproveDate = DateTime.Now.ToString();
                    bal.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manacct":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mandir":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcs":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.mgrLogin = Login(User.Identity.Name);
                    bal.mgrApproveDate = DateTime.Now.ToString();
                    bal.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);              
                 

                case "deny":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Denied";
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", bal.accountNumber + " " + bal.accountName + ": Stat Form Denial", bal);
                    Rsl = repo.BalUpdate(bal, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtmm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amadir":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcs":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcss":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.amgrLogin = Login(User.Identity.Name);
                    bal.amgrApproveDate = DateTime.Now.ToString();
                    bal.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adacct":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcsm":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcss":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready For Processing";
                    bal.adtrLogin = Login(User.Identity.Name);
                    bal.adtrApproveDate = DateTime.Now.ToString();
                    bal.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, bal);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                case "acct":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Ready for Processing ";
                    bal.dtrLogin = Login(User.Identity.Name);
                    bal.dtrApproveDate = DateTime.Now.ToString();
                    bal.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.BalUpdate(bal, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, bal);

                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                
                case "Complete":
                    bal.Id = Convert.ToInt32(newId);
                    bal.status = "Completed";
                    bal.accountingLogin = Login(User.Identity.Name);
                    bal.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.BalUpdate(bal, Approved);

                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(bal.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (bal.dtrLogin != null)
                    {
                        supervisors.ToList().Add(bal.dtrLogin);
                    }
                    if (bal.adtrLogin != null)
                    {
                        supervisors.ToList().Add(bal.adtrLogin);
                    }
                    if (bal.supvLogin != null)
                    {
                        supervisors.ToList().Add(bal.supvLogin);
                    }
                    if (bal.amgrLogin != null)
                    {
                        supervisors.ToList().Add(bal.amgrLogin);
                    }
                    if (bal.amgrLogin != null)
                    {
                        supervisors.ToList().Add(bal.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", bal.accountNumber + " " + bal.accountName + ": Stat Form Request");

                    return RedirectToAction("Queue", "Queue", null);
            }

           

            if (bal.Id <= 0)
            {
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    bal.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }
                bal.createdOn = Convert.ToString(DateTime.Now);
                switch (submitForm)
                {
                    case "tcsMan":
                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, bal);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, bal);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, bal);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, bal);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);


                    case "tcsadir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, bal);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, bal);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, bal);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":

                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, bal);
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":

                        bal.Id = Convert.ToInt32(newId);
                        bal.userId = Login(User.Identity.Name);
                        bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        bal.status = "Awaiting Approval";
                        TempData["Success"] = Resources.Resources.submitted;
                        Rsl = repo.BalModify(bal);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, bal);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }
                //string adminLogin = User.Identity.Name;
                //bal.userId = Login(adminLogin);
                //bal.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                //TempData["Success"] = Resources.Resources.submitted;
                //bal.status = "Awaiting Approval";              
                //Rsl = repo.BalModify(bal);


                return RedirectToAction("Queue", "Queue", null);
            }
        
          
            return RedirectToAction("Queue", "Queue", null);
        }
       
        public ActionResult wof(Wof wof, int id = -1)
        {
            if (id == 0)
            {
                id = Id;
            }
            Saved = false;
            oldForm = false;
            Session[SessionKeys.folderID] = 0;
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            List<Users> statusList = new List<Users>();
            statusList = repo.GetAdmins();

            

            string login = User.Identity.Name;
          
           
            if (id > 0)    // edit
            {              
                wof = repo.wof(id);
                if (wof.folderId == 0)
                {
                    newForm = true;
                }

                wof.admins = statusList;
                state = wof.status;
                Saved = true;
                oldForm = true;
                wof.callbackName = "OnSubmitWof";
                //wof.wasSaved = wasSaved;
                Session[SessionKeys.folderID] = wof.folderId;
                nsfFees = wof.NSFnumberFees;
                numFees = wof.lateNumberFees;
                //ViewBag.WasSaved = wasSaved;
                Id = wof.Id;
                wof.pageHeading = Resources.Resources.Wof+" #" + id.ToString() + "";
                StatItem temp = repo.GetEmployee(login);
                wof.role = temp.role;
                TempData["number"] = urepo.GetEmployee(wof.createdBy).tcsNumber;
                TempData["role"] = temp.role;
                wof.userId = temp.userId;
                return View("wof", wof);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l' && url.Length >= 33)
                {
                    url = url.Substring(0, 33);
                }
                wof = new Wof();
                wof.Id = -1;
                wof.admins = statusList;
                wof.status = "Awaiting Approval";
                wof.pageHeading = Resources.Resources.Wof;
                wof.callbackName = "OnSubmitWof";
                return View("wof", wof);
            }
           
        }
        [HttpPost]
        public ActionResult OnSubmitWof(Wof wof,string Approved, string submitForm)
        {
          

            int Rsl;
            wof.status = state;
            object newId = Id;
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();
            foreach (var user in adminList)
            {
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                wof.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            if (wof.accountNumber == null) { wof.accountNumber = ""; }
          
                if (wof.accountNumber.Length != 9 && wof.accountNumber != "" || Regex.Matches(wof.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                 if (Saved == true)
                {

                    wof.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("wof", new
                    {
                        wof = wof,
                        id = wof.Id
                    }
             );
                }
                if (Saved == false)
                {

                    wof.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("wof",
                          wof = wof
                  );
                }
            }

            if (wof.isLate == true)
            {

                if (wof.lateAmount == 0 || wof.lateNumberFees == 0 || wof.lateAmount == null || wof.lateReason == null)
                {
                    if (Saved == true)
                    {

                        wof.Id = Id;
                        TempData["Message"] = Resources.Resources.missinglate;
                        return RedirectToAction("wof", new
                        {
                            wof = wof,
                            id = wof.Id
                        }
                 );
                    }
                    if (Saved == false)
                    {

                        wof.Id = -1;
                        TempData["Message"] = Resources.Resources.missinglate;
                        return RedirectToAction("wof",
                              wof = wof
                      );
                    }
                }
            }
            if (wof.isNSF == true)
            {

                if (wof.NSFAmount == null || wof.NSFAmount == 0 || wof.NSFnumberFees == 0 || wof.NSFreason == null)
                {
                    if (Saved == true)
                    {

                        wof.Id = Id;
                        TempData["Message"] = Resources.Resources.missingnsf;
                        return RedirectToAction("wof", new
                        {
                            wof = wof,
                            id = wof.Id
                        }
                 );
                    }
                    if (Saved == false)
                    {

                        wof.Id = -1;
                        TempData["Message"] = Resources.Resources.missingnsf;
                        return RedirectToAction("wof",
                              wof = wof
                      );
                    }
                }
            
            }
            if (wof.isRepo == true)
            {

                if (wof.repoAmount == null || wof.repoAmount == 0 || wof.repoNumberFees == 0 || wof.repoReason == null)
                {
                    if (Saved == true)
                    {

                        wof.Id = Id;
                        TempData["Message"] = Resources.Resources.missingrepo;
                        return RedirectToAction("wof", new
                        {
                            wof = wof,
                            id = wof.Id
                        }
                 );
                    }
                    if (Saved == false)
                    {

                        wof.Id = -1;
                        TempData["Message"] = Resources.Resources.missingrepo;
                        return RedirectToAction("wof",
                              wof = wof
                      );
                    }
                }
            }
            if (wof.isCourt == true)
            {

                if (wof.courtAmount == null || wof.courtAmount == 0 || wof.courtNumberFees == 0 || wof.courtReason == null)
                {
                    if (Saved == true)
                    {

                        wof.Id = Id;
                        TempData["Message"] = "Missing Information for Court Fees";
                        return RedirectToAction("wof", new
                        {
                            wof = wof,
                            id = wof.Id
                        }
                 );
                    }
                    if (Saved == false)
                    {

                        wof.Id = -1;
                        TempData["Message"] = "Missing Information for Court Fees";
                        return RedirectToAction("wof",
                              wof = wof
                      );
                    }
                }
            }
            if (wof.isAttorney == true)
            {

                if (wof.attorneyAmount == null || wof.attorneyAmount == 0 || wof.attorneyNumberFees == 0 || wof.attorneyReason == null)
                {if (Saved == true)
                    {

                        wof.Id = Id;
                        TempData["Message"] = Resources.Resources.missingattorney;
                        return RedirectToAction("wof", new
                        {
                            wof = wof,
                            id = wof.Id
                        }
                 );
                    }
                    if (Saved == false)
                    {

                        wof.Id = -1;
                        TempData["Message"] = Resources.Resources.missingattorney;
                        return RedirectToAction("wof",
                              wof = wof
                      );
                    }
                }
            }
            if (wof.isInterest == true)
            {

                if (wof.interestAmount == 0|| wof.interestAmount==null)
                {
                    if (Saved == true)
                    {

                        wof.Id = Id;
                        TempData["Message"] = Resources.Resources.validint;
                        return RedirectToAction("wof", new
                        {
                            wof = wof,
                            id = wof.Id
                        }
                 );
                    }
                    if (Saved == false)
                    {

                        wof.Id = -1;
                        TempData["Message"] = Resources.Resources.validint;
                        return RedirectToAction("wof",
                              wof = wof
                      );
                    }
                }
            }
            switch (Approved)
            {
                case "deny":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Denied";
                    wof.createdOn = Convert.ToString(DateTime.Now);
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", wof.accountNumber + " " + wof.accountName + ": Stat Form Denial", wof);
                    Rsl = repo.WofUpdate(wof, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "Update":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = state;
                    wof.userId = Login(User.Identity.Name);                 
                    //if (wof.user != null)
                    //{
                    //    string savedUrl = url;

                    //    gen.SendEmail(savedUrl, wof.user, wof);
                    //    foreach (var user in wof.user)
                    //    {
                    //        wof.userList += " " + repo.GetEmployee(user).userId;

                    //    }
                    //}
                    Rsl = repo.WofUpdate(wof, Approved);
                    return RedirectToAction("Queue", "Queue", null);
            }
           
            if (wof.status == "Awaiting Approval")
            {
                wof.status = "";
            }

            if (wof.isInterest == true || wof.isHonered == true)
            {
             

                if (301<=wof.interestAmount&& wof.interestAmount<500)
                {
              
                    switch (Approved)
                    {

                        case "supacct":
                            //wof.Id = Convert.ToInt32(newId);
                            //wof.status = "SUP Approved";
                            //wof.supvLogin = Login(User.Identity.Name);
                            //wof.supvApproveDate = DateTime.Now.ToString();
                            //wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.WofUpdate(wof, Approved);
                            //string savedUrl = url;
                            //gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            string savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supdir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supkms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "manacct":
                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "manadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mandir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mankms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amcsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amdir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amkms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amacct":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "adtcm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtmm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcsm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtcs":
                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);



                        case "adadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "addir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adkms":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";

                            return RedirectToAction("Queue", "Queue", null);

                        case "adacct":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "acct":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = " Ready For Processing";
                            wof.dtrLogin = Login(User.Identity.Name);
                            wof.dtrApproveDate = DateTime.Now.ToString();
                            wof.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                    }
                }
                if (wof.interestAmount > 500)
                {
                    switch (Approved)
                    {

                        case "supacct":
                            //wof.Id = Convert.ToInt32(newId);
                            //wof.status = "SUP Approved";
                            //wof.supvLogin = Login(User.Identity.Name);
                            //wof.supvApproveDate = DateTime.Now.ToString();
                            //wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.WofUpdate(wof, Approved);
                            //string savedUrl = url;
                            //gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                           string savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supdir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supkms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "manacct":
                            //wof.Id = Convert.ToInt32(newId);
                            //wof.status = "MAN Approved ";
                            //wof.mgrLogin = Login(User.Identity.Name);
                            //wof.mgrApproveDate = DateTime.Now.ToString();
                            //Rsl = repo.WofUpdate(wof, Approved);
                            //savedUrl = url;
                            //gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "manadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mandir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mankms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "MAN Approved ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amcsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amdir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amkms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "AMAN Approved ";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amacct":

                            //wof.Id = Convert.ToInt32(newId);
                            //wof.status = "AMAN Approved ";
                            //wof.amgrLogin = Login(User.Identity.Name);
                            //wof.amgrApproveDate = DateTime.Now.ToString();
                            //wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.WofUpdate(wof, Approved);
                            //savedUrl = url;
                            //gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);


                        case "adtcm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtmm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcsm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtcs":
                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);



                        case "adadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "addir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adkms":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";

                            return RedirectToAction("Queue", "Queue", null);

                        case "adacct":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "acct":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = " Ready For Processing";
                            wof.dtrLogin = Login(User.Identity.Name);
                            wof.dtrApproveDate = DateTime.Now.ToString();
                            wof.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                    }
                }
            }

            if (wof.isNSF == true || wof.isLate == true)
            {
                if (wof.NSFAmount >= 3 || wof.lateAmount >= 3)
                {
                    switch (Approved)
                    {

                        case "supacct":
                            //wof.Id = Convert.ToInt32(newId);
                            //wof.status = "SUP Approved";
                            //wof.supvLogin = Login(User.Identity.Name);
                            //wof.supvApproveDate = DateTime.Now.ToString();
                            //wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            //Rsl = repo.WofUpdate(wof, Approved);
                            //string savedUrl = url;
                            //gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                          string  savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supdir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "suptms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "supkms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "SUP Approved";
                            wof.supvLogin = Login(User.Identity.Name);
                            wof.supvApproveDate = DateTime.Now.ToString();
                            wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "manacct":
                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mantms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mancss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "manadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mandir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "mankms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing ";
                            wof.mgrLogin = Login(User.Identity.Name);
                            wof.mgrApproveDate = DateTime.Now.ToString();
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtmm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amcsm":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtcs":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amtms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amdir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "amcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amkms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "amacct":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.amgrLogin = Login(User.Identity.Name);
                            wof.amgrApproveDate = DateTime.Now.ToString();
                            wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);


                        case "adtcm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtmm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcsm":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custMan, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtcs":
                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tcsSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);



                        case "adadir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, adir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "addir":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, dir, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adtms":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, tmcSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adcss":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, custSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "adkms":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, kmSup, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";

                            return RedirectToAction("Queue", "Queue", null);

                        case "adacct":


                            wof.Id = Convert.ToInt32(newId);
                            wof.status = "Ready For Processing";
                            wof.adtrLogin = Login(User.Identity.Name);
                            wof.adtrApproveDate = DateTime.Now.ToString();
                            wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                        case "acct":

                            wof.Id = Convert.ToInt32(newId);
                            wof.status = " Ready For Processing";
                            wof.dtrLogin = Login(User.Identity.Name);
                            wof.dtrApproveDate = DateTime.Now.ToString();
                            wof.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                            Rsl = repo.WofUpdate(wof, Approved);
                            savedUrl = url;
                            gen.SendEmail(savedUrl, acct, wof);
                            TempData["Success"] = "Stat Item was succesfully Approved";
                            return RedirectToAction("Queue", "Queue", null);

                    }
                }
            }
                switch (Approved)
            {
                case "supacct":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    string savedUrl = url;
                    gen.SendEmail(savedUrl, acct, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supkms":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.supvLogin = Login(User.Identity.Name);
                    wof.supvApproveDate = DateTime.Now.ToString();
                    wof.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manacct":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcs":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mandir":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.mgrLogin = Login(User.Identity.Name);
                    wof.mgrApproveDate = DateTime.Now.ToString();
                    wof.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "deny":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Denied";
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", wof.accountNumber + " " + wof.accountName + ": Stat Form Denial", wof);
                    Rsl = repo.WofUpdate(wof, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                   wof.Id = Convert.ToInt32(newId);
                   wof.status = "Ready For Processing";
                   wof.amgrLogin = Login(User.Identity.Name);
                   wof.amgrApproveDate = DateTime.Now.ToString();
                   wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtmm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcs":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amadir":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcss":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.amgrLogin = Login(User.Identity.Name);
                    wof.amgrApproveDate = DateTime.Now.ToString();
                    wof.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adacct":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcsm":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcss":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adadir":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adkms":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready For Processing";
                    wof.adtrLogin = Login(User.Identity.Name);
                    wof.adtrApproveDate = DateTime.Now.ToString();
                    wof.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, wof);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);
                case "acct":
                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Ready for Processing ";
                    wof.dtrLogin = Login(User.Identity.Name);
                    wof.dtrApproveDate = DateTime.Now.ToString();
                    wof.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.WofUpdate(wof, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, wof);

                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "Complete":

                    wof.Id = Convert.ToInt32(newId);
                    wof.status = "Completed";
                    wof.accountingLogin = Login(User.Identity.Name);
                    wof.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.WofUpdate(wof, Approved);
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(wof.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (wof.dtrLogin != null)
                    {
                        supervisors.ToList().Add(wof.dtrLogin);
                    }
                    if (wof.adtrLogin != null)
                    {
                        supervisors.ToList().Add(wof.adtrLogin);
                    }
                    if (wof.supvLogin != null)
                    {
                        supervisors.ToList().Add(wof.supvLogin);
                    }
                    if (wof.amgrLogin != null)
                    {
                        supervisors.ToList().Add(wof.amgrLogin);
                    }
                    if (wof.amgrLogin != null)
                    {
                        supervisors.ToList().Add(wof.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", wof.accountNumber + " " + wof.accountName + ": Stat Form Request");

                    return RedirectToAction("Queue", "Queue", null);
            }


          
            if (wof.Id <= 0)
            {
                string adminLogin = User.Identity.Name;
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    wof.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }





                wof.userId = Login(adminLogin);
                wof.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                wof.status = "Awaiting Approval";
                wof.createdOn = Convert.ToString(DateTime.Now);
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }
                switch (submitForm)
                {


                    case "tcsMan":

                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":
                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsadir":
                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":
                        wof.Id = Convert.ToInt32(newId);
                        Rsl = repo.WofModify(wof);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, wof);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }
       
               
            }
           
            return RedirectToAction("Queue", "Queue", null);
      
        }
       
        public ActionResult stm(Stm stm, int id = -1, bool wasSaved = false)
        {
            if (id == 0)
            {
                id = Id;
            }


            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            Saved = false;
        oldForm = false;
          Session[SessionKeys.folderID] = 0;
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();

            if (stm == null)
            {
                TempData["Message"] = "A 9-digit Account number is required";
            }
            string login = User.Identity.Name;

           
            if (id > 0)    // edit
            {
                stm = repo.stm(id);
                if (stm.folderId == 0)
                {
                    newForm = true;
                }

                state = stm.status;
                Saved = true;
                oldForm = true;
                Session[SessionKeys.folderID] = stm.folderId;
                //stm.admins = statusList;
                stm.callbackName = "OnSubmitStm";
                stm.wasSaved = wasSaved;
                ViewBag.WasSaved = wasSaved;
                Id = stm.Id;
                stm.pageHeading = Resources.Resources.Stm+" #" + id.ToString() + "";
                StatItem temp = repo.GetEmployee(login);
                stm.role = temp.role;
                TempData["number"] = urepo.GetEmployee(stm.createdBy).tcsNumber;
                TempData["role"] = temp.role;
                stm.userId = temp.userId;
                return View("stm", stm);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                stm = new Stm();
                stm.Id = -1;
                //stm.admins = statusList;
                stm.status = "Waiting Approval";
                stm.pageHeading = Resources.Resources.Stm;
                stm.callbackName = "OnSubmitStm";
                return View("stm", stm);
            }

        }
        [HttpPost]
        public ActionResult OnSubmitStm(Stm stm, IEnumerable<HttpPostedFileBase> files,string Approved, string submitForm)
        {
            int Rsl;
            stm.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            object newId = Id;
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();
            foreach (var user in adminList)
            {
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            if (stm.accountNumber == null) { stm.accountNumber = ""; }
            if (stm.accountNumber.Length != 9 && stm.accountNumber != "" || Regex.Matches(stm.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                if (Saved == true)
                {

                    stm.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("stm", new
                    {
                        stm = stm,
                        id = stm.Id
                    }
             );
                }
                if (Saved == false)
                {

                    stm.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("stm",
                          stm = stm
                  );
                }
            }
            if (stm.settlementDate==null)
            {
                if (Saved == true)
                {

                    stm.Id = Id;
                    TempData["Message"] = "Settlement date required";
                    return RedirectToAction("stm", new
                    {
                        stm = stm,
                        id = stm.Id
                    }
             );
                }
                if (Saved == false)
                {

                    stm.Id = -1;
                    TempData["Message"] = "Settlement date required";
                    return RedirectToAction("stm",
                          stm = stm
                  );
                }
               
            }
            if (stm.folderId == 0 && stm.accountNumber != "")
            {
                TempData["Message"] = "An attachment is required";
                stm.Id = -1;
                return RedirectToAction("stm", "Forms", stm);
            }
            stm.status = state;
            switch (Approved)
            {
                case "deny":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Denied";
                    stm.createdOn = Convert.ToString(DateTime.Now);
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", stm.accountNumber + " " + stm.accountName + ": Stat Form Denial", stm);
                    Rsl = repo.StmUpdate(stm, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "Update":
            stm.Id = Convert.ToInt32(newId);
            stm.status = state;
            stm.userId = Login(User.Identity.Name);                  
                 
            if (stm.user != null)
            {
                string savedUrl = url;

                //gen.SendEmail(savedUrl, stm.user, stm);
                        foreach (var user in stm.user)
                        {
                            stm.userList += " " + repo.GetEmployee(user).userId;
                        }
                    }
                    Rsl = repo.StmUpdate(stm, Approved);
                    return RedirectToAction("Queue", "Queue", null);

        }
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase file = Request.Files[i];
                if (file.ContentLength > 0)
                {
                    //And we have a directory for the settlement already

                    var filename = stm.folderId + "\\" + Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]), filename);

                    if (Directory.Exists(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]) + "\\" + stm.folderId))
                    {
                        file.SaveAs(path);
                    }
                    else
                    {
                        Directory.CreateDirectory(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]) + "\\" + stm.folderId);
                        file.SaveAs(path);
                    }
                }
            }
            switch (Approved)
            {
                case "deny":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Denied";
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", stm.accountNumber + " " + stm.accountName + ": Stat Form Denial", stm);
                    Rsl = repo.StmUpdate(stm, Approved);
                    return RedirectToAction("Queue", "Queue", null);

                case "supacct":
                    //stm.Id = Convert.ToInt32(newId);
                    //stm.status = "SUP Approved ";
                    //stm.supvLogin = Login(User.Identity.Name);
                    //stm.supvApproveDate = DateTime.Now.ToString();
                    //stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    //Rsl = repo.StmUpdate(stm, Approved);
                    //string savedUrl = url;
                    //gen.SendEmail(savedUrl, acct, stm);
                    TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                   string savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supkms":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "SUP Approved ";
                    stm.supvLogin = Login(User.Identity.Name);
                    stm.supvApproveDate = DateTime.Now.ToString();
                    stm.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manacct":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcs":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "manadir":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mandir":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "mankms":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.mgrLogin = Login(User.Identity.Name);
                    stm.mgrApproveDate = DateTime.Now.ToString();
                    stm.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amacct":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtmm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcs":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcss":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);



                case "amadir":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.amgrLogin = Login(User.Identity.Name);
                    stm.amgrApproveDate = DateTime.Now.ToString();
                    stm.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adacct":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcsm":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcss":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adadir":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adkms":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready For Processing";
                    stm.adtrLogin = Login(User.Identity.Name);
                    stm.adtrApproveDate = DateTime.Now.ToString();
                    stm.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);



                case "acct":
                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Ready for Processing ";
                    stm.dtrLogin = Login(User.Identity.Name);
                    stm.dtrApproveDate = DateTime.Now.ToString();
                    stm.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.StmUpdate(stm, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, stm);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "Complete":

                    stm.Id = Convert.ToInt32(newId);
                    stm.status = "Completed";
                    stm.accountingLogin = Login(User.Identity.Name);
                    stm.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.StmUpdate(stm, Approved);
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(stm.createdBy);
                    List<string> supervisors = new List<string>();

                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (stm.dtrLogin != null)
                    {
                        supervisors.ToList().Add(stm.dtrLogin);
                    }
                    if (stm.adtrLogin != null)
                    {
                        supervisors.ToList().Add(stm.adtrLogin);
                    }
                    if (stm.supvLogin != null)
                    {
                        supervisors.ToList().Add(stm.supvLogin);
                    }
                    if (stm.amgrLogin != null)
                    {
                        supervisors.ToList().Add(stm.amgrLogin);
                    }
                    if (stm.amgrLogin != null)
                    {
                        supervisors.ToList().Add(stm.mgrLogin);
                    }

                    gen.SendAlert(url, supervisors, "", stm.accountNumber + " " + stm.accountName + ": Stat Form Request");

                    return RedirectToAction("Queue", "Queue", null);
            }
            
            if (stm.Id <= 0)
            {
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    stm.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }
                if (stm.Id == 0 && Convert.ToInt32(Session[SessionKeys.folderID]) == 0)
                {
                    TempData["Message"] = "An attachment is required";
                    return RedirectToAction("stm", stm);
                }
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }
                string adminLogin = User.Identity.Name;
                stm.userId = Login(adminLogin);
                stm.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                stm.status = "Awaiting Approval";
                stm.createdOn = Convert.ToString(DateTime.Now);
                switch (submitForm)
                {
                    case "tcsMan":

                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":
                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);


                    case "tcsadir":
                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);


                    case "kmSup":
                        stm.Id = Convert.ToInt32(newId);
                        Rsl = repo.StmModify(stm);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, stm);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);
                }




            }
            return RedirectToAction("Queue", "Queue", null);
        }



       
        public ActionResult ddc(Ddc ddc,int id = -1, bool wasSaved = false)
        {
            if (id == 0)
            {
                id = Id;
            }
            Saved = false;
            oldForm = false;
            Session[SessionKeys.folderID] = 0;
            //HttpCookie cookie = Request.Cookies["_culture"];
            //if (cookie.Value == "es")
            //{
            //    TempData["lang"] = "es";
            //    QueueController queueMethods = new QueueController();
            //    queueMethods.ChangeLanguage("es");
            //}
            url = System.Web.HttpContext.Current.Request.RawUrl;
            if (url.ElementAt(url.Length - 2) == '-')
            {
                url = url.Substring(0, url.Length - 2);
            }
            //List<Users> statusList = new List<Users>();
            //statusList = repo.GetAdmins();
        
            string login = User.Identity.Name;
          
            

            if (id > 0)    // edit
            {
                ddc = repo.ddc(id);
                if (ddc.folderId == 0)
                {
                    newForm = true;
                }

                //ddc.admins = statusList;
                Saved = true;
                oldForm = true;
                ddc.callbackName = "OnSubmitDdc";
                ddc.wasSaved = wasSaved;
                ViewBag.WasSaved = wasSaved;
                Session[SessionKeys.folderID] = ddc.folderId;
                state = ddc.status;
                Id = ddc.Id;
                TempData["number"] = urepo.GetEmployee(ddc.createdBy).tcsNumber;
                ddc.pageHeading = Resources.Resources.Ddc+" #" + id.ToString() + "";
                StatItem temp = repo.GetEmployee(login);
                ddc.role = temp.role;
                TempData["role"] = temp.role;
                ddc.userId = temp.userId;

                return View("ddc", ddc);
            }
            else            // new, blank form
            {
                if (url.ElementAt(url.Length - 1) == 'l')
                {
                    url = url.Substring(0, 33);
                }
                ddc = new Ddc();
                //ddc.admins = statusList;
                ddc.Id = -1;
                ddc.status = "Waiting Approval";
                ddc.pageHeading = Resources.Resources.Ddc;
                ddc.callbackName = "OnSubmitDdc";
                return View("ddc", ddc);
            }
        }
        [HttpPost]
        public ActionResult OnSubmitDdc(Ddc ddc, string Approved, string submitForm)
        {
      
            int Rsl;
            object newId =Id;
       
            List<Users> adminList = new List<Users>();
            adminList = repo.GetAdmins();
            foreach (var user in adminList)
            {
                if (user.department == "tmcMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcMan.Add(user);
                }
                if (user.department == "tcsMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);

                    tcsMan.Add(user);
                }

                if (user.department == "tcsSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tcsSup.Add(user);
                }
                if (user.department == "tmcSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    tmcSup.Add(user);
                }

                if (user.department == "cusSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custSup.Add(user);
                }
                if (user.department == "cusMan")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    custMan.Add(user);
                }
                if (user.department == "kmSup")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    kmSup.Add(user);
                }
                //if (user.department == "acct")
                //{
                //    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                //    acct.Add(user);
                //}
                if (user.department == "adir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    adir.Add(user);
                }
                if (user.department == "dir")
                {
                    user.tcsNumber = user.tcsNumber.Remove(0, 5);
                    dir.Add(user);
                }
            }
            if (ddc.accountNumber == null) { ddc.accountNumber = ""; }
            if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
            {
                ddc.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
            }
            if (ddc.accountNumber.Length != 9 && ddc.accountNumber != "" || Regex.Matches(ddc.accountNumber, @"[a-zA-Z]").Count > 0)
            {
                 if (Saved == true)
                {

                    ddc.Id = Id;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("ddc", new
                    {
                        ddc = ddc,
                        id = ddc.Id
                    }
             );
                }
                if (Saved == false)
                {

                    ddc.Id = -1;
                    TempData["Message"] = Resources.Resources.digit;
                    return RedirectToAction("ddc",
                          ddc = ddc
                  );
                }
            }
            if (ddc.currentDueDate==""||ddc.currentDueDate==null)
            {
                if (Saved == true)
                {

                    ddc.Id = Id;
                    TempData["Message"] = "Please enter a Current Due Date";
                    return RedirectToAction("ddc", new
                    {
                        ddc = ddc,
                        id = ddc.Id
                    }
             );
                }
                if (Saved == false)
                {

                    ddc.Id = -1;
                    TempData["Message"] = "Please enter a New Next Due Date";
                    return RedirectToAction("ddc",
                          ddc = ddc
                  );
                }
            }
            if (ddc.nextDueDate == "" || ddc.nextDueDate == null)
            {
                if (Saved == true)
                {

                    ddc.Id = Id;
                    TempData["Message"] = "Please enter a New Next Due Date";
                    return RedirectToAction("ddc", new
                    {
                        ddc = ddc,
                        id = ddc.Id
                    }
             );
                }
                if (Saved == false)
                {

                    ddc.Id = -1;
                    TempData["Message"] = "Please enter a Current Due Date";
                    return RedirectToAction("ddc",
                          ddc = ddc
                  );
                }
            }
            if (Convert.ToDateTime(ddc.nextDueDate) < Convert.ToDateTime(ddc.currentDueDate))
            {
                if (Saved == false)
                {
                    TempData["Message"] = "Next Due Date Cannot be before Current Due Date";
                    ddc.Id = -1;
                    return RedirectToAction("ddc",
                          ddc = ddc
                    );
                }

                if (Saved == true)
                {
                    ddc.Id = Id;
                    TempData["Message"] = "Next Due Date Cannot be before Current Due Date";
                    return RedirectToAction("ddc", new
                    {
                        ddc = ddc,
                        id = ddc.Id
                    }
     );
                }
            }
          double currentduedate= Convert.ToDateTime(ddc.currentDueDate).Day;
            double nextduedate = Convert.ToDateTime(ddc.nextDueDate).Day;
            double dif = nextduedate - currentduedate;
            if (dif >= 16)
            {

                if (Saved == false)
                {
                    TempData["Message"] = Resources.Resources.errddc;
                    ddc.Id = -1;
                    return RedirectToAction("ddc",
                            ddc = ddc
                      );
                }

                if (Saved == true)
                {
                    ddc.Id = Id;
                    TempData["Message"] = Resources.Resources.errddc;
                    return RedirectToAction("ddc", new
                    {
                        ddc = ddc,
                        id = ddc.Id
                    }
     );
                }
            }
            if (nextduedate > 25)
            {

                if (Saved == false)
                {
                    TempData["Message"] = Resources.Resources.ddcp;
                    ddc.Id = -1;
                    return RedirectToAction("ddc",
                           ddc = ddc
                     );
                }

                if (Saved == true)
                {
                    ddc.Id = Id;
                    TempData["Message"] = Resources.Resources.ddcp;
                    return RedirectToAction("ddc", new
                    {
                        ddc = ddc,
                        id = ddc.Id
                    }
     );
                }
            }
            switch (Approved)
            {
                case "Update":

                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = state;
                    ddc.userId = Login(User.Identity.Name);
                 
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    return RedirectToAction("Queue", "Queue", null);
            }
         
            ddc.status = state;

            if (ddc.status == "Awaiting Approval")
            {
                ddc.status = "";
            }
            if (ddc.numberChanges > 1)
            {
                switch (Approved)
                {
                    case "supacct":
                        //ddc.Id = Convert.ToInt32(newId);
                        //ddc.status = "SUP Approved";
                        //ddc.supvLogin = Login(User.Identity.Name);
                        //ddc.supvApproveDate = DateTime.Now.ToString();
                        //ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        //Rsl = repo.DdcUpdate(ddc, Approved);
                        //string savedUrl = url;
                        //gen.SendEmail(savedUrl, acct, ddc);
                        TempData["Success"] = "The Stat Item could not be forwarded to Accounting because it is not Ready for Processing";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                       string savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptmm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcsm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptcs":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "suptms":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supcss":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supadir":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "supdir":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "supkms":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "SUP Approved";
                        ddc.supvLogin = Login(User.Identity.Name);
                        ddc.supvApproveDate = DateTime.Now.ToString();
                        ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "manacct":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantcm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantmm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancsm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantcs":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mantms":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mancss":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "manadir":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mandir":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "mankms":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.mgrLogin = Login(User.Identity.Name);
                        ddc.mgrApproveDate = DateTime.Now.ToString();
                        ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "amacct":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtcm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtmm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amcsm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtcs":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amtms":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amcss":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amadir":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amdir":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "amkms":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.amgrLogin = Login(User.Identity.Name);
                        ddc.amgrApproveDate = DateTime.Now.ToString();
                        ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adacct":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtcm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtmm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adcsm":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtcs":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tcsSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adtms":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, tmcSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "adcss":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, custSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adadir":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, adir, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                    case "addir":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, dir, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);


                    case "adkms":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready For Processing";
                        ddc.adtrLogin = Login(User.Identity.Name);
                        ddc.adtrApproveDate = DateTime.Now.ToString();
                        ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, kmSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);




                    case "acct":
                        ddc.Id = Convert.ToInt32(newId);
                        ddc.status = "Ready for Processing ";
                        ddc.dtrLogin = Login(User.Identity.Name);
                        ddc.dtrApproveDate = DateTime.Now.ToString();
                        ddc.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                        Rsl = repo.DdcUpdate(ddc, Approved);
                        savedUrl = url;
                        gen.SendEmail(savedUrl, acct, ddc);
                        TempData["Success"] = "Stat Item was succesfully Approved";
                        return RedirectToAction("Queue", "Queue", null);

                }
            }

            switch (Approved)
            {
                case "deny":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Denied";
                    ddc.createdOn = Convert.ToString(DateTime.Now);
                    string tcs = Convert.ToString(TempData["number"]);
                    tcs = tcs.Remove(0, 5);
                    gen.SendDenial(url, tcs, "", ddc.accountNumber + " " + ddc.accountName + ": Stat Form Denial", ddc);
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    return RedirectToAction("Queue", "Queue", null);
                case "supacct":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    string savedUrl = url;
                    gen.SendEmail(savedUrl, acct, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptmm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcsm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptcs":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "suptms":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supcss":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supadir":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "supdir":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "supkms":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.supvLogin = Login(User.Identity.Name);
                    ddc.supvApproveDate = DateTime.Now.ToString();
                    ddc.supvIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manacct":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantmm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancsm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantcs":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mantms":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mancss":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "manadir":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mandir":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "mankms":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.mgrLogin = Login(User.Identity.Name);
                    ddc.mgrApproveDate = DateTime.Now.ToString();
                    ddc.mgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "amacct":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtmm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcsm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtcs":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amtms":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amcss":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amadir":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amdir":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "amkms":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.amgrLogin = Login(User.Identity.Name);
                    ddc.amgrApproveDate = DateTime.Now.ToString();
                    ddc.amgrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adacct":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtmm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcsm":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custMan, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtcs":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tcsSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adtms":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, tmcSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "adcss":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, custSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adadir":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, adir, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);

                case "addir":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, dir, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "adkms":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready For Processing";
                    ddc.adtrLogin = Login(User.Identity.Name);
                    ddc.adtrApproveDate = DateTime.Now.ToString();
                    ddc.adtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, kmSup, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);




                case "acct":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Ready for Processing ";
                    ddc.dtrLogin = Login(User.Identity.Name);
                    ddc.dtrApproveDate = DateTime.Now.ToString();
                    ddc.dtrIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    savedUrl = url;
                    gen.SendEmail(savedUrl, acct, ddc);
                    TempData["Success"] = "Stat Item was succesfully Approved";
                    return RedirectToAction("Queue", "Queue", null);


                case "Complete":
                    ddc.Id = Convert.ToInt32(newId);
                    ddc.status = "Completed";
                    ddc.accountingLogin = Login(User.Identity.Name);
                    ddc.dispositionDate = DateTime.Now.ToString();
                    Rsl = repo.DdcUpdate(ddc, Approved);
                    StatItem temp = new StatItem();
                    temp = quesrepo.GetEmployee(ddc.createdBy);
                    List<string> supervisors = new List<string>();
                    if (temp.userId != null)
                    {
                        supervisors.Add(temp.userId);
                    }
                    if (ddc.dtrLogin != null)
                    {
                        supervisors.ToList().Add(ddc.dtrLogin);
                    }
                    if (ddc.adtrLogin != null)
                    {
                        supervisors.ToList().Add(ddc.adtrLogin);
                    }
                    if (ddc.supvLogin != null)
                    {
                        supervisors.ToList().Add(ddc.supvLogin);
                    }
                    if (ddc.amgrLogin != null)
                    {
                        supervisors.ToList().Add(ddc.amgrLogin);
                    }
                    if (ddc.amgrLogin != null)
                    {
                        supervisors.ToList().Add(ddc.mgrLogin);
                    }


                    gen.SendAlert(url, supervisors, "", ddc.accountNumber + " " + ddc.accountName + ": Stat Form Request");

                    return RedirectToAction("Queue", "Queue", null);
            
        }
            if (ddc.Id <= 0)
            {
                string adminLogin = User.Identity.Name;
                ddc.userId = Login(adminLogin);
                ddc.createdIn = urepo.GetReverseEmployee(User.Identity.Name).initials;
                ddc.createdOn = Convert.ToString(DateTime.Now);
                if (ddc.numberChanges <= 1)
                {
                    ddc.status = "Ready For Processing";
                }
                if (ddc.numberChanges > 1)
                {
                    ddc.status = "Awaiting Approval";
                }
                TempData["Success"] = Resources.Resources.submitted;
                if (url.Contains("?"))
                {
                    url = url.Substring(0, 11);
                }
                if (Convert.ToInt32(Session[SessionKeys.folderID]) != 0)
                {
                    ddc.folderId = Convert.ToInt32(Session[SessionKeys.folderID]);
                }

                switch (submitForm)
                {
                    case "tcsMan":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        string savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsSup":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tcsSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcMan":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tmcSup":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, tmcSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusSup":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "cusMan":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, custMan, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);


                    case "tcsadir":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, adir, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsdir":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, dir, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "tcsacct":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, acct, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                    case "kmSup":
                        ddc.Id = Convert.ToInt32(newId);
                        Rsl = repo.DdcModify(ddc);
                        savedUrl = url + Rsl;
                        gen.SendEmail(savedUrl, kmSup, ddc);
                        TempData["Success"] = "Stat Item was succesfully submitted";
                        return RedirectToAction("Queue", "Queue", null);

                        //gen.SendEmail(savedUrl, rev.user, rev);
                }

            }

            return RedirectToAction("Queue", "Queue", null);

        }

        [HttpPost]
        public ActionResult UploadFiles(IEnumerable<HttpPostedFileBase> files)
        {
            string TempPath = Path.Combine(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]));

            Random rnd = new Random();
            try
            {
                if (oldForm == false)
                {
                    int folder = rnd.Next(10000000);
                    Session[SessionKeys.folderID] = folder;
                }
                if (newForm == true)
                {
                    int folder = rnd.Next(10000000);
                    Session[SessionKeys.folderID] = folder;
                }
                int folders = Convert.ToInt32(Session[SessionKeys2.folderID]);
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];
                    if (file.ContentLength > 0)
                    {

                        var filename = Convert.ToInt32(Session[SessionKeys.folderID]) + "\\" + Path.GetFileName(file.FileName);
                        var path = Path.Combine(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]), filename);
                        if (Directory.Exists(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]) + "\\" + Convert.ToInt32(Session[SessionKeys.folderID])))
                        {
                            file.SaveAs(path);
                            oldForm = true;
                            newForm = false;
                        }
                        else
                        {
                            Directory.CreateDirectory(Server.MapPath(WebConfigurationManager.AppSettings["documentPath"]) + "\\" + Convert.ToInt32(Session[SessionKeys.folderID]));
                            file.SaveAs(path);
                            oldForm = true;
                            newForm = false;
                        }
                    }
                }

                return Json("Successful");
            }
            catch
            {

                return Json("Error");

            }

        }
        private byte[] ReadData(Stream stream)
        {
            byte[] buffer = new byte[16 * 1024];

            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }
        public FileResult Download(string fileName)
        {


            string contentType = string.Empty;

            if (fileName.Contains(".pdf"))
            {
                contentType = "application/pdf";
            }
            else if (fileName.Contains(".PDF"))
            {
                contentType = "application/pdf";
            }

            else if (fileName.Contains(".docx"))
            {
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }
            else if (fileName.Contains(".txt"))
            {
                contentType = "text/plain";
            }
            else if (fileName.Contains(".xlsx"))
            {
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }
            else if (fileName.Contains(".zip"))
            {
                contentType = "application/zip";
            }

            else
                contentType = System.Net.Mime.MediaTypeNames.Application.Octet;
            string item = WebConfigurationManager.AppSettings["documentPath"] + "\\" + fileName;
            return File(item, contentType);
        }

    }

    public static class Utility
    {
        public static DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            DataTable dt = new DataTable();
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }

                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    if (rows.Length > 1)
                    {
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i].Trim();
                        }
                        dt.Rows.Add(dr);
                    }
                }

            }


            return dt;
        }
      
        public static DataTable ConvertXSLXtoDataTable(string strFilePath, string connString)
        {
            OleDbConnection oledbConn = new OleDbConnection(connString);
            DataTable dt = new DataTable();
            try
            {

                oledbConn.Open();
                using (OleDbCommand cmd = new OleDbCommand("SELECT * FROM [Sheet1$]", oledbConn))
                {
                    OleDbDataAdapter oleda = new OleDbDataAdapter();
                    oleda.SelectCommand = cmd;
                    DataSet ds = new DataSet();
                    oleda.Fill(ds);

                    dt = ds.Tables[0];
                }
            }
            catch
            {
            }
            finally
            {

                oledbConn.Close();
            }

            return dt;

        }
    }
}