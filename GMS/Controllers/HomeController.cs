using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GMS.Models;
using System.Data.Entity.Validation;
using OfficeOpenXml;

namespace GMS.Controllers {
    public class HomeController : Controller {

        //METHODS FOR INITIAL VIEW CONTENT

        //Main page, should redirect here after successful login
        public ActionResult gmsHome() {
            History t = new History();
            Session["guestCount"] = 0;
            Session["host"] = null;
            Session["guestIsFound"] = null;
            Session["tabStatus"] = "tabIn";
            return View(t);
        }


        //Should refer to start page, refer back on sign out
        public ActionResult login() {
            ViewBag.Message = "";

            return View();
        }


        //Authorize with building account, switch on associated building
        public ActionResult currentGuests() {
            List<History> t = new List<History>();

            using (masterEntities db = new masterEntities()) {
                //Display all active guests across campus if user is Admin
                if ((bool)Session["adminAccess"] == true) {
                    List<History> allGuests = db.Histories.Where(x => x.outTime == null).ToList();

                    //Set History object t to needed values for active guest table
                    for (int i = 0; i < allGuests.Count(); i++) {
                        t.Add(new History());
                        t.ElementAt(i).Guest = new Guest();
                        t.ElementAt(i).Resident = new Resident();
                        t.ElementAt(i).Guest.firstName = allGuests.ElementAt(i).Guest.firstName;
                        t.ElementAt(i).Guest.lastName = allGuests.ElementAt(i).Guest.lastName;
                        t.ElementAt(i).Guest.gender = allGuests.ElementAt(i).Guest.gender;
                        t.ElementAt(i).guestID = allGuests.ElementAt(i).guestID;
                        t.ElementAt(i).inTime = allGuests.ElementAt(i).inTime;
                        t.ElementAt(i).bedspace = allGuests.ElementAt(i).bedspace;
                        t.ElementAt(i).Resident.firstName = allGuests.ElementAt(i).Resident.firstName;
                        t.ElementAt(i).Resident.lastName = allGuests.ElementAt(i).Resident.lastName;
                    }

                    return View(t);
                }


                string myBuilding = (string)Session["building"];
                List<History> guestList = db.Histories.Where(x => x.building == myBuilding && x.outTime == null).ToList();
                
                //Set History object t to needed values for active guest table
                for (int i = 0; i < guestList.Count(); i++) {
                    t.Add(new History());
                    t.ElementAt(i).Guest = new Guest();
                    t.ElementAt(i).Resident = new Resident();
                    t.ElementAt(i).Guest.firstName = guestList.ElementAt(i).Guest.firstName;
                    t.ElementAt(i).Guest.lastName = guestList.ElementAt(i).Guest.lastName;
                    t.ElementAt(i).Guest.gender = guestList.ElementAt(i).Guest.gender;
                    t.ElementAt(i).guestID = guestList.ElementAt(i).guestID;
                    t.ElementAt(i).inTime = guestList.ElementAt(i).inTime;
                    t.ElementAt(i).bedspace = guestList.ElementAt(i).bedspace;
                    t.ElementAt(i).Resident.firstName = guestList.ElementAt(i).Resident.firstName;
                    t.ElementAt(i).Resident.lastName = guestList.ElementAt(i).Resident.lastName;
                }
            }
            return View(t);
        }


        //Authorize with admin account access only
        public ActionResult admin() {
            History t = new History();
            Session["host"] = null;
            Session["guestIsFound"] = null;
            return View(t);
        }



        //METHODS FOR LOGIN VIEW CONTENT//

        //Login authentication method, will validation and redirect the user on successful login
        [HttpPost]
        public ActionResult authenticate(Account users) {
            using (masterEntities db = new masterEntities()) {
                //Returns null if no such user exists, returns with attributes from user table in database otherwise
                var userDetails = db.Users.Where(x => x.username == users.username && x.pword == users.password).FirstOrDefault();

                if (userDetails == null) {
                    users.errorMessage = "Invalid Credentials";
                    return View("login", users);
                }

                else {
                    //Set default session values to carry over into the other site functions
                    //NOTE: Session variables use cookies, so cookies must be enabled for this site to function
                    Session["username"] = userDetails.username;
                    Session["building"] = userDetails.associatedBuilding;
                    Session["adminAccess"] = userDetails.isAdmin;
                    Session["guestCount"] = 0;
                    Session["host"] = null;
                    Session["guestIsFound"] = null;
                    Session["tabStatus"] = "tabIn";
                    return View("gmsHome");
                }
            }

        }


        //Method to log out the user, and clear all Session information
        public ActionResult logout() {
            Session.Abandon();
            return RedirectToAction("login", "Home");
        }



        //METHODS FOR GMSHOME VIEW CONTENT//

        //Method to grab resident's profile after scanning zip card field
        [HttpPost]
        public ActionResult grabResident(History t) {
            using (masterEntities db = new masterEntities()) {
                //Remove leading 0's from accepted form result... for scanner input
                string zero = t.Resident.studentID.ToString();
                for (int i = 0; i < zero.Length; i++) {
                    if (zero[i] == '0') continue;
                    else {
                        int temp = 0;
                        //Parse will always be successful due to form validating numbers only input
                        int.TryParse(zero.Substring(i), out temp);
                        t.Resident.studentID = temp;
                        break;
                    }
                }

                var residentDetails = db.Residents.Where(x => x.studentID == t.Resident.studentID).FirstOrDefault();

                //Check if a resident with this ID exists
                if (residentDetails == null) {
                    TempData["msg"] = "<script>alert('No resident with this ID number exists');</script>";
                    return View("gmsHome", t);
                }

                //Fill in the Resident related fields based on table data
                t.hostID = residentDetails.studentID;
                t.Resident.studentID = residentDetails.studentID;
                t.Resident.firstName = residentDetails.firstName;
                t.Resident.lastName = residentDetails.lastName;
                t.Resident.building = residentDetails.building;
                t.building = residentDetails.building;
                t.Resident.room = residentDetails.room;
                t.bedspace = residentDetails.room;
                t.Resident.moveInDate = residentDetails.moveInDate;

                //Check if the resident does not live in this building
                if (t.Resident.building.ToUpper() != Session["username"].ToString().ToUpper()) {
                    TempData["msg"] = "<script>alert('This resident does not live in this building');</script>";
                    return View("gmsHome", t);
                }

                //Prevent null exceptions by providing default values for the Guest fields
                t.Guest = new Guest();
                t.Guest.firstName = "";
                t.Guest.lastName = "";
                t.Guest.gender = false;
                t.Guest.identityType = "";
                t.Guest.isStudent = false;
                t.Guest.prohibited = false;

                //Check how many guests the resident currently has registered
                int activeGuests = db.Histories.Where(x => x.hostID == t.Resident.studentID && x.outTime == null).Count();

                Session["host"] = t.Resident.firstName + " " + t.Resident.lastName;
                Session["guestIsFound"] = null;
                Session["guestCount"] = activeGuests;
                Session["tabStatus"] = "tabIn";

                return View("gmsHome", t);
            }

        }


        //Method to validate that a guest profile already exists in the database
        [HttpPost]
        public ActionResult grabGuest(History t) {
            //Remove leading 0's from accepted form result... for scanner input
            if (t.Guest.guestID != null) {
                string zero = t.Guest.guestID;
                for (int i = 0; i < zero.Length; i++) {
                    if (zero[i] == '0') continue;
                    else {
                        int temp = 0;
                        int.TryParse(zero.Substring(i), out temp);
                        t.Guest.guestID = temp.ToString();
                        break;
                    }
                }
            }


            using (masterEntities db = new masterEntities()) {
                var guestDetails = db.Guests.Where(x => x.guestID == t.Guest.guestID).FirstOrDefault();
                Session["tabStatus"] = "tabIn";

                //Guest Profile does not yet exist
                if (guestDetails == null) {
                    //We will have to create a new guest profile
                    //First check in the guest is a student (i.e. ID is a Zip Card ID)
                    var zipDetails = db.ZipCards.Where(x => x.zipID.ToString() == t.Guest.guestID).FirstOrDefault();
                    if (zipDetails == null) {
                        //Guest is not a student, their ID was not found in zip card table
                        //Redirect back to the gmsHome page, the newGuest form will have extra fields
                        Session["guestIsFound"] = "false";
                        t.Guest.firstName = "";
                        t.Guest.lastName = "";
                        t.Guest.gender = true;
                        t.Guest.prohibited = false;
                        t.Guest.isStudent = false;

                        return View("gmsHome", t);
                    }

                    else {
                        //Guest is a student, their zip card was found in the table
                        //Note: Guest assumed not to be prohibited if they do not have a profile yet
                        Guest g = new Guest();
                        g.guestID = zipDetails.zipID.ToString();
                        g.firstName = zipDetails.firstName;
                        g.lastName = zipDetails.lastName;
                        g.gender = zipDetails.gender;
                        g.identityType = "Zip";
                        g.prohibited = false;
                        g.isStudent = true;

                        //Add guest profile to Guests table
                        db.Guests.Add(g);
                        db.Entry(g).State = System.Data.Entity.EntityState.Added;
                        db.SaveChanges();

                        //Set Guest Field form values
                        t.guestID = g.guestID;
                        t.Guest.firstName = g.firstName;
                        t.Guest.lastName = g.lastName;
                        t.Guest.gender = g.gender;
                        t.Guest.prohibited = g.prohibited;
                        t.Guest.isStudent = g.isStudent;
                        t.Guest.identityType = g.identityType;

                        //Check to see if guest is prohibited, and throw warning to user
                        if (t.Guest.prohibited == true) {
                            TempData["msg"] = "<script>alert('This guest is prohibited from entering this building');</script>";
                            Session["guestIsFound"] = null;
                            return View("gmsHome", t);
                        }

                        Session["guestIsFound"] = "true";

                        return View("gmsHome", t);
                    }
                }

                //Guest Profile already exists
                else {
                    //Guest Fields - Used to fill form values
                    t.guestID = guestDetails.guestID;
                    t.Guest.guestID = guestDetails.guestID;
                    t.Guest.firstName = guestDetails.firstName;
                    t.Guest.lastName = guestDetails.lastName;
                    t.Guest.isStudent = guestDetails.isStudent;
                    t.Guest.gender = guestDetails.gender;
                    t.Guest.prohibited = guestDetails.prohibited;
                    t.Guest.identityType = guestDetails.identityType;

                    //Check to see if guest is prohibited, and throw warning to user
                    if (t.Guest.prohibited == true) {
                        Session["guestIsFound"] = null;
                        TempData["msg"] = "<script>alert('This guest is prohibited from entering this building');</script>";
                        return View("gmsHome", t);
                    }

                    Session["guestIsFound"] = "true";

                    //Send model data back to gmsHome page
                    return View("gmsHome", t);
                }
            }
        }


        //Method to validate, and then add new guest data into the system
        [HttpPost]
        public ActionResult newGuest(History t) {
                Session["tabStatus"] = "tabIn";
                //Check if this guest was not registered, if so create new guest profile
                if (Session["guestIsFound"].ToString() != "true") {
                    t.guestID = t.Guest.guestID;

                    //Create new guest profile
                    Guest g = new Guest();
                    g.guestID = t.Guest.guestID;
                    g.firstName = t.Guest.firstName;
                    g.lastName = t.Guest.lastName;
                    g.gender = t.Guest.gender;
                    g.identityType = t.Guest.identityType;
                    g.prohibited = false;
                    g.isStudent = false;

                    using (masterEntities db = new masterEntities()) {

                        //Add guest profile to Guests table
                        db.Guests.Add(g);
                        db.Entry(g).State = System.Data.Entity.EntityState.Added;
                        db.SaveChanges();
                    }
                }

                else {
                    t.Guest.guestID = t.guestID;
                }

            using (masterEntities db = new masterEntities()) {
                //Update history fields, then insert into table
                t.building = (string)Session["building"];
                t.updatedBy = (string)Session["username"];
                t.inTime = DateTime.Now;

                //Insert our new guest into History table
                db.Histories.Add(t);

                //NOTE: Without setting the state of the Guest & Resident table to unchanged,
                //it will attempt to add another guest & resident profile
                db.Entry(t.Guest).State = System.Data.Entity.EntityState.Unchanged;
                db.Entry(t.Resident).State = System.Data.Entity.EntityState.Unchanged;
                db.SaveChanges();

                //Reset form values after successful guest insertion
                int activeGuests = db.Histories.Where(x => x.hostID == t.Resident.studentID && x.outTime == null).Count();
                Session["guestIsFound"] = null;
                Session["guestCount"] = activeGuests;
            }

            return View("gmsHome", t);
        }


        //Method to check out a guest
        public ActionResult checkOut(History t) {
            using (masterEntities db = new masterEntities()) {
                //Check this guest out of all buildings, not just the one where they scanned out
                List<History> activeTransactions = db.Histories.Where(x => x.guestID == t.guestID && x.outTime == null).ToList();
                for (int i = 0; i < activeTransactions.Count(); i++) {
                    //Set the check out time to right now
                    activeTransactions.ElementAt(i).outTime = DateTime.Now;
                    db.Entry(activeTransactions.ElementAt(i)).State = System.Data.Entity.EntityState.Modified;

                    //Since Guest and Resident are virtual members, do not add a row for the foreign keys
                    db.Entry(activeTransactions.ElementAt(i).Guest).State = System.Data.Entity.EntityState.Unchanged;
                    db.Entry(activeTransactions.ElementAt(i).Resident).State = System.Data.Entity.EntityState.Unchanged;
                    db.SaveChanges();

                    //Set success message through Session object
                    Session["guestIsFound"] = "Guest successfully checked out!";
                }
            }

            t.guestID = "";
            Session["guestCount"] = -1;
            Session["tabStatus"] = "tabOut";
            return View("gmsHome", t);
        }


        //Method to check out a set of guests
        public ActionResult checkOutSet(History t, int[] set) {
            using (masterEntities db = new masterEntities()) {
                List<List<History>> actives = new List<List<History>>();
                for (int i = 0; i < set.Count(); i++) {
                    //LINQ does not support array index values, so use a temporary variable here
                    var temp = set[i];
                    actives.Add(db.Histories.Where(x => x.guestID == temp.ToString() && x.outTime == null).ToList());
                }

                for (int i = 0; i < actives.Count(); i++) {
                    for (int j = 0; j < actives.ElementAt(i).Count(); j++) {
                        actives.ElementAt(i).ElementAt(j).outTime = DateTime.Now;
                        db.Entry(actives.ElementAt(i).ElementAt(j)).State = System.Data.Entity.EntityState.Modified;

                        //Since Guest and Resident are virtual members, do not add a row for the foreign keys
                        db.Entry(actives.ElementAt(i).ElementAt(j).Guest).State = System.Data.Entity.EntityState.Unchanged;
                        db.Entry(actives.ElementAt(i).ElementAt(j).Resident).State = System.Data.Entity.EntityState.Unchanged;
                        db.SaveChanges();
                    }
                }

                //Reset form values after successful checkout
                int activeGuests = db.Histories.Where(x => x.hostID == t.Resident.studentID && x.outTime == null).Count();
                Session["guestIsFound"] = null;
                Session["guestCount"] = activeGuests;

                return View("gmsHome", t);
            }
        }



        //METHODS FOR CURRENT GUESTS VIEW CONTENT//

        //Method to check out a guest through an action link
        public ActionResult quickCheckOut(string gid) {
            using (masterEntities db = new masterEntities()) {
                List<History> activeTransactions = db.Histories.Where(x => x.guestID == gid && x.outTime == null).ToList();
                for (int i = 0; i < activeTransactions.Count(); i++) {
                    //Set the check out time to right now
                    activeTransactions.ElementAt(i).outTime = DateTime.Now;
                    db.Entry(activeTransactions.ElementAt(i)).State = System.Data.Entity.EntityState.Modified;
                    //Since Resident and Guest are virtual members, do not add a row for the foreign keys
                    db.Entry(activeTransactions.ElementAt(i).Guest).State = System.Data.Entity.EntityState.Unchanged;
                    db.Entry(activeTransactions.ElementAt(i).Resident).State = System.Data.Entity.EntityState.Unchanged;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("currentGuests");
        }



        //METHODS FOR ADMIN VIEW CONTENT//

        //Method to grab a guest's profile for prohibited admin check
        public ActionResult adminGrabGuest(History t) {
            //Remove leading 0's from accepted form result... for scanner input
            if (t.Guest.guestID != null) {
                string zero = t.Guest.guestID.ToString();
                for (int i = 0; i < zero.Length; i++) {
                    if (zero[i] == '0') continue;
                    else {
                        int temp = 0;
                        int.TryParse(zero.Substring(i), out temp);
                        t.Guest.guestID = temp.ToString();
                        break;
                    }
                }
            }

            using (masterEntities db = new masterEntities()) {
                var guestDetails = db.Guests.Where(x => x.guestID == t.Guest.guestID).FirstOrDefault();

                //Guest Profile does not yet exist
                if (guestDetails == null) {
                    TempData["msg"] = "<script>alert('No guest with this identity exists');</script>";
                    return View("admin", t);
                }

                else {
                    //Set Guest Field form values
                    t.guestID = guestDetails.guestID;
                    t.Guest.firstName = guestDetails.firstName;
                    t.Guest.lastName = guestDetails.lastName;
                    t.Guest.prohibited = guestDetails.prohibited;

                    //Check to see if guest is already prohibited
                    if (t.Guest.prohibited == true) {
                        Session["host"] = "This Guest is already marked as prohibited!";
                        Session["guestIsFound"] = null;
                        return View("admin", t);
                    }

                    Session["guestIsFound"] = "prohibitedCheck";

                    return View("admin", t);
                }
            }
        }


        //Method to mark a guest as prohibited
        public ActionResult markProhibited(History t) {
            using (masterEntities db = new masterEntities()) {
                Guest g = db.Guests.Where(x => x.guestID == t.Guest.guestID).FirstOrDefault();

                //Check if Guest ID did not match
                if (g == null) {
                    TempData["msg"] = "<script>alert('Guest ID did not match. Please search for guest to try again.');</script>";
                    Session["guestIsFound"] = null;
                    return View("admin", t);
                }

                //Otherwise, mark guest as prohibited
                else {
                    g.prohibited = t.Guest.prohibited;
                    g.prohibitedDate = DateTime.Now;
                    db.Entry(g).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //Since we return t, do not risk setting prohibited as true anywhere else in our model
                    t.Guest.prohibited = false;
                    Session["guestIsFound"] = null;
                    Session["host"] = "This guest has successfully been marked as prohibited.";
                    return View("admin", t);
                }
            }
        }


        //Method to export Resident profile history to an Excel file
        public ActionResult ResidentTransactionHistory(History t) {
            List<History> transactions = new List<History>();

            //Remove leading 0's from accepted form result... for scanner input
            string zero = t.Resident.studentID.ToString();
            for (int i = 0; i < zero.Length; i++) {
                if (zero[i] == '0') continue;
                else {
                    int temp = 0;
                    //Parse will always be successful due to form validating numbers only input
                    int.TryParse(zero.Substring(i), out temp);
                    t.Resident.studentID = temp;
                    break;
                }
            }

            using (masterEntities db = new masterEntities()) {
                ZipCard z = db.ZipCards.Where(x => x.zipID == t.Resident.studentID || (x.firstName == t.Resident.firstName && x.lastName == t.Resident.lastName)).FirstOrDefault();
                bool exceptionFlag = false;

                //Model.Resident.ZipCard has been null up to this point
                t.Resident.ZipCard = new ZipCard();
                if (z != null) t.Resident.ZipCard.uaNetID = z.uaNetID;

                //Check for Student ID first
                if (t.Resident.studentID != default(int)) {
                    transactions = db.Histories.Where(x => x.hostID == t.Resident.studentID).ToList();
                    if (transactions == null) exceptionFlag = true;
                }

                //Next check for Resident's first and last name if no Student ID was provided
                else if ((t.Resident.firstName != null && t.Resident.lastName != null) || exceptionFlag == true) {
                    transactions = db.Histories.Where(x => x.Resident.firstName == t.Resident.firstName && x.Resident.lastName == t.Resident.lastName).ToList();
                    if (transactions == null) exceptionFlag = true;
                    else exceptionFlag = false;
                }

                //If no results were found, then resident profile does not exist
                if (exceptionFlag == true) {
                    //Check if resident exists at all, and has simply never signed in a guest
                    if (z == null) {
                        TempData["msg"] = "<script>alert('No resident with this identity exists');</script>";
                        return View("admin", t);
                    }

                    else {
                        TempData["msg"] = "<script>alert('This resident has no associated guest history');</script>";
                        return View("admin", t);
                    }

                }

                //Create new Excel object (dependency: uses third party package EPPlus)
                ExcelPackage ex = new ExcelPackage();
                ExcelWorksheet sheet = ex.Workbook.Worksheets.Add("Report");

                //Set cell data for each column
                sheet.Cells["A1"].Value = "Resident Name";
                sheet.Cells["B1"].Value = "Bedspace";
                sheet.Cells["C1"].Value = "Host ID";
                sheet.Cells["D1"].Value = "UANet ID";
                sheet.Cells["E1"].Value = "Guest Name";
                sheet.Cells["F1"].Value = "Prohibited Guest?";
                sheet.Cells["G1"].Value = "Checked In At";
                sheet.Cells["H1"].Value = "Checked Out At";

                int pos = 2;
                foreach (var row in transactions) {
                    sheet.Cells[string.Format("A{0}", pos)].Value = (row.Resident.firstName + " " + row.Resident.lastName);
                    sheet.Cells[string.Format("B{0}", pos)].Value = row.bedspace;
                    sheet.Cells[string.Format("C{0}", pos)].Value = row.hostID;
                    sheet.Cells[string.Format("D{0}", pos)].Value = row.Resident.ZipCard.uaNetID;
                    sheet.Cells[string.Format("E{0}", pos)].Value = (row.Guest.firstName + " " + row.Guest.lastName);
                    sheet.Cells[string.Format("F{0}", pos)].Value = row.Guest.prohibited.ToString();
                    sheet.Cells[pos, 7].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss AM/PM";
                    sheet.Cells[string.Format("G{0}", pos)].Value = row.inTime;
                    sheet.Cells[pos, 8].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss AM/PM";
                    sheet.Cells[string.Format("H{0}", pos)].Value = row.outTime;
                    pos++;
                }

                //Sets cell width to autofit, but time format is a formula and thus needs to be set manually
                sheet.Cells["A:AZ"].AutoFitColumns();
                sheet.Column(7).Width = 24;
                sheet.Column(8).Width = 24;

                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment: filename=" + "ExcelReport.xlsx");
                Response.BinaryWrite(ex.GetAsByteArray());
                Response.End();

                return View("admin", t);
            }
        }


        //Method to export Guest profile history to an Excel file
        public ActionResult GuestTransactionHistory(History t) {
            List<History> transactions = new List<History>();
            if (t.guestID != null) {
                if (t.guestID.ElementAt(0) == '0') {
                    //Remove leading 0's from accepted form result... for scanner input
                    string zero = t.guestID.ToString();
                    for (int i = 0; i < zero.Length; i++) {
                        if (zero[i] == '0') continue;
                        else {
                            int temp = 0;
                            int.TryParse(zero.Substring(i), out temp);
                            t.guestID = temp.ToString();
                            break;
                        }
                    }
                }
            }

            using (masterEntities db = new masterEntities()) {
                bool exceptionFlag = false;

                //Check for Guest ID first
                if (t.guestID != null) {
                    transactions = db.Histories.Where(x => x.guestID == t.guestID).ToList();
                    if (transactions == null) exceptionFlag = true;
                }

                //Next check for Guest's first and last name if no Guest ID was provided
                else if ((t.Guest.firstName != null && t.Guest.lastName != null && exceptionFlag == false) || exceptionFlag == true) {
                    transactions = db.Histories.Where(x => x.Guest.firstName == t.Guest.firstName && x.Guest.lastName == t.Guest.lastName).ToList();
                    if (transactions == null) exceptionFlag = true;
                    else exceptionFlag = false;
                }

                //If no results were found, then guest profile does not exist
                if (exceptionFlag == true) {
                    TempData["msg"] = "<script>alert('No guest with this identity exists');</script>";
                    return View("admin", t);
                }

                ExcelPackage ex = new ExcelPackage();
                ExcelWorksheet sheet = ex.Workbook.Worksheets.Add("Report");

                //Set cell data for each column
                sheet.Cells["A1"].Value = "Guest Name";
                sheet.Cells["B1"].Value = "Guest ID";
                sheet.Cells["C1"].Value = "ID Type";
                sheet.Cells["D1"].Value = "Building";
                sheet.Cells["E1"].Value = "Bedspace";
                sheet.Cells["F1"].Value = "Host ID";
                sheet.Cells["G1"].Value = "Host UANet ID";
                sheet.Cells["H1"].Value = "Host Name";
                sheet.Cells["I1"].Value = "Checked In At";
                sheet.Cells["J1"].Value = "Checked Out At";

                int pos = 2;
                foreach (var row in transactions) {
                    sheet.Cells[string.Format("A{0}", pos)].Value = (row.Guest.firstName + " " + row.Guest.lastName);
                    sheet.Cells[string.Format("B{0}", pos)].Value = row.guestID;
                    sheet.Cells[string.Format("C{0}", pos)].Value = row.Guest.identityType;
                    sheet.Cells[string.Format("D{0}", pos)].Value = row.building;
                    sheet.Cells[string.Format("E{0}", pos)].Value = row.bedspace;
                    sheet.Cells[string.Format("F{0}", pos)].Value = row.hostID;
                    sheet.Cells[string.Format("G{0}", pos)].Value = row.Resident.ZipCard.uaNetID;
                    sheet.Cells[string.Format("H{0}", pos)].Value = row.Resident.firstName + " " + row.Resident.lastName;
                    sheet.Cells[pos, 9].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss AM/PM";
                    sheet.Cells[string.Format("I{0}", pos)].Value = row.inTime;
                    sheet.Cells[pos, 10].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss AM/PM";
                    sheet.Cells[string.Format("J{0}", pos)].Value = row.outTime;
                    pos++;
                }

                //Set column width
                sheet.Cells["A:AZ"].AutoFitColumns();
                sheet.Column(9).Width = 24;
                sheet.Column(10).Width = 24;

                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment: filename=" + "ExcelReport.xlsx");
                Response.BinaryWrite(ex.GetAsByteArray());
                Response.End();
            }

            return View("admin", t);
        }


    }
}