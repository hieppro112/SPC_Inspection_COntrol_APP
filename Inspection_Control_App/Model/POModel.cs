using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inspection_Control_App.Model
{
    public class POModel
    {
        public DateTime Time { get; set; }
        public string PONumber { get; set; }
        public int Lot { get; set; }
        public string ShipTo { get; set; }
        public string ShipBy { get; set; }
        public string Type { get; set; }
        public int Qty { get; set; }
        public string ExportDate { get; set; }
    }
}
