using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VersionHell
{
    public class VersionInsensitiveAssemblyNameComparer : IEqualityComparer<AssemblyName>
    {
        public bool Equals(AssemblyName x, AssemblyName y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(AssemblyName obj) => obj.Name.GetHashCode();
    }
}
