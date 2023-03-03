using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace WebApplication3.Models
{
    public class ChatContext : DbContext
    {
        public ChatContext() : base("jdbc") { }
        public static ChatContext Create() { return new ChatContext(); }

        public DbSet<User> Users { get; set; }
        public DbSet<Conversation> Conversations { get; set; }

    }
}