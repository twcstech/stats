using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using StatsGUI.Models;
using StatsGUI.DAL;
using System.Web.Configuration;
using System.Data.OleDb;
using System.Data;

namespace StatsGUI.Controllers
{
    public class UserController : Controller
    {
        public static string loads ="";
        QueueRepository qrepo = new QueueRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
        UserRepository repo = new UserRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
        // GET: User
        public ActionResult AddUser(string sortOrder = "")

        {
            

            Users userList = new Users();
            List<Users> users = repo.GetUsers();
            userList.userItems = users;
            userList.roleList = GetRoleList();
            ViewBag.Name = sortOrder == "Name" ? "name_desc" : "Name";
            ViewBag.Number = sortOrder == "Number" ? "num_desc" : "Number";
            ViewBag.Role = sortOrder == "Role" ? "role_desc" : "Role";
            var search = from s in users
                         select s;
            switch (sortOrder)
            {
                case "Name":
                    search = search.OrderBy(s => s.tcsName);
                    break;
                case "name_desc":
                    search = search.OrderByDescending(s => s.tcsName);
                    break;
                case "Number":
                    search = search.OrderBy(s => s.tcsNumber);
                    break;
                case "num_desc":
                    search = search.OrderByDescending(s => s.tcsNumber);
                    break;
                case "Role":
                    search = search.OrderBy(s => s.role);
                    break;
                case "role_desc":
                    search = search.OrderByDescending(s => s.role);
                    break;

            }
            userList.userItems = search;
            return View(userList);
        }
       [HttpPost]
        public ActionResult AddUser(Users user, string searchString="")
        {if (user.tcsName != null)
            {
                if (user.tcsName.Contains("'"))
                {
                    user.tcsName = user.tcsName.Replace(@"'", "");
                }
                if (user.tcsName.Contains("."))
                {
                    user.tcsName = user.tcsName.Replace(@".", "");
                }
                if (user.tcsName.EndsWith(" "))
                {
                    user.tcsName = user.tcsName.Replace(@" ", "");
                }
            }
            Users userList = new Users();
            List<Users> users = repo.GetUsers();
            
            userList.roleList = GetRoleList();

           
            var search = from s in users
                        select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                search = search.Where(s => s.tcsNumber.ToLower().Contains(searchString)
                || s.tcsName.ToLower().ToString().Contains(searchString)||s.tcsNumber.Contains(searchString.ToUpper())
                || s.tcsName.ToString().Contains(searchString.ToUpper())||s.tcsNumber.Contains(searchString)
                || s.tcsName.ToString().Contains(searchString) || s.role.ToString().Contains(searchString));
               
            }
        
            userList.userItems = search;
            if (user != null && user.tcsName != null)
            {
                repo.newEmployee(user);
                 return RedirectToAction("AddUser", "User");
            }
            return View(userList);
        }
       
        public ActionResult EditUser(string id)
        {
            Users user = repo.GetEmployee(id);
            user.roleList = GetRoleList();
           TempData["id"]= user.id;
            return View(user);

        }
        [HttpPost]
        public ActionResult EditUser(Users user)
        {
            user.id = Convert.ToString(TempData["id"]);
            if (user.tcsName.Contains("'"))
            {
                user.tcsName = user.tcsName.Replace("'", "");
            }
            repo.updateEmployee(user);

            return RedirectToAction("AddUser","User", null);
        }
        public ActionResult Update()
        {
            int row;           
            List<Users> updatewithInitials=Load();
            repo.deleteInitials();  
            row= repo.refreshInitials(updatewithInitials);
            repo.updateInitials();
            return RedirectToAction("AddUser");
        }
        //this method wil not debug on localhost, to debug comment this section or replace references with GetUsers(); do not publish without restoring this method
        public static List<Users> Load(string filename = "C:\\Users\\tcs2320\\Documents\\Meagsysintials.xlsx", string sheetName = "Meagsysintials")
        {
            
            UserRepository urepo = new UserRepository(WebConfigurationManager.ConnectionStrings["Main"].ConnectionString);
            List<Users> users = urepo.GetUsers();
            foreach (string connectionStringBase in new[]
                {
            "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=Excel 12.0;",
            "Provider=Microsoft.Jet.OLEDB.4.0;Data source={0};Extended Properties=Excel 8.0;"
        })
            {
                try
                {
                    string connectionString = String.Format(connectionStringBase, filename);
                    OleDbDataAdapter adapter = new OleDbDataAdapter(String.Format("select * from [{0}$]", sheetName), connectionString);
                    DataSet dataset = new DataSet();
                    adapter.Fill(dataset, "dummy");

                    System.Data.DataTable table = dataset.Tables["dummy"];
                    List<Users> result = new List<Users>();
                    foreach (System.Data.DataRow row in table.Rows)
                    {

                        Users item = new Users { };
                        item.tcsName = Convert.ToString(row.ItemArray[1]);
                        if (item.tcsName.Contains("'"))
                        {
                            item.tcsName = item.tcsName.Replace("'", string.Empty);
                        }
                        if (item.tcsName.Contains("."))
                        {
                            item.tcsName = item.tcsName.Replace(".", string.Empty);
                        }
                        item.initials = Convert.ToString(row.ItemArray[3]);
                        foreach (var row2 in users)
                        {
                            if (item.tcsName == row2.tcsName.ToUpper())
                            {
                                result.Add(item);
                                loads =item.tcsName+" "+item.initials;

                            }

                        }
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    loads = Convert.ToString(ex);
                    throw;
                }
               
            }

            throw new ArgumentOutOfRangeException("filename", "File does not contain import data in a known format.");
          
        }

        public ActionResult Delete(string id)
        {
            repo.deleteEmployee(id);

            return RedirectToAction("AddUser", "User", null);
        }
        private IEnumerable<SelectListItem> GetRoleList()
        {
            List<SelectListItem> statusList = new List<SelectListItem>();
            statusList.Add(new SelectListItem() { Value = "rep", Text = "rep" });
            statusList.Add(new SelectListItem() { Value = "manager", Text = "manager" });
            statusList.Add(new SelectListItem() { Value = "supervisor", Text = "supervisor" });
            statusList.Add(new SelectListItem() { Value = "ast manager", Text = "assistant manager" });
            statusList.Add(new SelectListItem() { Value = "assistant", Text = "assistant director" });
            statusList.Add(new SelectListItem() { Value = "director", Text = "director" });
            statusList.Add(new SelectListItem() { Value = "accountant", Text = "accountant" });
            return new SelectList(statusList, "Value", "Text");
        }

    }
}