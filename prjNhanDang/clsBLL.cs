using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace prjNhanDang
{
    class clsBLL
    {
        clsData obj;
        public clsBLL()
        {
            obj = new clsData();
        }

        public DataTable GetBasicData()
        {
            DataTable dt;
            dt = obj.GetTable("basicdata").Tables[0];
            return dt;
        }
        
        public DataTable GetBasicData(string date)
        {
            DataTable dtSACH, dttemp;
            DataView dv;

            dtSACH = obj.GetTable("basicdata").Tables[0];
            dv = dtSACH.DefaultView;
            dv.RowFilter = " DAT = '" + date + "'";

            dttemp = dv.ToTable();
            return dttemp;

        }
        
        public int InsertBasicData( string num_plate,string date, string time)
        {
            string sql = "Insert into basicdata(NUMBERPLATE,DAT,TIM) values('" + num_plate + "','" + date + "','" + time + "')";
            int t;
            t = obj.ExculSQL(sql);
            return t;
        }
    }
}
