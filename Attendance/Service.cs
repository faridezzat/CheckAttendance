using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Timers;
using zkemkeeper;

namespace Attendance
{
    class Service
    {
        private readonly Timer timer;
        private int branch;
        private string ipAddress;
        private int port;
        private CZKEM zk;
        public static string baseURL = Properties.Settings.Default.baseURL;

        public Service()
        {
            timer = new Timer(60000) { AutoReset = true };
            timer.Elapsed += Timer_Elapsed;
            branch = Properties.Settings.Default.branch;
            ipAddress = Properties.Settings.Default.ip;
            port = Properties.Settings.Default.port;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            pullData();
            uploadRecords();
        }

        public void start()
        {
            timer.Start();
        }
        public void stop()
        {
            timer.Stop();
        }

        private void log(string error)
        {
            string[] x = new string[] { error};
            File.AppendAllLines(@"log.txt", x);
        }




        public string response()
        {
            string x = string.Empty;
            HttpWebRequest request = WebRequest.Create(baseURL+"get.php") as HttpWebRequest;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
          

         
            using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
            {
                x = reader.ReadToEnd();
            }
            return x;
        }




        public void deserialize(string json)
        {
            try
            {
                var ad = JsonConvert.DeserializeObject<List<AttendanceData>>(json);

                foreach (var item in ad)
                {
                    item.process();
                }
                
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
        }


        private void pullData()
        {
            zk = new CZKEM();
            bool isConnected = zk.Connect_Net(ipAddress, port);
            if (!isConnected)
            {
                log("Unable to Connect " + DateTime.Now);
                return;
            }
            string enrollNO;
            int verifyMode;
            int inoutMode;
            int year;
            int month;
            int DayOfWeek;
            int hour;
            int minute;
            int second;
            int workerCode = 1;
            if (zk.ReadGeneralLogData(1))
            {
                bool canDelete = true;
                while (zk.SSR_GetGeneralLogData(branch, out enrollNO, out verifyMode, out inoutMode, out year, out month, out DayOfWeek, out hour, out minute, out second, ref workerCode))
                {
                    string date = year + "-" + month + "-" + DayOfWeek + " " + hour + ":" + minute + ":" + second;

                    if (!saveRecord(enrollNO, date, inoutMode))
                    {
                        log("Unable to SaveData   " + DateTime.Now);
                        canDelete = false;
                    }
                }
                bool deleteAllowed = Properties.Settings.Default.delete;
                if (canDelete && deleteAllowed)
                {
                    zk.ClearGLog(branch);
                }

            }
            zk.Disconnect();
        }


        private bool uploadRecord(int code, DateTime date, int type)
        {
            bool x = false;
            HttpWebRequest request = HttpWebRequest.Create(baseURL + string.Format("insert.php?code={0}&date={1}&type={2}&branch={3}",code,date.ToString("yyyy-MM-dd HH:mm:ss"),type,branch)) as HttpWebRequest;
            Console.WriteLine(request.RequestUri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
               x = Convert.ToBoolean(sr.ReadToEnd());
            }
            return x;
        }

        private void makeDone(int logid, bool result)
        {
            using (SqlConnection conn = con())
            {
                conn.Open();
                string query = string.Format("update CHECKINOUT set isDone = '{0}' where LOGID = {1}",result,logid);
                using (SqlCommand com = new SqlCommand(query, conn))
                {
                    com.ExecuteNonQuery();
                }

            }

        }

        private void uploadRecords()
        {

            using (SqlConnection conn = con())
            {
                conn.Open();
                string query = "select USERID, CHECKTIME, CHECKTYPE,LOGID from CHECKINOUT where isDone != 1";
                using (SqlCommand com = new SqlCommand(query,conn))
                {
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            makeDone(Convert.ToInt32(reader["LOGID"]), uploadRecord(Convert.ToInt32(reader["USERID"]),Convert.ToDateTime(reader["CHECKTIME"]), Convert.ToInt32(reader["CHECKTYPE"])));
                        }
                    }
                }

            }
        }

        private bool saveRecord(string code, string date, int type)
        {
            try
            {
                using (SqlConnection conn = con())
                {
                    conn.Open();
                    string query = string.Format("insert into CHECKINOUT (USERID, CHECKTIME, CHECKTYPE, SENSORID) values ({0},'{1}','{2}',{3})", code, date, type, branch);
                    log(query);
                    using (SqlCommand com = new SqlCommand(query, conn))
                    {
                        com.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public static SqlConnection con()
        {
            SqlConnection con = new SqlConnection(Properties.Settings.Default.connection);
            return con;
        }
    }
}
