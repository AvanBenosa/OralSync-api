
using DMD.DOMAIN.Enums;

namespace DMD.DOMAIN.Entities.Buildups
{
    public class FormTemplate : BaseEntity<int>
    {
        public int ClinicProfileId { get; set; }
        public string TemplateName { get; set;  }
        public string TemplateContent { get; set; }
        public TemplateType Type { get; set;  }
        public DateTime? Date { get;set;  }

    }
}
