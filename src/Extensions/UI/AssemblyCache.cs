//MIT license

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Web.Script;

namespace System.Web.UI;

// Caches Assembly APIs to improve performance
internal static class AssemblyCache {
    // PERF: Cache reference to System.Web.Extensions assembly. Use ScriptManager since it's guaranteed to be in S.W.E
    public static readonly Assembly SystemWebExtensions = typeof(ScriptManager).Assembly;
    public static readonly Assembly SystemWeb = typeof(Page).Assembly;

    internal static bool _useCompilationSection = true;

    // Maps string (assembly name) to Assembly
    private static readonly Hashtable _assemblyCache = Hashtable.Synchronized(new Hashtable());
    // Maps assembly to Version
    // internal so it can be manipulated by the unit test suite
    internal static readonly Hashtable _versionCache = Hashtable.Synchronized(new Hashtable());
    // Maps an assembly to its ajax framework assembly attribute. If it doesn't have one, it maps it to a null value
    private static readonly ConcurrentDictionary<Assembly, AjaxFrameworkAssemblyAttribute> _ajaxAssemblyAttributeCache =
        new ConcurrentDictionary<Assembly, AjaxFrameworkAssemblyAttribute>();

    //TODO presently stick with NIE
    private static CompilationSection CompilationSection {
        get {
            /* if (_compilationSection == null) {
                 _compilationSection = RuntimeConfig.GetAppConfig().Compilation;
             }
             return _compilationSection;*/
            throw new NotImplementedException();
        }
    }

    public static Version GetVersion(Assembly assembly) {
        Debug.Assert(assembly != null);
        Version version = (Version)_versionCache[assembly];
        if (version == null) {
            // use new AssemblyName() instead of assembly.GetName() so it works in medium trust
            version = new AssemblyName(assembly.FullName).Version;
            _versionCache[assembly] = version;
        }
        return version;
    }

    public static Assembly Load(string assemblyName) {
        Debug.Assert(!String.IsNullOrEmpty(assemblyName));
        Assembly assembly = (Assembly)_assemblyCache[assemblyName];
        if (assembly == null) {
            // _useCompilationSection must be set to false in a unit test environment since there
            // is no http runtime, and therefore no trust level is set.
            if (_useCompilationSection) {
                assembly = CompilationSection.LoadAssembly(assemblyName, true);
            }
            else {
                assembly = Assembly.Load(assemblyName);
            }
            _assemblyCache[assemblyName] = assembly;
        }
        return assembly;
    }

    public static bool IsAjaxFrameworkAssembly(Assembly assembly) {
        return (GetAjaxFrameworkAssemblyAttribute(assembly) != null);
    }

    public static AjaxFrameworkAssemblyAttribute GetAjaxFrameworkAssemblyAttribute(Assembly assembly) {
        Debug.Assert(assembly != null);
        AjaxFrameworkAssemblyAttribute ajaxFrameworkAssemblyAttribute;
        if (!_ajaxAssemblyAttributeCache.TryGetValue(assembly, out ajaxFrameworkAssemblyAttribute)) {
             ajaxFrameworkAssemblyAttribute = SafeGetAjaxFrameworkAssemblyAttribute(assembly);
            _ajaxAssemblyAttributeCache.TryAdd(assembly, ajaxFrameworkAssemblyAttribute);
        }
        return ajaxFrameworkAssemblyAttribute;
    }

    [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We do not want to throw from this method.")]
    internal static AjaxFrameworkAssemblyAttribute SafeGetAjaxFrameworkAssemblyAttribute(ICustomAttributeProvider attributeProvider) {
        try {
            foreach (Attribute attribute in attributeProvider.GetCustomAttributes(inherit: false)) {
                AjaxFrameworkAssemblyAttribute ajaxFrameworkAttribute = attribute as AjaxFrameworkAssemblyAttribute;
                if (ajaxFrameworkAttribute != null) {
                    return ajaxFrameworkAttribute;
                }
            }
        }
        catch {
            // Bug 34311: If we are unable to load the attribute, don't throw. 
        }
        return null;
    }
}
