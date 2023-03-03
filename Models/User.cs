using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication3.Models
{
    public class User
    {

        public User() { }
        public int id { get; set; }

        public String name { get; set; }

        public DateTime created_at { get; set; }
    }
}