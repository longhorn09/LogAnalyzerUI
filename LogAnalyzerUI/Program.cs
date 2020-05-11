/*****************************************************************
 * Initial creation : 7/10/2012 
 * Author           : Norman Tang 
 * 
 * Version History
 * 7/14/2012 - v0.03 - added RIP, bashed by efficiency adjustments, and char class inference (need fix bug with isabella bashed by)
 * 7/15/2012 - v0.04 - adds combat +, -, x, X indicators
 * 7/16/2012 - v0.05 - includes 1st person bash actions
 * 7/17/2012 - v0.06 - includes charge lag and windows forms GUI
 * 7/20/2012 - v0.07 - adds more actions like dk drain, paladin avenge, darkness spell, etc
 *****************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;

namespace LogAnalyzerUI
{
  public class EfficiencyObj
  {
    public string charname { get; set; }
    public float efficiency { get; set; }
    public string charclass { get; set; }
    public EfficiencyObj(string pCharname, float pEfficiency, string pCharclass)
    {
      this.charname = pCharname;
      this.efficiency = pEfficiency;
      this.charclass = pCharclass;
    }
  }

  public class SortClass : IComparer
  {

    public int Compare(object x, object y)
    {
      int retvalue = 0;
      EfficiencyObj obj1 = (EfficiencyObj)x;
      EfficiencyObj obj2 = (EfficiencyObj)y;
      if (obj1.efficiency > obj2.efficiency)
      {
        retvalue = -1;
      }
      else if (obj1.efficiency < obj2.efficiency)
      {
        retvalue = 1;
      }
      else
      {
        retvalue = 0;
      }
      return retvalue;
    }

  }

  public class PKAction
  {
    public string action { get; set; }
    public string victim { get; set; }
    public string attacker { get; set; }
    public int round { get; set; }
    public bool isSitting { get; set; }
    public string charclass { get; set; }

    public PKAction(string pAction, string pVictim, string pAttacker, int pRound)
    {
      action = pAction;
      victim = pVictim;
      attacker = pAttacker;
      round = pRound;
    }

    public PKAction(string pAction, string pVictim, string pAttacker, int pRound, string pCharclass)
    {
      action = pAction;
      victim = pVictim;
      attacker = pAttacker;
      round = pRound;
      charclass = pCharclass;
    }
  }


  static class Program
  {
    public const string version = "0.09";
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1());
    }
  }
}
