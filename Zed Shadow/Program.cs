#region
using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace ZedShadow
{
    internal class Program
    {
    	private static Orbwalking.Orbwalker Orbwalker;
        private static Menu Config;
        private static Obj_AI_Hero Target;
        private static Obj_AI_Hero myHero;
       	private static Spell Q;
		private static Spell W;
		private static Spell E;
		private static Spell R;
		private static SpellSlot IgniteSlot;
		private static float lastW;
		private static float MyMana;
		private static float QMana;
		private static float WMana;
		private static float EMana;
		private static bool wUsed;
		private static bool isDead;
		private static bool UseSwap;
		
		private static int RCastTick;
		
		private static Obj_AI_Minion wClone;
		private static bool CloneWCreated;
		private static bool CloneWFound;
		private static int CloneWTick;
		
		private static Obj_AI_Minion rClone;
		private static bool CloneRCreated;
		private static bool CloneRFound;
		private static int CloneRTick;
		private static Vector3 CloneRNearPosition;
		
		private static bool WCasted;		
		private static bool CheckW;
		
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        
        private static void Variables()
        {
        	UseSwap = true;
			lastW = 0;
			wClone = null;
			rClone = null;
			CloneWCreated = false;
			CloneWFound = false;
			CloneWTick = 0;
			CloneRCreated = false;
			CloneRFound = false;
			CloneRTick = 0;
			RCastTick = 0;
			WCasted = false;
			CheckW = false;
        }
          
        private static void Game_OnGameLoad(EventArgs args)
        {
        	myHero = ObjectManager.Player;
           	if (myHero.ChampionName != "Zed") return;
           	
            Q = new Spell(SpellSlot.Q, 900);
            W = new Spell(SpellSlot.W, 550);
            E = new Spell(SpellSlot.E, 290);
            R = new Spell(SpellSlot.R, 600);

            Q.SetSkillshot(0.25f, 45f, 902f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 40f, 1600f, false, SkillshotType.SkillshotLine);
            
			IgniteSlot = myHero.GetSpellSlot("SummonerDot");  
				
			Variables();
			
			Config = new Menu("Zed Shadow", "Zed Shadow", true);
			var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
			SimpleTs.AddToMenu(targetSelectorMenu);
			Config.AddSubMenu(targetSelectorMenu);
			
			Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
			Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
			
			Config.AddSubMenu(new Menu("Combo", "Combo"));
			Config.SubMenu("Combo").AddItem(new MenuItem("Fight", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
			Config.SubMenu("Combo").AddItem(new MenuItem("TypeCombo", "").SetValue(new StringList(new[] {"Use QWER","Use QWE"},0)));
			Config.SubMenu("Combo").AddItem(new MenuItem("SwapUlt", "Swap back if %HP").SetValue(new Slider(15,100,0)));
			Config.SubMenu("Combo").AddItem(new MenuItem("NoWWhenUlt", "Dont use W when ult").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("rSwap", "Swap to R shadow if safer when mark kills").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("wSwap", "Swap with W to get closer to target").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("igniteOptions", "Use Ignite in combo").SetValue(true));
                        
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("harassKey", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Harass").AddItem(new MenuItem("mode", "True = QWE, False = Q").SetValue(false));
            
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("farmKey", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("farmQ", "Use Q").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FarmE", "Use E").SetValue(true));          
                    
            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("Movement", "Move to Mouse in combo").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("autoIgnite", "KS with Ignite").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UsePacket", "Use Packet Cast").SetValue(true));
                      
			Config.AddSubMenu(new Menu("Drawings", "Drawings"));
			Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));				
			Config.AddToMainMenu();       
			
			Game.PrintChat("Zed Shadow loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
			GameObject.OnCreate += OnCreateObject;
			GameObject.OnDelete += OnDeleteObject;
			Drawing.OnDraw += Drawing_OnDraw;
			Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast; 		
        }
                          
        private static void Game_OnGameUpdate(EventArgs args)
        {     
        	
			UpdateMana();
			autoIgnite();
			CloneCheck();
			if (CheckW)
			{
				if (wClone != null ) WCasted = true;
				if (wClone == null ) WCasted = false;
			}
			if (RCastTick < Environment.TickCount - 5000) UseSwap = true;

			if (Q.IsReady() && E.IsReady() && W.IsReady()) Target = SimpleTs.GetTarget(1200, SimpleTs.DamageType.Physical);
 			else Target = SimpleTs.GetTarget(900, SimpleTs.DamageType.Physical);
        	
        	if (myHero.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR2")
        	{
 				foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && enemy.IsValidTarget(1200)))
 				{
 					if (enemy.HasBuff("zedulttargetmark")) 
 						Target = enemy;
 				}
        	}        	
			if (Config.Item("Fight").GetValue<KeyBind>().Active) 
			{
				if (Config.Item("Movement").GetValue<bool>()) Orbwalker.SetAttack(false);
				if (Config.Item("TypeCombo").GetValue<StringList>().SelectedIndex == 0) Fight(Target);
				else if (Config.Item("TypeCombo").GetValue<StringList>().SelectedIndex == 1) Fight2(Target);
			}
			else Orbwalker.SetAttack(true);
			if (CheckW)
				if (Config.Item("harassKey").GetValue<KeyBind>().Active) 
					Harass(Target);
			CheckW = false;
        }
        
        
        private static void CloneCheck()
        {
        	
        	if(CloneWCreated && !CloneWFound)
				SearchForClone("W");
			if(CloneRCreated && !CloneRFound)
				SearchForClone("R");
        	if(wClone != null && (CloneWTick < Environment.TickCount - 4000))
			{
				wClone = null;
				CloneWCreated = false;
				CloneWFound = false;
				wUsed = false;
			}

			if(rClone != null && (CloneRTick < Environment.TickCount - 6000))
			{
				rClone = null;
				CloneRCreated = false;
				CloneRFound = false;
			}
			CheckW = true;
        }
        
        private static void SearchForClone(string p)
		{
			Obj_AI_Minion shadow;
			if(p != null && p == "W")
			{
				shadow = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(hero => (hero.Name == "Shadow" && hero.IsAlly && (hero != rClone)));
				if(shadow != null)
				{
					wClone = shadow;
					CloneWFound = true;
					CloneWTick = Environment.TickCount;
				}
			}
			if (p == null || p != "R") 
				return;
			shadow = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(hero => ((hero.ServerPosition.Distance(CloneRNearPosition)) < 50) && hero.Name == "Shadow" && hero.IsAlly && hero != wClone);
			if (shadow == null) 
				return;
			rClone = shadow;
			CloneRFound = true;
			CloneRTick = Environment.TickCount;
		}

        private static void OnCreateObject(GameObject sender, EventArgs args)
		{        	
			var spell = (Obj_SpellMissile)sender;
			var unit = spell.SpellCaster.Name;
			var name = spell.SData.Name;
			if(unit == ObjectManager.Player.Name && name == "ZedShadowDashMissile")
				CloneWCreated = true;
			if(unit == ObjectManager.Player.Name && name == "ZedUltMissile")
			{
				CloneRCreated = true;
				CloneRNearPosition = myHero.ServerPosition;
			}
        	if (sender.Name.Contains("Zed_Base_R_buf_tell.troy")) 
        		isDead = true;
		}
        
        private static void OnDeleteObject(GameObject sender, EventArgs args)
		{
        	if (sender.Name.Contains("Zed_Base_R_buf_tell.troy")) 
        		isDead = false;
		}	
        
        private static void UpdateMana()
        {
        	MyMana = myHero.Mana;
        	QMana = myHero.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
			WMana = myHero.Spellbook.GetSpell(SpellSlot.W).ManaCost;
			EMana = myHero.Spellbook.GetSpell(SpellSlot.E).ManaCost;
        }
                   

 		private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
        	if (sender.IsMe && args.SData.Name == "ZedShadowDash")
        	{
				wUsed = true;
				lastW = Environment.TickCount;
			}
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
            
            var drawR = Config.Item("RRange").GetValue<Circle>();
            if (drawR.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, R.Range, drawR.Color);
            }
        }
 		
 		private static void CastItems(Obj_AI_Hero enemy)
 		{
 				if (myHero.Distance(enemy) <= 480)
 				{
 					if (Items.HasItem(3144) && Items.CanUseItem(3144)) 
 					{
 						Items.UseItem(3144,enemy);
 					}
 					if (Items.HasItem(3153) && Items.CanUseItem(3153)) 
 					{
 						Items.UseItem(3153,enemy);
 					}
 				}
 				if (myHero.Distance(enemy) <= 400)
 				{
 					if (Items.HasItem(3146) && Items.CanUseItem(3146)) 
 					{
 						Items.UseItem(3146,enemy);
 					}
 				}
 				if (myHero.Distance(enemy) <= 300)
 				{
 					if (Items.HasItem(3184) && Items.CanUseItem(3184)) 
 					{
 						Items.UseItem(3184,enemy);
 					}
 					if (Items.HasItem(3143) && Items.CanUseItem(3143)) 
 					{
 						Items.UseItem(3143,enemy);
 					}
 					if (Items.HasItem(3074) && Items.CanUseItem(3074)) 
 					{
 						Items.UseItem(3074,enemy);
 					}
 					if (Items.HasItem(3131) && Items.CanUseItem(3131))
 					{
 						Items.UseItem(3131,enemy);
 					}
 					if (Items.HasItem(3077) && Items.CanUseItem(3077)) 
 					{
 						Items.UseItem(3077,enemy);
 					}
 					if (Items.HasItem(3142) && Items.CanUseItem(3142)) 
 					{
 						Items.UseItem(3142,enemy);
 					}
 				}
 				if (myHero.Distance(enemy) <= 1000)
 				{
 					if (Items.HasItem(3023) && Items.CanUseItem(3023)) 
 					{
 						Items.UseItem(3023,enemy);
 					}
 				}

 		}
 		
 		private static void autoIgnite()
        {
        	var AutoIgnite = Config.Item("autoIgnite").GetValue<bool>();
        	var ignitedmg = 0;
        	if (AutoIgnite && IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
        	{
        		foreach (var enemyhero in ObjectManager.Get<Obj_AI_Hero>().Where(enemyhero => enemyhero.IsValidTarget(600)))
        		{
        			ignitedmg = 50 + 20 * myHero.Level;
        			if (enemyhero.Health <= ignitedmg) 
        				myHero.SummonerSpellbook.CastSpell(IgniteSlot,enemyhero);
        		}        
        	}
        }
 		
 		private static void Swap(Obj_AI_Hero target)
 		{
 			float wDist = 0;
 			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
 			if (UseSwap)
 			{
 				if (target == null) return;

 				if (wClone != null) wDist = target.Distance(wClone.Position);
 				else return;
 				if (myHero.Distance(target) > 250)
 				{
 					if ((wDist > 0) && (wDist != 0) && (myHero.Distance(target) > wDist) && W.IsReady() && !E.IsReady())				
 						W.Cast();	
 				}
 			}
 		}
 		 		
        private static int CountEnemies(Obj_AI_Base point,float range)
        {
        	int ChampCount = 0;
        	foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && enemy.IsValidTarget(750) && enemy.Distance(point.Position) <= range))
        		ChampCount = ChampCount + 1 ;
            return ChampCount;
        }
        		
 		private static void Fight(Obj_AI_Hero target)
 		{
 			var wSwap = Config.Item("wSwap").GetValue<bool>();
 			var rSwap = Config.Item("rSwap").GetValue<bool>();
 			var NoWWhenUlt = Config.Item("NoWWhenUlt").GetValue<bool>();
 			var igniteOptions = Config.Item("igniteOptions").GetValue<bool>();
 			var SwapUlt = Config.Item("SwapUlt").GetValue<Slider>().Value;
 			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
 			if (wSwap) Swap(target);
	
 			if (target == null) return;
 			
 			if ( !target.HasBuff("JudicatorIntervention") || !target.HasBuff("Undying Rage") )
 			{
 				if (R.IsReady() && MyMana > (QMana + EMana)) 
 					CastR(target);
 				if (!R.IsReady() || rClone != null )
 				{
 					if (myHero.Spellbook.GetSpell(SpellSlot.W).Name != "zedw2" && W.IsReady() && ( (myHero.Distance(target) < 700) || (myHero.Distance(target) > 125 && !R.IsReady()) ) )
 					{
 						if (!NoWWhenUlt && !( (myHero.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR2") || rClone != null ) )
 						{
 							if (MyMana > (WMana+EMana)) W.Cast(target,UsePacket);
 						}
 					}	
 					if ((!W.IsReady() || wClone != null || NoWWhenUlt || wUsed) && (!R.IsReady() || rClone != null))
 					{ 						
 						CastE(target);
 						if (myHero.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Cooldown ||
 						                    myHero.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.NotLearned ||
 						                    rClone != null)
 							CastQ(target);
 					}
 				}
 			}
 			if (igniteOptions && target.HasBuff("zedulttargetmark") && IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready
 			   														&& myHero.Distance(target) <= 600)
 			{
 				myHero.SummonerSpellbook.CastSpell(IgniteSlot,target); 
 			}
 			CastItems(target);
 			if (R.IsReady() && rClone != null && rSwap)
 				if (isDead && CountEnemies(myHero,250) > CountEnemies(rClone,250))
 				{	
 				    UseSwap = false;
 					R.Cast();
					RCastTick = Environment.TickCount;
 				}
 			if (myHero.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR2" && ((myHero.Health/myHero.MaxHealth*100) <= SwapUlt))
 				R.Cast();
 			var UltDmg = Damage.GetAutoAttackDamage(myHero,target,true)+(0.15*(myHero.Spellbook.GetSpell(SpellSlot.R).Level+0.5)*
 				                                                             (80+Damage.GetSpellDamage(myHero,target,SpellSlot.Q)*2)+
 				                                                             (Damage.GetSpellDamage(myHero,target,SpellSlot.E)));
 			if (UltDmg >= target.Health)
 			{
 				if (myHero.Distance(target) < 1125 && myHero.Distance(target) > 750)
 				{
 					var DashPos = myHero.Position + Vector3.Normalize(target.Position - myHero.Position) * 550;
 					if (Q.IsReady() && E.IsReady() && R.IsReady() && wClone == null && rClone == null)
 					{
 						if (myHero.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash") 
 							W.Cast(DashPos,UsePacket);
 					}
 					if (wClone != null && rClone == null)
 					{
 						W.Cast(myHero,UsePacket);
 						R.Cast(target,UsePacket);
 					}
 				}
 			} 							
 		}
 		
 		private static void Fight2(Obj_AI_Hero target)
 		{
 			var wSwap = Config.Item("wSwap").GetValue<bool>();
 			var rSwap = Config.Item("rSwap").GetValue<bool>();
 			var igniteOptions = Config.Item("igniteOptions").GetValue<bool>();
 			var SwapUlt = Config.Item("SwapUlt").GetValue<Slider>().Value;
 			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
 			if (wSwap) Swap(target);

 			if (target == null) return;

			if (!target.HasBuff("JudicatorIntervention") || !target.HasBuff("Undying Rage")) 
			{
				if (myHero.Spellbook.GetSpell(SpellSlot.W).Name != "zedw2" && W.IsReady() && ( (myHero.Distance(target) < 700) || (myHero.Distance(target) > 125) ) )
				{
					if (MyMana > (EMana + WMana )) W.Cast(target,UsePacket);					
				}
					
				if (!W.IsReady() || wClone != null || wUsed)
				{					
					CastE(target);
					CastQ(target);						
				}					
			}			 			
			if (igniteOptions && target.HasBuff("zedulttargetmark") && IgniteSlot != SpellSlot.Unknown && myHero.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready
			    																						&& myHero.Distance(target) <= 600)
			{
				myHero.SummonerSpellbook.CastSpell(IgniteSlot,target);
			}
			CastItems(target);
			if (R.IsReady() && rClone != null && rSwap)
			{
				if (isDead && CountEnemies(myHero,250) > CountEnemies(rClone,250))
				{
					UseSwap = false;
 					R.Cast();
 					RCastTick = Environment.TickCount;
				}
			}
			if (myHero.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR2" && ((myHero.Health/myHero.MaxHealth*100) <= SwapUlt))
 				R.Cast();
 		}
 		
 		private static void Harass(Obj_AI_Hero target)
 		{
 			if (target == null) return;
 			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
 			var mode = Config.Item("mode").GetValue<bool>();
 			if (mode)
 			{
 				
 				
 				if (Q.IsReady() && W.IsReady() && (myHero.Distance(target) < 800) && (MyMana > QMana+WMana+EMana))
 				{
 					if (Environment.TickCount > (lastW + 1000) && !WCasted && myHero.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash")
					{
							W.Cast(target,UsePacket);
							if (wUsed) E.Cast();					
					}
 				}
 				if (wUsed) CastQ(target);
 				if (!W.IsReady())
 				{
 					CastQ(target);
 					CastQClone(target);
 				}
 				CastE(target);
 				if (myHero.Distance(target) < 1450 && myHero.Distance(target) > 900)
 				{
 					var DashPos = myHero.Position + Vector3.Normalize(target.Position - myHero.Position) * 550;
 					if (Q.IsReady() && W.IsReady() && (MyMana > QMana+WMana))
 					{
 						if (!WCasted && myHero.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash") W.Cast(DashPos,UsePacket);
 					}
 					if (wClone != null) CastQClone(target);
 				}
 			}
 			else
 			{
 				if (Q.IsReady() && myHero.Distance(target) < Q.Range) CastQ(target);
 				else if (E.IsReady() && myHero.Distance(target) < 280) CastE(target);
 				else if (wClone != null)
 				{
 					CastE(target);
 					CastQClone(target);
 				}
 			}
 		}
 		
 		private static void CastQ(Obj_AI_Hero enemy)
 		{
 			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
 			if (Q.IsReady())
 			{
 				if (enemy.Distance(myHero) <= Q.Range || enemy.Distance(wClone) <= Q.Range || enemy.Distance(rClone) <= Q.Range)
 				{
 					Q.Cast(enemy,UsePacket);	
 				}
 			}
 			else return;
 		}
 		
 		private static void CastQClone(Obj_AI_Hero enemy)
 		{
 			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
 			if (Q.IsReady()) 
 			{
 				if(enemy.Distance(wClone) < Q.Range)
 				{
 					Q.Cast(enemy,UsePacket);

 				}
 			}
 			else return;
 		}
 		
 		
        private static void CastE(Obj_AI_Hero enemy)
		{
        	if (E.IsReady())
        	{
        		if (enemy.Distance(myHero) <= 280 || enemy.Distance(wClone) <= 280 || enemy.Distance(rClone) <= 280)	
        		{
					E.Cast();

        		}
        	}
        	else return;
		}

 		private static void CastR(Obj_AI_Hero enemy)
 		{
 			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
 			if (!R.IsReady()) return;
 			if (myHero.Distance(enemy) <= 625 && R.IsReady() && myHero.Spellbook.GetSpell(SpellSlot.R).Name != "ZedR2")
 			{
 				R.Cast(enemy,UsePacket);
		
 			}
 		}
    }
}
