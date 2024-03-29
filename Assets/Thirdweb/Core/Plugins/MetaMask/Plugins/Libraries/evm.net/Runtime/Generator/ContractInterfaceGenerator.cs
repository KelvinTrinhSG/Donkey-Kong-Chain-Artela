using System;
using System.Linq;
using evm.net.Models.ABI;

namespace evm.net.Generator
{
    public class ContractInterfaceGenerator : CodeGenerator
    {
        private string contractName;
        private string bytecode;
        private ContractABI abi;

        public ContractInterfaceGenerator(string rootNamespace, string contractName, string bytecode, ContractABI abi)
        {
            _context = new GeneratorContext(rootNamespace, contractName);
            this.contractName = contractName;
            this.bytecode = bytecode;
            this.abi = abi;
        }

        public void WriteAutoGeneratedComment()
        {
            WriteLine("//------------------------------------------------------------------------------");
            WriteLine("// This code was generated by a tool.");
            WriteLine("//");
            WriteLine("//   Tool : MetaMask Unity SDK ABI Code Generator");
            WriteLine($"//   Input filename:  {contractName}.sol");
            WriteLine($"//   Output filename: {Filename}.cs");
            WriteLine("//");
            WriteLine("// Changes to this file may cause incorrect behavior and will be lost when");
            WriteLine("// the code is regenerated.");
            WriteLine("// <auto-generated />");
            WriteLine("//------------------------------------------------------------------------------");
            WriteLine();
        }

        public void WriteUsings()
        {
            WriteLine("using System;");
            WriteLine("using System.Numerics;");
            WriteLine("using System.Threading.Tasks;");
            WriteLine("using evm.net;");
            WriteLine("using evm.net.Models;");
            WriteLine();
        }

        public void WriteNamespace()
        {
            WriteLine($"namespace {_context.RootNamespace}");
            StartCodeBlock();
        }

        public void WriteBackedTypeInfo()
        {
            var typedName = contractName + "Backing";
            WriteLine("#if UNITY_EDITOR || !ENABLE_MONO");
            WriteLine($"[BackedType(typeof({typedName}))]");
            WriteLine("#endif");
            
            // Build backing contract type class
            _context.AddGenerator(new ContractTypeGenerator(contractName, abi, _context));
        }

        public void WriteInterfaceName()
        {
            WriteBackedTypeInfo();
            WriteLine($"public interface {contractName} : IContract");
            StartCodeBlock();
        }

        public void WriteBytecode()
        {
            if (string.IsNullOrWhiteSpace(bytecode))
                return;
            
            WriteLine($"public static readonly string Bytecode = \"{bytecode}\";");
            WriteLine();
        }
        
        public void WriteFunction(ABIDef def)
        {
            switch (def.DefType)
            {
                case ABIDefType.Constructor:
                    WriteConstructor(def);
                    return;
                case ABIDefType.Function:
                    WriteABIFunction(def.AsFunction());
                    return;
            }
        }

        public void WriteConstructor(ABIDef def)
        {
            WriteLine("[EvmConstructorMethod]");
            WriteLine($"Task<{contractName}> DeployNew({BuildParameters(def.Inputs)});");
            WriteLine();
        }

        public void WriteABIFunction(ABIFunction def)
        {
            bool isView = def.StateMutability == ABIStateMutability.Pure ||
                          def.StateMutability == ABIStateMutability.View;
            string abiReturnType = null;
            string returnAttribute = null;
            if (isView)
            {
                if (def.Outputs.Length == 1)
                {
                    var output = def.Outputs[0];
                    if (ParameterConverter.StrictEvmToType.ContainsKey(output.TypeName))
                    {
                        var abiType = ParameterConverter.StrictEvmToType[output.TypeName];
                        abiReturnType = abiType.Name;
                    }
                    else if (ParameterConverter.DynamicEvmToType.ContainsKey(output.TypeName))
                    {
                        var abiType = ParameterConverter.DynamicEvmToType[output.TypeName];
                        abiReturnType = abiType.Name;
                        returnAttribute = $"[return: EvmParameterInfo(Type = \"{output.TypeName}\")]";
                    }
                    else if (output.TypeName == "tuple")
                    {
                        var tupleType = BuildNamedTuple(output);
                        abiReturnType = tupleType;
                    }
                    else
                    {
                        throw new Exception("Unsupported evm type " + output.TypeName);
                    }
                }
                else if (def.Outputs.Length > 1)
                {
                    abiReturnType = "Tuple<";
                    for (int i = 0; i < def.Outputs.Length; i++)
                    {
                        var output = def.Outputs[i];
                        if (ParameterConverter.StrictEvmToType.ContainsKey(output.TypeName))
                        {
                            var abiType = ParameterConverter.StrictEvmToType[output.TypeName];
                            abiReturnType += abiType.Name;
                        }
                        else if (output.TypeName == "tuple")
                        {
                            var tupleType = BuildNamedTuple(output);
                            abiReturnType += tupleType;
                        }
                        else
                        {
                            throw new Exception("Unsupported evm type " + output.TypeName);
                        }

                        if (i + 1 < def.Outputs.Length)
                        {
                            abiReturnType += ", ";
                        }
                    }

                    abiReturnType += ">";
                }
            }
            else
            {
                abiReturnType = "Transaction";
            }

            WriteLine($"[EvmMethodInfo(Name = \"{def.Name}\", View = {isView.ToString().ToLower()})]");
            if (!string.IsNullOrWhiteSpace(returnAttribute))
                WriteLine(returnAttribute);
            WriteLine($"Task<{abiReturnType}> {ToFunctionName(def.Name)}({BuildParameters(def.Inputs)});");
            WriteLine();
        }

        public string BuildParameters(ABIParameter[] parameters)
        {
            if (parameters.Length == 0)
                return string.Empty;
            return string.Join(", ", parameters.Select(BuildParameter).Append(CallOptionsParameter));
        }

        public string BuildNamedTuple(ABIParameter parameter)
        {
            var typeName = parameter.InternalType;
            if (string.IsNullOrWhiteSpace(typeName))
                typeName = parameter.Name;

            if (typeName.StartsWith("struct"))
                typeName = typeName.Replace("struct", "").Trim();
                
            _context.GenerateTuple(typeName, parameter);
            return typeName;
        }

        public const string CallOptionsParameter = "CallOptions options = default";

        public string BuildParameter(ABIParameter parameter)
        {
            var literalName = parameter.Name;
            var parmName = literalName;
            if (!IsValidIdentifier(parmName))
                parmName = $"@{parmName}";

            string typeName;
            if (ParameterConverter.StrictEvmToType.ContainsKey(parameter.TypeName))
            {
                var t = ParameterConverter.StrictEvmToType[parameter.TypeName];
                typeName = t.Name;
            }
            else if (ParameterConverter.DynamicEvmToType.ContainsKey(parameter.TypeName))
            {                
                var abiType = ParameterConverter.DynamicEvmToType[parameter.TypeName];
                typeName = abiType.Name;
                return $"[EvmParameterInfo(Type = \"{parameter.TypeName}\", Name = \"{literalName}\")] {typeName} {parmName}";
            }
            else if (parameter.TypeName == "tuple")
            {
                typeName = BuildNamedTuple(parameter);
            }
            else
            {
                throw new Exception("Unsupported evm type " + parameter.TypeName);
            }
            
            return literalName != parmName ? $"[EvmParameterInfo(Type = \"{parameter.TypeName}\", Name = \"{literalName}\")] {typeName} {parmName}" : $"{typeName} {parmName}";
        }

        protected override void DoWrite()
        {
            WriteAutoGeneratedComment();
            WriteUsings();
            WriteNamespace();
            WriteInterfaceName();
            WriteBytecode();
            foreach (var def in abi)
            {
                WriteFunction(def);
            }
            CompleteCodeBlocks();
        }

        public override string Filename
        {
            get
            {
                return contractName;
            }
        }
    }
}