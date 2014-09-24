#region
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace LightningRyze
{
    internal class Program
    {
    	private static Orbwalking.Orbwalker Orbwalker;
        private static Menu Config;
        private static string LastCast;
        private static float LastFlashTime;
        private static Obj_AI_Hero target;
       	private static Spell Q;
		private static Spell W;
		private static Spell E;
		private static Spell R;
		private static SpellSlot IgniteSlot;
		
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
          
        private static void Game_OnGameLoad(EventArgs args)
        {
           	if (ObjectManager.Player.ChampionName != "Ryze") return;
           	
       		Q = new Spell(SpellSlot.Q, 625);
			W = new Spell(SpellSlot.W, 600);
			E = new Spell(SpellSlot.E, 600);
			R = new Spell(SpellSlot.R);
			IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");  
			
			Config = new Menu("Lightning Ryze", "Lightning Ryze", true);
			var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
			SimpleTs.AddToMenu(targetSelectorMenu);
			Config.AddSubMenu(targetSelectorMenu);
			
			Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
			Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
			
			Config.AddSubMenu(new Menu("Combo", "Combo"));
			Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
			Config.SubMenu("Combo").AddItem(new MenuItem("TypeCombo", "").SetValue(new StringList(new[] {"Mixed mode","Burst combo","Long combo"},0)));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Harass").AddItem(new MenuItem("HQ", "Use Q").SetValue(true));
			Config.SubMenu("Harass").AddItem(new MenuItem("HW", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HE", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("FQ", "Use Q").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FW", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FE", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("JungleFarm").AddItem(new MenuItem("JQ", "Use Q").SetValue(true));
			Config.SubMenu("JungleFarm").AddItem(new MenuItem("JW", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JE", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal", "Use Kill Steal").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("AutoIgnite", "Use Ignite").SetValue(true));
            
            Config.AddSubMenu(new Menu("Extra", "Extra"));
            Config.SubMenu("Extra").AddItem(new MenuItem("UseWGap", "Use W GapClose").SetValue(true));
            Config.SubMenu("Extra").AddItem(new MenuItem("UsePacket", "Use Packet Cast").SetValue(true));
                      
			Config.AddSubMenu(new Menu("Drawings", "Drawings"));
			Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("WERange", "W+E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.AddToMainMenu();       
			
			Game.PrintChat("Lightning Ryze loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
			Orbwalking.BeforeAttack += OrbwalkingOnBeforeAttack;
			Drawing.OnDraw += Drawing_OnDraw;
			Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast; 	
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;			
        }
                          
        private static void Game_OnGameUpdate(EventArgs args)
        {         
        	target = SimpleTs.GetTarget(625, SimpleTs.DamageType.Magical);
			if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
			{
				if (Config.Item("TypeCombo").GetValue<StringList>().SelectedIndex == 0) ComboMixed();
				else if (Config.Item("TypeCombo").GetValue<StringList>().SelectedIndex == 1) ComboBurst();
				else if (Config.Item("TypeCombo").GetValue<StringList>().SelectedIndex == 2) ComboLong();
			}
			if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
			if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active ||
			    Config.Item("FreezeActive").GetValue<KeyBind>().Active) Farm();
			if (Config.Item("JungActive").GetValue<KeyBind>().Active) JungleFarm();
        }
        
        private static void Drawing_OnDraw(EventArgs args)
        {
        	var drawQ = Config.Item("QRange").GetValue<Circle>();
            if (drawQ.Active && !ObjectManager.Player.IsDead)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, drawQ.Color);
            }

            var drawWE = Config.Item("WERange").GetValue<Circle>();
            if (drawWE.Active && !ObjectManager.Player.IsDead)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, W.Range, drawWE.Color);
            }
        }
        
        private static void OrbwalkingOnBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
		{
			if (Config.Item("ComboActive").GetValue<KeyBind>().Active || Config.Item("HarassActive").GetValue<KeyBind>().Active)
				args.Process = !(Q.IsReady() || W.IsReady() || E.IsReady() || ObjectManager.Player.Distance(args.Target) >= 600);
		}
        
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
        	var UseW = Config.Item("UseWGap").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (ObjectManager.Player.HasBuff("Recall") || ObjectManager.Player.IsWindingUp) return;  
        	if (UseW && W.IsReady()) W.CastOnUnit(gapcloser.Sender,UsePacket);
        }
        
        
        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
        	if (sender.IsMe)
        	{
        		if (args.SData.Name.ToLower() == "overload")
				{
					LastCast = "Q";
				}
				else if (args.SData.Name.ToLower() == "runeprison")
				{
					LastCast = "W";
				}
				else if (args.SData.Name.ToLower() == "spellflux")
				{
					LastCast = "E";
				}
				else if (args.SData.Name.ToLower() == "desperatepower")
				{
					LastCast = "R";
				}
				else if (args.SData.Name.ToLower() == "summonerflash")
        		{
        			LastFlashTime = Environment.TickCount;
        		}
        	}	
        }
        
       	private static void ComboMixed()
        {
        	var UseR = Config.Item("UseR").GetValue<bool>();
        	var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	if (target == null) return;
        	if (UseIgnite && DamageLib.IsKillable(target, new[] {DamageLib.SpellType.Q,DamageLib.SpellType.E,DamageLib.SpellType.W,DamageLib.SpellType.IGNITE}))
        	{
        		if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && ObjectManager.Player.Distance(target) <= 600)
					ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);        			
        	}
        	if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target,UsePacket);
        	else
        	{
        		if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.Q}) && Q.IsReady())
        		{
        		  	Q.CastOnUnit(target,UsePacket);
        		}
        		else if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.E}) && E.IsReady())
        		{
        		  	E.CastOnUnit(target,UsePacket);       	
        		}
        		else if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.W}) && W.IsReady())
        		{
        		    W.CastOnUnit(target,UsePacket);                   	
        		}
        		else if (ObjectManager.Player.Distance(target) >= 575 && target.Path.Count() > 0 &&
													target.Path[0].Distance(ObjectManager.Player.ServerPosition) >
														ObjectManager.Player.Distance(target) && W.IsReady())
        		    W.CastOnUnit(target,UsePacket);    
				else
				{
					var comboDmg = DamageLib.CalcMagicDmg(((ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level * 25)) + (0.2 * ObjectManager.Player.FlatMagicDamageMod), target) * 2 +
						DamageLib.CalcMagicDmg(((ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level * 35)) + (0.2 * ObjectManager.Player.FlatMagicDamageMod), target) +
						DamageLib.CalcMagicDmg(((ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level * 20)) + (0.2 * ObjectManager.Player.FlatMagicDamageMod), target);
					if ((Q.IsReady() && W.IsReady() && E.IsReady() && comboDmg > target.Health) || comboDmg > target.MaxHealth)
					{
						if (Q.IsReady()) Q.CastOnUnit(target,UsePacket);
						if (R.IsReady() && UseR) CastR();
						if (W.IsReady()) W.CastOnUnit(target,UsePacket);
						if (E.IsReady()) E.CastOnUnit(target,UsePacket);
					}
					else if (Math.Abs(ObjectManager.Player.PercentCooldownMod) >= 0.2)
					{
						if (CountEnemyInRange(target,300) > 1)
						{
							if (LastCast == "Q")
							{
								if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
								if (R.IsReady() && UseR) CastR();
								if (!R.IsReady()) W.CastOnUnit(target ,UsePacket);
								if (!R.IsReady() && !W.IsReady()) E.CastOnUnit(target ,UsePacket);
							}
							else Q.CastOnUnit(target,UsePacket);
						}
						else
						{
							if (LastCast == "Q")
							{
								if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
								if (W.IsReady()) W.CastOnUnit(target ,UsePacket);
								if (!W.IsReady()) E.CastOnUnit(target ,UsePacket);
								if (!W.IsReady() && !E.IsReady() && UseR) CastR();
							}
							else 
							{
								if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
							}
						}
					}
					else
					{
						if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
						else if (R.IsReady() && UseR) CastR();
						else if (E.IsReady()) E.CastOnUnit(target ,UsePacket);
						else if (W.IsReady()) W.CastOnUnit(target ,UsePacket);
					}
				}
        	}
        }
        
        private static void ComboBurst()
        {
        	var UseR = Config.Item("UseR").GetValue<bool>();
        	var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	if (target == null) return;
        	if (UseIgnite && DamageLib.IsKillable(target, new[] {DamageLib.SpellType.Q,DamageLib.SpellType.E,DamageLib.SpellType.W,DamageLib.SpellType.IGNITE}))
        	{
        		if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && ObjectManager.Player.Distance(target) <= 600)
					ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);        			
        	}
        	if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target ,UsePacket);
        	else
        	{
        		if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.Q}) && Q.IsReady())
        		{
        		  	Q.CastOnUnit(target,UsePacket);
        		}
        		else if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.E}) && E.IsReady())
        		{
        		  	E.CastOnUnit(target,UsePacket);       	
        		}
        		else if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.W}) && W.IsReady())
        		{
        		    W.CastOnUnit(target,UsePacket);     		    
        		}
        		else if (ObjectManager.Player.Distance(target) >= 575 && target.Path.Count() > 0 &&
														target.Path[0].Distance(ObjectManager.Player.ServerPosition) >
														ObjectManager.Player.Distance(target) && W.IsReady())
        		    W.CastOnUnit(target,UsePacket);
        		else
        		{
        			if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
					else if (R.IsReady() && UseR) CastR();
					else if (E.IsReady()) E.CastOnUnit(target ,UsePacket);
					else if (W.IsReady()) W.CastOnUnit(target ,UsePacket);
        		}
        	}		
        }
        
        private static void ComboLong()
        {
        	var UseR = Config.Item("UseR").GetValue<bool>();
        	var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	if (target == null) return;
        	if (UseIgnite && DamageLib.IsKillable(target, new[] {DamageLib.SpellType.Q,DamageLib.SpellType.E,DamageLib.SpellType.W,DamageLib.SpellType.IGNITE}))
        	{
        		if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && ObjectManager.Player.Distance(target) <= 600)
					ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);        				
        	}
        	if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target ,UsePacket);
        	else
        	{
        		if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.Q}) && Q.IsReady())
        		{
        		  	Q.CastOnUnit(target,UsePacket);
        		}
        		else if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.E}) && E.IsReady())
        		{
        		  	E.CastOnUnit(target,UsePacket);       	
        		}
        		else if (DamageLib.IsKillable(target, new[] {DamageLib.SpellType.W}) && W.IsReady())
        		{
        		    W.CastOnUnit(target,UsePacket);                   	
        		}
        		else if (ObjectManager.Player.Distance(target) >= 575 && target.Path.Count() > 0 &&
														target.Path[0].Distance(ObjectManager.Player.ServerPosition) >
														ObjectManager.Player.Distance(target) && W.IsReady())
        		    W.CastOnUnit(target,UsePacket);
        		else
        		{
        			if (CountEnemyInRange(target,300) > 1)
					{
						if (LastCast == "Q")
						{
							if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
							if (R.IsReady() && UseR) CastR();
							if (!R.IsReady()) W.CastOnUnit(target ,UsePacket);
							if (!R.IsReady() && !W.IsReady()) E.CastOnUnit(target ,UsePacket);
						}
						else Q.CastOnUnit(target,UsePacket);
					}
        			else
        			{
        				if (LastCast == "Q")
        				{
        					if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
        					if (W.IsReady()) W.CastOnUnit(target ,UsePacket);
        					if (!W.IsReady()) E.CastOnUnit(target ,UsePacket);
        					if (!W.IsReady() && !E.IsReady() && R.IsReady() && UseR) CastR();
        				}
        				else
        				{
        					if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
        				}
        			}
        		}
        	}
        }
        
       	private static void Harass()
        {
        	var UseQ = Config.Item("HQ").GetValue<bool>();
        	var UseW = Config.Item("HW").GetValue<bool>();
        	var UseE = Config.Item("HE").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	if (ObjectManager.Player.Distance(target) <= 625 )
        	{
        		if (UseQ && Q.IsReady()) Q.CastOnUnit(target,UsePacket);
        		if (UseW && W.IsReady()) W.CastOnUnit(target,UsePacket);
        		if (UseE && E.IsReady()) E.CastOnUnit(target,UsePacket);
        	}     	
        }
        
        private static void Farm()
        {
        	var UseQ = Config.Item("FQ").GetValue<bool>();
        	var UseW = Config.Item("FW").GetValue<bool>();
        	var UseE = Config.Item("FE").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,MinionTypes.All,MinionTeam.All, MinionOrderTypes.MaxHealth);
        	if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
        	{
        		if (UseQ && Q.IsReady())
				{
        			foreach (var minion in allMinions)
					{
						if (minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion,(int)(ObjectManager.Player.Distance(minion) * 1000 / 1400)) <=
																						DamageLib.getDmg(minion, DamageLib.SpellType.Q) - 10)
						{
							Q.CastOnUnit(minion,UsePacket);
							return;
						}
					}
				}
        		else if (UseW && W.IsReady())
				{
					foreach (var minion in allMinions)
					{
						if (minion.IsValidTarget(W.Range) && minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.W) - 10)
						{
							W.CastOnUnit(minion,UsePacket);
							return;
						}
					}
				}
				else if (UseE && E.IsReady())
				{
					foreach (var minion in allMinions)
					{
						if (minion.IsValidTarget(E.Range) && HealthPrediction.GetHealthPrediction(minion,(int)(ObjectManager.Player.Distance(minion) * 1000 / 1000)) <=
																	DamageLib.getDmg(minion, DamageLib.SpellType.E) - 10)
						{
							E.CastOnUnit(minion,UsePacket);
							return;
						}
					}
				}
        	}
        	else if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
        	{
        		foreach (var minion in allMinions)
				{
        			if (UseQ && Q.IsReady()) Q.CastOnUnit(minion,UsePacket);			
					if (UseW && W.IsReady()) W.CastOnUnit(minion,UsePacket);		
					if (UseE && E.IsReady()) E.CastOnUnit(minion,UsePacket);					
				}
        	}
        }
        
        private static void JungleFarm()
        {
        	var UseQ = Config.Item("JQ").GetValue<bool>();
        	var UseW = Config.Item("JW").GetValue<bool>();
        	var UseE = Config.Item("JE").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	var jungminions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,MinionTypes.All,MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
			if (jungminions.Count > 0)
			{
				var minion = jungminions[0];
				if (UseQ && Q.IsReady()) Q.CastOnUnit(minion,UsePacket);
				if (UseW && W.IsReady()) W.CastOnUnit(minion,UsePacket);
				if (UseE && E.IsReady()) E.CastOnUnit(minion,UsePacket);
			}
        }
        
        private static void KillSteal()
        {
        	var AutoIgnite = Config.Item("AutoIgnite").GetValue<bool>();
        	var KillSteal = Config.Item("KillSteal").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	if (AutoIgnite && IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
        	{
        		foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => ObjectManager.Player.Distance(enemy) <= 600 && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead))
        		{
        			if (DamageLib.IsKillable(enemy, new[] {DamageLib.SpellType.IGNITE}))
						ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, enemy);        				
        		}
        	}
        	if (KillSteal & (Q.IsReady() || W.IsReady() || E.IsReady()))
        	{
        		foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => ObjectManager.Player.Distance(enemy) <= Q.Range && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead))
        		{
        			if (Q.IsReady() && DamageLib.IsKillable(enemy, new[] {DamageLib.SpellType.Q})) Q.CastOnUnit(enemy,UsePacket);
        			if (W.IsReady() && DamageLib.IsKillable(enemy, new[] {DamageLib.SpellType.W})) W.CastOnUnit(enemy,UsePacket);
        			if (E.IsReady() && DamageLib.IsKillable(enemy, new[] {DamageLib.SpellType.E})) E.CastOnUnit(enemy,UsePacket);
        		}
        	
        	}
        }
        
        private static void CastR()
        {
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	if (UsePacket) Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0,SpellSlot.R)).Send();
        	else R.Cast();
        }
        
        private static int CountEnemyInRange(Obj_AI_Hero target,float range)
        {
        	int count = 0;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => ObjectManager.Player.Distance3D(enemy,true) <= range*range && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead)) 
            {
        		count = count + 1 ;
            }
            return count;
        }
    }
}