using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Globalization;
using Gizmox.Controls;
using JDataEngine;
using JurisAuthenticator;
using JurisUtilityBase.Properties;
using System.Data.OleDb;

namespace JurisUtilityBase
{
    public partial class UtilityBaseMain : Form
    {
        #region Private  members

        private JurisUtility _jurisUtility;

        #endregion

        #region Public properties

        public string CompanyCode { get; set; }

        public string JurisDbName { get; set; }

        public string JBillsDbName { get; set; }

        public int FldClient { get; set; }

        public int FldMatter { get; set; }

        public string billAtty = "";

        public string fromPracticeClass = "";
        public string toPracticeClass = "";

        private string clients = "";
        private string matters = "";

        #endregion

        #region Constructor

        public UtilityBaseMain()
        {
            InitializeComponent();
            _jurisUtility = new JurisUtility();
        }

        #endregion

        #region Public methods

        public void LoadCompanies()
        {
            var companies = _jurisUtility.Companies.Cast<object>().Cast<Instance>().ToList();
//            listBoxCompanies.SelectedIndexChanged -= listBoxCompanies_SelectedIndexChanged;
            listBoxCompanies.ValueMember = "Code";
            listBoxCompanies.DisplayMember = "Key";
            listBoxCompanies.DataSource = companies;
//            listBoxCompanies.SelectedIndexChanged += listBoxCompanies_SelectedIndexChanged;
            var defaultCompany = companies.FirstOrDefault(c => c.Default == Instance.JurisDefaultCompany.jdcJuris);
            if (companies.Count > 0)
            {
                listBoxCompanies.SelectedItem = defaultCompany ?? companies[0];
            }
        }

        #endregion

        #region MainForm events

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void listBoxCompanies_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_jurisUtility.DbOpen)
            {
                _jurisUtility.CloseDatabase();
            }
            CompanyCode = "Company" + listBoxCompanies.SelectedValue;
            _jurisUtility.SetInstance(CompanyCode);
            JurisDbName = _jurisUtility.Company.DatabaseName;
            JBillsDbName = "JBills" + _jurisUtility.Company.Code;
            _jurisUtility.OpenDatabase();
            if (_jurisUtility.DbOpen)
            {
                ///GetFieldLengths();
            }

            string TkprIndex;
            cbFrom.ClearItems();
            string SQLTkpr = "select prctclscode + ' ' + prctclsdesc as practiceclass from PracticeClass";
            DataSet myRSTkpr = _jurisUtility.RecordsetFromSQL(SQLTkpr);

            if (myRSTkpr.Tables[0].Rows.Count == 0)
                cbFrom.SelectedIndex = 0;
            else
            {
                foreach (DataTable table in myRSTkpr.Tables)
                {

                    foreach (DataRow dr in table.Rows)
                    {
                        TkprIndex = dr["practiceclass"].ToString();
                        cbFrom.Items.Add(TkprIndex);
                    }
                }

            }

            string TkprIndex2;
            cbTo.ClearItems();
            string SQLTkpr2 = "select prctclscode + ' ' + prctclsdesc as practiceclass from PracticeClass";
            DataSet myRSTkpr2 = _jurisUtility.RecordsetFromSQL(SQLTkpr2);


            if (myRSTkpr2.Tables[0].Rows.Count == 0)
                cbTo.SelectedIndex = 0;
            else
            {
                foreach (DataTable table in myRSTkpr2.Tables)
                {

                    foreach (DataRow dr in table.Rows)
                    {
                        TkprIndex2 = dr["practiceclass"].ToString();
                        cbTo.Items.Add(TkprIndex2);
                    }
                }

            }

            string TkprIndex3;
            cbBillAtty.ClearItems();
            string SQLTkpr3 = "select cast(EmpSysNbr as varchar) + '     ' + empinitials + case when len(empinitials)=1 then '     ' when len(empinitials)=2 then '     ' when len(empinitials)=3 then '   ' else '  ' end +  empname as employee from employee where empvalidastkpr='Y' and (empsysnbr in (select billtobillingatty from billto) or empsysnbr in (select clibillingatty from client) ) order by empinitials";
            DataSet myRSTkpr3 = _jurisUtility.RecordsetFromSQL(SQLTkpr3);

            if (myRSTkpr3.Tables[0].Rows.Count == 0)
                cbBillAtty.SelectedIndex = 0;
            else
            {

                    foreach (DataRow dr in myRSTkpr3.Tables[0].Rows)
                    {
                        TkprIndex3 = dr["employee"].ToString();
                        cbBillAtty.Items.Add(TkprIndex3);
                    }

            }

        }



        #endregion

        #region Private methods

        private void DoDaFix()
        {
            // Enter your SQL code here
            // To run a T-SQL statement with no results, int RecordsAffected = _jurisUtility.ExecuteNonQueryCommand(0, SQL);
            // To get an ADODB.Recordset, ADODB.Recordset myRS = _jurisUtility.RecordsetFromSQL(SQL);


                string BT="";
                string CM2="";
                string deletion = "";

                if (rbCM.Checked)
                {  CM2 = " Clients and Matters will be changed from "; }
                if (rbClient.Checked)
                {  CM2 = "Clients will be changed from "; }
                if (rbMatter.Checked)
                {  CM2 = "Matters will be changed from "; }
                if (checkBoxDeleteAfter.Checked)
                    deletion = ". " + fromPracticeClass + " will be deleted.";

                string RST = BT.ToString() + "" + CM2.ToString() + "" + fromPracticeClass + " to " + toPracticeClass + deletion;

                var rsBoth = MessageBox.Show(RST.ToString() + ". Do you wish to continue?", "Practice Class Reassignment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (rsBoth==DialogResult.No)
                { MessageBox.Show("Please check selections and try again."); }
                else
                { 
                    //remember to get list of items to change because we only want them in the post report, not all record associated with the new practice class


                        if (rbCM.Checked)
                        {

                            UpdateClientAndMatter();

                            Cursor.Current = Cursors.Default;
                            Application.DoEvents();
                            toolStripStatusLabel.Text = "Practice Classes Updated for matters and clients: Old: " + fromPracticeClass + "; New: " + toPracticeClass;
                            statusStrip.Refresh();

                        }

                        if (rbClient.Checked)
                        {

                            UpdateClient();

                            Cursor.Current = Cursors.Default;
                            Application.DoEvents();
                            toolStripStatusLabel.Text = "Practice Classes Updated for clients: Old: " + fromPracticeClass + "; New: " + toPracticeClass;
                            statusStrip.Refresh();

                        }

                        if (rbMatter.Checked)
                        {
                            UpdateMatter();


                            Cursor.Current = Cursors.Default;
                            Application.DoEvents();
                            toolStripStatusLabel.Text = "Practice Classes Updated for matters: Old: " + fromPracticeClass + "; New: " + toPracticeClass;
                            statusStrip.Refresh();

                        }

                        if (checkBoxDeleteAfter.Checked)
                            deleteSelectedPracticeClass();

                    
                    UpdateStatus("Practice Class Update Complete", 5, 5);
                    string LogNote = "Practice class assignment - " + fromPracticeClass + " to " + toPracticeClass;
                    WriteLog(LogNote.ToString());
                    //outputs the post report
                    var result = MessageBox.Show("Process Complete! Would you like to see the post log?", "Log confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        string reportSQL = getPostReportSQL();

                        DataSet ds = _jurisUtility.RecordsetFromSQL(reportSQL);

                        ReportDisplay rpds = new ReportDisplay(ds);
                        rpds.Show();
                    }
                    matters = "";
                    clients = "";
                }


                


            }

        
        private void UpdateClient()
        {  
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            toolStripStatusLabel.Text = "Updating Client Practice Classes....";
            statusStrip.Refresh();
            UpdateStatus("Client Practice Classes", 1, 1);
            string SQL = getReportSQL();
            DataSet cl = _jurisUtility.RecordsetFromSQL(SQL);
            for (int i = 0; i < cl.Tables[0].Rows.Count; i++)
                clients = clients + "'" + cl.Tables[0].Rows[i][0].ToString() + "',"; //cliid
            clients = clients.TrimEnd(',');
            string CC3 = "update client set CliPracticeClass = '" + toPracticeClass + "' where CliPracticeClass = '" + fromPracticeClass + "'";
            if ((checkBox1.Checked) )
                CC3 = CC3 + " and CliBillingAtty = " + billAtty;
            _jurisUtility.ExecuteNonQueryCommand(0, CC3);

           }

        private void UpdateMatter()
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            toolStripStatusLabel.Text = "Updating Matter Practice Classes....";
            statusStrip.Refresh();
            UpdateStatus("Matter Practice Classes", 1, 1);
            string SQL = getReportSQL();
            DataSet ma = _jurisUtility.RecordsetFromSQL(SQL);
            for (int i = 0; i < ma.Tables[0].Rows.Count; i++)
                matters = matters + "'" + ma.Tables[0].Rows[i][0].ToString() + "',"; //matid
            matters = matters.TrimEnd(',');
            string CC3 = "update m set MatPracticeClass = '" + toPracticeClass + "' from matter as m inner join billto as b on m.matbillto=b.billtosysnbr where m.matPracticeClass = '" + fromPracticeClass + "'";
            if ((checkBox1.Checked))
                CC3 = CC3 + " and b.BillToBillingAtty = " + billAtty;


            _jurisUtility.ExecuteNonQueryCommand(0, CC3);

        }

        private void UpdateClientAndMatter()
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            toolStripStatusLabel.Text = "Updating Matter and Client Practice Classes....";
            statusStrip.Refresh();
            UpdateStatus("Matter and Client Practice Classes", 1, 1);
            string SQL = getReportSQL();
            DataSet ma = _jurisUtility.RecordsetFromSQL(SQL);
            for (int i = 0; i < ma.Tables[0].Rows.Count; i++)
                matters = matters + ma.Tables[0].Rows[i][4].ToString() + ","; //matsysnbr
            matters = matters.TrimEnd(',');


            string CC3 = "update m set MatPracticeClass = '" + toPracticeClass + "' from matter as m inner join billto as b on m.matbillto=b.billtosysnbr where m.matPracticeClass = '" + fromPracticeClass + "'";
            if ((checkBox1.Checked))
                CC3 = CC3 + " and b.BillToBillingAtty = " + billAtty;
            _jurisUtility.ExecuteNonQueryCommand(0, CC3);
            CC3 = "update client set CliPracticeClass = '" + toPracticeClass + "' where CliPracticeClass = '" + fromPracticeClass + "'";
            if ((checkBox1.Checked) )
                CC3 = CC3 + " and CliBillingAtty = " + billAtty;
            _jurisUtility.ExecuteNonQueryCommand(0, CC3);
        }

        private void deleteSelectedPracticeClass()
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            toolStripStatusLabel.Text = "Deleting PracticeClass: " + fromPracticeClass + "....";
            statusStrip.Refresh();
            UpdateStatus("Deleting PracticeClass", 1, 1);
            DataSet ds = _jurisUtility.RecordsetFromSQL("Select count(*) from practclassglacct where pgapractclass='" + fromPracticeClass + "'");
            if (ds.Tables[0].Rows[0][0].ToString().Equals("0"))
            {
                string sql = "select count(clicode) + count( matcode) from client" +
                                        " inner join practiceclass on clipracticeclass = prctclscode" +
                                        " inner join matter on matclinbr = clisysnbr" +
                                        " where clipracticeclass='" + fromPracticeClass + "' or  matpracticeclass='" + fromPracticeClass + "'";
                DataSet ds1 = _jurisUtility.RecordsetFromSQL(sql);
                if (ds1.Tables[0].Rows[0][0].ToString().Equals("0"))
                {
                    string CC3 = "Delete from practiceclass where practiceclass.prctclscode= '" + fromPracticeClass + "'";
                    _jurisUtility.ExecuteNonQueryCommand(0, CC3);
                    CC3 = "Delete from documenttree where dtparentid=14 and dtkeyT = '" + fromPracticeClass + "'";
                    _jurisUtility.ExecuteNonQueryCommand(0, CC3);

                    //since they deleted a practice class, we have to refresh the list of practice classes
                    string TkprIndex;
                    cbFrom.ClearItems();
                    string SQLTkpr = "select prctclscode + ' ' + prctclsdesc as practiceclass from PracticeClass";
                    DataSet myRSTkpr = _jurisUtility.RecordsetFromSQL(SQLTkpr);

                    if (myRSTkpr.Tables[0].Rows.Count == 0)
                        cbFrom.SelectedIndex = 0;
                    else
                    {
                        foreach (DataTable table in myRSTkpr.Tables)
                        {

                            foreach (DataRow dr in table.Rows)
                            {
                                TkprIndex = dr["practiceclass"].ToString();
                                cbFrom.Items.Add(TkprIndex);
                            }
                        }

                    }

                    cbFrom.SelectedIndex = 0;
                    cbFrom.Refresh();

                    string TkprIndex2;
                    cbTo.ClearItems();
                    string SQLTkpr2 = "select prctclscode + ' ' + prctclsdesc as practiceclass from PracticeClass";
                    DataSet myRSTkpr2 = _jurisUtility.RecordsetFromSQL(SQLTkpr2);


                    if (myRSTkpr2.Tables[0].Rows.Count == 0)
                        cbTo.SelectedIndex = 0;
                    else
                    {
                        foreach (DataTable table in myRSTkpr2.Tables)
                        {

                            foreach (DataRow dr in table.Rows)
                            {
                                TkprIndex2 = dr["practiceclass"].ToString();
                                cbTo.Items.Add(TkprIndex2);
                            }
                        }

                    }

                    cbTo.SelectedIndex = 0;
                    cbTo.Refresh();


                }
                else
                    MessageBox.Show("PracticeClass " + fromPracticeClass + " cannot be deleted because some client/matters are still assigned to it." + "\r\n" + "This is common when using the Billing Timekeeper option and should not cause alarm.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
                MessageBox.Show("PracticeClass " + fromPracticeClass + " cannot be deleted because of Practice Class GL Accounting.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

        }

        private bool VerifyFirmName()
        {
            //    Dim SQL     As String
            //    Dim rsDB    As ADODB.Recordset
            //
            //    SQL = "SELECT CASE WHEN SpTxtValue LIKE '%firm name%' THEN 'Y' ELSE 'N' END AS Firm FROM SysParam WHERE SpName = 'FirmName'"
            //    Cmd.CommandText = SQL
            //    Set rsDB = Cmd.Execute
            //
            //    If rsDB!Firm = "Y" Then
            return true;
            //    Else
            //        VerifyFirmName = False
            //    End If

        }

        private bool FieldExistsInRS(DataSet ds, string fieldName)
        {

            foreach (DataColumn column in ds.Tables[0].Columns)
            {
                if (column.ColumnName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool IsDate(String date)
        {
            try
            {
                DateTime dt = DateTime.Parse(date);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static bool IsNumeric(object Expression)
        {
            double retNum;

            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
            return isNum; 
        }

        private void WriteLog(string comment)
        {
            string sql = "Insert Into UtilityLog(ULTimeStamp,ULWkStaUser,ULComment) Values(convert(datetime,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'),cast('" +  GetComputerAndUser() + "' as varchar(100))," + "cast('" + comment.ToString() + "' as varchar(8000)))";
            _jurisUtility.ExecuteNonQueryCommand(0, sql);
        }

        private string GetComputerAndUser()
        {
            var computerName = Environment.MachineName;
            var windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var userName = (windowsIdentity != null) ? windowsIdentity.Name : "Unknown";
            return computerName + "/" + userName;
        }

        /// <summary>
        /// Update status bar (text to display and step number of total completed)
        /// </summary>
        /// <param name="status">status text to display</param>
        /// <param name="step">steps completed</param>
        /// <param name="steps">total steps to be done</param>
        private void UpdateStatus(string status, long step, long steps)
        {
            labelCurrentStatus.Text = status;

            if (steps == 0)
            {
                progressBar.Value = 0;
                labelPercentComplete.Text = string.Empty;
            }
            else
            {
                double pctLong = Math.Round(((double)step/steps)*100.0);
                int percentage = (int)Math.Round(pctLong, 0);
                if ((percentage < 0) || (percentage > 100))
                {
                    progressBar.Value = 0;
                    labelPercentComplete.Text = string.Empty;
                }
                else
                {
                    progressBar.Value = percentage;
                    labelPercentComplete.Text = string.Format("{0} percent complete", percentage);
                }
            }
        }
        private void DeleteLog()
        {
            string AppDir = Path.GetDirectoryName(Application.ExecutablePath);
            string filePathName = Path.Combine(AppDir, "VoucherImportLog.txt");
            if (File.Exists(filePathName + ".ark5"))
            {
                File.Delete(filePathName + ".ark5");
            }
            if (File.Exists(filePathName + ".ark4"))
            {
                File.Copy(filePathName + ".ark4", filePathName + ".ark5");
                File.Delete(filePathName + ".ark4");
            }
            if (File.Exists(filePathName + ".ark3"))
            {
                File.Copy(filePathName + ".ark3", filePathName + ".ark4");
                File.Delete(filePathName + ".ark3");
            }
            if (File.Exists(filePathName + ".ark2"))
            {
                File.Copy(filePathName + ".ark2", filePathName + ".ark3");
                File.Delete(filePathName + ".ark2");
            }
            if (File.Exists(filePathName + ".ark1"))
            {
                File.Copy(filePathName + ".ark1", filePathName + ".ark2");
                File.Delete(filePathName + ".ark1");
            }
            if (File.Exists(filePathName ))
            {
                File.Copy(filePathName, filePathName + ".ark1");
                File.Delete(filePathName);
            }

        }

            

        private void LogFile(string LogLine)
        {
            string AppDir = Path.GetDirectoryName(Application.ExecutablePath);
            string filePathName = Path.Combine(AppDir, "VoucherImportLog.txt");
            using (StreamWriter sw = File.AppendText(filePathName))
            {
                sw.WriteLine(LogLine);
            }	
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            DoDaFix();
        }

        private void btExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cbFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            fromPracticeClass = cbFrom.Text;
            fromPracticeClass = fromPracticeClass.Split(' ')[0];
        }

        private void cbTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            toPracticeClass = cbTo.Text;
            toPracticeClass = toPracticeClass.Split(' ')[0];
        }

        private void buttonReport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(toPracticeClass) || string.IsNullOrEmpty(fromPracticeClass))
                MessageBox.Show("Please select from both Practice Class drop downs", "Selection Error");
            else
            {
                //generates output of the report for before and after the change will be made to client
                string SQLTkpr = getReportSQL();

                DataSet ds = _jurisUtility.RecordsetFromSQL(SQLTkpr);

                ReportDisplay rpds = new ReportDisplay(ds);
                rpds.Show();

            }
        }

        private string getReportSQL()
        {
            string reportSQL = "";
            //if client
            if (rbClient.Checked)
            {
                reportSQL = "select clisysnbr as ClientID, clicode as ClientCode, clireportingname as ClientName, empinitials as ClientBillingAtty, clipracticeclass as OldPracticeClass, '" + toPracticeClass + "' as NewPracticeClass  from client" +
                        " inner join practiceclass on clipracticeclass = prctclscode" +
                         " inner join employee on empsysnbr=CliBillingAtty" +
                        " where clipracticeclass='" + fromPracticeClass + "'";
                if ((checkBox1.Checked))
                    reportSQL = reportSQL + " and CliBillingAtty = " + billAtty;
            }
            //if matter
            else if (rbMatter.Checked)
            {
                reportSQL = "select matsysnbr as MatterID, matcode as MatterCode, matreportingname as MatterName, matpracticeclass as PracticeClass, empinitials as MatterBillAtty, '" + toPracticeClass + "' as NewPracticeClass from matter" +
                        " inner join practiceclass on matpracticeclass = prctclscode" +
                        " inner join billto on MatBillTo=billtosysnbr" +
                        " inner join employee on empsysnbr=billto.billtobillingatty" +
                        " where matpracticeclass='" + fromPracticeClass + "'";
                if ((checkBox1.Checked))
                    reportSQL = reportSQL + " and BillToBillingAtty = " + billAtty;
            }
            //if both
            else if (rbCM.Checked)
            {
                reportSQL = "select clicode as ClientCode, clireportingname as ClientName, matcode as MatterCode, matreportingname as MatterName, matsysnbr as MatterID, matBill.empinitials as MatterBillAtty, cliBill.empinitials as ClientBillAtty, clipracticeclass as OldClientPracticeClass,matpracticeclass as OldMatterPracticeClass, '" + toPracticeClass + "' as NewPracticeClass from client" +
                                        " inner join practiceclass on clipracticeclass = prctclscode" +
                                        " inner join matter on matclinbr = clisysnbr" +
                                        " inner join billto on matbillto=billtosysnbr" +
                                        " inner join employee matBill on matBill.empsysnbr=billto.billtobillingatty" +
                                        " inner join employee cliBill on cliBill.empsysnbr=client.clibillingatty" +
                                        " where clipracticeclass='" + fromPracticeClass + "' or  matpracticeclass='" + fromPracticeClass + "'";
                if ((checkBox1.Checked))
                    reportSQL = reportSQL + " and (BillToBillingAtty = " + billAtty + " or CliBillingAtty = " + billAtty + ")";
            }
            return reportSQL;
        }

        private string getPostReportSQL()
        {
            string reportSQL = "";
            //if client
            if (rbClient.Checked)
            {
                reportSQL = "select clicode as ClientCode, clireportingname as ClientName, clipracticeclass as PracticeClass, empinitials as ClientBillingAtty from client" +
                        " inner join practiceclass on clipracticeclass = prctclscode" +
                         " inner join employee on empsysnbr=CliBillingAtty" +
                        " where clisysnbr in (" + clients + ")";
                if ((checkBox1.Checked))
                    reportSQL = reportSQL + " and CliBillingAtty = " + billAtty;
            }
            //if matter
            else if (rbMatter.Checked)
            {
                reportSQL = "select matcode as MatterCode, matreportingname as MatterName, matpracticeclass as PracticeClass, empinitials as MatterBillAtty from matter" +
                        " inner join practiceclass on matpracticeclass = prctclscode" +
                        " inner join billto on MatBillTo=billtosysnbr" +
                        " inner join employee on empsysnbr=billto.billtobillingatty" +
                        " where matsysnbr in (" + matters + ")";
                if ((checkBox1.Checked))
                    reportSQL = reportSQL + " and BillToBillingAtty = " + billAtty;
            }
            //if both
            else if (rbCM.Checked)
            {
                reportSQL = "select clicode as ClientCode, clireportingname as ClientName, matcode as MatterCode, matreportingname as MatterName, clipracticeclass as ClientPracticeClass,matpracticeclass as MatterPracticeClass,matBill.empinitials as MatterBillAtty, cliBill.empinitials as ClientBillAtty from client" +
                                        " inner join practiceclass on clipracticeclass = prctclscode" +
                                        " inner join matter on matclinbr = clisysnbr" +
                                        " inner join billto on matbillto=billtosysnbr" +
                                        " inner join employee matBill on matBill.empsysnbr=billto.billtobillingatty" +
                                        " inner join employee cliBill on cliBill.empsysnbr=client.clibillingatty" +
                                        " where matsysnbr in (" + matters + ")";
                if ((checkBox1.Checked))
                    reportSQL = reportSQL + " and (BillToBillingAtty = " + billAtty + " or CliBillingAtty = " + billAtty + ")";

            }
            return reportSQL;
        }

        private void checkBoxDeleteAfter_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            cbBillAtty.Visible = checkBox1.Checked;

        }

        private void cbBillAtty_SelectedIndexChanged(object sender, EventArgs e)
        {
            billAtty = cbBillAtty.Text.Split(' ')[0];
        }

    }
}
