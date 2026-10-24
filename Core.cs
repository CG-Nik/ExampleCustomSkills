using Alta;
using CustomSkillsAPI;
using MelonLoader;
using MelonLoader.Utils;
using System.Reflection;
using UnityEngine;

[assembly: MelonInfo(typeof(ExampleCustomSkills.Core), "ExampleCustomSkills", "1.0.0", "CGNik", null)]
[assembly: MelonGame("Alta", "A Township Tale")]

namespace ExampleCustomSkills
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");

            // these set up the hooks that CustomSkillsAPI provides
            // the point of the hooks is to ensure that multiple mods can all set their skills up correctly
            CustomSkillsAPI.Core.SetUpProgressionSlots += SetUpProgressionSlots;
        }

        // this is where you should do all of your stuff
        private void SetUpProgressionSlots()
        {
            // this loads an asset bundle containing a partially set up skill object and the texture for the skill
            AssetBundle assetBundle;
            using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleCustomSkills.AssetBundles.!examplecustomskills"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }
            // this loads the skill from the asset bundle
            // when making the object for this skill, make sure that the hash you set is between 1-65535, otherwise the skill will have bugs
            // also, try to ensure that your hash is unique from any vanilla and modded hashes
            // you also need to take note of any values that you set in the inspector that reference objects from the base game, and set those to None, then instead set them in C#
            // the reasoning behind this is because the game doesn't maintain that reference, so it creates an entirely new instance of that object
            // in order to prevent it from USING the new instance, it sets the field to the correct object, but to prevent creating the object to begin with, you need to not reference the object in the inspector
            // do note, you CAN safely reference objects from the same asset bundle
            ProfessionSkill customSkill = assetBundle.LoadAsset<ProfessionSkill>("Side Power Hit Example.asset");
            // this is used for most references to the skill, as this allows things that need references to the ProfessionSkill, the PickupBasedSkill, and the PowerHit to work
            PowerHit sidePowerHit = customSkill as PowerHit;

            // these set the values of several values that need to be set here, despite being possible to set in the inspector
            // the reason why these need to be set again is due to the assetBundle importing method creating new instances of objects if they get set in the inspector

            // these set the values of two skills for later use
            PowerHit powerHit = ProfessionSkill.All.Where(skill => skill.Hash == 51076u).First() as PowerHit;
            PowerStinger powerStinger = ProfessionSkill.All.Where(skill => skill.Hash == 60198u).First() as PowerStinger;

            // this sets the targetImpactChannel of the skill to be the same as Power Hit's targetImpactChannel
            // if not set, the skill can't be charged up, due to the tool's impactChannel and the skill's impactChannel being different instances
            typeof(PowerHit).GetField("targetImpactChannel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sidePowerHit,
                typeof(PowerHit).GetField("targetImpactChannel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(powerHit) // this is CombatImpactChannel
            );
            // this sets the inputStructure of the skill to be the same as Power Stinger's inputStructure
            // if not set, the changes made to HandOnSideStructure by HandOnSideStructurePatch don't apply, since it's a different HandOnSideStructure instance
            // as this shows, you should set all values that would create a new instance again, even if they wouldn't normally break anything
            typeof(PickupBasedSkill).GetField("inputStructure", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sidePowerHit,
                typeof(PickupBasedSkill).GetField("inputStructure", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(powerStinger) // this is HandOnSideStructure
            );
            // this sets the hitPositionEffect of the skill to be the same as PowerHit
            // if not set, the hitPositionEffect will have different instances within its values
            typeof(PickupBasedSkill).GetField("hitPositionEffect", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sidePowerHit,
                typeof(PickupBasedSkill).GetField("hitPositionEffect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(powerHit)
            );
            // this sets the hitPositionAndBladeAlignedEffect of the skill to be the same as PowerHit
            // if not set, the hitPositionAndBladeAlignedEffect will have different instances within its values
            typeof(PickupBasedSkill).GetField("hitPositionAndBladeAlignedEffect", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sidePowerHit,
                typeof(PickupBasedSkill).GetField("hitPositionAndBladeAlignedEffect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(powerHit)
            );
            // while it's not set again here due to this custom skill not requiring it, you may want to set the Target Achievement if you want one
            // since different ProfessionSkill extensions can have different values that may need to be set again, I'm not gonna list every value that should be set again here
            // additionally, these aren't set by the API FixProfessionSkillInspectorValues method since they aren't on ProfessionSkill itself, meaning you need to manually set them

            // this handles all of the stuff that the API can reasonably do automatically
            // specifically, it adds to the correct dictionary, and also fixes the practice glyph sprites for the skill
            CustomSkillsAPI.Core.AddProfessionSkill(
                sidePowerHit,
                [
                    ((Sprite[])typeof(ProfessionSkill).GetField("practiceGlyphSprites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(powerStinger))[0], // this is Glyph_Ability_HoldSideBack
                    ((Sprite[])typeof(ProfessionSkill).GetField("practiceGlyphSprites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(powerHit))[1] // this is Glyph_AbilityHitTrigger
                ]
            );

            // this API method handles everything to do with making and adding a progression slot
            // it returns a ProgressionSlot, being the created ProgressionSlot, incase you want to set anything else with the ProgressionSlot
            // there's one value on ProgressionSlot that you may want to set, being inheritSlots, which refers to every ProgressionSlot that is dependant on the ProgressionSlot
                // the values that need to be here are automatically handled by the API for modded skills that depend on this ProgressionSlot
                // this means that you'd only need to manually set anything if you were trying to have a vanilla ProgressionSlot be dependant on a modded ProgressionSlot
            // professionSkill refers to the ProfessionSkill that you're making this ProgressionSlot for
            // name refers to the name of this ProgressionSlot
            // cost refers to how many skill points this ProgressionSlot will cost
            // dependencies refers to what ProgressionSlots are directly reqiured to buy this ProgressionSlot
                // i'm not sure how it works with more than 1 item in the list, maybe it requires at least one, maybe it requires both
            // hash refers to the hash of this ProgressionSlot
                // i don't think it HAS to be in the 1-65535 range like the ProfessionSkill's hash, but you should just to be safe
                // like with the skill, try to ensure that your progression slot has a unique hash from any vanilla or modded hashes
            // position refers to the position that the ProgressionSlot will show up at relative to some center that the skill tree has
                // reasonable values are from -0.5f to 0.5f on each axis, for the most part
                // the way it positions it is by parenting it to the center object, and (at least for the melee tree, but probably all of them) the center is rotated
                // this means that the axes aren't exactly normal, so the x axis might not actually be the x axis
            // progressionPath refers to the ProgressionPath of the skill tree that this ProgressionSlot will be put on
            // slotIcon refers to the texture of the slot's icon
                // this should be loaded with an assetbundle
                // you can choose to not set this, in which case the game defaults it to the dynamite strike slot icon when as an orb, and no slot icon when practicing the skill
            CustomSkillsAPI.Core.AddProgressionSlot(
                sidePowerHit,
                "Side Power Hit",
                1u,
                new List<uint>([7141u]),
                51077u,
                new Vector3(0f, 0.1f, 0.1f),
                ProgressionPath.Melee,
                assetBundle.LoadAsset<Texture2D>("SidePowerHitSlotIcon Example.png")
            );
        }
    }
}