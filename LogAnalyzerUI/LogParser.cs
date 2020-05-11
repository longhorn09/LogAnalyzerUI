using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;


namespace LogAnalyzerUI
{
  public class LogParser
  {
    public string MakeSpace(int spacecount)
    {
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < spacecount; i++)
      {
        sb.Append(" ");
      }
      return sb.ToString();
    }

    public void insertAction(Hashtable hash, int round, string attacker, string action, string victim)
    {
      ArrayList arr = null;
      if (hash[attacker] == null)
      {
        arr = new ArrayList();
        hash[attacker] = arr;
      }
      arr = (ArrayList)hash[attacker];
      arr.Add(new PKAction(action, victim, attacker, round));
      hash[attacker] = arr;
    }


    public void insertAction(Hashtable hash, int round, string attacker, string action, string victim, string charclass)
    {
      ArrayList arr = null;
      if (hash[attacker] == null)
      {
        arr = new ArrayList();
        hash[attacker] = arr;
      }
      arr = (ArrayList)hash[attacker];
      if (arr != null && arr.Count > 0
        && ((PKAction)arr[arr.Count - 1]).round == round
        && !action.Equals("unkw spell")
        && ((PKAction)arr[arr.Count - 1]).action.Equals("unkw spell"))
      {
        arr.RemoveAt(arr.Count - 1);
      }
      //this case removes duplicates caused by spells like force missile, granite hand, magic missile
      else if (arr != null && arr.Count > 0
        && ((PKAction)arr[arr.Count - 1]).round == round
        && ((PKAction)arr[arr.Count - 1]).action == action
        && ((PKAction)arr[arr.Count - 1]).attacker == attacker
        && ((PKAction)arr[arr.Count - 1]).victim == victim
        )
      {
        arr.RemoveAt(arr.Count - 1);
      }

      arr.Add(new PKAction(action, victim, attacker, round, charclass));
      hash[attacker] = arr;
    }

    public void removeLastAction(Hashtable hash, string attacker)
    {
      ArrayList arr = null;
      if (hash[attacker] == null)
      {
        arr = new ArrayList();
        hash[attacker] = arr;
      }
      arr = (ArrayList)hash[attacker];
      if (arr.Count > 0)
      {
        arr.RemoveAt(arr.Count - 1);
      }
    }

    public void ParseCombat(string pov, string logPath)
    {
      StreamReader sr = null;
      string fullPath = new FileInfo(logPath).Directory.FullName
                                       + @"\"
                                       + Path.GetFileNameWithoutExtension(logPath)
                                       + "_analysis.txt";
      StreamWriter sw = new StreamWriter(fullPath);
      string line = null;
      int roundCount = 1;
      int combatLines = 0;
      
      Hashtable hash = new Hashtable();
      string promptLine = @"^(\d+)H (\d+)V (\d+)X ([0-9\.]+% )?(SC:[A-Z][a-z]+ )?(\d+)C ([A-Za-z0-9 \,\.\-\'\:\]\[\(\)\*]*)\>";
      string patPOV = @"^POV:(\s*[A-Z][a-z]+)$";
      string patLeave = @"^([A-Za-z]+) (leaves|flies) (north|east|south|west|down|up)\.$";
      string patEnter = @"^([A-Z][a-z]+) (flies in|arrives) from (the )?(above|below|east|west|north|south)\.$";
      string pat1 = @"^([A-Za-z \,\.\-\']+) (barely )?(bites|hits|claws|cleaves|slashes|pierces|stings|shoots|bludgeons|smites|crushes|drains|stabs|whips|plunks) (a fiery arrow at )?([A-Za-z \,\.\-\']+)(\, blasting (him|her|it|them))?\.$";
      string pat2 = @"^([A-Za-z \,\.\-\']+) (obliterates|annihilates|massacres|bruises|misses) ([A-Za-z \,\.\-\']+) with (his|her|their|its|a) (plunk|hit|shoot|crush|drain|sting|slash|cleave|stab|pierce|claw|bite|bludgeon|smite)\.$";
      string pat22 = @"^You (barely )?(bite|hit|claw|cleave|slash|pierce|sting|shoot|bludgeon|smite|crush|drain|stab|whip|plunk) (a fiery arrow at )?([A-Za-z \,\.\-\']+)(\, blasting (him|her|it|them))?\.$";
      string pat23 = @"^You (obliterate|annihilate|massacre|bruise|miss) ([A-Za-z \,\.\-\']+) with (a|your) (plunk|hit|shoot|crush|drain|sting|slash|cleave|stab|pierce|claw|bite|bludgeon|smite)\.$";

      string pat100 = @"^Suddenly ([A-Z][a-z]+) stabs you in the back\.$";
      string pat101 = @"([A-Z][a-z]+) focuses on ([A-Z]?[a-z]+) and utters the words, '([a-z\s]+)'\.$";
      string pat102 = @"([A-Z][a-z]+) conjures a rapidly growing mass of thorny vines\.$";
      string pat103 = @"^([A-Z][a-z]+) heals ([A-Z][a-z]+)\.$";
      string pat104 = @"^You are sent sprawling as ([A-Z][a-z]+) crashes into you\.$";
      string pat105 = @"([A-Z][a-z]+) utters some strange words\.$";
      string attacker = string.Empty;
      string victim = string.Empty;
      string action = string.Empty;
      string caster = string.Empty;
      string pat106 = @"^With a twinkle, a cloud of light appears\.$";
      string pat107 = @"^You shiver as ([A-Z][a-z]+) unleashes a world of nightmares\.$";
      string pat108 = @"([A-Z][a-z]+) gives ([A-Z][a-z]+) a solid punch in the face\.$";
      string pat109 = @"([A-Z][a-z]+) crashes into ([A-Z][a-z]+) in a thundering collision, knocking (him|her) to the ground!$";
      string pat110 = @"You utter the words, '([a-z\s]+)'";
      string pat111 = @"^([A-Z][a-z]+) conjures a writhing mass of inky black tentacles!$";
      string pat112 = @"([A-Z][a-z]+) focuses harshly on ([A-Z][a-z]+) and utters some strange words\.$";
      string pat113 = @"^([A-Z][a-z]+) has a gleam in (his|her) eye as a granite hand gives ([A-Z][a-z]+) a vicious backhand slap\.$";
      string pat114 = @"^([A-Z][a-z]+) screams in pain as lightning from ([A-Z][a-z]+) penetrates (him|her)\.$";
      string pat115 = @"^Flesh burns as ([A-Z][a-z]+) enshrouds ([A-Z][a-z]+) in a cloak of flames\.$";
      string pat116 = @"^([A-Z][a-z]+) stumbles and falls while trying to bash ([A-Z][a-z]+)\.$";
      string pat117 = @"^([A-Z][a-z]+) creates an icicle\.$";
      string pat118 = @"^([A-Z][a-z]+) sends a chilling blast of air at ([A-Z][a-z]+), giving (him|her|it) a severe case of frostbite\.$"; //cone of cold
      string pat119 = @"^([A-Z][a-z]+) calls up the roots and vines to whip ([A-Z][a-z]+)\.$";  //elemental fist
      string pat120 = @"^Flames leap from ([A-Z][a-z]+)'s fingertips and burn ([A-Z][a-z]+)\.$";
      string pat121 = @"^([A-Z][a-z]+) wavers under the impact of the thunderbolt sent by ([A-Z][a-z]+)\.$"; //thunderbolt
      string pat122 = @"([A-Z][a-z]+) heroically rescues ([A-Z][a-z]+)\.$"; //rescue
      string pat123 = @"A blastwave detonates from ([A-Z][a-z]+)\'s hands\!$"; //blastwave
      string pat124 = @"([A-Z][a-z]+)\'s chain lightning causes ([A-Z][a-z]+) to shake and spasm\.$"; //chain lightning
      string pat125 = @"THWAP! ([A-Z][a-z]+)\'s tenebrous orb detonates, slamming ([A-Z][a-z]+) with dark energy\.$"; //tenebrous orb
      string pat126 = @"([A-Z][a-z]+) grins as (his|her) force missile crashes into ([A-Z][a-z]+)\.$"; //force missile
      string pat127 = @"^([A-Z][a-z]+) receives the full blast of a thunderbolt from ([A-Z][a-z]+) ... and is no more\.$";
      string pat128 = @"^([A-Z][a-z]+)\'s bash powers through ([A-Z][a-z]+)\'s flesh shield, sending (him|her) sprawling\.$";
      string pat129 = @"^([A-Z][a-z]+) focuses harshly on ([A-Za-z]+) and utters the words, '([a-z\s]+)'\.$";
      string pat130 = @"([A-Z][a-z]+) utters the words, '([a-z\s]+)'\.$";
      string pat131 = @"^You\'ve never heard of such a spell\.";
      string pat132 = @"([A-Z][a-z]+) wiggles (his|her) fingers as (he|she) outlines ([A-Z][a-z]+)\'s head in the air\.$";
      string pat133 = @"([A-Z][a-z]+)'s rapid, unexpected knuckle thrust to ([A-Z][a-z]+)'s throat does some damage\.$";
      string pat134 = @"([A-Z][a-z]+) gazes at ([A-Za-z]+) with a look of evilness\.$";  //could be 'you'
      string pat135 = @"([A-Z][a-z]+) tries in vain to disarm ([A-Z][a-z]+)'s weapon\.$";
      string pat136 = @"([A-Z][a-z]+) lunges at you with a ([a-z\s\'\-\,]+)\, but you easily avoid (his|her) attempt to backstab you\.$";
      string pat137 = @"You focus your purity on ([A-Z][a-z]+)'s ([a-z]+)!";
      string pat138 = @"^([A-Z][a-z]+)\'s hands send forth blinding rays of sunlight\!$";
      string pat139 = @"([A-Z][a-z]+) gives out a wild cry and points a shaking finger at ([A-Z][a-z]+)\.$";
      string pat140 = @"([A-Z][a-z]+) roars\.$";
      string pat141 = @"With blinding speed, ([A-Z][a-z]+) manages to unbalance ([A-Z][a-z]+)\.$";
      string pat142 = @"^([A-Z][a-z]+) holds (his|her) holy symbol high for everyone to see\.";
      string pat143 = @"([A-Z][a-z]+) attacks ([A-Z][a-z]+) with lightning fast series of attacks that leaves (him|her) defenseless\.";
      string pat144 = @"([A-Z][a-z]+) suddenly lunges forward, slamming (his|her) skull into ([A-Z][a-z]+)\'s face with a sickening crunch\.$"; //headbutt
      string pat145 = @"([A-Z][a-z]+)'s skin turns grey and granite-like\.";
      //leave this as is, target could be "you" or "Propername"
      string pat146 = @"([A-Z][a-z]+) focuses on ([A-Za-z]+) and utters some strange words\.$"; 
      string pat147 = @"You yell and inspire your companions to fight harder\!$";               // rally cry / paladin
      string pat148 = @"([A-Z][a-z]+) creates a lightning storm\.$";
      string pat149 = @"([A-Z][a-z]+) connects with a precision thrust, wounding ([A-Z][a-z]+) severely\.$";
      string pat150 = @"([A-Z][a-z]+) is shredded by shards of ice\.$";
      string pat151 = @"([A-Z][a-z]+) attempts to strike at ([A-Z][a-z]+) with (his|her) lance\.$";  // strike / paladin
      string pat152 = @"([A-Z][a-z]+) dives into ([A-Z][a-z]+) and savagely bites (him|her), tearing a huge chunk from (his|her) flesh\.$"; //bite, ocean
      string pat153 = @"([A-Z][a-z]+) sends forth a giant wave\.$"; //whale, ocean
      string pat154 = @"([A-Z][a-z]+) gives (his|her) ([a-z\s\,\'\-]+) a quick thrust and twist, as (he|she) strikes ([A-Z][a-z]+) in the back with it\.$";
      string pat155 = @"([A-Z][a-z]+) leaps past ([A-Z][a-z]+), unbalancing them\.$";
      string pat156 = @"([A-Z][a-z]+) glares into ([A-Z][a-z]+)\'s eyes, hypnotizing (him|her)\.$";
      string pat157 = @"([A-Z][a-z]+) places [A-Za-z \,\.\-\']+ in ([A-Z][a-z]+)'s back\, resulting in some strange noises and some blood\.$";
      string pat158 = @"A twisted look of pain appears on ([A-Z][a-z]+)'s face as (he|she) suddenly discovers ([A-Z][a-z]+)'s [A-Za-z \,\.\-\']+ in (his|her) back\.$";
      string pat159 = @"([A-Z][a-z]+) writhes in pain as some unseen force torments (him|her)\.$";
      string pat160 = @"([A-Z][a-z]+) jumps on ([A-Z][a-z]+), striking (her|him) savagely\.$";
      string pat161 = @"([A-Z][a-z]+) delivers a quick snap kick to ([A-Z][a-z]+)\. ([A-Z][a-z]+) staggers and tries to recover\.$";
      string pat162 = @"Chunks of ([A-Z][a-z]+)'s flesh are ripped out and gathered by ([A-Z][a-z]+)\.$";
      string pat163 = @"([A-Z][a-z]+) growls at ([A-Z][a-z]+)!$";
      string pat164 = @"([A-Z][a-z]+) tries to punch ([A-Z][a-z]+), but (he|she) deftly avoids the blow\.$";
      string pat165 = @"([A-Z][a-z]+) swings madly at ([A-Z][a-z]+) with [a-z\s\-\'\,]+, cutting (him|her) a deep wound\.$";
      string pat166 = @"([A-Z][a-z]+) thrashes ([A-Z][a-z]+) good, forcing them to the ground\.$"; //mountain, passive
      string pat167 = @"([A-Z][a-z]+) attempts to perform a coup on ([A-Z][a-z]+), but fails terribly\.$"; //thief fail coup
      string pat168 = @"([A-Z][a-z]+) gazes at ([A-Za-z\s\-\,\']+) with a pathetic look of evilness\.$";  //dk fail gaze
      string pat169 = @"^A greenish-purple mist fills the air\.$"; //blackrobe, acid mist
      string pat170 = @"([A-Z][a-z]+) shines with a golden light\.$";
      string pat171 = @"([A-Z][a-z]+)'s hands issue a spray of prismatic color!$";
      //-- Warrior 1st person POV ---
      string pat172 = @"You try to bash ([A-Z][a-z]+), but you miss and tumble forward onto your face\.$";
      string pat173 = @"You crash into ([A-Z][a-z]+) in a bone crunching bash, sending (him|her) sprawling to the ground\.$";
      string pat174 = @"([A-Z][a-z]+) doesn't recover as you deliver the bash -- (he|she) is dead\.$";
      string pat175 = @"([A-Z][a-z]+) gazes at you and you start to feel (he|she) may be an old friend\.$"; //fail gaze, dark knight
      string pat176 = @"([A-Z][a-z]+) makes a strange sound but is suddenly very silent, as you place [A-Za-z\s\,\-\']+ in (her|his) back\.$";  //thief backstab
      string pat177 = @"([A-Z][a-z]+) twists around to see you place [A-Za-z\s\,\-\']+ in (his|her) back\.$";  //thief backstab
      string pat178 = @"([A-Z][a-z]+) reaches behind (herself|himself) quickly, only to discover your [A-Za-z\s\,\-\']+ in (his|her) back\.$"; //thief backstab
      string pat179 = @"([A-Z][a-z]+) unleashes a swarm of flying piranha at you!$";
      string pat180 = @"([A-Z][a-z]+) sends a swarm of flying piranha at ([A-Z][a-z]+)\.$";
      string pat181 = @"([A-Z][a-z]+) charges at ([A-Z][a-z]+), but (he|she) managed to move away\.$";
      string pat182 = @"([A-Z][a-z]+) swings madly at ([A-Z][a-z]+) with ([A-Za-z\s\,\-\']+), knocking (him|her) to the ground\.$";
      string pat183 = @"What a self-sacrificing act! You're such a hero!$";
      string pat184 = @"([A-Z][a-z]+) comes to your rescue! You're thankful, but a bit dazed\.$";
      string pat185 = @"([A-Z][a-z]+) yells and leaps into the fray\.$"; //rally cry, paladin
      string pat186 = @"([A-Z][a-z]+) spasms as ([A-Z][a-z]+) places (his|her) hand on ([A-Z][a-z]+)'s chest\.$"; //dk, drain
      string pat187 = @"The room is suddenly enveloped in darkness\.$";
      string pat188 = @"([A-Z][a-z]+) focuses (his|her) purity on ([A-Z][a-z]+)'s ([a-z]+)!$";
      string pat189 = @"([A-Z][a-z]+) forces ([A-Z][a-z]+) to the ground\.$";
      string pat190 = @"With an amazing coup, ([A-Z][a-z]+) thumps ([A-Z][a-z]+)'s skull real hard, ([A-Z][a-z]+) looks lost and grins like a chump\.$"; //thief, coup
      string pat191 = @"([A-Z][a-z]+) lays (his|her) hands on ([A-Z][a-z]+) and utters a prayer\.$";

      //-- Barb 1st person POV ---
      string pat192 = @"You charge into ([A-Z][a-z]+), but (he|she) manages to hold firm\.$"; //barb charge fail
      string pat193 = @"You box ([A-Z][a-z]+)'s ears with two of your frantic swings\.$"; //barb assail
      string pat194 = @"You can't find the courage to dive into the breach\.$";    //barbarian itb fail
      string pat195 = @"With a crazed scream, you jump into the fray and attack like a wild person\.$"; //barb itb
      string pat196 = @"From deep in your belly comes forth a barbaric YAWP\.$";    //barbarian battlecry
      string pat197 = @"You start swinging to the entropic chords of oblivion\.$";  //barbarian ghostdance
      string pat198 = @"([A-Z][a-z]+) thrusts at ([A-Z][a-z]+), but ([A-Z][a-z]+) slides easily away\.$"; //dk fail thrust
      string pat199 = @"([A-Z][a-z]+) screams wildly and cusses at ([A-Za-z]+)\.$"; //shaman hex
      //ocean scout instincts
      //string pat199 = @"([A-Z][a-z]+) dives into ([A-Z][a-z]+) and savagely bites (him|her), tearing a huge chunk from (his|her) flesh\.$";//shark instinct
      string pat200 = @"([A-Z][a-z]+) grabs at ([A-Z][a-z]+) and attempts to electrocute (him|her), but fails badly\.$"; //2 rounds of actor lag
      string pat201 = @"([A-Z][a-z]+) is left quivering as ([A-Z][a-z]+) clutches (him|her) and unleashes a brutal zap\.$";//1 round of actor lag
      string pat202 = @"([A-Z][a-z]+) comes to your aid, but fails miserably\.$"; //fail rescue
      string pat203 = @"^The air swirls and a funnel cloud forms\.$"; //druid tornado
      string pat204 = @"([A-Z][a-z]+) appears to get crazy for a second\.$"; //barbarian dance fail
      string pat205 = @"([A-Z][a-z]+) charge into the breach before (him|her) sounding a great yell\.$"; //barb into the breach
      string pat206 = @"([A-Z][a-z]+)  manages to avoid ([A-Z][a-z]+)\'\s grasp\.$"; //dk drain fail
      string pat207 = @"([A-Z][a-z]+) slams into ([A-Z][a-z]+) and pushes (him|her) out of the way of battle\.$"; //barb rescue
      string pat208 = @"([A-Z][a-z]+) tries to shove ([A-Z][a-z]+) out of the way of battle\.$"; //barb autorescue fail
      string pat209 = @"([A-Z][a-z]+) barrels into ([A-Z][a-z]+), knocking (him|her) to the ground\.$"; //barb charge bash
      string pat210 = @"([A-Z][a-z]+) lets out a mighty yell and fights like crazy\!$"; // shaman frenzy
      string pat211 = @"([A-Z][a-z]+)\'s wounds begin to heal\.$"; //shaman regen
      string pat212 = @"([A-Z][a-z]+) calls forth some dark and (file|vile) spirits and sends them at ([A-Z][a-z]+)\!$"; //shaman vile spirits
      string pat213 = @"([A-Z][a-z]+) gives you a solid punch in the face\.$"; //getting punched
      string pat214 = @"([A-Z][a-z]+) shudders slightly and begins to glow from within\.$";


      //** Warrior bash killing blow
      string pat250 = @"([A-Z][a-z]+) crashes into ([A-Z][a-z]+) in a devastating bash, killing (him|her) mercilessly\.$";
      string pat251 = @"([A-Z][a-z]+) dodges to the (left|right), avoiding ([A-Z][a-z]+)'s backstab";
      string pat252 = @"([A-Z][a-z]+) issues the order \'(.+)\'\.$";

      //** Barbarian 3rd person *
      string pat253 = @"With a crazed scream, ([A-Z][a-z]+) jumps into the fray and attacks like a wild person\.$"; //assail
      string pat254 = @"([A-Z][a-z]+) begins to assault ([A-Z][a-z]+)\!"; //legendary warrior assault
      string pat255 = @"([A-Z][a-z]+) charges into the breach, sounding a great yell\.$"; //barbarian itb
      string pat256 = @"([A-Z][a-z]+) yells a battle cry and fights like a madman\!$"; //barbarian wardance
      //** Thief legendary dodge *
      string pat257 = @"^([A-Z][a-z]+) dodges to the (right|left), avoiding ([A-Z][a-z]+)\'s ([A-Za-z\s]+)$"; //thief dodge

      //** Scout 1st person *
      string pat260 = @"You roar in anger\.$";  //scout 1st person

      //**more Barb 1st person **
      string pat270 = @"You charge at ([A-Z][a-z]+), but (he|she|it) managed to move away\.$";
      string pat271 = @"You barrel into ([A-Z][a-z]+), knocking (him|her|it) to the ground\.$";
      string pat272 = @"You swing ([a-z\s\,\'\-]+) madly at ([A-Z][a-z]+), knocking (him|her|it) to the ground\.$";
      string pat273 = @"You charge into ([A-Z][a-z]+), but they manage to hold firm\.$";

      string pat280 = @"([A-Z][a-z]+) unleashes a wave of crippling power\!$";
      string pat281 = @"([A-Z][a-z]+) glows a bright lunar white for a moment\.$";
      string pat282 = @"([A-Z][a-z]+) seems to move a little more fluidly\.$";
      string pat283 = @"([A-Z][a-z]+) attempts to strike at ([A-Z][a-z]+) with (his|her) ([A-Za-z ]+)\.$"; //paladin strike
      string pat284 = @"([A-Z][a-z]+) appears to be angry at you!"; //legend thief retaliate

      //** DISABLES *********************************************************************
      string pat300 = @"([A-Z][a-z]+) attempts to break free of the black tentacles, but fails!$";
      string pat301 = @"([A-Z][a-z]+) is dead! R.I.P\.$";
      string pat302 = @"([A-Z][a-z]+) succumbs to the nightmare and falls into a deep, dark sleep\.$";
      string pat303 = @"([A-Z][a-z]+) screams in pain as (he|she) is constricted by inky black tentacles\.$";
      string pat304 = @"([A-Z][a-z]+) is stunned!$";
      string pat305 = @"([A-Z][a-z]+) is paralyzed!$";
      string pat306 = @"Your limbs freeze in place!$";

      //** RECALLS *********************************************************************
      string pat400 = @"([A-Z][a-z]+) disappears\.$";
      string pat401 = @"([A-Z][a-z]+) disappears suddenly\.$";  //Dracos shard

      //** Sky scout *******************************************************************
      string pat500 = @"([A-Z][a-z]+) takes aim at ([A-Z][a-z]+)\.$";
      string pat501 = @"([A-Z][a-z]+) [a-z]+ ([A-Z][a-z]+) with a plunk\.$";
      string pat502 = @"([A-Z][a-z]+) plunks ([A-Z][a-z]+) ([a-z ]+)\.$";
      string pat503 = @"([A-Z][a-z]+) bolsters (his|her) friends\.$";
      string pat504 = @"^You aim at ([A-Z][a-z]+)\.$";
      //string pat505 = @"^You [a-z]+ ([A-Z][a-z]+) with a plunk\.$";  //this will get caught by pat501
      string pat506 = @"^You plunk ([A-Z][a-z]+) ([a-z ]+)\.$";
      string pat507 = @"^You bolster your friends, easing their nerves\.$";


      //** FILES ************************************************************************
      sr = new StreamReader(File.OpenRead(logPath));

      while ((line = sr.ReadLine()) != null)
      {
        attacker = string.Empty;
        victim = string.Empty;
        action = string.Empty;
        if ((Regex.Match(line, pat1).Success ||
            Regex.Match(line, pat2).Success ||
            Regex.Match(line, pat22).Success ||
            Regex.Match(line, pat23).Success) && !line.ToLower().StartsWith("the corpse of ") && !line.ToLower().StartsWith("suddenly "))
        {
          combatLines++;
        }

        if (Regex.Match(line, patPOV).Success)
        {
          pov = Regex.Match(line, patPOV).Groups[1].Value.Trim();
        }

        if (Regex.Match(line, patEnter).Success)
        {
          attacker = Regex.Match(line, patEnter).Groups[1].Value;
          victim = string.Empty;
          action = "arrives";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat400).Success)
        {
          attacker = Regex.Match(line, pat400).Groups[1].Value;
          victim = string.Empty;
          action = "recalls";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }

        if (Regex.Match(line, pat401).Success)
        {
          attacker = Regex.Match(line, pat401).Groups[1].Value;
          victim = string.Empty;
          action = "leaves";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, patLeave).Success)
        {
          attacker = Regex.Match(line, patLeave).Groups[1].Value;
          victim = string.Empty;
          action = "leaves";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat100).Success)
        {
          attacker = Regex.Match(line, pat100).Groups[1].Value;
          victim = pov;
          action = "backstab";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat101).Success)
        {
          attacker = Regex.Match(line, pat101).Groups[1].Value;
          if (Regex.Match(line, pat101).Groups[2].Value.Equals("you"))
          {
            victim = pov;
          }
          else
          {
            victim = Regex.Match(line, pat101).Groups[2].Value;
          }
          action = Regex.Match(line, pat101).Groups[3].Value;
          this.insertAction(hash, roundCount, attacker, action, victim, "cleric");
        }
        if (Regex.Match(line, pat102).Success)
        {
          attacker = Regex.Match(line, pat102).Groups[1].Value;
          victim = string.Empty;
          action = "entangle";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat103).Success)
        {
          attacker = Regex.Match(line, pat103).Groups[1].Value;
          victim = Regex.Match(line, pat103).Groups[2].Value;
          action = "heal";
          this.insertAction(hash, roundCount, attacker, action, victim, "cleric");
        }
        if (Regex.Match(line, pat104).Success)
        {
          attacker = Regex.Match(line, pat104).Groups[1].Value;
          victim = pov;
          action = "bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
          this.insertAction(hash, roundCount, victim, "bashed by", attacker, string.Empty);
        }
        if (Regex.Match(line, pat105).Success)
        {
          caster = Regex.Match(line, pat105).Groups[1].Value;
        }
        if (Regex.Match(line, pat106).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "healing cloud";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat107).Success)
        {
          attacker = Regex.Match(line, pat107).Groups[1].Value;
          victim = string.Empty;
          action = "nightmare";
          this.insertAction(hash, roundCount, attacker, action, victim, "black robe");
        }
        if (Regex.Match(line, pat108).Success)
        {
          attacker = Regex.Match(line, pat108).Groups[1].Value;
          victim = Regex.Match(line, pat108).Groups[2].Value;
          action = "punch";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat109).Success)
        {
          attacker = Regex.Match(line, pat109).Groups[1].Value;
          victim = Regex.Match(line, pat109).Groups[2].Value;
          action = "bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
          this.insertAction(hash, roundCount, victim, "bashed by", attacker, string.Empty);
        }
        if (Regex.Match(line, pat110).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = Regex.Match(line, pat110).Groups[1].Value;
          switch (action)
          {
            case "sunray":
              this.insertAction(hash, roundCount, attacker, action, victim, "cleric");
              break;
            case "blastwave":
              this.insertAction(hash, roundCount, attacker, action, victim, "red robe");
              break;
            case "strength":
              this.insertAction(hash, roundCount, attacker, action, victim, "red robe");
              break;
            case "haste":
              this.insertAction(hash, roundCount, attacker, action, victim, "red robe");
              break;
            default:
              this.insertAction(hash, roundCount, attacker, action, victim, "cleric");
              break;
          }
        }
        if (Regex.Match(line, pat111).Success)
        {
          attacker = Regex.Match(line, pat111).Groups[1].Value;
          victim = string.Empty;
          action = "malevolent tentacles";
          this.insertAction(hash, roundCount, attacker, action, victim, "black robe");
        }
        if (Regex.Match(line, pat112).Success)
        {
          attacker = Regex.Match(line, pat112).Groups[1].Value;
          caster = attacker;
          victim = Regex.Match(line, pat112).Groups[2].Value;
          action = "unkw spell";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat113).Success)
        {
          attacker = Regex.Match(line, pat113).Groups[1].Value;
          victim = Regex.Match(line, pat113).Groups[3].Value;
          action = "granite hand";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat114).Success)
        {
          attacker = Regex.Match(line, pat114).Groups[2].Value;
          victim = Regex.Match(line, pat114).Groups[1].Value;
          action = "call lightning";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat115).Success)
        {
          attacker = Regex.Match(line, pat115).Groups[1].Value;
          victim = Regex.Match(line, pat115).Groups[2].Value;
          action = "flame shroud";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat116).Success)
        {
          attacker = Regex.Match(line, pat116).Groups[1].Value;
          victim = Regex.Match(line, pat116).Groups[2].Value;
          action = "bash miss";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat118).Success)
        {
          attacker = Regex.Match(line, pat118).Groups[1].Value;
          victim = Regex.Match(line, pat118).Groups[2].Value;
          action = "cone of cold";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat119).Success)
        {
          attacker = Regex.Match(line, pat119).Groups[1].Value;
          victim = Regex.Match(line, pat119).Groups[2].Value;
          action = "elemental fist";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat120).Success)
        {
          attacker = Regex.Match(line, pat120).Groups[1].Value;
          victim = Regex.Match(line, pat120).Groups[2].Value;
          action = "fire storm";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat121).Success)
        {
          attacker = Regex.Match(line, pat121).Groups[2].Value;
          victim = Regex.Match(line, pat121).Groups[1].Value;
          action = "thunderbolt";
          this.insertAction(hash, roundCount, attacker, action, victim, "black robe");
        }
        if (Regex.Match(line, pat122).Success)
        {
          attacker = Regex.Match(line, pat122).Groups[1].Value;
          victim = Regex.Match(line, pat122).Groups[2].Value;
          action = "rescue";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat123).Success)
        {
          attacker = Regex.Match(line, pat123).Groups[1].Value;
          victim = string.Empty;
          action = "blastwave";
          this.insertAction(hash, roundCount, attacker, action, victim, "red robe");
        }
        if (Regex.Match(line, pat124).Success)
        {
          attacker = Regex.Match(line, pat124).Groups[1].Value;
          victim = Regex.Match(line, pat124).Groups[2].Value;
          action = "chain lightning";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat125).Success)
        {
          attacker = Regex.Match(line, pat125).Groups[1].Value;
          victim = string.Empty;//Regex.Match(line, pat125).Groups[2].Value;
          action = "tenebrous orb";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat126).Success)
        {
          attacker = Regex.Match(line, pat126).Groups[1].Value;
          victim = Regex.Match(line, pat126).Groups[3].Value;
          action = "force missile";
          this.insertAction(hash, roundCount, attacker, action, victim, "red robe");
        }
        if (Regex.Match(line, pat127).Success)
        {
          attacker = Regex.Match(line, pat127).Groups[2].Value;
          victim = Regex.Match(line, pat127).Groups[1].Value;
          action = "thunderbolt";
          this.insertAction(hash, roundCount, attacker, action, victim, "red robe");
        }
        if (Regex.Match(line, pat128).Success)
        {
          attacker = Regex.Match(line, pat128).Groups[1].Value;
          victim = Regex.Match(line, pat128).Groups[2].Value;
          action = "bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
          this.insertAction(hash, roundCount, victim, "bashed by", attacker, string.Empty);
        }
        if (Regex.Match(line, pat129).Success)
        {
          attacker = Regex.Match(line, pat129).Groups[1].Value;
          victim = Regex.Match(line, pat129).Groups[2].Value;
          if (victim.Equals("you"))
          {
            victim = pov;
          }
          action = Regex.Match(line, pat129).Groups[3].Value;
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat130).Success)
        {
          caster = Regex.Match(line, pat130).Groups[1].Value; 
          attacker = Regex.Match(line, pat130).Groups[1].Value;
          victim = string.Empty;
          action = Regex.Match(line, pat130).Groups[2].Value;
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat131).Success)
        {
          this.removeLastAction(hash, pov);
        }
        if (Regex.Match(line, pat132).Success)
        {
          attacker = Regex.Match(line, pat132).Groups[1].Value;
          victim = Regex.Match(line, pat132).Groups[4].Value;
          action = "stupefy";
          this.insertAction(hash, roundCount, attacker, action, victim, "shaman");
        }
        if (Regex.Match(line, pat133).Success)
        {
          attacker = Regex.Match(line, pat133).Groups[1].Value;
          victim = Regex.Match(line, pat133).Groups[2].Value;
          action = "throat punch";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat134).Success)
        {
          attacker = Regex.Match(line, pat134).Groups[1].Value;
          victim = Regex.Match(line, pat134).Groups[2].Value;
          action = "gaze";
          this.insertAction(hash, roundCount, attacker, action, victim, "dark knight");
        }

        if (Regex.Match(line, pat135).Success)
        {
          attacker = Regex.Match(line, pat135).Groups[1].Value;
          victim = Regex.Match(line, pat135).Groups[2].Value;
          action = "disarm";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat136).Success)
        {
          attacker = Regex.Match(line, pat136).Groups[1].Value;
          victim = pov;
          action = "backstab miss";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat137).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat137).Groups[1].Value;
          action = "avenge";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat138).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "sunray";
          this.insertAction(hash, roundCount, attacker, action, victim, "cleric");
        }
        if (Regex.Match(line, pat139).Success)
        {
          attacker = Regex.Match(line, pat139).Groups[1].Value;
          victim = Regex.Match(line, pat139).Groups[2].Value;
          action = "shaman unkw";
          this.insertAction(hash, roundCount, attacker, action, victim, "shaman");
        }
        if (Regex.Match(line, pat140).Success)
        {
          attacker = Regex.Match(line, pat140).Groups[1].Value;
          victim = string.Empty;
          action = "roar";
          this.insertAction(hash, roundCount, attacker, action, victim, "mountain");
        }
        if (Regex.Match(line, pat260).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "roar";
          this.insertAction(hash, roundCount, attacker, action, victim, "mountain");
        }
        if (Regex.Match(line, pat141).Success)
        {
          attacker = Regex.Match(line, pat141).Groups[1].Value;
          victim = Regex.Match(line, pat141).Groups[2].Value;
          action = "unbalance";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat142).Success)
        {
          if (caster == null || caster.Length == 0)
          {
            caster = Regex.Match(line, pat142).Groups[1].Value;
          }
          attacker = caster;
          victim = string.Empty;
          action = "holy word";
          this.insertAction(hash, roundCount, attacker, action, victim, "cleric");
        }
        if (Regex.Match(line, pat143).Success)
        {
          attacker = Regex.Match(line, pat143).Groups[1].Value;
          victim = Regex.Match(line, pat143).Groups[2].Value;
          action = "impair";
          this.insertAction(hash, roundCount, attacker, action, victim, "dark knight");
        }
        if (Regex.Match(line, pat144).Success)
        {
          attacker = Regex.Match(line, pat144).Groups[1].Value;
          victim = Regex.Match(line, pat144).Groups[3].Value;
          action = "head butt";
          this.insertAction(hash, roundCount, attacker, action, victim, "mountain");
        }
        if (Regex.Match(line, pat145).Success)
        {
          attacker = caster;
          victim = Regex.Match(line, pat145).Groups[1].Value;
          action = "stone skin";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat146).Success)
        {
          attacker = Regex.Match(line, pat146).Groups[1].Value;
          caster = attacker;
          if (Regex.Match(line, pat146).Groups[2].Value.Equals("you"))
          {
            victim = pov;
          }
          else
          {
            victim = Regex.Match(line, pat146).Groups[2].Value;
          }
          action = "unkw spell";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat147).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "rally cry";
          this.insertAction(hash, roundCount, attacker, action, victim, "paladin");
        }
        if (Regex.Match(line, pat148).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "lightning storm";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat149).Success)
        {
          attacker = Regex.Match(line, pat149).Groups[1].Value;
          victim = Regex.Match(line, pat149).Groups[2].Value;
          action = "thrust";
          this.insertAction(hash, roundCount, attacker, action, victim, "dark knight");
        }
        if (Regex.Match(line, pat150).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "ice storm";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat151).Success)
        {
          attacker = Regex.Match(line, pat151).Groups[1].Value;
          victim = Regex.Match(line, pat151).Groups[2].Value;
          action = "strike";
          this.insertAction(hash, roundCount, attacker, action, victim, "paladin");
        }
        if (Regex.Match(line, pat152).Success)
        {
          attacker = Regex.Match(line, pat152).Groups[1].Value;
          victim = Regex.Match(line, pat152).Groups[2].Value;
          action = "shark instinct";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean");
        }
        if (Regex.Match(line, pat153).Success)
        {
          attacker = Regex.Match(line, pat153).Groups[1].Value;
          victim = string.Empty;
          action = "whale";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean");
        }
        if (Regex.Match(line, pat154).Success)
        {
          attacker = Regex.Match(line, pat154).Groups[1].Value;
          victim = Regex.Match(line, pat154).Groups[5].Value;
          action = "backstab";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat155).Success)
        {
          attacker = Regex.Match(line, pat155).Groups[1].Value;
          victim = Regex.Match(line, pat155).Groups[2].Value;
          action = "frog";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean");
        }
        if (Regex.Match(line, pat156).Success)
        {
          attacker = Regex.Match(line, pat156).Groups[1].Value;
          victim = Regex.Match(line, pat156).Groups[2].Value;
          action = "siren";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean");
        }
        if (Regex.Match(line, pat157).Success)
        {
          attacker = Regex.Match(line, pat157).Groups[1].Value;
          victim = Regex.Match(line, pat157).Groups[2].Value;
          action = "backstab";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat158).Success)
        {
          attacker = Regex.Match(line, pat158).Groups[3].Value;
          victim = Regex.Match(line, pat158).Groups[1].Value;
          action = "backstab";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat159).Success)
        {
          attacker = caster;
          victim = Regex.Match(line, pat159).Groups[1].Value;
          action = "pain";
          this.insertAction(hash, roundCount, attacker, action, victim, "black robe");
        }
        if (Regex.Match(line, pat160).Success)
        {
          attacker = Regex.Match(line, pat160).Groups[1].Value;
          victim = Regex.Match(line, pat160).Groups[2].Value;
          action = "instinct";
          this.insertAction(hash, roundCount, attacker, action, victim, "mountain");
        }
        if (Regex.Match(line, pat161).Success)
        {
          attacker = Regex.Match(line, pat161).Groups[1].Value;
          victim = Regex.Match(line, pat161).Groups[2].Value;
          action = "kick";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat162).Success)
        {
          attacker = Regex.Match(line, pat162).Groups[2].Value;
          victim = Regex.Match(line, pat162).Groups[1].Value;
          action = "rend";
          this.insertAction(hash, roundCount, attacker, action, victim, "black robe");
        }
        if (Regex.Match(line, pat163).Success)
        {
          attacker = Regex.Match(line, pat163).Groups[1].Value;
          victim = Regex.Match(line, pat163).Groups[2].Value;
          action = "retaliate";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat164).Success)
        {
          attacker = Regex.Match(line, pat164).Groups[1].Value;
          victim = Regex.Match(line, pat164).Groups[2].Value;
          action = "punch miss";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat164).Success)
        {
          attacker = Regex.Match(line, pat164).Groups[1].Value;
          victim = Regex.Match(line, pat164).Groups[2].Value;
          action = "punch miss";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat165).Success)
        {
          attacker = Regex.Match(line, pat165).Groups[1].Value;
          victim = Regex.Match(line, pat165).Groups[2].Value;
          action = "charge";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat166).Success)
        {
          attacker = Regex.Match(line, pat166).Groups[1].Value;
          victim = Regex.Match(line, pat166).Groups[2].Value;
          action = "thrash";
          this.insertAction(hash, roundCount, attacker, action, victim, "mountain");
        }
        if (Regex.Match(line, pat167).Success)
        {
          attacker = Regex.Match(line, pat167).Groups[1].Value;
          victim = Regex.Match(line, pat167).Groups[2].Value;
          action = "coup fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat168).Success)
        {
          attacker = Regex.Match(line, pat168).Groups[1].Value;
          victim = Regex.Match(line, pat168).Groups[2].Value;
          action = "gaze fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "dark knight");
        }
        if (Regex.Match(line, pat169).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "acid mist";
          this.insertAction(hash, roundCount, attacker, action, victim, "black robe");
        }
        if (Regex.Match(line, pat170).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "amelioration";
          this.insertAction(hash, roundCount, attacker, action, victim, "cleric");
        }
        if (Regex.Match(line, pat171).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "prismatic spray";
          this.insertAction(hash, roundCount, attacker, action, victim, "white robe");
        }
        if (Regex.Match(line, pat172).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat172).Groups[1].Value;
          action = "bash miss";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat173).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat173).Groups[1].Value;
          action = "bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }

        if (Regex.Match(line, pat250).Success)  //bash killing blow
        {
          attacker = Regex.Match(line, pat250).Groups[1].Value;
          victim = Regex.Match(line, pat250).Groups[2].Value;
          action = "bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat254).Success)  //legendary warrior assault
        {
          attacker = Regex.Match(line, pat254).Groups[1].Value;
          victim = Regex.Match(line, pat254).Groups[2].Value;
          action = "assault";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat255).Success)  //barbarian itb
        {
          attacker = Regex.Match(line, pat255).Groups[1].Value;
          victim = string.Empty;
          action = "into the breach";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat256).Success)  //barbarian itb
        {
          attacker = Regex.Match(line, pat256).Groups[1].Value;
          victim = string.Empty;
          action = "wardance";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }

        if (Regex.Match(line, pat251).Success)  //fail backstab
        {
          ///string pat251 = @"([A-Z][a-z]+) dodges to the (left|right), avoiding ([A-Z][a-z]+)'s backstab";
          attacker = Regex.Match(line, pat251).Groups[3].Value;
          victim = Regex.Match(line, pat251).Groups[1].Value;
          action = "backstab miss";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }

        if (Regex.Match(line, pat252).Success)  //order charmy
        {
          attacker = Regex.Match(line, pat252).Groups[1].Value;
          victim = string.Empty;
          action = "order " + Regex.Match(line, pat252).Groups[2].Value;
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        

        if (Regex.Match(line, pat174).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat174).Groups[1].Value;
          action = "bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat175).Success)
        {
          attacker = Regex.Match(line, pat175).Groups[1].Value;
          victim = pov;
          action = "gaze fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "dark knight");
        }
        if (Regex.Match(line, pat176).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat176).Groups[1].Value;
          action = "backstab";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat177).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat177).Groups[1].Value;
          action = "backstab";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat178).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat178).Groups[1].Value;
          action = "backstab";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat179).Success)
        {
          attacker = Regex.Match(line, pat179).Groups[1].Value;
          victim = pov;
          action = "piranha";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean");
        }
        if (Regex.Match(line, pat180).Success)
        {
          attacker = Regex.Match(line, pat180).Groups[1].Value;
          victim = Regex.Match(line, pat180).Groups[2].Value;
          action = "piranha";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean");
        }
        if (Regex.Match(line, pat181).Success)
        {
          attacker = Regex.Match(line, pat181).Groups[1].Value;
          victim = Regex.Match(line, pat181).Groups[2].Value;
          action = "charge miss";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat182).Success)
        {
          attacker = Regex.Match(line, pat182).Groups[1].Value;
          victim = Regex.Match(line, pat182).Groups[2].Value;
          action = "charge";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
          this.insertAction(hash, roundCount, victim, "charged by", attacker, string.Empty);   //this pattern means victim was bashed
        }
        if (Regex.Match(line, pat183).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "rescue";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat184).Success)
        {
          attacker = Regex.Match(line, pat184).Groups[1].Value;
          victim = pov;
          action = "rescue";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat185).Success)
        {
          attacker = Regex.Match(line, pat185).Groups[1].Value;
          victim = string.Empty;  
          action = "rally cry";
          this.insertAction(hash, roundCount, attacker, action, victim, "paladin");
        }
        if (Regex.Match(line, pat186).Success)
        {
          attacker = Regex.Match(line, pat186).Groups[2].Value;
          victim = Regex.Match(line, pat186).Groups[1].Value;
          action = "drain";
          this.insertAction(hash, roundCount, attacker, action, victim, "dark knight");
        }
        if (Regex.Match(line, pat187).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "darkness";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat188).Success)
        {
          attacker = Regex.Match(line, pat188).Groups[1].Value;
          victim = Regex.Match(line, pat188).Groups[3].Value;
          action = "avenge " + Regex.Match(line, pat188).Groups[4].Value;
          this.insertAction(hash, roundCount, attacker, action, victim, "paladin");
        }
        if (Regex.Match(line, pat189).Success)
        {
          attacker = Regex.Match(line, pat189).Groups[2].Value; 
          victim = Regex.Match(line, pat189).Groups[1].Value;
          action = "bashed by";   //really avenged by
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat190).Success)
        {
          attacker = Regex.Match(line, pat190).Groups[1].Value;
          victim = Regex.Match(line, pat190).Groups[2].Value;
          action = "coup";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat191).Success)
        {
          attacker = Regex.Match(line, pat191).Groups[1].Value;
          victim = Regex.Match(line, pat191).Groups[3].Value;
          action = "lay";
          this.insertAction(hash, roundCount, attacker, action, victim, "paladin");
        }
        if (Regex.Match(line, pat192).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat192).Groups[1].Value;
          action = "charge fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat193).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat193).Groups[1].Value;
          action = "assail";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat253).Success)
        {
          attacker = Regex.Match(line, pat253).Groups[1].Value;
          victim = string.Empty;
          action = "assail";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat194).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "into the breach fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }

        if (Regex.Match(line, pat195).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "into the breach";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat196).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "battlecry";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat197).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "ghostdance";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat198).Success)
        {
          attacker = Regex.Match(line, pat198).Groups[1].Value;
          victim = Regex.Match(line, pat198).Groups[2].Value;
          action = "thrust fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "dark knight");
        }/*
        if (Regex.Match(line, pat199).Success)
        {
          attacker = Regex.Match(line, pat199).Groups[1].Value;
          victim = Regex.Match(line, pat199).Groups[2].Value;
          action = "shark instinct";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean scout");
        }*/
        if (Regex.Match(line, pat199).Success)
        {
          attacker = Regex.Match(line, pat199).Groups[1].Value;
          victim = Regex.Match(line, pat199).Groups[2].Value;
          if (victim.Equals("you"))
          {
            victim = pov;
          }
          action = "hex";
          this.insertAction(hash, roundCount, attacker, action, victim, "shaman");
        }

        if (Regex.Match(line, pat200).Success)
        {
          attacker = Regex.Match(line, pat200).Groups[1].Value;
          victim = Regex.Match(line, pat200).Groups[2].Value;
          action = "eel fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean");
        }
        if (Regex.Match(line, pat201).Success)
        {
          attacker = Regex.Match(line, pat201).Groups[2].Value;
          victim = Regex.Match(line, pat201).Groups[1].Value;
          action = "eel instinct";
          this.insertAction(hash, roundCount, attacker, action, victim, "ocean");
        }
        if (Regex.Match(line, pat202).Success)
        {
          attacker = Regex.Match(line, pat202).Groups[1].Value;
          victim = pov;
          action = "rescue fail";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat203).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "tornado";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }
        if (Regex.Match(line, pat204).Success)
        {
          attacker = Regex.Match(line, pat204).Groups[1].Value;
          victim = string.Empty;
          action = "dance unkw";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat205).Success)
        {
          attacker = Regex.Match(line, pat205).Groups[1].Value;
          victim = string.Empty;
          action = "into the breach";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat206).Success)
        {
          attacker = Regex.Match(line, pat206).Groups[2].Value;
          victim = Regex.Match(line, pat206).Groups[1].Value;
          action = "drain fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "death knight");
        }
        if (Regex.Match(line, pat207).Success)
        {
          attacker = Regex.Match(line, pat207).Groups[1].Value;
          victim = Regex.Match(line, pat207).Groups[2].Value;
          action = "autorescue";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat208).Success)
        {
          attacker = Regex.Match(line, pat208).Groups[1].Value;
          victim = Regex.Match(line, pat208).Groups[2].Value;
          action = "autorescue fail";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat209).Success)
        {
          attacker = Regex.Match(line, pat209).Groups[1].Value;
          victim = Regex.Match(line, pat209).Groups[2].Value;
          action = "charge bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
          //***********************************************************************************
          //* Need to properly deduct charge bash
          //***********************************************************************************
        }
        if (Regex.Match(line, pat270).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat270).Groups[1].Value;
          action = "charge miss";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat271).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat271).Groups[1].Value;
          action = "charge bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat272).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat272).Groups[2].Value;
          action = "charge bash";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat273).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat273).Groups[1].Value;
          action = "charge";
          this.insertAction(hash, roundCount, attacker, action, victim, "barbarian");
        }
        if (Regex.Match(line, pat210).Success)
        {
          attacker = Regex.Match(line, pat210).Groups[1].Value;
          victim = string.Empty;
          action = "frenzy";
          this.insertAction(hash, roundCount, attacker, action, victim, "shaman");
        }
        if (Regex.Match(line, pat211).Success)
        {
          attacker = caster;
          victim = Regex.Match(line, pat211).Groups[1].Value;
          action = "regenerate";
          this.insertAction(hash, roundCount, attacker, action, victim, "shaman");
        }
        if (Regex.Match(line, pat212).Success)
        {
          attacker = Regex.Match(line, pat212).Groups[1].Value;
          victim = Regex.Match(line, pat212).Groups[3].Value;
          action = "vile spirits";
          this.insertAction(hash, roundCount, attacker, action, victim, "shaman");
        }
        if (Regex.Match(line, pat213).Success)
        {
          attacker = Regex.Match(line, pat213).Groups[1].Value;
          victim = pov;
          action = "punch";
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
        }
        if (Regex.Match(line, pat214).Success)
        {
          attacker = caster;// Regex.Match(line, pat214).Groups[1].Value;
          //caster = attacker;
          victim = string.Empty;// Regex.Match(line, pat112).Groups[2].Value;
          action = "resist";
          this.insertAction(hash, roundCount, attacker, action, victim, "druid");
        }

        if (Regex.Match(line, pat280).Success)
        {
          attacker = Regex.Match(line, pat280).Groups[1].Value;
          victim = string.Empty;
          action = "crippling wave";
          this.insertAction(hash, roundCount, attacker, action, victim, "red robe");
        }
        if (Regex.Match(line, pat281).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "cleanse";
          this.insertAction(hash, roundCount, attacker, action, victim, "white robe");
        }
        if (Regex.Match(line, pat282).Success)
        {
          attacker = caster;
          victim = string.Empty;
          action = "free action";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat283).Success)
        {
          attacker = Regex.Match(line, pat283).Groups[1].Value;
          victim = Regex.Match(line, pat283).Groups[2].Value;
          action = "strike";
          this.insertAction(hash, roundCount, attacker, action, victim, "paladin");
        }
        if (Regex.Match(line, pat284).Success)
        {
          attacker = Regex.Match(line, pat284).Groups[1].Value;
          victim = pov;
          action = "retaliate";
          this.insertAction(hash, roundCount, attacker, action, victim, "thief");
        }
        if (Regex.Match(line, pat257).Success)
        {
          attacker = Regex.Match(line, pat257).Groups[3].Value;
          victim = Regex.Match(line, pat257).Groups[1].Value;
          if (Regex.Match(line, pat257).Groups[4].Value == "bash")
          {
            action = Regex.Match(line, pat257).Groups[4].Value + " dodge";
          }
          this.insertAction(hash, roundCount, attacker, action, victim, "warrior");
          this.insertAction(hash, roundCount, victim, "dodged " + Regex.Match(line, pat257).Groups[4].Value, attacker, "thief");
        }
        if (Regex.Match(line, pat500).Success)
        {
          attacker = Regex.Match(line, pat500).Groups[1].Value;
          victim = Regex.Match(line, pat500).Groups[2].Value;
          action = "aim";
          this.insertAction(hash, roundCount, attacker, action, victim, "sky scout");
        }
        if (Regex.Match(line, pat501).Success)
        {
          if (Regex.Match(line, pat501).Groups[1].Value.Equals("You"))
          {
            attacker = pov;
          }
          else
          {
            attacker = Regex.Match(line, pat501).Groups[1].Value;
          }
          victim = Regex.Match(line, pat501).Groups[2].Value;
          action = "volley";
          this.insertAction(hash, roundCount, attacker, action, victim, "sky scout");
        }
        if (Regex.Match(line, pat502).Success)
        {
          attacker = Regex.Match(line, pat502).Groups[1].Value;
          victim = Regex.Match(line, pat502).Groups[2].Value;
          action = "volley";
          this.insertAction(hash, roundCount, attacker, action, victim, "sky scout");
        }
        if (Regex.Match(line, pat503).Success)
        {
          attacker = Regex.Match(line, pat503).Groups[1].Value;
          victim = string.Empty;
          action = "bolster";
          this.insertAction(hash, roundCount, attacker, action, victim, "sky scout");
        }
        if (Regex.Match(line, pat504).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat504).Groups[1].Value;
          action = "aim";
          this.insertAction(hash, roundCount, attacker, action, victim, "sky scout");
        }/*
        if (Regex.Match(line, pat505).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat505).Groups[1].Value;
          action = "volley";
          this.insertAction(hash, roundCount, attacker, action, victim, "sky scout");
        }*/
        if (Regex.Match(line, pat506).Success)
        {
          attacker = pov;
          victim = Regex.Match(line, pat506).Groups[1].Value;
          action = "volley";
          this.insertAction(hash, roundCount, attacker, action, victim, "sky scout");
        }
        if (Regex.Match(line, pat507).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "bolster";
          this.insertAction(hash, roundCount, attacker, action, victim, "sky scout");
        }

        /*
        string pat500 = @"([A-Z][a-z]+) takes aim at ([A-Z][a-z]+)\.$";
        string pat501 = @"([A-Z][a-z]+) [a-z]+ ([A-Z][a-z]+) with a plunk\.$";
        string pat502 = @"([A-Z][a-z]+) plunks ([A-Z][a-z]+) ([a-z ]+)\.$";
        string pat503 = @"([A-Z][a-z]+) bolsters (his|her) friends\.$";
         * */

        //** Disables **********************************************************************
        if (Regex.Match(line, pat300).Success)
        {
          attacker = Regex.Match(line, pat300).Groups[1].Value;
          victim = string.Empty;
          action = "tentacled";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat301).Success)
        {
          attacker = Regex.Match(line, pat301).Groups[1].Value;
          victim = string.Empty;
          action = "R.I.P.";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat302).Success)
        {
          attacker = Regex.Match(line, pat302).Groups[1].Value;
          victim = string.Empty;
          action = "stunned";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }

        if (Regex.Match(line, pat303).Success)
        {
          attacker = Regex.Match(line, pat303).Groups[1].Value;
          victim = string.Empty;
          action = "tentacled";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat304).Success)
        {
          attacker = Regex.Match(line, pat304).Groups[1].Value;
          victim = string.Empty;
          action = "stunned";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat305).Success)
        {
          attacker = Regex.Match(line, pat305).Groups[1].Value;
          victim = string.Empty;
          action = "paralyzed";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }
        if (Regex.Match(line, pat306).Success)
        {
          attacker = pov;
          victim = string.Empty;
          action = "paralyzed";
          this.insertAction(hash, roundCount, attacker, action, victim, string.Empty);
        }

        if (Regex.Match(line, promptLine).Success)
        {
          if (combatLines >= 2)
          {
            roundCount++;
          }
          combatLines = 0; //reset combatLines now that we encountered prompt          
        }
      }
      sr.Close();
      sr = null;
      //roundCount--; //need to subtract 1

      CreateReport(roundCount, hash, sw);

      Process.Start(fullPath);
      sw.Close();
      sw = null;
    }

    public void CreateReport(int roundcount, Hashtable hash, StreamWriter sw)
    {
      ArrayList summaryList = new ArrayList();
      sw.WriteLine("Oligo San's Log Analyzer v" + Program.version + "\r\n");
      sw.WriteLine("Total combat rounds: " + roundcount + "\n");
      IDictionaryEnumerator denum = hash.GetEnumerator();
      DictionaryEntry dentry;
      ArrayList arr = null;
      int actionedRounds = 0;
      int lastRound = 0;
      int adjDenom = 0;
      int bashedCount = 0;
      int lastBashedRound = 0;
      int deathRound = roundcount;
      bool isGone = false;

      // the below variable is to help determine whether a char should get credit for action in a round
      // it is needed if there are multiple actions in one round, some of which are not action worthy
      bool isRoundActionCredit = false;  
      PKAction tmp = null;
      while (denum.MoveNext()) //go thru all characters in the Hashtable
      {
        deathRound = roundcount;
        dentry = (DictionaryEntry)denum.Current;
        //********************************************
        //** Reset some variables for new chars
        //********************************************
        isRoundActionCredit = false;
        isGone = false;
        actionedRounds = 0;
        bashedCount = 0;
        lastRound = 0;
        adjDenom = roundcount;
        lastBashedRound = 0;
        //********************************************
        sw.WriteLine("\r\n[" + dentry.Key + "]");

        arr = (ArrayList)dentry.Value;  //cast the HashTable value to an ArrayList, this is the chain of actions for 1 char
        
        if (arr != null && arr.Count > 0)
        {
          for (int i = 0; i < arr.Count; i++)
          {
            tmp = (PKAction)arr[i];
            if (tmp.action.Equals("bash")
              || tmp.action.Equals("bash miss")
              || tmp.action.Equals("backstab")
              || tmp.action.Equals("backstab miss")
              || tmp.action.Equals("charge"))
            {
              adjDenom--;
              isGone = false;
            }
            if (tmp.round > lastRound)   //brand new round
            {
              if (isRoundActionCredit)
              {
                actionedRounds++; //need to also remember to do if last round since iterator i won't advance to this again if last round
              }
              isRoundActionCredit = false; //reset action credit
              lastRound = tmp.round;
              if (tmp.victim.Trim().Length > 0)
              {
                if (tmp.round < 10)
                {
                  //Double space after Round
                  sw.Write("\r\nRound  " + tmp.round + " : " + tmp.action + MakeSpace(15 - tmp.action.Length) + " -> " + tmp.victim + MakeSpace(17 - tmp.victim.Length));
                }
                else
                {
                  //Single space after Round
                  sw.Write("\r\nRound " + tmp.round + " : " + tmp.action + MakeSpace(15 - tmp.action.Length) + " -> " + tmp.victim + MakeSpace(17 - tmp.victim.Length));
                }
              }
              else
              {
                if (tmp.round < 10)
                {
                  //Double space after Round
                  sw.Write("\r\nRound  " + tmp.round + " : " + tmp.action);
                }
                else
                {
                  //Single space after Round
                  sw.Write("\r\nRound " + tmp.round + " : " + tmp.action);
                }
              }
              if (!tmp.action.Equals("R.I.P.")
                && !tmp.action.Equals("paralyzed")
                && !tmp.action.Equals("stunned")
                && !tmp.action.Equals("bashed by")
                && !tmp.action.Equals("tentacled")
                && !tmp.action.Equals("charged by")
                && !tmp.action.Equals("arrives")
                && !tmp.action.Equals("leaves")
                && !tmp.action.Equals("teleport")
                && !tmp.action.Equals("recalls"))
              {
                isRoundActionCredit = true;
                isGone = false;
              }
              else
              {
                switch (tmp.action)
                {
                  case "R.I.P.":
                    deathRound = tmp.round;
                    break;
                  case "arrives":
                    isGone = false;
                    break;
                  case "bashed by":
                    lastBashedRound = tmp.round;
                    bashedCount += 1;
                    isGone = false;
                    break;
                  case "charged by":
                    lastBashedRound = tmp.round;
                    bashedCount += 1;
                    isGone = false;
                    break;
                  case "tentacled":
                    adjDenom -= 1;
                    isGone = false;
                    break;
                  case "teleport":
                  case "recalls":
                  case "leaves":
                    isGone = true; //don't do denominator adjustment til end of round                    
                    break;
                  case "stunned":
                  case "paralyzed":
                    adjDenom -= 1;
                    isGone = false;
                    break;
                }
              }
            }
            else   //more than one action in same round, ie. punch/bash
            {
              if (!tmp.action.Equals("R.I.P.")
                && !tmp.action.Equals("stunned")
                && !tmp.action.Equals("paralyzed")
                && !tmp.action.Equals("bashed by")
                && !tmp.action.Equals("tentacled")
                && !tmp.action.Equals("charged by")
                && !tmp.action.Equals("arrives")
                && !tmp.action.Equals("leaves")
                && !tmp.action.Equals("recalls")
                && !tmp.action.Equals("teleport"))
              {
                isRoundActionCredit = true;
                isGone = false;
              }
              //check disables in 2nd half of round
              else if (tmp.action.Equals("tentacled") || tmp.action.Equals("stunned") 
                || tmp.action.Equals("bashed by") || tmp.action.Equals("charged by"))
              {
                // ********* NOTE: TODO: HAVE NOT CODED R.I.P. case if occurred later in same round ***************
                switch (tmp.action)
                {
                  case "tentacled":
                    adjDenom -= 1;
                    isGone = false;
                    break;
                  case "stunned":
                    adjDenom -= 1;
                    isGone = false;
                    break;
                  case "bashed by":
                    isGone = false;
                    if (tmp.round > lastBashedRound)
                    {
                      bashedCount += 1;
                      lastBashedRound = tmp.round;
                    }
                    break;
                  case "charged by":
                    isGone = false;
                    if (tmp.round > lastBashedRound)
                    {
                      bashedCount += 1;
                      lastBashedRound = tmp.round;
                    }
                    break;
                }
              } //endif check of disables
              else if (tmp.action.Equals("teleport") || tmp.action.Equals("recall") || tmp.action.Equals("leaves"))
              {
                isGone = true;
              } //end check if char left battle field
              else if (tmp.action.Equals("arrives"))
              {
                isGone = false;
              }
              //**** WHY IN THE WORLD DO I HAVE SPECIAL CASE FOR CLERICS?!!!!! ***
              if (tmp.victim.Trim().Length > 0 && !tmp.charclass.Equals("cleric"))
              {
                sw.Write("\n" + MakeSpace(11) + tmp.action + MakeSpace(15 - tmp.action.Length) + " -> " + tmp.victim);
              }
              else if (!tmp.charclass.Equals("cleric"))
              {
                sw.Write("\n" + MakeSpace(11) + tmp.action);
              }
              else if (tmp.action.Equals("R.I.P."))
              {
                deathRound = tmp.round;
              }
            } //end of else clause where character does multiple actions in 1 round

            // This logic for when char does positive action but no more subsequent actions in array so loop quits and he doesn't get proper credit
            if (isRoundActionCredit && (i == (arr.Count - 1)))
            {
              actionedRounds++;
            }

            //if (isGone && isRoundActionCredit == false) //need to make adjustments to denominator to determine how many rounds he's gone
            if (isGone) //need to make adjustments to denominator to determine how many rounds he's gone
            {
              if (i == (arr.Count - 1)) //basically, user has left battle field and no more actions
              {
                if (tmp.round < roundcount) //the user has left battlefield and no more actions, so adjust appropriate # of rounds
                {
                  adjDenom -= (roundcount - tmp.round);
                }
              }
              else //user has left battle field, but has more actions so he may return, need to determine at what round he returns to adjust 
              {
                if (tmp.attacker.Equals("Bryce"))
                {
                  Console.WriteLine("blah");
                }
                if (((PKAction)arr[i + 1]).round > tmp.round)
                {
                  adjDenom -= (((PKAction)arr[i + 1]).round - tmp.round);
                  //then do adjustments, otherwise, ignore
                }
              }              
            }
          }  //end of for loop for actions for 1 char
          sw.WriteLine("\r\n-------------------------------------------------------");
          if (deathRound < roundcount)
          {
            adjDenom -= (roundcount - deathRound);
          }
          if (bashedCount > 0)
          {
            adjDenom -= (bashedCount * 2);
          }
          if (adjDenom <= 0)
          {
            adjDenom = 1;
          }
          sw.WriteLine(dentry.Key + " Adj. Efficiency: " + ((float)actionedRounds / adjDenom) * 100 + "% (" + actionedRounds + "/" + adjDenom + ")");
          if ((float)actionedRounds / adjDenom > 1)
          {
            summaryList.Add(new EfficiencyObj((string)dentry.Key
                          , 1.0f
                          , string.Empty)
                          );
          }
          else
          {
            if (adjDenom <= 0)
            {
              adjDenom = 1;
            }
            summaryList.Add(new EfficiencyObj((string)dentry.Key
                          , (float)actionedRounds / adjDenom
                          , string.Empty)
                          );
          }

        }
      }
      if (summaryList != null && summaryList.Count > 0)
      {
        CreateEfficiencyReport(summaryList, hash, sw);
        CreateBattleReport(summaryList, hash, roundcount,sw);
      }
    }

    public void CreateBattleReport(ArrayList summaryList, Hashtable hash, int roundcount, StreamWriter sw)
    {
      ArrayList combatArr = null;
      sw.WriteLine("-------------------------------------------------------");
      PKAction tmp = null;
      bool isDead = false;
      bool isGone = false;
      bool isBashed = false;
      bool isBashLag = false;
      bool isDisabled = false;
      bool isAction = false;
      int maxActionRound = 0;
      //loop thru the summaryList, this is sorted by Efficiency DESC
      for (int i = 0; i < summaryList.Count; i++)
      {
        sw.Write(((EfficiencyObj)summaryList[i]).charname + MakeSpace(18 - ((EfficiencyObj)summaryList[i]).charname.Length));
        if (((EfficiencyObj)summaryList[i]).charname.Equals("Bryce"))
        {
          string a = "its Bryce";
        }
        combatArr = (ArrayList)hash[((EfficiencyObj)summaryList[i]).charname];
        isBashed = false;
        isBashLag = false;
        isDead = false;
        isGone = false;
        isDisabled = false;
        maxActionRound = 0;
        if (combatArr.Count > 0)
        {
          maxActionRound = ((PKAction)combatArr[combatArr.Count - 1]).round;
        }
        for (int j = 1; j <= roundcount; j++)  //This loop will do exactly the # of combat rounds, and test inaction
        {
          isAction = false;  // <-- describe exactly how and what this variable is for?
          for (int k = 0; k < combatArr.Count; k++) //Go through the ArrayList of actions for a character
          {
            if (((PKAction)combatArr[k]).round == j)
            {
              tmp = (PKAction)combatArr[k];
              switch (tmp.action)
              {
                case "R.I.P.":
                  isDead = true;
                  isGone = true;
                  if (!isAction)
                  {
                    sw.Write("X");
                  }
                  break;
                case "teleport":
                   isGone = true;
                  if (!isAction && !isDead)
                  {
                    sw.Write("+");
                  }
                  break;
                case "recalls":
                case "leaves":
                  isGone = true;
                  if (!isAction && !isDead)
                  {
                    sw.Write("X");
                  }
                  break;
                case "arrives":
                  isGone = false;
                  break;
                case "stunned":
                  isDisabled = true;
                  isGone = false;
                  if (!isAction)
                  {
                    sw.Write("x");
                  }
                  break;
                case "bashed by":
                  isBashed = true;
                  isGone = false;
                  if (!isAction)
                  {
                    sw.Write("x");
                  }
                  break;
                case "charged by":
                  isBashed = true;
                  isGone = false;
                  if (!isAction)
                  {
                    sw.Write("x");
                  }
                  break;
                case "tentacled":
                  isDisabled = true;
                  isGone = false;
                  if (!isAction)
                  {
                    sw.Write("x");
                  }
                  break;
                default:
                  isBashed = false;
                  isDisabled = false;
                  isGone = false;
                  if (tmp.action.Equals("bash") || tmp.action.Equals("bash missed") || tmp.action.Equals("charge"))
                  {                    
                    isBashLag = true;
                  }
                  if (!isAction)
                  {
                    sw.Write("+");
                  }
                  break;
              }
              if (!tmp.action.Equals("arrives"))  //give action credit for all but Arrival
              {
                isAction = true;
              }
              if (k + 1 < combatArr.Count && ((PKAction)combatArr[k + 1]).round > j)
              {
                break; //don't break if positive action, may have gotten disabled/bashed later same round
              }
            }
            else if (((PKAction)combatArr[k]).round > j)
            {
              isAction = true;
              if (isBashLag)
              {
                isBashLag = false;
                sw.Write("x");
              }
              else if (isBashed)
              {
                isBashed = false;
                sw.Write("x");
              }
              else if (isGone)
              {
                sw.Write("X");
              }
              else
              {
                sw.Write("-");
              }
              break;
            }
          }  //end k arraylist loop
          if (!isAction)
          {
            if (isDead)
            {
              sw.Write("X");
            }
            else if (isGone && !isDead)
            {
              sw.Write("X");
            }
            else if (j > maxActionRound && (isBashLag || isBashed))
            {
              sw.Write("x");
              isBashLag = false;
              isBashed = false;
            }
            else if (isDisabled)
            {
              sw.Write("x");
            }
            else
            {
              sw.Write("-");
            }

          }
        }  //end j round loop - this ends the # of combat rounds loop
        sw.Write("  " + ((EfficiencyObj)summaryList[i]).efficiency * 100 + "%");
        sw.Write("\r\n");
      }
      sw.WriteLine("\r\n + acted");
      sw.WriteLine(" - not acted");
      sw.WriteLine(" x disabled (cannot act)");
      sw.WriteLine(" X not present (or dead)");
    }

    public void CreateEfficiencyReport(ArrayList arr, Hashtable hash, StreamWriter sw)
    {
      string charclass = string.Empty;
      sw.WriteLine("\r\n-------------------------------------------------------");
      sw.WriteLine("Character (" + arr.Count + ")" + MakeSpace(18 - "Character ()".Length - arr.Count.ToString().Length) + "Adj. Efficiency" + MakeSpace(4) + "Class");
      sw.WriteLine("-------------------------------------------------------");
      IComparer sorter = new SortClass();
      arr.Sort(sorter);
      for (int i = 0; i < arr.Count; i++)
      {
        charclass = string.Empty;
        charclass = GetCharClass(hash, ((EfficiencyObj)arr[i]).charname);
        sw.WriteLine(((EfficiencyObj)arr[i]).charname
                          + MakeSpace(18 - ((EfficiencyObj)arr[i]).charname.Length)
                          + ((EfficiencyObj)arr[i]).efficiency * 100 + "%"
                          + MakeSpace(18 - (((EfficiencyObj)arr[i]).efficiency * 100).ToString().Length)
                          + charclass);

      }
    }

    public string GetCharClass(Hashtable hash, string charname)
    {
      ArrayList arr = null;
      string retvalue = string.Empty;
      arr = (ArrayList)hash[charname];
      if (arr != null && arr.Count > 0)
      {
        for (int i = 0; i < arr.Count; i++)
        {
          if (((PKAction)arr[i]).charclass != null && ((PKAction)arr[i]).charclass.Length > 0)
          {
            retvalue = ((PKAction)arr[i]).charclass;
            break;
          }
        }
      }
      return retvalue;
    }
  }
}

