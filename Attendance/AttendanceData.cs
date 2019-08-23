using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Attendance
{
    class AttendanceData
    {

        public int ID { get; set; }
        public DateTime Date { get; set; }
        public int Code { get; set; }
        public int branch { get; set; }
        public int type { get; set; }
        public string done { get; set; }





        public void process()
        {
            if (isDataSaved())
            {

               
                HttpWebRequest request = HttpWebRequest.Create(string.Format(Service.baseURL + "update.php?id={0}", ID)) as HttpWebRequest;
                request.GetResponse();
              
            }

        }



        public bool isDataSaved()
        {
            try
            {
                using (SqlConnection conn = Service.con())
                {
                    conn.Open();
                    string query = string.Format("insert into CHECKINOUT (USERID, CHECKTIME, CHECKTYPE, VERIFYCODE, SENSORID) values ({0},'{1}',{2},0,{3})", Code,Date,type,branch);

                    using (SqlCommand com = new SqlCommand(query, conn))
                    {
                        com.ExecuteNonQuery();
                    }
                   
                }
            }
            catch (Exception e)
            {

                return false;
            }
            return true;
        }
    }
}
