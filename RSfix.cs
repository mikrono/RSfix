using Terraria.ModLoader;
using System.Reflection;
using System;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using System.Collections;
using System.Linq;
using System.ComponentModel;
using System.IO;
using Terraria.Localization;

namespace RSfix
{
	public class RSfix : Mod
	{
		private static MethodInfo TransToString = null;
        private static MethodInfo MakeSetupLdstrFolds = null;
        private static MethodInfo ExportGetUsingHjson = null;
        public static IList EntryList = null;

        public override void PostSetupContent()
        {
            TransToString = RUtils.FindTargetMethod("_ReplaceString_", "Translation", "ToString", BindingFlags.Public | BindingFlags.Instance);
            MakeSetupLdstrFolds = RUtils.FindTargetMethod("_ReplaceString_", "Make", "SetupLdstrFolds", BindingFlags.NonPublic | BindingFlags.Static);
            ExportGetUsingHjson = RUtils.FindTargetMethod("_ReplaceString_", "Export", "GetUsingHjson", BindingFlags.Public | BindingFlags.Instance, new Type[] {});

            if (TransToString != null)
            {
                ModifyToString += IL_ModifyToString;
            }

            if (MakeSetupLdstrFolds != null)
            {
                ModifySetupLdstrFolds += IL_ModifySetupLdstrFolds;
            }

            if (ExportGetUsingHjson != null)
            {
                ModifyGetUsingHjson += IL_ModifyGetUsingHjson;
            }
        }



        public override void Unload()
        {
            ModifyToString -= IL_ModifyToString;
            ModifySetupLdstrFolds -= IL_ModifySetupLdstrFolds;
            ModifyGetUsingHjson -= IL_ModifyGetUsingHjson;
        }

        private void IL_ModifyToString(ILContext il)
        {
            var c = new ILCursor(il);

            c.Next = null;
            c.GotoPrev(MoveType.Before, i => i.MatchCall<string>("Concat"));

            c.EmitDelegate<Func<string, string>>(TransValue =>
            {
                if (TransValue == " ")
                {
                    return $"\"{TransValue}\"";
                }
                else
                {
                    return TransValue;
                }
            });

            c.GotoPrev(MoveType.Before, i => i.MatchLdstr("^(?:{|}|\\[|\\]|:|,|\"|')"));
            string new_pattern = @"^({|}|\\|\[|\]|:|,|""|')";
            c.Next.Operand = new_pattern;
        }

        private static void IL_ModifySetupLdstrFolds(ILContext il)
        {
            var c = new ILCursor(il);

            Type Type_Translation = RUtils.FindType("_ReplaceString_", "Translation");
            Type Type_Entry = RUtils.FindType("_ReplaceString_", "Entry");



            c.GotoNext(MoveType.After,
                i => i.MatchNewobj<System.Text.StringBuilder>(),
                i => i.MatchStloc(2));

            c.Index += 2;

            c.RemoveRange(11);

            c.EmitDelegate<Func<object, object>>((rootEntry) =>
            {
                IList TranslastionList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Type_Translation));

                foreach (var entry in rootEntry as IList)
                {
                    if (entry.GetType() == Type_Translation)
                    {
                        TranslastionList.Add(entry);
                    }
                }
                return TranslastionList.GetEnumerator();
            });




            
            c.GotoNext(MoveType.After,
                i => i.MatchCallvirt<System.Text.StringBuilder>("Clear"),
                i => i.MatchPop(),
                i => i.MatchLdloc(3));

            c.Index += 2;


            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, Type_Entry.GetField("children", BindingFlags.Public | BindingFlags.Instance));
            c.Emit(OpCodes.Ldarg_2);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<object, string, object>>((rootEntry, path, root) =>
            {
                foreach (var entry in rootEntry as IList)
                {
                    if (entry.GetType() != Type_Translation)
                    {
                        string newPath = path + "/" + root.GetType().GetField("name").GetValue(root);
                        if (!Directory.Exists(newPath))
                        {
                            Directory.CreateDirectory(newPath);
                        }
                        MakeSetupLdstrFolds.Invoke(null, new object[] { entry, entry, newPath});
                    }
                }
            });
        }

        private static void IL_ModifyGetUsingHjson(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                i => i.MatchCall("Terraria.Localization.Language", "get_ActiveCulture"));

            c.Index += 6;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<object, List<(string, string)>, List<(string, string)>>> ((Export, hjson) =>
            {
                string modPrefix = "Mods." + ((Mod)Export.GetType().GetField("mod").GetValue(Export)).Name;
                string[] exclude =
                {
                    "ItemName",
                    "ItemTooltip",
                    "ProjectileName",
                    "DamageClassName",
                    "InfoDisplayName",
                    "BiomeName",
                    "BuffName",
                    "BuffDescription",
                    "NPCName",
                    "Prefix",
                    "Containers"
                };
            
                foreach (var (key, value) in typeof(LocalizationLoader).GetField("translations", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<string, ModTranslation>)
                {
                    if (key.Contains(modPrefix) && !exclude.Any((string s) => key.Contains(s)))
                    {
                        hjson.Add((key, value.GetTranslation(Language.ActiveCulture)));
                    }
                }
                return hjson;
            });
            c.Emit(OpCodes.Stloc_0);
        }
        
        private event ILContext.Manipulator ModifyToString
        {
            add => HookEndpointManager.Modify(TransToString, value);
            remove => HookEndpointManager.Remove(TransToString, value);
        }

        private event ILContext.Manipulator ModifySetupLdstrFolds
        {
            add => HookEndpointManager.Modify(MakeSetupLdstrFolds, value);
            remove => HookEndpointManager.Remove(MakeSetupLdstrFolds, value);
        }

        private event ILContext.Manipulator ModifyGetUsingHjson
        {
            add => HookEndpointManager.Modify(ExportGetUsingHjson, value);
            remove => HookEndpointManager.Remove(ExportGetUsingHjson, value);
        }
    }
}