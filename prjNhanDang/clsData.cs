using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace prjNhanDang
{
    class clsData
    {
        string strConnection;
        OleDbConnection cnn;
        public clsData()
        {
            strConnection = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=userdata.mdb";
            cnn = new OleDbConnection(strConnection);
        }

        public void OpenConnect()
        {
            if (cnn.State == ConnectionState.Closed)
                cnn.Open();
        }

        public void CloseConnect()
        {
            cnn.Close();
        }

        public DataSet GetTable(string tblName)
        {
            DataSet ds = new DataSet();
            OleDbDataAdapter da;
            StringBuilder sql = new StringBuilder();

            sql.Append(" SELECT * FROM " + tblName);
            this.OpenConnect();
            da = new OleDbDataAdapter(sql.ToString(), cnn);
            da.Fill(ds);
            this.CloseConnect();
            return ds;
        }
        public int ExculSQL(string sql)
        {
            int res = 0;
            OleDbCommand cm;
            if (cnn.State==ConnectionState.Closed)
                this.OpenConnect();
            cm = new OleDbCommand(sql, cnn);
            res = cm.ExecuteNonQuery();
            this.CloseConnect();
            return res;
        }
    }
}
