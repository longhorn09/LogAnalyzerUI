using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;

namespace LogAnalyzerUI
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
    }


    private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
    {

    }
    //open text file in Notepad
    //http://stackoverflow.com/questions/4055266/open-a-file-with-notepad-in-c-sharp

    private void button1_Click(object sender, EventArgs e)
    {
      CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
      TextInfo textInfo = cultureInfo.TextInfo;
      string pov = null;
      pov = textInfo.ToTitleCase(textBox1.Text.Trim());
      //textBox2.Text;
      LogParser parseObj = new LogParser();
      parseObj.ParseCombat(pov, textBox2.Text);
    }

    private void button2_Click(object sender, EventArgs e)
    {
      OpenFileDialog fdlg = new OpenFileDialog();
      fdlg.Title = "Select Arctic log file to analyze";
      fdlg.InitialDirectory = @"c:\";
      fdlg.Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|Log files (*.log)|*.log";
      fdlg.FilterIndex = 2;
      fdlg.RestoreDirectory = true;
      if (fdlg.ShowDialog() == DialogResult.OK)
      {
        textBox2.Text = fdlg.FileName;
      }
    }


  }
}
