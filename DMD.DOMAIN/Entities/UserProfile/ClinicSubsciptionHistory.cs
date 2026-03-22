using DMD.DOMAIN.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.DOMAIN.Entities.UserProfile
{
    public class ClinicSubsciptionHistory : BaseEntity<int>
    {
        public int ClinicProfileId { get; set; }
        public DateTime PaymentDate { get; set; }
        public SubscriptionType Subsciption { get; set; }
        public double TotalAmount { get; set;  }
    }
}
