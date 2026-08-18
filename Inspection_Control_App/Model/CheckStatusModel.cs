using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inspection_Control_App.Model
{
    public class CheckStatusModel
    {
        public string PONumber { get; set; }
        public string NumInspec { get; set; } // WiP, Đang check, Rảnh
        public string Typename { get; set; }
        public string Roname { get; set; }
        public string Index { get; set; }
        public StatusCheck Status { get; set; } // Đang check, Rảnh
        public DateTime? SavedAt { get; set; }
    }
}
