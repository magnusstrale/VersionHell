using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace VersionHell
{
    public class DependencyNode
    {
        private static Dictionary<AssemblyName, List<DependencyNode>> _allAssemblies = new Dictionary<AssemblyName, List<DependencyNode>>(new VersionInsensitiveAssemblyNameComparer());
        private static List<(DependencyNode, AssemblyName)> _missingAssemblies = new List<(DependencyNode, AssemblyName)>();
        private DependencyNode _parent;
        private List<DependencyNode> _children;
        private AssemblyName _assemblyName;

        public DependencyNode(AssemblyName node, DependencyNode parent = null)
        {
            _parent = parent;
            _children = new List<DependencyNode>();
            _assemblyName = node;
        }

        private void Add(DependencyNode child)
        {
            _children.Add(child);
            var assemblyName = child._assemblyName;
            if (!_allAssemblies.TryGetValue(assemblyName, out var sameAssemblies))
            {
                sameAssemblies = new List<DependencyNode>();
                _allAssemblies[assemblyName] = sameAssemblies;
            }
            sameAssemblies.Add(child);
        }

        public static DependencyNode Build(string fullName)
        {
            var rootAssembly = Assembly.ReflectionOnlyLoad(fullName);
            var root = new DependencyNode(rootAssembly.GetName());
            root.Traverse(rootAssembly);
            return root;
        }

        private void Traverse(Assembly source)
        {
            foreach (var assemblyName in source.GetReferencedAssemblies())
            {
                if (IsAlreadyExamined(assemblyName)) continue;
                var child = new DependencyNode(assemblyName, this);
                Add(child);
                var assembly = Load(assemblyName);
                if (assembly == null) continue;
                child.Traverse(assembly);
            }
        }

        private static bool IsAlreadyExamined(AssemblyName assemblyName)
        {
            if (_allAssemblies.TryGetValue(assemblyName, out var sameAssemblies))
            {
                if (sameAssemblies.Any(d => d._assemblyName.FullName == assemblyName.FullName)) return true;
            }
            return false;
        }

        private Assembly Load(AssemblyName assemblyName)
        {
            try
            {
                return Assembly.ReflectionOnlyLoad(assemblyName.FullName);
            }
            catch (FileLoadException)
            {
                // Version mismatch will give this exception
                return null;
            }
            catch (FileNotFoundException)
            {
                // If the assembly cannot be found at all
                _missingAssemblies.Add((this, assemblyName));
                return null;
            }
        }

        public void Report()
        {
            Console.WriteLine();
            foreach (var kvp in _allAssemblies)
            {
                var assemblyName = kvp.Key;
                var sameAssemblies = kvp.Value;
                if (sameAssemblies.Count == 0) continue;
                if (sameAssemblies.All(a => a._assemblyName.Version == assemblyName.Version)) continue;

                Console.WriteLine("====================================");
                WriteColor("Mismatch for " + assemblyName, ConsoleColor.DarkRed);
                Console.WriteLine();
                foreach (var node in sameAssemblies)
                {
                    WriteNode(node, ConsoleColor.Red);
                    Console.WriteLine();
                }
            }

            Console.WriteLine("====================================");
            WriteColor("Missing assemblies:", ConsoleColor.DarkYellow);
            Console.WriteLine();
            foreach (var missing in _missingAssemblies)
            {
                var node = missing.Item1;
                var assemblyName = missing.Item2;
                {
                    WriteColor(assemblyName.ToString(), ConsoleColor.Yellow);
                    Console.Write(" referenced by \r\n");
                    Console.WriteLine(node.ToString(NodePart.Full | NodePart.SplitLine));
                }
            }
        }

        private static void WriteColor(string message, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = oldColor;
        }

        private static void WriteNode(DependencyNode node, ConsoleColor color = ConsoleColor.White)
        {
            Console.Write(node.ToString(NodePart.Path | NodePart.SplitLine));
            WriteColor("    " + node.ToString(NodePart.Leaf), color);
            Console.WriteLine();
        }

        public override string ToString() => ToString(NodePart.Full);

        public string ToString(NodePart part)
        {
            var n = this;
            string value = "";
            var list = new List<DependencyNode>();
            if ((part & NodePart.Leaf) == NodePart.Leaf)
            {
                list.Add(n);
            }
            if ((part & NodePart.Path) == NodePart.Path)
            {
                while (n._parent != null)
                {
                    n = n._parent;
                    list.Insert(0, n);
                }
                if ((part & NodePart.SplitLine) == NodePart.SplitLine)
                {
                    var indent = 0;
                    foreach (var node in list)
                    {
                        for (int i = 0; i < indent; i++)
                        {
                            value += ' ';
                        }
                        value += node._assemblyName.FullName + " -> \r\n";
                        indent += 4;
                    }

                }
                else
                {
                    foreach (var node in list)
                    {
                        value += node._assemblyName.FullName + " -> ";
                    }
                }
                return value;
            }

            return n._assemblyName.FullName;
        }
    }

    [Flags]
    public enum NodePart
    {
        Full = 3,
        Path = 1,
        Leaf = 2,
        SplitLine = 4
    }
}
