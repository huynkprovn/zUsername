#region
using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using SharpDX;
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
        private static Obj_AI_Hero myHero;
        private static bool UseShield;
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
        	myHero = ObjectManager.Player;
           	if (myHero.ChampionName != "Ryze") return;
           	
       		Q = new Spell(SpellSlot.Q, 625);
			W = new Spell(SpellSlot.W, 600);
			E = new Spell(SpellSlot.E, 600);
			R = new Spell(SpellSlot.R);
			IgniteSlot = myHero.GetSpellSlot("SummonerDot");  
			
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
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Harass").AddItem(new MenuItem("HQ", "Use Q").SetValue(true));
			Config.SubMenu("Harass").AddItem(new MenuItem("HW", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HE", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
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
            Config.SubMenu("Extra").AddItem(new MenuItem("UseSera", "Use Seraphs Embrace").SetValue(true));
            Config.SubMenu("Extra").AddItem(new MenuItem("HP", "When % HP").SetValue(new Slider(20,100,0)));
            Config.SubMenu("Extra").AddItem(new MenuItem("UseWGap", "Use W GapCloser").SetValue(true));
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
        	target = SimpleTs.GetTarget(Q.Range+25, SimpleTs.DamageType.Magical);
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
			if (Config.Item("UseSera").GetValue<bool>()) UseItems();
        }
        
        private static void Drawing_OnDraw(EventArgs args)
        {
        	var drawQ = Config.Item("QRange").GetValue<Circle>();
            if (drawQ.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, Q.Range, drawQ.Color);
            }

            var drawWE = Config.Item("WERange").GetValue<Circle>();
            if (drawWE.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, W.Range, drawWE.Color);
            }
        }
        
        private static void OrbwalkingOnBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
		{
			if (Config.Item("ComboActive").GetValue<KeyBind>().Active || Config.Item("HarassActive").GetValue<KeyBind>().Active)
				args.Process = !(Q.IsReady() || W.IsReady() || E.IsReady() || myHero.Distance(args.Target) >= 600);
		}
        
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
        	var UseW = Config.Item("UseWGap").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (myHero.HasBuff("Recall") || myHero.IsWindingUp) return;  
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
        	if (sender.IsEnemy && (sender.Type == GameObjectType.obj_AI_Hero || sender.Type == GameObjectType.obj_AI_Turret))
        	{
        		if (SpellData.SpellName.Any(Each => Each.Contains(args.SData.Name)) || (args.Target == myHero && myHero.Distance(sender) <= 700))
        			UseShield = true;
        	}
        }
        
        private static bool IsFacing(Obj_AI_Base enemy)
        {
        	if (enemy.Path.Count() > 0 && enemy.Path[0].Distance(myHero.ServerPosition) > myHero.Distance(enemy))
        		return false;
        	else return true;	        	
        }
        
       	public static bool IgniteKillable(Obj_AI_Hero source, Obj_AI_Base target)
       	{
       		return Damage.GetSummonerSpellDamage(myHero, target,Damage.SummonerSpell.Ignite) > target.Health;
       	}
       
       	public static bool IsKillable(Obj_AI_Hero source, Obj_AI_Base target, IEnumerable<SpellSlot> spellCombo)
       	{
       		return Damage.GetComboDamage(source, target, spellCombo) > target.Health;
       	}
       	
       	public static float GetDistanceSqr(Obj_AI_Hero source, Obj_AI_Base target)
       	{
       		return Vector2.DistanceSquared(source.Position.To2D(),target.ServerPosition.To2D());
       	}
       	
       	private static void UseItems()
       	{
       		var myHP = myHero.Health/myHero.MaxHealth*100;
       		var ConfigHP = Config.Item("HP").GetValue<Slider>().Value;
       		if (myHP <= ConfigHP && Items.HasItem(3040) && Items.CanUseItem(3040) && UseShield) 
       		{
       			Items.UseItem(3040);
       			UseShield = false;
       		}
       	}
        
       	private static void ComboMixed()
        {
        	var UseR = Config.Item("UseR").GetValue<bool>();
        	var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	if (target == null) 
        	{
        		return;
        	}
        	if (UseIgnite && IgniteKillable(myHero,target))
        	{
        		if (IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && myHero.Distance(target) <= 600)
					myHero.SummonerSpellbook.CastSpell(IgniteSlot, target);        			
        	}
        	if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target,UsePacket);
        	else
        	{
        		if (IsKillable(myHero,target, new[] {SpellSlot.Q}) && Q.IsReady())
        		{
        		  	Q.CastOnUnit(target,UsePacket);
        		}
        		else if (IsKillable(myHero,target, new[] {SpellSlot.E}) && E.IsReady())
        		{
        		  	E.CastOnUnit(target,UsePacket);       	
        		}
        		else if (IsKillable(myHero,target, new[] {SpellSlot.W}) && W.IsReady())
        		{
        		    W.CastOnUnit(target,UsePacket);                   	
        		}
        		else if (myHero.Distance(target) >= 575 && !IsFacing(target) && W.IsReady())
        		{
        		    W.CastOnUnit(target,UsePacket);    
        		}
				else
				{
					if (Q.IsReady() && W.IsReady() && E.IsReady() && IsKillable(myHero,target,new[] {SpellSlot.Q,SpellSlot.W,SpellSlot.E}))
					{
						if (Q.IsReady()) Q.CastOnUnit(target,UsePacket);
						if (R.IsReady() && UseR) CastR();
						if (W.IsReady()) W.CastOnUnit(target,UsePacket);
						if (E.IsReady()) E.CastOnUnit(target,UsePacket);
					}
					else if (Math.Abs(myHero.PercentCooldownMod) >= 0.2)
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
        	if (UseIgnite && IgniteKillable(myHero,target))
        	{
        		if (IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && myHero.Distance(target) <= 600)
					myHero.SummonerSpellbook.CastSpell(IgniteSlot, target);        			
        	}
        	if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target ,UsePacket);
        	else
        	{
        		if (IsKillable(myHero,target, new[] {SpellSlot.Q}) && Q.IsReady())
        		{
        		  	Q.CastOnUnit(target,UsePacket);
        		}
        		else if (IsKillable(myHero,target, new[] {SpellSlot.E}) && E.IsReady())
        		{
        		  	E.CastOnUnit(target,UsePacket);       	
        		}
        		else if (IsKillable(myHero,target, new[] {SpellSlot.W}) && W.IsReady())
        		{
        		    W.CastOnUnit(target,UsePacket);     		    
        		}
        		else if (myHero.Distance(target) >= 575 && !IsFacing(target) && W.IsReady())
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
        	if (UseIgnite && IgniteKillable(myHero,target))
        	{
        		if (IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && myHero.Distance(target) <= 600)
					myHero.SummonerSpellbook.CastSpell(IgniteSlot, target);        				
        	}
        	if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target ,UsePacket);
        	else
        	{
        		if (IsKillable(myHero,target, new[] {SpellSlot.Q}) && Q.IsReady())
        		{
        		  	Q.CastOnUnit(target,UsePacket);
        		}
        		else if (IsKillable(myHero,target, new[] {SpellSlot.E}) && E.IsReady())
        		{
        		  	E.CastOnUnit(target,UsePacket);       	
        		}
        		else if (IsKillable(myHero,target, new[] {SpellSlot.W}) && W.IsReady())
        		{
        		    W.CastOnUnit(target,UsePacket);                   	
        		}
        		else if (myHero.Distance(target) >= 575 && !IsFacing(target) && W.IsReady())
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
        	if (myHero.Distance(target) <= 625 )
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
        	var allMinions = MinionManager.GetMinions(myHero.ServerPosition, Q.Range,MinionTypes.All,MinionTeam.All, MinionOrderTypes.MaxHealth);
        	if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
        	{
        		if (UseQ && Q.IsReady())
				{
        			foreach (var minion in allMinions)
					{
						if (minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion,(int)(myHero.Distance(minion) * 1000 / 1400)) <=
        				    Damage.GetComboDamage(myHero,minion,new[] {SpellSlot.Q}))
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
						if (minion.IsValidTarget(W.Range) && minion.Health < Damage.GetComboDamage(myHero,minion,new[] {SpellSlot.W}))
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
						if (minion.IsValidTarget(E.Range) && HealthPrediction.GetHealthPrediction(minion,(int)(myHero.Distance(minion) * 1000 / 1000)) <=
																	Damage.GetComboDamage(myHero,minion,new[] {SpellSlot.E}))
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
        	var jungminions = MinionManager.GetMinions(myHero.ServerPosition, Q.Range,MinionTypes.All,MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
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
        	if (AutoIgnite && IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
        	{
        		foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => myHero.Distance(enemy) <= 600 && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead))
        		{
        			if (IgniteKillable(myHero,enemy))
						myHero.SummonerSpellbook.CastSpell(IgniteSlot, enemy);        				
        		}
        	}
        	if (KillSteal & (Q.IsReady() || W.IsReady() || E.IsReady()))
        	{
        		foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => myHero.Distance(enemy) <= Q.Range && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead))
        		{
        			if (Q.IsReady() && IsKillable(myHero,enemy, new[] {SpellSlot.Q})) Q.CastOnUnit(enemy,UsePacket);
        			if (W.IsReady() && IsKillable(myHero,enemy, new[] {SpellSlot.W})) W.CastOnUnit(enemy,UsePacket);
        			if (E.IsReady() && IsKillable(myHero,enemy, new[] {SpellSlot.E})) E.CastOnUnit(enemy,UsePacket);
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
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => myHero.Distance3D(enemy,true) <= range*range && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead)) 
            {
        		count = count + 1 ;
            }
            return count;
        }
    }
}
