using System;
using System.Collections.Generic;
using System.Text;

namespace RegPlaywright.Model
{
    class Info
    {
        private string uid;
        private string pass;

        private string ua;
        private string ten;
        private string ho;
        private string sdt;
        private string birth_day;
        private string birth_month;
        private string birth_year;
        private string cookiemoi;
        private string cookie;
        private string status;
        private string ip;
        private string cookiedatr;
        private string cookieFr;
        private DateTime datecreate;

        public string Ua { get => ua; set => ua = value; }
        public string Ten { get => ten; set => ten = value; }
        public string Ho { get => ho; set => ho = value; }
        public string Sdt { get => sdt; set => sdt = value; }
        public string Cookie { get => cookie; set => cookie = value; }
        public string Uid { get => uid; set => uid = value; }
        public string Pass { get => pass; set => pass = value; }
        public string Cookiemoi { get => cookiemoi; set => cookiemoi = value; }
        public string Status { get => status; set => status = value; }
        public string Ip { get => ip; set => ip = value; }
        public string Birth_day { get => birth_day; set => birth_day = value; }
        public string Birth_month { get => birth_month; set => birth_month = value; }
        public string Birth_year { get => birth_year; set => birth_year = value; }
        public DateTime Datecreate { get => datecreate; set => datecreate = value; }
        public string CookieFr { get => cookieFr; set => cookieFr = value; }
        public string Cookiedatr { get => cookiedatr; set => cookiedatr = value; }
    }
}
