#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace GarenUlt
{
    internal class Program
    {
    	private static Orbwalking.Orbwalker Orbwalker;
        private static Menu Config;
        private static Obj_AI_Hero myHero;
		private static Spell R;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            myHero = ObjectManager.Player;
           	if (myHero.ChampionName != "Garen") return;
           	
            R = new Spell(SpellSlot.R, 400);
         							
			Config = new Menu("Garen Ult", "Garen Ult", true);
			
			Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
			Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
			
			Config.AddSubMenu(new Menu("Ultimate", "Ultimate"));
			Config.SubMenu("Ultimate").AddItem(new MenuItem("HP", "Auto R if %HP").SetValue(new Slider(10,100,0)));
            
            Game.PrintChat("Garen Ult loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
        }
       
        private static void Game_OnGameUpdate(EventArgs args)
        {
			AutoR();
        }
               
        private static void AutoR()
        {
        	var HP = Config.Item("HP").GetValue<Slider>().Value;
        	foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && enemy.IsValidTarget(450)))
        	{
        		if (myHero.Distance(enemy) <= 400)
        		{
        			var TargetHP = enemy.Health/enemy.MaxHealth*100;
        			var DmgUlt = Damage.GetSpellDamage(myHero,enemy,SpellSlot.R);
        			if (TargetHP <= HP || enemy.Health < DmgUlt)
        				if (!enemy.HasBuff("JudicatorIntervention") || !enemy.HasBuff("Undying Rage"))
        					R.Cast(enemy,true);
        		}      
        	}
        }
    }
}