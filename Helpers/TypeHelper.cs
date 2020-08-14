using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Monocle;
using System.Reflection;
using Microsoft.Xna.Framework;
using Celeste.Mod.Helpers;
using NLua;
using System.Collections;

namespace Celeste.Mod.PandorasBox
{
    public class TypeHelper
    {
        public static Type GetTypeFromString(string name)
        {
            return FakeAssembly.GetFakeEntryAssembly().GetType(name);
        }

        public static List<Type> GetTypesFromString(string name, char sep=',')
        {
            List<Type> types = new List<Type>();

            if (string.IsNullOrEmpty(name))
            {
                return types;
            }

            foreach (String s in name.Split(sep))
            {
                types.Add(TypeHelper.GetTypeFromString(s));
            }

            return types;
        }
    }
}
