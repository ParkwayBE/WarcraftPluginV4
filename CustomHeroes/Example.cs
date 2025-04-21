using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Core;
using WarcraftPlugin.Core.Effects;
using WarcraftPlugin.Events.ExtendedEvents;
using WarcraftPlugin.Helpers;
using WarcraftPlugin.Models;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

/////////////////////////////////////////////////////////////////////////////////
/// Every "using" is basically you're importing certain functions that are either premade inside the WarcraftPlugin for example the helpers
/// If you are missing a correct using for whatever you want to code, the error will be something like "Are you missing a directive?"
/// /////////////////////////////////////////////////////////////////////////////////

namespace WarcraftPlugin.Classes // Every class needs to be within this namespace
{
    public class Example : WarcraftClass // this is your race class where you'll be having MOST of your functions in, you should name it the same as your file
    {
        public override string DisplayName => "Example"; // This is just the name that you will see in-game when choosing this race


        public override Color DefaultColor => Color.White; // leave this at white unless you want a specific color.

        public override List<IWarcraftAbility> Abilities =>
        [
            new WarcraftAbility("AbilityName1", "Description text"),
            new WarcraftAbility("AbilityName2", "Description text"),
            new WarcraftAbility("AbilityName3", "Description text"),
            new WarcraftCooldownAbility("UltimateSkillName", "Description text", 60f) // the "60f" represents a float value of 60, which in this case is our ultimate cooldown.
        ];

        public override void Register()
        {
            //// Inside the Register, you register the Events your race will be using
            /// In our case, we'll be covering 4 basic Events + the ultimate
            HookEvent<EventPlayerSpawn>(PlayerSpawn);
            HookEvent<EventWeaponFire>(PlayerShoot);
            HookEvent<EventPlayerHurt>(PlayerHurt);
            // the function name between () can litterally be anything, as long as it matches with the name you use below for this function.
            // To show this I will rename the PlayerHurtOther to something you wouldn't usually do, but it's perfectly fine to do.
            HookEvent<EventPlayerHurtOther>(ThisFunctionTriggersWhenHurtingAnotherPlayer);
            ////////////////////////////////////////////
            HookAbility(3, Ultimate);
            // The 3 stands for index 3. Meaning your ability 1 has index 0, ability 2 has index 1, ability 3 has index 2 and ability 4/ultimate has index 3. 
            // The ultimate here is referencing the ultimate function we have below in this file
        }
        private void PlayerSpawn(EventPlayerSpawn spawn)
        {
            // private means that this should just be used within this class
            // When making helper functions that you might store outside a racefile you're going to want to use public instead of private.
            // Lets start by doing something simple in spawn, we'll be giving the player some bonus health depending on the ability level.
            // the term "Player" is globally defined, and you can use this in most cases.
            WarcraftPlugin.Instance.AddTimer(0.2f, () => // This code is to delay all the effects we'll be calling in PlayerSpawn
            {
                // why are we delaying it? because the CS2 engine sets your health and speed etc all to the default value on spawn.
                // The timing of this can overlap with our code, so we want our buffs in spawn to trigger just slightly after spawning to avoid issues.

                // The first thing you want to do in almost every function is open with a nullcheck like this:
                if (!Player.IsValid || Player.PlayerPawn?.Value == null || !Player.IsAlive())
                    return;

                // it may seem silly as you think hey I just spawned, how can my value be null or how can I not be alive?
                // There's many ways this can go wrong, so you want to introduce nullchecks before having most of your code
                // Also note that when we are using a delay like we are doing here in playerspawn, it's especially good to use nullchecks
                // because even though it's 0.2 seconds alot can happen in this time

                Player.SetHp(200); // This is the simpelest code to just straightup set the player's health to 200.
                // However this is not really how I'd recommend doing it most of the time, because you want abilities to scale with their level
                // So this is how we do that:
                int abilityLevel = WarcraftPlayer.GetAbilityLevel(0); // remember Index number? we use the 0 here to say hey we want to get the level of the ability with index 0, in our case that's ability 1
                int boostedHealth = 100 + (abilityLevel * 10); // We are storing a value on the name boostedHealth
                // when our ability is level 5, this will give a value of 100 + (5 * 10) = 150 health
                Player.SetHp(boostedHealth); // now we are setting the users health depending on his ability level.
            });
        }

        private void ThisFunctionTriggersWhenHurtingAnotherPlayer(EventPlayerHurtOther @event)
        {
            // This is the function that triggers when you are shooting someone.
            var pawn = Player.PlayerPawn.Value; // we are getting the value of our user so we can change it after
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(2);

            if (!attacker.IsValid || !victim.IsValid || !victim.IsAlive()) return;

            if (attacker.TeamNum == victim.TeamNum)
                return;

            // underneath this we'll be storing the name of the weapon we are shooting someone with
            // Then we'll check if it's a deagle, if it's a deagle then we will allow it to deal additional damage            

            var activeWeaponName = pawn.WeaponServices!.ActiveWeapon.Value.DesignerName;
            if (activeWeaponName == "weapon_deagle")
            {
                var damageBonus = WarcraftPlayer.GetAbilityLevel(2) * 12; // We are setting the damageBonus to a maximum of 60 additional damage for each shot.
                @event.AddBonusDamage(damageBonus);
                attacker.PrintToChat($" {ChatColors.Green}You dealt {@event.DmgHealth} additional damage!");
                // I will explain everything about PrintToChat etc in the next event.
            }
        }

        private void PlayerHurt(EventPlayerHurt @event)
        {
            // This is the function that triggers when the USER is the victim.
            var victim = @event.Userid; // we store the victim's ID under the variablename victim for easy use
            var attacker = @event.Attacker; // Same as above but for attacker
            if (attacker == null || victim == null || !attacker.IsValid || !victim.IsValid) return;
            // Again we check if any of the players that are related to this event are valid to avoid nullerrors

            // Next I'll show you how you can make an ability only trigger x% of the time and even scale this % with abilitylevel
            int abilityLevel = WarcraftPlayer.GetAbilityLevel(2); // We used index 0 for the last one, lets say this skill is the ability 3, so index 2!
            // we can keep the same name since this is only within our PlayerHurt function
            // if we need multiple abilities their levels then we should do this one for the next one:
            int abilityLevel1 = WarcraftPlayer.GetAbilityLevel(1);
            float evasionChance = 0.4f; // if we were to use this evasionChance below as parameter, it would result in 40% chance to activate
            // This is a flat value, lets change it up so it scales like this:
            float scalingEvasionChance = abilityLevel / 10f;
            // remember to use an f behind the number whenever you're using a float
            // for an integer like the ability level, we don't need this "f".

            if (Random.Shared.NextDouble() < scalingEvasionChance) // replace scalingEvasionChance with EvasionChance to simply use a flat 40% chance.
            {
                int dmgDealt = @event.DmgHealth; // This is how you can store how much damage is being dealt in a integer. we'll use this below.
                @event.IgnoreDamage(); // You can check the wiki for more information about damage and damage prevention, but this is the general method to avoid damage.

                // Lets look at a few ways we can give visual confirmation on what we are doing with our skill:
                victim.PrintToChat("⚡ You evaded incoming damage!");
                victim.PrintToChat($" {ChatColors.Green}⚡ You evaded incoming damage!");
                victim.PrintToChat($" ⚡ You evaded incoming damage from {attacker.PlayerName}!");
                victim.PrintToCenter($"Evaded {dmgDealt} damage");
                Console.WriteLine($"[Evasion] {victim.PlayerName} evaded {dmgDealt} damage from {attacker.PlayerName} !");


                // Take a moment to see the difference between all these messages
                // First thing to notice is that we 3 different ways to get feedback info:
                // - PrintToChat : mostly used in finished races, for visual feedback on certain abilities
                // PrintToCenter : If you have like a stacking effect that updates often but still want to display it without spamming the chat, this is perfect. Note you can't use colors for this.
                // Console.WriteLine : Probably the most important one to start out with, because the console is your best friend when it comes to finding out issues.
                // Lets say something doesn't work, just paste a few console writelines inbetween your code, that helps with narrowing down the problem.

                // Next thing you might notice is that something there's a "$" in front of the ""
                // This dollarsign says to the system hey, in this message there's going to information depending on situation
                // For example the attacker's playername won't always be the same, but if we still want to log this properly, this is how
                // Same for how much damage is being dealt. We store this number already under the int dmgDealt
                // So when we call it inside our "text box" the system knows it doesn't have to print out the raw text but look for the values given

                // Next thing you might see is that we have a {ChatColors.Green} 
                // What's important to know about this is that you use most colors, but if you are using it at the START like we are doing in our example
                // Then there HAS to be a space in front of the first bracket : 
                victim.PrintToChat($" {ChatColors.Green}⚡ This text will be green!");
                victim.PrintToChat($"{ChatColors.Green}⚡ This text will NOT be green because there's no space in front of our first opening bracket!");
            }
        }

        private void PlayerShoot(EventWeaponFire @event)
        {
            if (Player.IsValid && Player.IsAlive())
            {
                Console.WriteLine("You just shot a bullet!");
                var activeWeapon = Player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value;
                if (activeWeapon != null) // Making sure again that there's no null errors
                {
                    activeWeapon.Clip1 = activeWeapon.GetVData<CBasePlayerWeaponVData>().MaxClip1;
                    Console.WriteLine($"[INFO] {Player.PlayerName}'s ammo refilled to max ({activeWeapon.Clip1})!");
                    // Everytime the WeaponFire event is triggered, we are getting the player's weapon, and settings its clip to max
                }
            }
        }

        private void Ultimate()
        {
            // This ultimate is the ultimate I'm using in LaserLightShow race
            // This ultimate does the following: On pressing ult: Shoot a colorfull laser in the direction you're looking
            // After a brief delay, an explosion will occur at the center of the spot where you were looking.
            // I'll be explaining how I did this one
            int abilityLevel0 = WarcraftPlayer.GetAbilityLevel(0);
            int abilityLevel1 = WarcraftPlayer.GetAbilityLevel(1);
            int abilityLevel2 = WarcraftPlayer.GetAbilityLevel(2);
            // I made the ultimate radius scalable with all the ability levels
            // Currently this is completely useless because you can't level your ult untill you're maxlevel currently
            // However I have plans to change this in the future, that's why I did this already.

            int AbilityLevelMult = abilityLevel0 * abilityLevel1 * abilityLevel2;
            // if we are max level, AbilityLevelMult = 125 (5*5*5)
            float radius = 900f + AbilityLevelMult; // This makes the radius 1025 at max level.

            var eyePos = Player.EyePosition(); // We are storing the player's eye position because want to originate t
            eyePos.Z += 30f; // We are slighly raising the height of the starting point, to avoid it obstructive effect (AKA we still wanna see stuff clearly)
            var forward = Player.PlayerPawn.Value.EyeAngles.ToForward();
            // I'm using a ToForward function I added in the LaserLightShow file if you want to take a look.
            // This won't work without that code, but really all it does is calculate what angle is the one in front of us.
            var targetPos = eyePos + forward * 1000f;

            // here we set our targetPosition, meaning we start from our eyepos + little increase in height
            // We shoot it forward for a 1000 units length

            Color[] beamColors = { Color.Red, Color.Green, Color.Blue };
            Vector[] offsets = {
                new Vector(5f, 0, 0),
                new Vector(-5f, 0, 0),
                new Vector(0, 5f, 0)
            }; // The initial laser has 3 colors, Red Green and blue, we offset them slightly off eachother so they don't overlap, and are each visible on their own

            foreach (var offset in offsets)
            {
                var beamStart = eyePos + offset;
                var jitteredEnd = targetPos + new Vector(0, 0, Random.Shared.Next(-10, 10));

                Warcraft.DrawLaserBetween(beamStart, jitteredEnd, beamColors[Array.IndexOf(offsets, offset)], duration: 1.5f, width: 4f);
                // DrawLaserBetween is a function made by BoinK I believe, if you hover it in your code editor you can see its different arguments that it takes
                // In fact this is a very big TIP, whenever you don't know how to use a function you can do the following:
                // - Hover the function to see its parameters in this case it's 'vector StartPos, vector EndPos, Color, duration and width'
                // If that's not clear enough, you should be able to select the name "DrawLaserBetween" right click it and "Go to Definition"
                // This will bring you to do the file and place where this function is defined, so you can see for yourself how it works.
            }


            WarcraftPlugin.Instance.AddTimer(1.5f, () =>
            {
                // This is the second part of the ultimate, the part that deals damage
                // First things first, the entire effect is really only just the following line:
                Warcraft.SpawnExplosion(targetPos, (AbilityLevelMult - 50f), radius, Player, KillFeedIcon.prop_exploding_barrel);
                // SpawnExplosion uses a targetPosition where we spawn the explosion
                // it uses a radius, it uses the parameter from the user of the effect and an optional parameter to customize the killfeedicon
                // Everything below this is basically purely cosmetic (Effects)
                int beamCount = 32; // This is the amount of beams that will shoot out the center
                float angleStep = 360f / beamCount;


                // I'm not going to pretend to 300% understand everything underneath this, because math has never been my strongsuit
                // But really all it does it create random directions for each beam to go to
                for (int i = 0; i < beamCount; i++)
                {
                    double theta = Random.Shared.NextDouble() * 2 * Math.PI;
                    double phi = Math.Acos(2 * Random.Shared.NextDouble() - 1);
                    float x = (float)(Math.Sin(phi) * Math.Cos(theta));
                    float y = (float)(Math.Sin(phi) * Math.Sin(theta));
                    float z = (float)Math.Cos(phi);

                    var dir = new Vector(x, y, z);
                    var end = targetPos + dir * radius;

                    var color = Color.FromArgb(Random.Shared.Next(256), Random.Shared.Next(256), Random.Shared.Next(256));
                    // this is basically us also randomizing the color each beam will get. the RGB values go up to 255. so we pick 3 random numbers to gain our random color
                    Warcraft.DrawLaserBetween(targetPos, end, color, duration: 2.5f, width: 2f);
                }
            });

            Player.PrintToChat($" {ChatColors.Green}Disintigrate{ChatColors.Default} Ultimate activated!");
            // Again visual confirmation for the ultimate
            StartCooldown(3); // This is important, as it starts the ultimate cooldown. in our case from this example it's 60 seconds.
        }


    }
}









