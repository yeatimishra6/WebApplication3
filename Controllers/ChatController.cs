﻿using Microsoft.Azure.KeyVault.Models;
using PusherServer;
using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication3.Models;
using Xamarin.Essentials;

namespace WebApplication3.Controllers
{
    public class ChatController : Controller
    {
        private Pusher pusher;
        public ChatController()
        {
            var options = new PusherOptions();
            options.Cluster = "PUSHER_APP_CLUSTER";
            pusher = new Pusher(
              "PUSHER_APP_ID",
              "PUSHER_APP_KEY",
              "PUSHER_APP_SECRET", options);
        }
        public ActionResult Index()
        {
            if (Session["user"] == null)
            {
                return Redirect("/");
            }
            var currentUser = (Models.User)Session["user"];
            using (var db = new Models.ChatContext())
            {
                ViewBag.allUsers = db.Users.Where(u => u.name != currentUser.name)
                                 .ToList();
            }
            ViewBag.currentUser = currentUser;
            return View();
        }
        public JsonResult ConversationWithContact(int contact)
        {
            if (Session["user"] == null)
            {
                return Json(new { status = "error", message = "User is not logged in" });
            }
            var currentUser = (Models.User)Session["user"];
            var conversations = new List<Models.Conversation>();
            using (var db = new Models.ChatContext())
            {
                conversations = db.Conversations.
                                  Where(c => (c.receiver_id == currentUser.id && c.sender_id == contact) || (c.receiver_id == contact && c.sender_id == currentUser.id))
                                  .OrderBy(c => c.created_at)
                                  .ToList();
            }
            return Json(new { status = "success", data = conversations }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult SendMessage()
        {
            if (Session["user"] == null)
            {
                return Json(new { status = "error", message = "User is not logged in" });
            }
            var currentUser = (User)Session["user"];
            var contact = Convert.ToInt32(Request.Form["contact"]);
            string socket_id = Request.Form["socket_id"];
            Conversation convo = new Conversation
            {
                sender_id = currentUser.id,
                message = Request.Form["message"],
                receiver_id = contact
            };
            using (var db = new Models.ChatContext())
            {
                db.Conversations.Add(convo);
                db.SaveChanges();
            }
            var conversationChannel = getConvoChannel(currentUser.id, contact);
            pusher.TriggerAsync(
              conversationChannel,
              "new_message",
              convo,
              new TriggerOptions() { SocketId = socket_id });
            return Json(convo);
        }
        [HttpPost]
        public JsonResult MessageDelivered(int message_id)
        {
            Conversation convo = null;
            using (var db = new Models.ChatContext())
            {
                convo = db.Conversations.FirstOrDefault(c => c.id == message_id);
                if (convo != null)
                {
                    convo.status = Conversation.messageStatus.Delivered;
                    db.Entry(convo).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

            }
            string socket_id = Request.Form["socket_id"];
            var conversationChannel = getConvoChannel(convo.sender_id, convo.receiver_id);
            pusher.TriggerAsync(
              conversationChannel,
              "message_delivered",
              convo,
              new TriggerOptions() { SocketId = socket_id });
            return Json(convo);
        }
        private String getConvoChannel(int user_id, int contact_id)
        {
            if (user_id > contact_id)
            {
                return "private-chat-" + contact_id + "-" + user_id;
            }
            return "private-chat-" + user_id + "-" + contact_id;
        }
    }

}