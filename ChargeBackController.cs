using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;
using StatsGUI.DAL;
using StatsGUI.Models;

using System.Collections;

using System.Collections.Specialized;
using System.Data;
using StatsGUI.Models;
using System.Collections.Generic;


namespace StatsGUI.Controllers
{
    public class ChargeBackController : Controller
    {
        QueueRepository repo = new QueueRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);

        #region Current User
        public DataTable ChargeBackCodeTable(DataTable chargebackcodes = null)
        {
            DataTable codes = new DataTable();
            if (Session["CodeTable"] == null)
            {
                codes = chargebackcodes;
                Session["CodeTable"] = codes;
            }
            else
            {
                codes = (DataTable)Session["CodeTable"];
            }
            return codes;
        }


        #endregion
        #region chargebacks
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            return RedirectToAction("Chargeback");
        }
        [Authorize(Roles = "Admin")]
        //public ActionResult ChargeBack()
        //{
        //    { 


        //    List<Queues> read;
        //    QueueRepository repo = new QueueRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
        //    //Convert to view model to avoid JSON serialization problems due to circular references.
 
        //    //stored procedure returns requested customer parameters
        //    read = repo.GetPendingQueues();

           
        //    return View(read);

        //}

        //}
        public FileResult Download(string fileName)
        {
            //This will set the current user viewbag variable which is on the _layout page
     
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


        public ActionResult QueueDetails(int id, string type)
        {
            {
                if (type == "Advanced Payment")
                {
                    return RedirectToAction("GetAP", new { id = id });
                }
                if (type == "Send PIF")
                {
                    return RedirectToAction("GetPIF", new { id = id });
                }
                if (type == "Reverse Payment")
                {
                    return RedirectToAction("GetRP", new { id = id });
                }
                if (type == "Reimburse")
                {
                    return RedirectToAction("GetReimburse", new { id = id });
                }
                if (type == "Update Customer")
                {
                    return RedirectToAction("GetCustomer", new { id = id });
                }
                if (type == "Waive Outstanding Fees")
                {
                    return RedirectToAction("GetOutstanding", new { id = id });
                }
                if (type == "Deferment")
                {
                    return RedirectToAction("GetDeferment", new { id = id });
                }
                if (type == "Waive Small Balance") {
               return RedirectToAction("GetWaive", new { id = id });
                }
                if (type == "Settlement")
                {
                    return RedirectToAction("GetSettlement", new { id = id });
                }
            }
            return View();

        }
    
        //public ActionResult GetDeferment(int id)
        //{
        //    Deferement retVal = new Deferement();
        //    retVal = repo.GetDeferement(id);
        //    retVal.Documents = repo.testBuildDocumentList(retVal.Directory);

        //    return View(retVal);
        //}
        //[HttpPost]
        //public ActionResult GetDeferment(Deferement retVal)
        //{
            
        //    retVal = repo.GetDeferement(retVal.ID);
        //    retVal.Documents = repo.testBuildDocumentList(retVal.Directory);
        //    return RedirectToAction("ChargeBack");
        //}
        public ActionResult GetRP(int id)
        {
            ReveresedPayments retVal = new ReveresedPayments();
            retVal = repo.GetRP(id);
            return View(retVal);
        }
        [HttpPost]
        public ActionResult GetRP(ReveresedPayments retVal)
        {

            repo.UpdateAdvanceReport(retVal);
            retVal = repo.GetRP(retVal.ID);
            return View(retVal);
        }
        public ActionResult GetWaive(int id)
    {
        Waived retVal = new Waived();
        retVal = repo.GetWaive(id);
        return View(retVal);
    }
        [HttpPost]
        public ActionResult GetWaive(Waived retVal)
        {

            repo.UpdateWaiveReport(retVal);
            retVal = repo.GetWaive(retVal.ID);
            return View(retVal);
        }
        //public ActionResult GetSettlement(int id)
        //{
        //    Settlement retVal = new Settlement();
        //    retVal = repo.GetSettlement(id);
        //    retVal.Documents = repo.testBuildDocumentList(retVal.Directory);
        //    return View(retVal);
        //}
        [HttpPost]
        public ActionResult GetSettlement(Settlement retVal)
        {

            repo.UpdateSettlementReport(retVal);
            retVal = repo.GetSettlement(retVal.ID);
            return View(retVal);
        }
        public ActionResult GetCustomer(int id)
        {
            CusAccount retVal = new CusAccount();
            retVal = repo.GetCustomer(id);
            return View(retVal);
        }
        [HttpPost]
        public ActionResult GetCustomer(CusAccount retVal)
        {

            repo.UpdateCustomerReport(retVal);
            retVal = repo.GetCustomer(retVal.AccountNumber);
            return View(retVal);
        }
        public ActionResult GetPIF(int id)
        {
            CusAccount retVal = new CusAccount();
            retVal = repo.GetPIF(id);
            return View(retVal);
        }
        [HttpPost]
        public ActionResult GetPIF(CusAccount retVal)
        {

            repo.UpdatePIFReport(retVal);
            retVal = repo.GetPIF(retVal.AccountNumber);
            return View(retVal);
        }
        public ActionResult GetOutstanding(int id)
        {
            Fee retVal = new Fee();
            retVal = repo.GetWaiveO(id);
            return View(retVal);
        }
        [HttpPost]
        public ActionResult GetReimburse(Fee retVal)
        {

            repo.UpdateReimburseReport(retVal);
            retVal = repo.GetReimbursement(retVal.ID);
            return View(retVal);
        }
        public ActionResult GetReimburse(int id)
        {
            Fee retVal = new Fee();
            retVal = repo.GetReimbursement(id);
            return View(retVal);
        }
        [HttpPost]
        public ActionResult GetOutstanding(Fee retVal)
        {

            repo.UpdateWOReport(retVal);
            retVal = repo.GetWaiveO(retVal.ID);
            return View(retVal);
        }

        public ActionResult GetAP(int id)
        {
            AdvancedPayments retVal = new AdvancedPayments();
            retVal = repo.GetAP(id);
            return View(retVal);
        }
        [HttpPost]
        public ActionResult GetAP(AdvancedPayments retVal)
        {
            
             repo.UpdateAPReport(retVal);
            retVal = repo.GetAP(retVal.ID);
            return View(retVal);
        }
        
        #endregion
    }
}