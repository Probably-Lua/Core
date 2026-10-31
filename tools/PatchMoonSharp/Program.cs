using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: PatchMoonSharp <merged-ProbablyLua.dll> <patched-ProbablyLua.dll> <game-managed-directory>");
    return 2;
}

string input = Path.GetFullPath(args[0]);
string output = Path.GetFullPath(args[1]);
string managedDirectory = Path.GetFullPath(args[2]);
if (!File.Exists(input))
    throw new FileNotFoundException("Merged assembly not found.", input);

if (!File.Exists(Path.Combine(managedDirectory, "Assembly-CSharp.dll")))
    throw new FileNotFoundException("Game Assembly-CSharp.dll not found in Managed directory.", Path.Combine(managedDirectory, "Assembly-CSharp.dll"));

if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Input and output assembly paths must differ.");

using var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(managedDirectory);
resolver.AddSearchDirectory(Path.GetDirectoryName(input)!);

using var assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters
{
    InMemory = true,
    ReadSymbols = false,
    AssemblyResolver = resolver
});

ModuleDefinition module = assembly.MainModule;
TypeDefinition targetType = module.GetTypes().SingleOrDefault(type =>
    type.FullName == "MoonSharp.Interpreter.Platforms.StandardPlatformAccessor")
    ?? throw new InvalidOperationException("Merged MoonSharp type not found. Patch the ILRepack standalone DLL, not the intermediate DLL.");

MethodDefinition method = targetType.Methods.SingleOrDefault(m => m.Name == "OS_Execute")
    ?? throw new InvalidOperationException("MoonSharp OS_Execute was not found. Inspect the MoonSharp version before publishing.");

if (!method.HasBody)
    throw new InvalidOperationException("MoonSharp OS_Execute has no IL body.");

static IEnumerable<string> ProcessCalls(ModuleDefinition target)
{
    foreach (TypeDefinition type in target.GetTypes())
    {
        foreach (MethodDefinition current in type.Methods)
        {
            if (!current.HasBody)
                continue;

            foreach (Instruction instruction in current.Body.Instructions)
            {
                if (instruction.Operand is not MethodReference called)
                    continue;

                if (called.DeclaringType.FullName.StartsWith("System.Diagnostics.Process", StringComparison.Ordinal))
                    yield return $"{current.FullName} -> {called.FullName}";
            }
        }
    }
}

var originalProcessCalls = ProcessCalls(module).ToArray();
if (!originalProcessCalls.Any(call => call.Contains("StandardPlatformAccessor::OS_Execute", StringComparison.Ordinal)))
    throw new InvalidOperationException("Expected process API references inside OS_Execute were not found. The library may have changed; refusing an unverified patch.");

// Leave the signature intact, so callers still link, but never start a process.
var body = method.Body;
body.Instructions.Clear();
body.ExceptionHandlers.Clear();
body.Variables.Clear();
body.InitLocals = false;
body.MaxStackSize = 8;

// Use the merged assembly's core library reference instead of this tool's .NET runtime.
var exceptionType = new TypeReference("System", "NotSupportedException", module, module.TypeSystem.CoreLibrary);
var constructor = new MethodReference(".ctor", module.TypeSystem.Void, exceptionType) { HasThis = true };
constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
ILProcessor il = body.GetILProcessor();
il.Emit(OpCodes.Ldstr, "Process execution is not supported by Probably Lua.");
il.Emit(OpCodes.Newobj, constructor);
il.Emit(OpCodes.Throw);

string[] remainingProcessCalls = ProcessCalls(module).ToArray();
if (remainingProcessCalls.Length != 0)
    throw new InvalidOperationException("Process API calls remain in the packed DLL; refusing to publish it:\n" + string.Join("\n", remainingProcessCalls));

string directory = Path.GetDirectoryName(output)!;
Directory.CreateDirectory(directory);
string temporary = Path.Combine(directory, ".ProbablyLua.patch-" + Guid.NewGuid().ToString("N") + ".dll");
try
{
    assembly.Write(temporary);
    using var check = AssemblyDefinition.ReadAssembly(temporary, new ReaderParameters
    {
        AssemblyResolver = resolver
    });

    var checkedType = check.MainModule.GetTypes().Single(t => t.FullName == targetType.FullName);
    var checkedMethod = checkedType.Methods.Single(m => m.Name == method.Name);
    var instructions = checkedMethod.Body.Instructions;
    if (instructions.Count != 3 ||
        instructions[0].OpCode != OpCodes.Ldstr ||
        instructions[1].OpCode != OpCodes.Newobj ||
        instructions[2].OpCode != OpCodes.Throw ||
        ProcessCalls(check.MainModule).Any())
        throw new InvalidOperationException("Post-write IL verification failed.");

    File.Move(temporary, output, overwrite: true);
}

finally
{
    if (File.Exists(temporary))
        File.Delete(temporary);
}

Console.WriteLine($"Patched: {output}");
Console.WriteLine($"Removed {originalProcessCalls.Length} process API reference(s) from the packed assembly.");
return 0;
