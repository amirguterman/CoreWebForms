// MIT License.

using Microsoft.Extensions.FileProviders;

namespace WebForms.Compiler.Dynamic;

internal interface IPageCompiler
{
    Task<ICompiledPage> CompilePageAsync(IFileProvider files, string path, CancellationToken token);
}
