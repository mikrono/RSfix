using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace RSfix
{
    public static class RUtils
    {
        public static MethodInfo FindTargetMethod(string ModName, string className, string methodName, BindingFlags flags)
        {
            MethodInfo targetMethod = FindType(ModName, className).GetMethod(methodName, flags);


            if (targetMethod != null)
            {
                return targetMethod;
            }
            else
            {
                Console.WriteLine("Failed to find a method.");
            }


            Console.WriteLine($"RSfix\nmod name : {ModName}\nclass name : {className}\nmethod name : {methodName}\nbinding flags : {flags.ToString()}");

            return null;
        }

        public static MethodInfo FindTargetMethod(string ModName, string className, string methodName, BindingFlags flags, Type[] types)
        {
            MethodInfo targetMethod = FindType(ModName, className).GetMethod(methodName, flags, types);


            if (targetMethod != null)
            {
                return targetMethod;
            }
            else
            {
                Console.WriteLine("Failed to find a method.");
            }


            Console.WriteLine($"RSfix\nmod name : {ModName}\nclass name : {className}\nmethod name : {methodName}\nbinding flags : {flags.ToString()}");

            return null;
        }

        public static Type FindType(string ModName, string className)
        {
            Mod mod = ModLoader.GetMod(ModName);

            if (mod != null)
            {
                Type targetClass = null;
                Assembly modAssembly = mod.GetType().Assembly;

                foreach (Type t in modAssembly.GetTypes())
                {
                    if (t.Name == className)
                    {
                        targetClass = t;
                    }
                }

                if (targetClass != null)
                {
                    return targetClass;
                }
                else
                {
                    Console.WriteLine("Filed to find a class.");
                }
            }
            else
            {
                Console.WriteLine("Failed to find a mod.");
            }

            return null;
        }
    }
}
