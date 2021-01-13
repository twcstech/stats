using StatsGUI.DAL;
using StatsGUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace StatsGUI.Controllers
{
    public class AccountingController : Controller
    {
        AccountingRepository repo = new AccountingRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
        // GET: Accounting
        [HttpGet]
        public ActionResult Table(int id = 0)
        {
            AccountingRepository repos = new AccountingRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
            StatsRepository repo = new StatsRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
            Counts approvals = new Counts();
            approvals.apCount = repo.getAPCount();
            approvals.cusCount = repo.getCusCount();
            approvals.defCount = repo.getDefCount();
            approvals.ofCount = repo.getOFCount();
            approvals.pifCount = repo.getPIFCount();
            approvals.rfCount = repo.getRFCount();
            approvals.rpCount = repo.getRPCount();
            approvals.setCount = repo.getSetCount();
            approvals.wfCount = repo.getWFCount();
            if (id == 1)
            {
                List<Deferement> read = new List<Deferement>();
                read = repos.GetDeferements();
                approvals.Deferments = read;
            }
            if (id == 2)
            {
                List<Settlement> read = new List<Settlement>();
                read = repos.GetSettlements();
                approvals.Settlements = read;
            }
            if (id == 3)
            {
                List<Fee> read = new List<Fee>();
                read = repos.GetOutstandingFees();
                approvals.oustandingFees = read;
            }
            if (id == 4)
            {
                List<CusAccount> read = new List<CusAccount>();
                read = repos.GetCustomers();
                approvals.Account = read;
            }
            if (id == 5)
            {
                List<AdvancedPayments> read = new List<AdvancedPayments>();
                read = repos.GetaPayments();
                approvals.AP = read;
            }
            if (id == 6)
            {
                List<ReveresedPayments> read = new List<ReveresedPayments>();
                read = repos.GetrPayments();
                approvals.RP = read;
            }
            if (id == 7)
            {
                List<CusAccount> read = new List<CusAccount>();
                read = repos.GetPIF();
                approvals.PIF = read;
            }
            if (id == 8)
            {
                List<Fee> read = new List<Fee>();
                read = repos.GetRFees();
                approvals.reimburseFees = read;
            }
            if (id == 9)
            {
                List<Waived> read = new List<Waived>();
                read = repos.GetWaviedFees();
                approvals.waivedFees = read;
            }
            return View(approvals);
        }
        public ActionResult GetDeferment(int id)
        {
            Deferement retVal = new Deferement();
            retVal = repo.GetDeferement(id);
            retVal.Documents = repo.testBuildDocumentList(retVal.Directory);

            return View(retVal);
        }

        public ActionResult GetRP(int id)
        {
            ReveresedPayments retVal = new ReveresedPayments();
            retVal = repo.GetRP(id);
            return View(retVal);
        }
  
        public ActionResult GetWaive1(int id)
        {
            Waived retVal = new Waived();
            retVal = repo.GetWaive(id);
            return View(retVal);
        }
  
        public ActionResult GetSettlement(int id)
        {
            Settlement retVal = new Settlement();
            retVal = repo.GetSettlement(id);
            retVal.Documents = repo.testBuildDocumentList(retVal.Directory);
            return View(retVal);
        }

        public ActionResult GetCustomer(int id)
        {
            CusAccount retVal = new CusAccount();
            retVal = repo.GetCustomer(id);
            return View(retVal);
        }
 
        public ActionResult GetPIF(int id)
        {
            CusAccount retVal = new CusAccount();
            retVal = repo.GetPIF(id);
            return View(retVal);
        }

        public ActionResult GetOutstanding(int id)
        {
            Fee retVal = new Fee();
            retVal = repo.GetWaiveO(id);
            return View(retVal);
        }
      
        public ActionResult GetReimburse(int id)
        {
            Fee retVal = new Fee();
            retVal = repo.GetReimbursement(id);
            return View(retVal);
        }

        public ActionResult GetAP(int id)
        {
            AdvancedPayments retVal = new AdvancedPayments();
            retVal = repo.GetAP(id);
            return View(retVal);
        }
     

    }
}