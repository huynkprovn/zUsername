#region
using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
#endregion

namespace LightningLux
{
    internal class Program
    {
        private static Menu Config;
        private static Obj_AI_Hero target;
        private static Obj_AI_Hero Ally;
        private static Obj_AI_Hero myHero;
       	private static Spell Q;
		private static Spell W;
		private static Spell E;
		private static Spell R;
		private static SpellSlot IgniteSlot;
		private static GameObject EObject;
		private static HitChance HitC;
		
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
          
        private static void Game_OnGameLoad(EventArgs args)
        {
        	myHero = ObjectManager.Player;
           	if (myHero.ChampionName != "Lux") return;
           	
            Q = new Spell(SpellSlot.Q, 1175);
            W = new Spell(SpellSlot.W, 1075);
            E = new Spell(SpellSlot.E, 1100);
            R = new Spell(SpellSlot.R, 3340);

            Q.SetSkillshot(0.25f, 80f, 1200f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 150f, 1200f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.15f, 275f, 1300f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(1.35f, 190f, float.MaxValue, false, SkillshotType.SkillshotLine);
            
			IgniteSlot = myHero.GetSpellSlot("SummonerDot");  
			
			Config = new Menu("Lightning Lux", "Lightning Lux", true);
			var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
			SimpleTs.AddToMenu(targetSelectorMenu);
			Config.AddSubMenu(targetSelectorMenu);
			
            var orbwalkerMenu = new Menu("LX-Orbwalker", "LX-Orbwalker");
            LXOrbwalker.AddToMenu(orbwalkerMenu);
            Config.AddSubMenu(orbwalkerMenu);
			
			Config.AddSubMenu(new Menu("Combo", "Combo"));
			Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseE", "Use E").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R if Killable").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseItems", "Use Items").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Harass").AddItem(new MenuItem("HQ", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HE", "Use E").SetValue(true));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("JungSteal", "JungSteal!").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("FQ", "Use Q").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FW", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FE", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FMP", "My MP %").SetValue(new Slider(15,100,0)));
            
            Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseQ", "Use Q").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseE", "Use E").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseR", "Use R").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KIgnite", "Use Ignite").SetValue(true));
            
            Config.AddSubMenu(new Menu("AutoShield", "AutoShield"));
            Config.SubMenu("AutoShield").AddItem(new MenuItem("WAllies", "Auto W for Allies").SetValue(true));
            Config.SubMenu("AutoShield").AddItem(new MenuItem("AutoW", "Auto W when Lux is targeted").SetValue(true));
            Config.SubMenu("AutoShield").AddItem(new MenuItem("HP", "Allies HP %").SetValue(new Slider(60,100,0)));
            Config.SubMenu("AutoShield").AddItem(new MenuItem("MP", "My MP %").SetValue(new Slider(30,100,0)));
                    
            Config.AddSubMenu(new Menu("ExtraSettings", "ExtraSettings"));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UseQE", "Only E if target trapped").SetValue(false));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("AutoE2", "Auto pop E").SetValue(true));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UseQGap", "Q on GapCloser").SetValue(true));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("HitChance", "HitChance").SetValue(new StringList(new[] {"Low","Medium","High","Very High"},2)));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UsePacket", "Use Packet Cast").SetValue(true));
            
            Config.AddSubMenu(new Menu("UltSettings", "UltSettings"));
            Config.SubMenu("UltSettings").AddItem(new MenuItem("RHit", "Auto R if hit").SetValue(new StringList(new[] {"None","2 target","3 target","4 target","5 target"},1)));
            Config.SubMenu("UltSettings").AddItem(new MenuItem("RTrap", "Auto R if trapped").SetValue(false));
            
			Config.AddSubMenu(new Menu("Drawings", "Drawings"));
			Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));			
			Config.AddToMainMenu();       
			
			Game.PrintChat("Lightning Lux v1.1 loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
			LXOrbwalker.BeforeAttack += LXOrbwalker_BeforeAttack;
			Drawing.OnDraw += Drawing_OnDraw;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;	
			Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;  
			GameObject.OnCreate += OnCreateObject;
			GameObject.OnDelete += OnDeleteObject;			
        }
                          
        private static void Game_OnGameUpdate(EventArgs args)
        {         
        	KillSteal();
        	GrabAlly();
        	
        	target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
        	
			if (Config.Item("ComboActive").GetValue<KeyBind>().Active) UseCombo();				
			else if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
			else if (Config.Item("FarmActive").GetValue<KeyBind>().Active) Farm();
			else if (Config.Item("JungSteal").GetValue<KeyBind>().Active) JungSteal();
			
			if (Config.Item("WAllies").GetValue<bool>()) AutoShield();
			if (Config.Item("AutoE2").GetValue<bool>()) CastE2();
			if (Config.Item("RTrap").GetValue<bool>()) RTrapped();
			
			if (Config.Item("HitChance").GetValue<StringList>().SelectedIndex == 0) HitC = HitChance.Low;
			else if (Config.Item("HitChance").GetValue<StringList>().SelectedIndex == 1) HitC = HitChance.Medium;
			else if (Config.Item("HitChance").GetValue<StringList>().SelectedIndex == 2) HitC = HitChance.High;
			else if (Config.Item("HitChance").GetValue<StringList>().SelectedIndex == 3) HitC = HitChance.VeryHigh;
			
			var Count = 0;
			if (Config.Item("RHit").GetValue<StringList>().SelectedIndex == 0) Count = 0;
			else if (Config.Item("RHit").GetValue<StringList>().SelectedIndex == 1) Count = 2;
			else if (Config.Item("RHit").GetValue<StringList>().SelectedIndex == 2) Count = 3;
			else if (Config.Item("RHit").GetValue<StringList>().SelectedIndex == 3) Count = 4;
			else if (Config.Item("RHit").GetValue<StringList>().SelectedIndex == 4) Count = 5;
			
			if (Count >= 2) RHit(Count);
        }
        
        private static void OnCreateObject(GameObject sender, EventArgs args)
		{
        	if (sender.Name.Contains("LuxLightstrike_tar_green"))
				EObject = sender;
		}
        
        private static void OnDeleteObject(GameObject sender, EventArgs args)
		{
			if (sender.Name.Contains("LuxLightstrike_tar_green"))
				EObject = null;
		}	
        
        private static void GrabAlly()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(W.Range) && hero.IsAlly && !hero.IsDead))
            {
            	if (Ally == null) Ally = hero;
            	else if (hero.Health/hero.MaxHealth < Ally.Health/Ally.MaxHealth) Ally = hero;
            }
        }
        
        private static void RHit(int x)
        {
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	var rtarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
        	R.CastIfWillHit(rtarget,x,UsePacket);
        }
        
        private static void RTrapped()
        {
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Range) && !hero.IsDead && hero.HasBuff("LuxLightBindingMis")))
        		R.Cast(hero,UsePacket);
        }
        
        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
			var UseW = Config.Item("AutoW").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			var ComboActive = Config.Item("ComboActive").GetValue<KeyBind>().Active;
			var FarmActive = Config.Item("FarmActive").GetValue<KeyBind>().Active;
			var FW = Config.Item("FW").GetValue<bool>();
			var MP = Config.Item("FMP").GetValue<Slider>().Value;
			if (sender.IsEnemy && sender.Type == GameObjectType.obj_AI_Minion) 
			{
				if (FW && W.IsReady() && FarmActive && args.Target.Name == myHero.Name && myHero.Mana/myHero.MaxMana*100 >= MP )
				{
					if (Ally == null) W.Cast(sender,UsePacket);
					else W.CastIfHitchanceEquals(Ally, HitC ,UsePacket);
				}
				
			}
			if (UseW && W.IsReady() && sender.IsEnemy && (sender.Type == GameObjectType.obj_AI_Hero || sender.Type == GameObjectType.obj_AI_Turret ))
				if (SpellData.SpellName.Any(Each => Each.Contains(args.SData.Name)) || (args.Target == myHero && myHero.Distance(sender) <= 550))
					if (Ally == null) W.Cast(sender,UsePacket);
					else W.CastIfHitchanceEquals(Ally, HitC ,UsePacket);
        }
        
        private static void Drawing_OnDraw(EventArgs args)
        {
        	var drawQ = Config.Item("QRange").GetValue<Circle>();
            if (drawQ.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, Q.Range, drawQ.Color);
            }

            var drawW = Config.Item("WRange").GetValue<Circle>();
            if (drawW.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, W.Range, drawW.Color);
            }
            
            var drawE = Config.Item("ERange").GetValue<Circle>();
            if (drawE.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, E.Range, drawE.Color);
            }   
			if (Config.Item("JungSteal").GetValue<KeyBind>().Active && !myHero.IsDead)  
			{
				Utility.DrawCircle(Game.CursorPos, 900, Color.White);
			}
        }
        
        private static void LXOrbwalker_BeforeAttack(LXOrbwalker.BeforeAttackEventArgs args)
		{
			if (Config.Item("ComboActive").GetValue<KeyBind>().Active || Config.Item("HarassActive").GetValue<KeyBind>().Active)
				args.Process = !(Q.IsReady() || W.IsReady() || E.IsReady() || myHero.Distance(args.Target) >= 600);
		}
        
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
        	var UseQ = Config.Item("UseQGap").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (myHero.HasBuff("Recall") || myHero.IsWindingUp) return;  
        	if (UseQ && Q.IsReady() && GetDistanceSqr(myHero,gapcloser.Sender) <= Q.Range * Q.Range) Q.CastIfHitchanceEquals(gapcloser.Sender, HitC ,UsePacket);
        }
        
        private static bool IsFacing(Obj_AI_Base source, Obj_AI_Base target)
		{
			if (!source.IsValid || !target.IsValid) return false;			
			if (source.Path.Count() > 0 && source.Path[0].Distance(target.ServerPosition) < target.Distance(source))
				return true;
			else return false;				
		}
        
       private static void AutoShield()
        {
       	 	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
       	 	var HP = Config.Item("HP").GetValue<Slider>().Value;
       	 	var MP = Config.Item("MP").GetValue<Slider>().Value;
       	 	if (myHero.Mana/myHero.MaxMana*100 >= MP)
       	 	{
            	foreach (var hero in from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(W.Range) && hero.IsAlly ) let heroPercent = hero.Health/hero.MaxHealth*100 let shieldPercent = HP where heroPercent <= shieldPercent select hero)
            		W.CastIfHitchanceEquals(hero, HitC ,UsePacket);
       	 	}
        }
       
       	public static bool IgniteKillable(Obj_AI_Base target)
       	{
       		return Damage.GetSummonerSpellDamage(myHero, target,Damage.SummonerSpell.Ignite) > target.Health;
       	}
              	
       	public static float GetDistanceSqr(Obj_AI_Hero source, Obj_AI_Base target)
       	{
       		return Vector2.DistanceSquared(source.Position.To2D(),target.ServerPosition.To2D());
       	}
        
        private static bool DFGDamage(Obj_AI_Hero enemy)
        {
        	var dmgQ = myHero.GetSpellDamage(enemy, SpellSlot.Q);
        	var dmgE = myHero.GetSpellDamage(enemy, SpellSlot.E);
        	var dmgR = myHero.GetSpellDamage(enemy, SpellSlot.R);
        	if (Q.IsReady() && E.IsReady() && R.IsReady() && (dmgQ+dmgE+dmgR)*1.2f > enemy.Health)
        		return true;
        	else if (!Q.IsReady() && E.IsReady() && R.IsReady() && (dmgE+dmgR)*1.2f > enemy.Health)
        		return true;
        	else if (Q.IsReady() && !E.IsReady() && R.IsReady() && (dmgQ+dmgR)*1.2f > enemy.Health)
        		return true;
        	else if (Q.IsReady() && E.IsReady() && !R.IsReady() && (dmgQ+dmgE)*1.2f > enemy.Health)
        		return true;
			else if (Q.IsReady() && !E.IsReady() && !R.IsReady() && dmgQ*1.2f > enemy.Health)
        		return true;
			else if (!Q.IsReady() && E.IsReady() && !R.IsReady() && dmgE*1.2f > enemy.Health)
        		return true; 
			else if (!Q.IsReady() && !E.IsReady() && R.IsReady() && dmgR*1.2f > enemy.Health)					
        		return true; 		
			return false;			
        }
                
       	private static void UseCombo()
        {
        	var UseQ = Config.Item("UseQ").GetValue<bool>();
        	var UseW = Config.Item("UseW").GetValue<bool>();
        	var UseE = Config.Item("UseE").GetValue<bool>();
        	var UseR = Config.Item("UseR").GetValue<bool>();
        	var UseItems = Config.Item("UseItems").GetValue<bool>();
        	var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	var UseQE = Config.Item("UseQE").GetValue<bool>();
        	if (target == null) return;
        	
        	if (UseItems && DFGDamage(target) && GetDistanceSqr(myHero,target) <= 750 * 750)
        	{
        		if (Items.CanUseItem(3128)) Items.UseItem(3128,target);
        		if (Items.CanUseItem(3188)) Items.UseItem(3188,target);
        	}
        	if (UseQ && Q.IsReady() && GetDistanceSqr(myHero,target) <= Q.Range * Q.Range)
        	{
        		Q.CastIfHitchanceEquals(target, HitC ,UsePacket);
        		if (target.IsValidTarget(550) && target.HasBuff("luxilluminatingfraulein"))
        			myHero.IssueOrder(GameObjectOrder.AttackUnit, target);
        	}
        	if (UseW && W.IsReady() && IsFacing(target,myHero) && myHero.Distance(target) <= 550)
        	{
        		W.Cast(target,UsePacket);
        	}
        	if (UseE && E.IsReady() && GetDistanceSqr(myHero,target) <= E.Range * E.Range)
        	{
        		if (UseQE)
        		{
        			if (target.HasBuff("LuxLightBindingMis"))
        			{
						E.Cast(target ,UsePacket);
						CastE2();
        			}
        		}
        		else
        		{
        			E.CastIfHitchanceEquals(target, HitC ,UsePacket);
					CastE2();
        		}
        		if (target.IsValidTarget(550) && target.HasBuff("luxilluminatingfraulein"))
        			myHero.IssueOrder(GameObjectOrder.AttackUnit, target);
        	}
        	if (UseR && R.IsReady() && R.IsKillable(target))
        	{
        		if (target.Health <= Damage.GetAutoAttackDamage(myHero,target,true) && myHero.Distance(target) < 550)
        				myHero.IssueOrder(GameObjectOrder.AttackUnit, target);
				else R.Cast(target ,UsePacket);					
        	}
        	if (UseIgnite && IgniteKillable(target))
        	{
        		if (IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && myHero.Distance(target) <= 600)
        			if (target.Health <= Damage.GetAutoAttackDamage(myHero,target,true) && myHero.Distance(target) < 550)
        				myHero.IssueOrder(GameObjectOrder.AttackUnit, target);
					else myHero.SummonerSpellbook.CastSpell(IgniteSlot, target);     		
        	}
        }
       	
        
        private static void Harass()
        {
        	var UseQ = Config.Item("HQ").GetValue<bool>();
        	var UseE = Config.Item("HE").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	var UseQE = Config.Item("UseQE").GetValue<bool>();
        	if (target == null) return;
        	if (UseQ && Q.IsReady() && GetDistanceSqr(myHero,target) <= Q.Range * Q.Range)
        	{
				Q.CastIfHitchanceEquals(target, HitC ,UsePacket);
        		if (target.IsValidTarget(550) && target.HasBuff("luxilluminatingfraulein"))
        			myHero.IssueOrder(GameObjectOrder.AttackUnit, target);
        	}
        	if (UseE && E.IsReady() && GetDistanceSqr(myHero,target) <= E.Range * E.Range)
        	{
        	    if (UseQE)
        		{
        			if (target.HasBuff("LuxLightBindingMis"))
        			{
						E.CastIfHitchanceEquals(target, HitC ,UsePacket);
						CastE2();
        			}
        		}
        		else
        		{
        			E.CastIfHitchanceEquals(target, HitC ,UsePacket);
					CastE2();
        		}
        		if (target.IsValidTarget(550) && target.HasBuff("luxilluminatingfraulein"))
        			myHero.IssueOrder(GameObjectOrder.AttackUnit, target);
        	}
        } 
        
        private static void JungSteal()       	
        {
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	var Minions = MinionManager.GetMinions(Game.CursorPos, 1000, MinionTypes.All, MinionTeam.Neutral);
        	foreach (var minion in Minions.Where(minion => minion.IsVisible && !minion.IsDead ))
        	{
        		if (minion.Name.Contains("AncientGolem") ||minion.Name.Contains("LizardElder") || minion.Name.Contains("Dragon") || minion.Name.Contains("Worm"))
        		{
        			if (Q.IsReady() && GetDistanceSqr(myHero,minion) <= Q.Range * Q.Range && Q.IsKillable(minion)) Q.Cast(minion,UsePacket);
        			else if (E.IsReady() && GetDistanceSqr(myHero,minion) <= E.Range * E.Range && E.IsKillable(minion))
        			{
        				E.Cast(minion,UsePacket);
        				E.CastOnUnit(minion,UsePacket);
        			}
        			else if (R.IsReady() && minion.IsValidTarget(R.Range) && R.IsKillable(minion)) R.Cast(minion,UsePacket);
        		}
        	}
        }
        
        private static void KillSteal()
        {
        	var UseQ = Config.Item("KUseQ").GetValue<bool>();
        	var UseE = Config.Item("KUseE").GetValue<bool>();
        	var UseR = Config.Item("KUseR").GetValue<bool>();
        	var UseIgnite = Config.Item("KIgnite").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	if (UseQ || UseE || UseR || UseIgnite)
        	{
        		foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Range) && hero.IsEnemy && !hero.IsDead))
       			{
        			if (UseQ && Q.IsReady() && GetDistanceSqr(myHero,hero) <= Q.Range * Q.Range && Q.IsKillable(hero))
        				Q.CastIfHitchanceEquals(hero, HitC ,UsePacket);
        			else if (UseE && E.IsReady() && GetDistanceSqr(myHero,hero) <= E.Range * E.Range && E.IsKillable(hero))
        			{
						E.CastIfHitchanceEquals(hero, HitC ,UsePacket);
						CastE2();
        			}
        			else if (UseR && R.IsReady() && hero.IsValidTarget(R.Range) && R.IsKillable(hero))
        			{
        				if (hero.Health <= Damage.GetAutoAttackDamage(myHero,hero,true) && myHero.Distance(hero) < 550)
        					myHero.IssueOrder(GameObjectOrder.AttackUnit, hero);
        				else R.Cast(hero ,UsePacket);						
        			}
        			else if (UseIgnite && IgniteKillable(hero))
					{
						if (IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && myHero.Distance(hero) <= 600)
						{
							if (hero.Health <= Damage.GetAutoAttackDamage(myHero,hero,true) && myHero.Distance(hero) < 550)
								myHero.IssueOrder(GameObjectOrder.AttackUnit, hero);
							else myHero.SummonerSpellbook.CastSpell(IgniteSlot, hero);  	
						}
					}
       		 	}	
        	}
        	
        }
                
               
        private static void CastE2()
		{
        	if (EObject == null) return;
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			foreach (Obj_AI_Hero current in ObjectManager.Get<Obj_AI_Hero>())
				if (!current.IsMe && current.IsEnemy && Vector3.Distance(EObject.Position, current.ServerPosition) <= E.Width)
					E.CastOnUnit(myHero,UsePacket);					
		}
       	
        private static void Farm()
        {
        	var UseQ = Config.Item("FQ").GetValue<bool>();
        	var UseE = Config.Item("FE").GetValue<bool>();
        	var UsePacket = Config.Item("UsePacket").GetValue<bool>();
        	var MP = Config.Item("FMP").GetValue<Slider>().Value;
        	var Minions = MinionManager.GetMinions(myHero.Position, E.Range, MinionTypes.All, MinionTeam.NotAlly);
        	if (Minions.Count == 0 ) return;
        	if (myHero.Mana/myHero.MaxMana*100 >= MP)
        	{
        		if (UseQ && Q.IsReady())
        		{
        			var castPostion = MinionManager.GetBestLineFarmLocation(Minions.Select(minion => minion.ServerPosition.To2D()).ToList(), Q.Width, Q.Range);
					Q.Cast(castPostion.Position, UsePacket);
        		}
        		if (UseE && E.IsReady())
        		{
        			var castPostion = MinionManager.GetBestCircularFarmLocation(Minions.Select(minion => minion.ServerPosition.To2D()).ToList(), E.Width, E.Range);
					E.Cast(castPostion.Position, UsePacket);
					E.CastOnUnit(myHero,UsePacket);
        		}
        	}
        }         	
    }
}
