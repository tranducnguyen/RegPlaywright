using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using RegPlaywright.Model;

namespace RegPlaywright.Controller
{
    class DbAction
    {
        private string UserDb = "Filename=RegAccDB.db;connection=shared";
        public void AddUser(List<Info> newList)
        {
            using (LiteDatabase db = new LiteDatabase(UserDb))
            {
                db.Timeout = TimeSpan.FromSeconds(200);
                ILiteCollection<Info> List = db.GetCollection<Info>("User");
                newList.ForEach(item =>
                {
                    List.Insert(item);
                });
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public void CleanUser()
        {
            using (LiteDatabase db = new LiteDatabase(UserDb))
            {
                db.Timeout = TimeSpan.FromSeconds(200);
                ILiteCollection<Info> List = db.GetCollection<Info>("User");
                List.DeleteAll();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public List<Info> GetUser()
        {
            using (LiteDatabase db = new LiteDatabase(UserDb))
            {
                db.Timeout = TimeSpan.FromSeconds(200);
                ILiteCollection<Info> List = db.GetCollection<Info>("User");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return List.FindAll().ToList();
            }
        }
        public void AddPhone(PhoneList Phone)
        {
            if (!string.IsNullOrEmpty(Phone.Phone))
                using (LiteDatabase db = new LiteDatabase(UserDb))
                {
                    db.Timeout = TimeSpan.FromSeconds(200);
                    ILiteCollection<PhoneList> List = db.GetCollection<PhoneList>("Phone");
                    List.Insert(Phone);
                }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public void AddUA(UAList UA)
        {
            if (!string.IsNullOrEmpty(UA.UA))
                using (LiteDatabase db = new LiteDatabase(UserDb))
                {
                    db.Timeout = TimeSpan.FromSeconds(200);
                    ILiteCollection<UAList> List = db.GetCollection<UAList>("UA");
                    if (List.Count() > 0)
                    {
                        var UAOld = List.FindOne(item => item.UA.Contains(UA.UA));
                        if (UAOld != null)
                        {
                            int iCheckpoint = int.Parse(UA.CheckPoint) + int.Parse(UAOld.CheckPoint);
                            int iSuccess = int.Parse(UA.Success) + +int.Parse(UAOld.Success);
                            UAOld.CheckPoint = iCheckpoint.ToString();
                            UAOld.Success = iSuccess.ToString();
                            List.Update(UAOld);
                        }
                        else
                        {
                            List.Insert(UA);
                        }
                    }
                    else
                    {
                        List.Insert(UA);
                    }
                }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public void AddIP(IPInfo IP)
        {
            if (!string.IsNullOrEmpty(IP.IP))
                using (LiteDatabase db = new LiteDatabase(UserDb))
                {
                    db.Timeout = TimeSpan.FromSeconds(200);
                    ILiteCollection<IPInfo> List = db.GetCollection<IPInfo>("IP");
                    if (List.Count() > 0)
                    {
                        var UAOld = List.FindOne(item => item.IP.Contains(IP.IP));
                        if (UAOld != null)
                        {
                            int iCheckpoint = int.Parse(IP.CheckPoint) + int.Parse(UAOld.CheckPoint);
                            int iSuccess = int.Parse(IP.Success) + +int.Parse(UAOld.Success);
                            UAOld.CheckPoint = iCheckpoint.ToString();
                            UAOld.Success = iSuccess.ToString();
                            List.Update(UAOld);
                        }
                        else
                        {
                            List.Insert(IP);
                        }
                    }
                    else
                    {
                        List.Insert(IP);
                    }
                }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public bool CheckPhone(string Phone)
        {
            if (!string.IsNullOrEmpty(Phone))
                using (LiteDatabase db = new LiteDatabase(UserDb))
                {
                    db.Timeout = TimeSpan.FromSeconds(200);
                    ILiteCollection<PhoneList> List = db.GetCollection<PhoneList>("Phone");
                    if (List.Count() > 0)
                    {
                        var check = List.Find(item => item.Phone.Contains(Phone));
                        if (check.FirstOrDefault() != null)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            return true;
                        }
                    }
                }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return false;
        }
        public void CleanId()
        {
            using (LiteDatabase db = new LiteDatabase(UserDb))
            {
                db.Timeout = TimeSpan.FromSeconds(200);
                ILiteCollection<Test> List = db.GetCollection<Test>("Test");

                List.DeleteAll();
            }
        }
    }
}
