using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToshibaBinary2DbClassLibrary.Model
{
    internal class ProdDateAndShift
    {
        public DateTime ProdDate { get; set; }
        public string ShiftName { get; set; }

        public static ProdDateAndShift GetShiftInfo(DateTime currentTime)
        {
            ProdDateAndShift info = new ProdDateAndShift();
            TimeSpan time = currentTime.TimeOfDay;

            // Shift A: 07:00:00 to 15:29:59
            if (time >= new TimeSpan(7, 0, 0) && time < new TimeSpan(15, 30, 0))
            {
                info.ShiftName = "A";
                info.ProdDate = currentTime.Date;
            }
            // Shift B: 15:30:00 to 23:59:59
            else if (time >= new TimeSpan(15, 30, 0) && time < new TimeSpan(24, 0, 0))
            {
                info.ShiftName = "B";
                info.ProdDate = currentTime.Date;
            }
            // Shift C: 00:00:00 to 06:59:59
            else
            {
                info.ShiftName = "C";
                info.ProdDate = currentTime.Date.AddDays(-1);
            }

            return info;
        }
    }
}
