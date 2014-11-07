#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
#endregion

namespace hi_im_gosu
{
    class Vayne
    {
        public static Spell E;
        public static Spell Q;
        public static Orbwalking.Orbwalker orbwalker;
        public static Menu menu;
        public static string[] interrupt;
        public static string[] notarget;
        public static string[] gapcloser;
        public static Activator AActivator;
        public static Menu QuickSilverMenu;
        public static int playerHit;
		public static bool gotHit = false;

        static void Main(string[] args)
        {
        	CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        public static void Game_OnGameLoad(EventArgs args)
        {
        	if (ObjectManager.Player.ChampionName != "Vayne") return;
        	
        	AActivator = new Activator();
            menu = new Menu("Hi Im Gosu", "Hi Im Gosu", true);

            menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            orbwalker = new Orbwalking.Orbwalker(menu.SubMenu("Orbwalker"));
            
            var TargetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(TargetSelectorMenu);
            menu.AddSubMenu(TargetSelectorMenu);
            
			var items = menu.AddSubMenu(new Menu("Items", "Items"));
			items.AddItem(new MenuItem("BOTRK", "Blade of the Ruined King").SetValue(true));
			items.AddItem(new MenuItem("GHOSTBLADE", "Youmuu's Ghostblade").SetValue(true));
			items.AddItem(new MenuItem("DIVINE", "Sword of the Divine").SetValue(true));			
			QuickSilverMenu = new Menu("Quick Silver Sash", "QuickSilverSash");
			items.AddSubMenu(QuickSilverMenu);
			QuickSilverMenu.AddItem(new MenuItem("AnyStun", "Any Stun").SetValue(true));
			QuickSilverMenu.AddItem(new MenuItem("AnySnare", "Any Snare").SetValue(true));
			QuickSilverMenu.AddItem(new MenuItem("AnyTaunt", "Any Taunt").SetValue(true));
			foreach (var t in AActivator.BuffList)
				foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
					if (t.ChampionName == enemy.ChampionName)
						QuickSilverMenu.AddItem(new MenuItem(t.BuffName, t.DisplayName).SetValue(t.DefaultValue));
			
			var potions = menu.AddSubMenu(new Menu("Summoner Spell", "Summoner"));
			potions.AddItem(new MenuItem("Heal", "Use Heal").SetValue(true));
			potions.AddItem(new MenuItem("Barrier", "Use Barrier").SetValue(true));
			potions.AddItem(new MenuItem("HPercent", "HP %").SetValue(new Slider(20,100,0)));
			
			var tumbles = menu.AddSubMenu(new Menu("Wall Tumbles", "Tumbles"));
			tumbles.AddItem(new MenuItem("DrakeWallT", "Tumble Drake Wall!").SetValue(new KeyBind("W".ToCharArray()[0], KeyBindType.Press)));
			tumbles.AddItem(new MenuItem("MidWallT", "Tumble Mid Wall").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
			tumbles.AddItem(new MenuItem("DrawCD", "Draw Drake Wall Circle").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
			tumbles.AddItem(new MenuItem("DrawCM", "Draw Mid Wall Circle").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
			
            menu.AddItem(new MenuItem("UseET", "Use E (Toggle)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
            menu.AddItem(new MenuItem("UseEInterrupt", "Use E To Interrupt").SetValue(true));
            menu.AddItem(new MenuItem("PushDistance", "E Push Distance").SetValue(new Slider(425, 475, 300)));
            menu.AddItem(new MenuItem("UseQC", "Use Q").SetValue(true));
            menu.AddItem(new MenuItem("UseEC", "Use E").SetValue(true));
            menu.AddItem(new MenuItem("UseEaa", "Use E after auto").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Toggle)));
            menu.AddSubMenu(new Menu("Gapcloser List", "gap"));
            menu.AddSubMenu(new Menu("Gapcloser List 2", "gap2"));
            menu.AddSubMenu(new Menu("Interrupt List", "int"));
            Q = new Spell(SpellSlot.Q, 0f);
            E = new Spell(SpellSlot.E, float.MaxValue);

            gapcloser = new[]
            {
                "AkaliShadowDance", "Headbutt", "DianaTeleport", "IreliaGatotsu", "JaxLeapStrike", "JayceToTheSkies",
                "MaokaiUnstableGrowth", "MonkeyKingNimbus", "Pantheon_LeapBash", "PoppyHeroicCharge", "QuinnE",
                "XenZhaoSweep", "blindmonkqtwo", "FizzPiercingStrike", "RengarLeap"
            };
            notarget = new[]
            {
                "AatroxQ", "GragasE", "GravesMove", "HecarimUlt", "JarvanIVDragonStrike", "JarvanIVCataclysm", "KhazixE",
                "khazixelong", "LeblancSlide", "LeblancSlideM", "LeonaZenithBlade", "UFSlash", "RenektonSliceAndDice",
                "SejuaniArcticAssault", "ShenShadowDash", "RocketJump", "slashCast"
            };
            interrupt = new[]
            {
                "KatarinaR", "GalioIdolOfDurand", "Crowstorm", "Drain", "AbsoluteZero", "ShenStandUnited", "UrgotSwap2",
                "AlZaharNetherGrasp", "FallenOne", "Pantheon_GrandSkyfall_Jump", "VarusQ", "CaitlynAceintheHole",
                "MissFortuneBulletTime", "InfiniteDuress", "LucianR"
            };
            for (int i = 0; i < gapcloser.Length; i++)
                menu.SubMenu("gap").AddItem(new MenuItem(gapcloser[i], gapcloser[i])).SetValue(true);
            for (int i = 0; i < notarget.Length; i++)
                menu.SubMenu("gap2").AddItem(new MenuItem(notarget[i], notarget[i])).SetValue(true);
            for (int i = 0; i < interrupt.Length; i++)
                menu.SubMenu("int").AddItem(new MenuItem(interrupt[i], interrupt[i])).SetValue(true);
            
            E.SetTargetted(0.25f, 2200f);
			menu.AddToMainMenu();
			Game.PrintChat("Hi Im Gosu loaded!!!"); 
			
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameProcessPacket += OnGameProcessPacket;
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
        }

        public static void Game_ProcessSpell(Obj_AI_Base hero, GameObjectProcessSpellCastEventArgs args)
        {
            if (menu.Item("UseEInterrupt").GetValue<bool>() && hero.IsValidTarget(550f) &&
              								  menu.Item(args.SData.Name).GetValue<bool>())
                if (interrupt.Any(str => str.Contains(args.SData.Name))) E.Cast(hero);

            if (gapcloser.Any(str => str.Contains(args.SData.Name)) && args.Target == ObjectManager.Player &&
                					hero.IsValidTarget(550f) && menu.Item(args.SData.Name).GetValue<bool>())
                E.Cast(hero);

            if (notarget.Any(str => str.Contains(args.SData.Name)) &&
               			 Vector3.Distance(args.End, ObjectManager.Player.Position) <= 300 && hero.IsValidTarget(550f) &&
               			 menu.Item(args.SData.Name).GetValue<bool>())
                E.Cast(hero);
        }
        
        public static void Drawing_OnDraw(EventArgs args)
		{
        	if (Utility.Map.GetMap()._MapType == Utility.Map.MapType.SummonersRift)
        	{
				if (menu.Item("DrawCD").GetValue<Circle>().Active)
					Drawing.DrawCircle(new Vector3(11590.95f, 4656.26f, 0f), 80f, System.Drawing.Color.White);
				if (menu.Item("DrawCM").GetValue<Circle>().Active)
					Drawing.DrawCircle(new Vector3(6623, 8649, 0f), 80f, System.Drawing.Color.White);
        	}
		}
        
        public static void OnGameProcessPacket(GamePacketEventArgs args)
		{
			byte[] packet = args.PacketData;
			if (packet[0] == Packet.S2C.Damage.Header)
			{
				Packet.S2C.Damage.Struct damage = Packet.S2C.Damage.Decoded(args.PacketData);
				var source = damage.SourceNetworkId;
				var target = damage.TargetNetworkId;
				playerHit = target;
				gotHit = true;
			}
		}

        public static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe)
            {
                Obj_AI_Hero tar = (Obj_AI_Hero) target;
                if (menu.Item("UseEaa").GetValue<KeyBind>().Active)
                {
                    E.Cast(target);
                    menu.Item("UseEaa").SetValue<KeyBind>(new KeyBind("G".ToCharArray()[0], KeyBindType.Toggle));
                }

                if (orbwalker.ActiveMode.ToString() == "Combo" && menu.Item("UseQC").GetValue<bool>() && Q.IsReady())
                {
                    var after = ObjectManager.Player.Position + Normalize(Game.CursorPos - ObjectManager.Player.Position)*300;                              
                    var disafter = Vector3.DistanceSquared(after, tar.Position);
                    if ((disafter < 630*630) && disafter > 150*150) Q.Cast(Game.CursorPos);                       
                    if (Vector3.DistanceSquared(tar.Position, ObjectManager.Player.Position) > 630*630 && disafter < 630*630)                        															
                       	Q.Cast(Game.CursorPos);
                }
            }
        }

        public static Vector3 Normalize(Vector3 A)
        {
            double distance = Math.Sqrt(A.X*A.X + A.Y*A.Y);
            return new Vector3(new Vector2((float) (A.X/distance)), (float) (A.Y/distance));
        }
        
        public static void CheckChampionBuff()
		{
			foreach (var t1 in ObjectManager.Player.Buffs)
			{
				foreach (var t in QuickSilverMenu.Items)
				{
					if (QuickSilverMenu.Item(t.Name).GetValue<bool>())
					{
						if (t1.Name.ToLower().Contains(t.Name.ToLower()))
						{
							if (Items.HasItem(3139)) Items.UseItem(3139);
							if (Items.HasItem(3140)) Items.UseItem(3140);
						}
					}
					if (QuickSilverMenu.Item("AnySnare").GetValue<bool>() && ObjectManager.Player.HasBuffOfType(BuffType.Snare))
					{
						if (Items.HasItem(3139)) Items.UseItem(3139);
						if (Items.HasItem(3140)) Items.UseItem(3140);
					}
					if (QuickSilverMenu.Item("AnyStun").GetValue<bool>() && ObjectManager.Player.HasBuffOfType(BuffType.Stun))
					{
						if (Items.HasItem(3139)) Items.UseItem(3139);
						if (Items.HasItem(3140)) Items.UseItem(3140);
					}
					if (QuickSilverMenu.Item("AnyTaunt").GetValue<bool>() && ObjectManager.Player.HasBuffOfType(BuffType.Taunt))
					{
						if (Items.HasItem(3139)) Items.UseItem(3139);
						if (Items.HasItem(3140)) Items.UseItem(3140);
					}
				}
			}
		}
        
        public static void SummonerSpell()
        {
        	var Heal = menu.Item("Heal").GetValue<bool>();
			var Barrier = menu.Item("Barrier").GetValue<bool>();
			var HPercent = menu.Item("HPercent").GetValue<Slider>().Value;
			var HealSlot = Utility.GetSpellSlot(ObjectManager.Player, "SummonerHeal");
			var BarrierSlot = Utility.GetSpellSlot(ObjectManager.Player, "SummonerBarrier");
			if (Heal && HealSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(HealSlot) == SpellState.Ready)
			{
				if ((int)(ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth) <= HPercent && playerHit == ObjectManager.Player.NetworkId && gotHit)
				{
				    ObjectManager.Player.SummonerSpellbook.CastSpell(HealSlot);
					gotHit = false;
				}
			}
			else if (Barrier && BarrierSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(BarrierSlot) == SpellState.Ready)
			{
				if ((int)(ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth) <= HPercent && playerHit == ObjectManager.Player.NetworkId && gotHit)
				{
				    ObjectManager.Player.SummonerSpellbook.CastSpell(BarrierSlot);
					gotHit = false;
				}
			}
        }
                
        public static void UseItems()
        {
        	SummonerSpell();
        	CheckChampionBuff();
			
        	if (orbwalker.ActiveMode.ToString() != "Combo") return;
        	
        	var botrk = menu.Item("BOTRK").GetValue<bool>();
			var ghostblade = menu.Item("GHOSTBLADE").GetValue<bool>();
			var divine = menu.Item("DIVINE").GetValue<bool>();
			var target = orbwalker.GetTarget();
			if (botrk)
			{
				if (target != null && target.Type == ObjectManager.Player.Type && target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 450)
				{
					var hasCutGlass = Items.HasItem(3144);
					var hasBotrk = Items.HasItem(3153);
					if (hasBotrk || hasCutGlass)
					{
						var itemId = hasCutGlass ? 3144 : 3153;
						var damage = ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Botrk);
						if (hasCutGlass || ObjectManager.Player.Health + damage < ObjectManager.Player.MaxHealth)
							Items.UseItem(itemId, target);
					}
				}
			}
			if (ghostblade && target != null && target.Type == ObjectManager.Player.Type && Orbwalking.InAutoAttackRange(target))
				Items.UseItem(3142);
			if (divine && target != null && target.Type == ObjectManager.Player.Type && Orbwalking.InAutoAttackRange(target))
				Items.UseItem(3131);
       	}

        public static void Game_OnGameUpdate(EventArgs args)
        {
        	UseItems();
        	
        	if (menu.Item("DrakeWallT").GetValue<KeyBind>().Active) DrakeWall();
			else if (menu.Item("MidWallT").GetValue<KeyBind>().Active) MidWall();

            if ((!E.IsReady()) ||
                ((orbwalker.ActiveMode.ToString() != "Combo" || !menu.Item("UseEC").GetValue<bool>()) &&
                 !menu.Item("UseET").GetValue<KeyBind>().Active)) return;

            foreach (var hero in from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(550f))
                let prediction = E.GetPrediction(hero)
                where NavMesh.GetCollisionFlags(
                    prediction.UnitPosition.To2D()
                        .Extend(ObjectManager.Player.ServerPosition.To2D(),
                            -menu.Item("PushDistance").GetValue<Slider>().Value)
                        .To3D())
                    .HasFlag(CollisionFlags.Wall) || NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D()
                            .Extend(ObjectManager.Player.ServerPosition.To2D(),
                                -(menu.Item("PushDistance").GetValue<Slider>().Value/2))
                            .To3D())
                        .HasFlag(CollisionFlags.Wall)
                select hero)   E.Cast(hero);
        }
        
        public static void DrakeWall()
		{
			Vector2 DrakeWallQPos = new Vector2(11334.74f, 4517.47f);
			if (ObjectManager.Player.Position.X < 11540 || ObjectManager.Player.Position.X > 11600 || ObjectManager.Player.Position.Y < 4638 || ObjectManager.Player.Position.Y > 4712)
				Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(11590.95f, 4656.26f)).Send();
			else
			{
				Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(11590.95f, 4656.26f)).Send();
				Q.Cast(DrakeWallQPos, true);
			}
		}
		public static void MidWall()
		{
			Vector2 MidWallQPos = new Vector2(6010.5869140625f, 8508.8740234375f);
			if (ObjectManager.Player.Position.X < 6600 || ObjectManager.Player.Position.X > 6660 || ObjectManager.Player.Position.Y < 8630 || ObjectManager.Player.Position.Y > 8680)
				Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(6623, 8649)).Send();
			else
			{
				Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(6623, 8649)).Send();
				Q.Cast(MidWallQPos, true);
			}
		}
    }
}
