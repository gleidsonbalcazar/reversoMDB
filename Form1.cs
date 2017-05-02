using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using System.IO; // File.Exists()
using System.Data.OleDb;
using System.Linq;
using System.Text.RegularExpressions;

// OleDbConnection, OleDbDataAdapter, OleDbCommandBuilder

namespace reversoMDB
{
    public partial class Form1 : Form
    {
        string DBPath;

        OleDbConnection conn;
        OleDbDataAdapter adapter;
        DataTable dtMain;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (comboBoxTables.SelectedItem == null) return;

            adapter = new OleDbDataAdapter("SELECT * FROM [" + comboBoxTables.SelectedItem.ToString() + "]", conn);
            
            new OleDbCommandBuilder(adapter);

            dtMain = new DataTable();
            adapter.Fill(dtMain);
            dtMain.Columns["id"].ReadOnly = true; // deprecate id field edit to prevent exceptions
            dataGridView1.DataSource = dtMain;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (adapter == null) return;

            adapter.Update(dtMain);
        }

        // show tooltip (not intrusive MessageBox) when user trying to input letters into INT column cell
        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (dtMain.Columns[e.ColumnIndex].DataType == typeof(Int64) ||
                dtMain.Columns[e.ColumnIndex].DataType == typeof(Int32) ||
                dtMain.Columns[e.ColumnIndex].DataType == typeof(Int16))
            {
                Rectangle rectColumn;
                rectColumn = dataGridView1.GetColumnDisplayRectangle(e.ColumnIndex, false);

                Rectangle rectRow;
                rectRow = dataGridView1.GetRowDisplayRectangle(e.RowIndex, false);

                toolTip1.ToolTipTitle = "This field is for numbers only.";
                toolTip1.Show(" ",
                          dataGridView1,
                          rectColumn.Left, rectRow.Top + rectRow.Height);
            }
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            toolTip1.Hide(dataGridView1);
        }

      
        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) 
            {
                txtArquivo.Text = openFileDialog1.FileName;
            }
        }

        private string randomize()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[8];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);
            return finalString;
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int count = 0;
            string[] arquivoFilenameName;
            string path = @"Arquivos\";
            string arquivo = openFileDialog1.FileNames.FirstOrDefault();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var ext = Regex.Match(arquivo, @"(.{3})\s*$");
            string arquivoNovo = randomize() + '.' + ext;
            File.Copy(arquivo, path + arquivoNovo);
            lblOcultoArquivo.Text = arquivoNovo;
            lblMsg.Text = (arquivo + ": Arquivo Baixado.");

            AbrirMDB();
        }

        private void AbrirMDB()
        {
            txtArquivo.Enabled = false;
            button1.Enabled = false;
            DBPath = Application.StartupPath + "\\Arquivos\\" + lblOcultoArquivo.Text;

            // create DB via ADOX if not exists
            if (!File.Exists(DBPath))
            {
                ADOX.Catalog cat = new ADOX.Catalog();
                cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + DBPath);
                cat = null;
            }

            // connect to DB
            conn = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + DBPath);
            conn.Open();

            // create table "Table_1" if not exists
            // DO NOT USE SPACES IN TABLE AND COLUMNS NAMES TO PREVENT TROUBLES WITH SAVING, USE _
            // OLEDBCOMMANDBUILDER DON'T SUPPORT COLUMNS NAMES WITH SPACES
            try
            {
                using (OleDbCommand cmd = new OleDbCommand("CREATE TABLE [Table_1] ([id] COUNTER PRIMARY KEY, [text_column] MEMO, [int_column] INT);", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { if (ex != null) ex = null; }

            // get all tables from DB
            using (DataTable dt = conn.GetSchema("Tables"))
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i].ItemArray[dt.Columns.IndexOf("TABLE_TYPE")].ToString() == "TABLE")
                    {
                        comboBoxTables.Items.Add(dt.Rows[i].ItemArray[dt.Columns.IndexOf("TABLE_NAME")].ToString());
                    }
                }
            }
        }
    }
}
