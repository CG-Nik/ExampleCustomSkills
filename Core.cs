using Alta;
using Alta.Impact;
using Alta.Networking;
using Alta.Networking.Scripts.Player;
using CustomSkillsAPI;
using HarmonyLib;
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
        private List<ProgressionSlot> progressionSlots = [];

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");

            // these set up the hooks that CustomSkillsAPI provides
            // the point of the hooks is to ensure that multiple mods can all set their skills up correctly
            CustomSkillsAPI.Core.SetUpProgressionSlots += _SetUpProgressionSlots;
            CustomSkillsAPI.Core.AddProgressionSlots += _AddProgressionSlots;
            CustomSkillsAPI.Core.AddInherits += _AddInherits;
        }

        // this is where a mod should set up progression slots and profession skills
        // assuming you copy this example mod's code as a base for another custom skill mod, this is the only hook that you need to modify the code for
        // the other hooks shouldn't be modified
        private void _SetUpProgressionSlots()
        {
            // this loads an asset bundle containing a partially set up skill object and the texture for the skill
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(MelonEnvironment.ModsDirectory, "ExampleCustomSkills/AssetBundles/!examplecustomskills"));
            // this loads the skill from the asset bundle
            // when making the object for this skill, make sure that the hash you set is between 1-65535, otherwise the skill will have bugs
            // also remember that, like targetImpactChannel, some things will need to be set again even if set in the inspector, due to the bundle generating a new instance of that value
            // also, try to ensure that your hash is unique from any vanilla and modded hashes
            ProfessionSkill customSkill = assetBundle.LoadAsset<ProfessionSkill>("Side Power Hit.asset");
            // this is used later for things that need to reference the specific ProfessionSkill extension and not just ProfessionSkill
            PowerHit sidePowerHit = customSkill as PowerHit;

            // these set the values of several values that need to be set again, despite being possible to set in the inspector
            // the reason why these need to be set again is due to the assetBundle importing method creating new instances of objects, which can break things

            // this sets the targetImpactChannel of the skill to be the same as Power Hit's targetImpactChannel
            // if not set, the skill can't be charged up, due to the tool's impactChannel and the skill's impactChannel being different instances
            typeof(PowerHit).GetField("targetImpactChannel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sidePowerHit, typeof(PowerHit).GetField("targetImpactChannel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ProfessionSkill.All.Where(skill => skill.Hash == 51076u).First() as PowerHit));
            // this sets the inputStructure of the skill to be the same as Power Stinger's inputStructure
            // if not set, the changes made to HandOnSideStructure by HandOnSideStructurePatch don't apply, since it's a different HandOnSideStructure instance
            // as this shows, you should set all values that would create a new instance again, even if they wouldn't normally break anything
            typeof(PickupBasedSkill).GetField("inputStructure", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sidePowerHit, typeof(PickupBasedSkill).GetField("inputStructure", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ProfessionSkill.All.Where(skill => skill.Hash == 60198u).First() as PowerStinger));

            // this is called to ensure that the dictionary that the custom skill will be added to is actually set up
            ProfessionSkill.CheckItems();
            // this gets the dictionary's value so the skill can be added to it
            Dictionary<uint, ProfessionSkill> items = (Dictionary<uint, ProfessionSkill>)typeof(HashedGeneralValue<ProfessionSkill>).GetField("items", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            // this adds the skill to the dictionary of all skills
            items.Add(customSkill.Hash, customSkill);
            // this creates the ProgressionSlot instance that will have the custom skill
            // a ProgressionSlot is the physical bubble that a skill is on
            ProgressionSlot progressionSlot = new(customSkill);
            // this foreach is used to set various values for the progression slot
            foreach (var fieldInfo in progressionSlot.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                switch (fieldInfo.Name)
                {
                    case "name": // this is the name of the progression slot
                        // if not manually set, this will default to the name of the skill that the progression slot uses
                        fieldInfo.SetValue(progressionSlot, "Side Power Hit");
                        break;
                    case "cost": // this is the amount of skill points that the progression slot costs
                        // this defaults to 1 point, so you can remove this if you want your skill to cost 1 point
                        fieldInfo.SetValue(progressionSlot, 1u);
                        break;
                    case "dependencies": // this sets all of the progression slots that are required to get this progression slot
                        // in this case, PowerStrike is required to get this progression slot
                        // you just need to set the value to be the one directly before this progression slot, not all of the ones preceding it
                        // as in, Air Slash only has Power Hit as a dependency, it doesn't ALSO have Power Strike
                        fieldInfo.SetValue(progressionSlot, new List<uint>([7141u]));
                        break;
                    case "hash": // this sets the hash of the progression slot
                        // like the name of the progression slot, this will be automatically set for you if not manually set
                        // while the custom skill object needs to have a hash between 1-65535, I don't think that the progression slot needs to have a hash in that range
                        // to be safe, you should make the hash be within that range, though
                        // the hash, if not manually set, will be equal to the skill's hash, plus a value
                        // that value depends on whether or not this progression slot has any dependencies
                        // if it does have dependencies, it adds all of the dependencies' hashes to its hash
                        // if it doesn't have any dependencies, it adds a random int from 1-1000
                        // this means that the hash will be variable if it doesn't depend on anything and doesn't have its hash manually set, so I recommend manually setting a hash regardless
                        // like with the skill, try to ensure that your progression slot has a unique hash from any vanilla or modded hashes
                        fieldInfo.SetValue(progressionSlot, 51077u);
                        break;
                    case "position": // this sets the position of the progression slot in the skill tree
                        // this is relative to a center that the skill tree has
                        // I believe that this position is either in world space or the center used by the skill trees is weirdly rotated
                        // Either way, generally just don't expect the values you have to set to make sense
                        fieldInfo.SetValue(progressionSlot, new Vector3(0, 0, 0));
                        break;
                    case "slotIcon": // this sets the icon of the progression slot
                        // this is set up in such a way where the game won't break if there is no icon found
                        // it not breaking if there's no icon doesn't really matter for custom skills, though, as custom skills will break if their skill object can't be found anyways
                        // if the texture is null, some mining skill's icon is used by the game as a placeholder
                        Texture2D texture = null;
                        if (assetBundle)
                        {
                            texture = assetBundle.LoadAsset<Texture2D>("SidePowerHitSlotIcon.png");
                        }
                        fieldInfo.SetValue(progressionSlot, texture);
                        break;
                    case "inheritSlots": // this sets which skills are dependent upon this skill
                        // for example, Power Hit would (with no custom skills added) have Air Slash and Ground Slam set for this
                        // you don't need to add modded skills to this, as the "AddInherits" call sets those for you
                        // this case can be removed if you aren't trying to have a vanilla skill be dependent upon a modded skill
                        fieldInfo.SetValue(progressionSlot, new List<ProgressionSlot>([]));
                        break;
                    default:
                        break;
                }
            }
            // this sets the default values of "hash" and "name", sets up the "indentLevel" field, and sets the skill tree that this skill should be on
            progressionSlot.Validate(ProfessionSkillTree.GetProfessionTree(ProgressionPath.Melee));
            // this adds the progression slot to the list of progression slots that _AddProgressionSlots() will add
            progressionSlots.Add(progressionSlot);
        }

        // this is where a mod will actually add their set up progression slots to their respective skill trees
        // this hook should not be modified, you should just copy it from this mod as is
        private void _AddProgressionSlots()
        {
            foreach (ProgressionSlot progressionSlot in progressionSlots)
            {
                ProfessionSkillTree professionSkillTree = ProfessionSkillTree.GetProfessionTree(progressionSlot.Path);

                Dictionary<uint, ProgressionSlot> slotMap = (Dictionary<uint, ProgressionSlot>)professionSkillTree.GetType().GetField("slotMap", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(professionSkillTree);
                slotMap[progressionSlot.Hash] = progressionSlot;
                professionSkillTree.Slots.Add(progressionSlot);
            }
        }

        // this is where a mod will call .AddInherit() on every progression slot they're adding
        // this hook should not be modified, you should just copy it from this mod as is
        private void _AddInherits()
        {
            foreach (ProgressionSlot progressionSlot in progressionSlots)
            {
                progressionSlot.AddInherit(ProfessionSkillTree.GetProfessionTree(progressionSlot.Path));
            }
        }
    }
}