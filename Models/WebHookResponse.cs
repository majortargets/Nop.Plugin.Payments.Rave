using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.Rave.Models
{
    class WebHookResponse
    {
        public int id { get; set; }
        public string txRef { get; set; }
        public string flwRef { get; set; }
        public string orderRef { get; set; }
        public string paymentPlan { get; set; }
        public string createdAt { get; set; }
        public string amount { get; set; }
        public string charged_amount { get; set; }
        public string status { get; set; }
        public string IP { get; set; }
        public string currency { get; set; }
        public Customer customer { get; set; }
        public Entity entity { get; set; }
    }

        public class Customer
        {
            public int id { get; set; }
            public string phone { get; set; }
            public string fullName { get; set; }
            public string customertoken { get; set; }
            public string email { get; set; }
            public string createdAt { get; set; }
            public string updatedAt { get; set; }
            public string deletedAt { get; set; }
            public int AccountId { get; set; }
        }

    public class Entity
    {
        public string card6 { get; set; }
        public string card_last4 { get; set; }
    }

      
    }

