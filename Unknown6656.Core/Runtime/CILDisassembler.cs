using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Unknown6656.Runtime;


public static class CILDisassembler
{
    public static string Disassemble(MethodInfo method) =>
        method.GetMethodBody() is { } body ? Disassemble(body) : throw new ArgumentException("The given method does not define a method body.", nameof(method));

    public static string Disassemble(MethodBody method!!)
    {
        IList<LocalVariableInfo> variables = method.LocalVariables;
        IList<ExceptionHandlingClause> handlers = method.ExceptionHandlingClauses;
        StringBuilder il = new();

        il.AppendLine("{");
        il.AppendLine($"    .maxstack {method.MaxStackSize}");

        if (variables.Count is int l and > 0)
        {
            il.AppendLine($"    .locals {(method.InitLocals ? "init" : "")} (");

            for (int i = 0; i < l; ++i)
                il.AppendLine($"        [{variables[i].LocalIndex}] {variables[i].LocalType} {(variables[i].IsPinned ? "pinned" : "")}{(i < l - 1 ? "," : "")}");

            il.AppendLine($"    )");
        }

        foreach (string line in Disassemble(method?.GetILAsByteArray()))
            il.AppendLine($"    {line}");

        il.AppendLine("}");


        // TODO


        throw new NotImplementedException();
    }

    public static unsafe IEnumerable<string> Disassemble(byte[]? bytes)
    {
        List<string> lines = new();

        if (bytes is { })
            fixed (byte* start = bytes)
            {
                byte* ptr = start;
                byte* end = start + bytes.Length;

                while (ptr < end)
                    try
                    {
                        if (Process(ptr) is (int adv, string instr))
                        {
                            ptr += adv;
                            lines.Add(instr);
                        }
                        else
                            break;
                    }
                    catch
                    {
                        lines.Add("<error while processing bytes>");

                        break;
                    }
            }

        return lines;
    }

    private static string ResolveToken(int token) => $"<0x{token:x8}>"; // TODO

    private static unsafe (int adv, string instruction)? Process(byte* ptr)
    {
        T get<T>(int offset = 1) where T : unmanaged => *(T*)(ptr + 1);

        switch (*ptr)
        {
            case 0x00: return (1, "nop");
            case 0x01: return (1, "break");
            case 0x02: return (1, "ldarg.0");
            case 0x03: return (1, "ldarg.1");
            case 0x04: return (1, "ldarg.2");
            case 0x05: return (1, "ldarg.3");
            case 0x06: return (1, "ldloc.0");
            case 0x07: return (1, "ldloc.1");
            case 0x08: return (1, "ldloc.2");
            case 0x09: return (1, "ldloc.3");
            case 0x0a: return (1, "stloc.0");
            case 0x0b: return (1, "stloc.1");
            case 0x0c: return (1, "stloc.2");
            case 0x0d: return (1, "stloc.3");
            case 0x0e: return (2, $"ldarg.s {ptr[1]}");
            case 0x0f: return (2, $"ldarga.s {ptr[1]}");
            case 0x10: return (2, $"starg.s {ptr[1]}");
            case 0x11: return (2, $"ldloc.s {ptr[1]}");
            case 0x12: return (2, $"ldloca.s {ptr[1]}");
            case 0x13: return (2, $"stloc.s {ptr[1]}");
            case 0x14: return (1, "ldnull");
            case 0x15: return (1, "ldc.i4.m1");
            case 0x16: return (1, "ldc.i4.0");
            case 0x17: return (1, "ldc.i4.1");
            case 0x18: return (1, "ldc.i4.2");
            case 0x19: return (1, "ldc.i4.3");
            case 0x1a: return (1, "ldc.i4.4");
            case 0x1b: return (1, "ldc.i4.5");
            case 0x1c: return (1, "ldc.i4.6");
            case 0x1d: return (1, "ldc.i4.7");
            case 0x1e: return (1, "ldc.i4.8");
            case 0x1f: return (1, "ldc.i4.s");
            case 0x20: return (5, $"ldc.i4 0x{get<int>():x8}");
            case 0x21: return (9, $"ldc.i8 0x{get<long>():x16}");
            case 0x22: return (5, $"ldc.r4 {get<float>()}");
            case 0x23: return (9, $"ldc.r8 {get<double>()}");
            case 0x25: return (1, "dup");
            case 0x26: return (1, "pop");
            case 0x27: return (5, $"jmp {ResolveToken(get<int>())}");
            case 0x28: return (5, $"call {ResolveToken(get<int>())}");
            case 0x29: return (5, $"calli {ResolveToken(get<int>())}");
            case 0x2a: return (1, "ret");
            case 0x2b: return (2, $"br.s L{ptr[1]:x4}");
            case 0x2c: return (2, $"brfalse.s L{ptr[1]:x4}");
            case 0x2d: return (2, $"brtrue.s L{ptr[1]:x4}");
            case 0x2e: return (2, $"beq.s L{ptr[1]:x4}");
            case 0x2f: return (2, $"bge.s L{ptr[1]:x4}");


            case 0x30: return (1, "bgt.s");
            case 0x31: return (1, "ble.s");
            case 0x32: return (1, "blt.s");
            case 0x33: return (1, "bne.un.s");
            case 0x34: return (1, "bge.un.s");
            case 0x35: return (1, "bgt.un.s");
            case 0x36: return (1, "ble.un.s");
            case 0x37: return (1, "blt.un.s");
            case 0x38: return (1, "br");
            case 0x39: return (1, "brfalse");
            case 0x3a: return (1, "brtrue");
            case 0x3b: return (1, "beq");
            case 0x3c: return (1, "bge");
            case 0x3d: return (1, "bgt");
            case 0x3e: return (1, "ble");
            case 0x3f: return (1, "blt");
            case 0x40: return (1, "beq.un");
            case 0x41: return (1, "bge.un");
            case 0x42: return (1, "bgt.un");
            case 0x43: return (1, "ble.un");
            case 0x44: return (1, "blt.un");
            case 0x45: return (1, "switch");
            case 0x46: return (1, "ldind.i1");
            case 0x47: return (1, "ldind.u1");
            case 0x48: return (1, "ldind.i2");
            case 0x49: return (1, "ldind.u2");
            case 0x4a: return (1, "ldind.i4");
            case 0x4b: return (1, "ldind.u4");
            case 0x4c: return (1, "ldind.i8");
            case 0x4d: return (1, "ldind.i");
            case 0x4e: return (1, "ldind.r4");
            case 0x4f: return (1, "ldind.r8");
            case 0x50: return (1, "ldind.ref");
            case 0x51: return (1, "stind.r4");
            case 0x52: return (1, "stind.i1");
            case 0x53: return (1, "stind.i2");
            case 0x54: return (1, "stind.i4");
            case 0x55: return (1, "stind.i8");
            case 0x56: return (1, "stind.r4");
            case 0x57: return (1, "stind.r8");
            case 0x58: return (1, "add");
            case 0x59: return (1, "sub");
            case 0x5a: return (1, "mul");
            case 0x5b: return (1, "div");
            case 0x5c: return (1, "div.un");
            case 0x5d: return (1, "rem");
            case 0x5e: return (1, "rem.un");
            case 0x5f: return (1, "and");
            case 0x60: return (1, "or");
            case 0x61: return (1, "xor");
            case 0x62: return (1, "shl");
            case 0x63: return (1, "shr");
            case 0x64: return (1, "shr.un");
            case 0x65: return (1, "neg");
            case 0x66: return (1, "not");
            case 0x67: return (1, "conv.i1");
            case 0x68: return (1, "conv.i2");
            case 0x69: return (1, "conv.i4");
            case 0x6a: return (1, "conv.i8");
            case 0x6b: return (1, "conv.r4");
            case 0x6c: return (1, "conv.r8");
            case 0x6d: return (1, "conv.u4");
            case 0x6e: return (1, "conv.u8");
            case 0x6f: return (1, "callvirt");
            case 0x70: return (1, "cpobj");
            case 0x71: return (1, "ldobj");
            case 0x72: return (1, "ldstr");
            case 0x73: return (1, "newobj");
            case 0x74: return (1, "castclass");
            case 0x75: return (1, "isinst");
            case 0x76: return (1, "conv.r.un");
            case 0x79: return (1, "unbox");
            case 0x7a: return (1, "throw");
            case 0x7b: return (1, "ldfld");
            case 0x7c: return (1, "ldflda");
            case 0x7d: return (1, "stfld");
            case 0x7e: return (1, "ldsfld");
            case 0x7f: return (1, "ldsflda");
            case 0x80: return (1, "stsfld");
            case 0x81: return (1, "stobj");
            case 0x82: return (1, "conv.ovf.i1.un");
            case 0x83: return (1, "conv.ovf.i2.un");
            case 0x84: return (1, "conv.ovf.i4.un");
            case 0x85: return (1, "conv.ovf.i8.un");
            case 0x86: return (1, "conv.ovf.u1.un");
            case 0x87: return (1, "conv.ovf.u2.un");
            case 0x88: return (1, "conv.ovf.u4.un");
            case 0x89: return (1, "conv.ovf.u8.un");
            case 0x8a: return (1, "conv.ovf.i.un");
            case 0x8b: return (1, "conv.ovf.u.un");
            case 0x8c: return (1, "box");
            case 0x8d: return (1, "newarr");
            case 0x8e: return (1, "ldlen");
            case 0x8f: return (1, "ldelema");
            case 0x90: return (1, "ldelem.i1");
            case 0x91: return (1, "ldelem.u1");
            case 0x92: return (1, "ldelem.i2");
            case 0x93: return (1, "ldelem.u2");
            case 0x94: return (1, "ldelem.i4");
            case 0x95: return (1, "ldelem.u4");
            case 0x96: return (1, "ldelem.i8");
            case 0x97: return (1, "ldelem.i");
            case 0x98: return (1, "ldelem.r4");
            case 0x99: return (1, "ldelem.r8");
            case 0x9a: return (1, "ldelem.ref");
            case 0x9b: return (1, "stelem.i");
            case 0x9c: return (1, "stelem.i1");
            case 0x9d: return (1, "stelem.i2");
            case 0x9e: return (1, "stelem.i4");
            case 0x9f: return (1, "stelem.i8");
            case 0xa0: return (1, "stelem.r4");
            case 0xa1: return (1, "stelem.r8");
            case 0xa2: return (1, "stelem.ref");
            case 0xa3: return (1, "ldelem");
            case 0xa4: return (1, "stelem");
            case 0xa5: return (1, "unbox.any");
            case 0xb3: return (1, "conv.ovf.i1");
            case 0xb4: return (1, "conv.ovf.u1");
            case 0xb5: return (1, "conv.ovf.i2");
            case 0xb6: return (1, "conv.ovf.u2");
            case 0xb7: return (1, "conv.ovf.i4");
            case 0xb8: return (1, "conv.ovf.u4");
            case 0xb9: return (1, "conv.ovf.i8");
            case 0xba: return (1, "conv.ovf.u8");
            case 0xc2: return (1, "refanyval");
            case 0xc3: return (1, "ckfinite");
            case 0xc6: return (1, "mkrefany");
            case 0xd0: return (1, "ldtoken");
            case 0xd1: return (1, "conv.u2");
            case 0xd2: return (1, "conv.u1");
            case 0xd3: return (1, "conv.i");
            case 0xd4: return (1, "conv.ovf.i");
            case 0xd5: return (1, "conv.ovf.u");
            case 0xd6: return (1, "add.ovf");
            case 0xd7: return (1, "add.ovf.un");
            case 0xd8: return (1, "mul.ovf");
            case 0xd9: return (1, "mul.ovf.un");
            case 0xda: return (1, "sub.ovf");
            case 0xdb: return (1, "sub.ovf.un");
            case 0xdc: return (1, "endfinally");
            case 0xdd: return (1, "leave");
            case 0xde: return (1, "leave.s");
            case 0xdf: return (1, "stind.i");
            case 0xe0: return (1, "conv.u");

            case 0xfe when ptr[1] is 0x00: return (1, "arglist");
            case 0xfe when ptr[1] is 0x01: return (1, "ceq");
            case 0xfe when ptr[1] is 0x02: return (1, "cgt");
            case 0xfe when ptr[1] is 0x03: return (1, "cgt.un");
            case 0xfe when ptr[1] is 0x04: return (1, "clt");
            case 0xfe when ptr[1] is 0x05: return (1, "clt.un");
            case 0xfe when ptr[1] is 0x06: return (1, "ldftn");
            case 0xfe when ptr[1] is 0x07: return (1, "ldvirtftn");
            case 0xfe when ptr[1] is 0x09: return (1, "ldarg");
            case 0xfe when ptr[1] is 0x0a: return (1, "ldarga");
            case 0xfe when ptr[1] is 0x0b: return (1, "starg");
            case 0xfe when ptr[1] is 0x0c: return (1, "ldloc");
            case 0xfe when ptr[1] is 0x0d: return (1, "ldloca");
            case 0xfe when ptr[1] is 0x0e: return (1, "stloc");
            case 0xfe when ptr[1] is 0x0f: return (1, "localloc");
            case 0xfe when ptr[1] is 0x11: return (1, "endfilter");
            case 0xfe when ptr[1] is 0x12: return (1, "unaligned.");
            case 0xfe when ptr[1] is 0x13: return (1, "volatile.");
            case 0xfe when ptr[1] is 0x14: return (1, "tail.");
            case 0xfe when ptr[1] is 0x15: return (1, "initobj");
            case 0xfe when ptr[1] is 0x16: return (1, "constrained.");
            case 0xfe when ptr[1] is 0x17: return (1, "cpblk");
            case 0xfe when ptr[1] is 0x18: return (1, "initblk")
            case 0xfe when ptr[1] is 0x19: return (1, "no.");
            case 0xfe when ptr[1] is 0x1a: return (1, "rethrow");
            case 0xfe when ptr[1] is 0x1c: return (1, "sizeof");
            case 0xfe when ptr[1] is 0x1d: return (1, "refanytype");
            case 0xfe when ptr[1] is 0x1e: return (1, "readonly.");


            case 0x24:
            case 0x77:
            case 0x78:
            case >= 0xa6 and <= 0xb2:
            case >= 0xbb and <= 0xc1:
            case 0xc4:
            case 0xc5:
            case >= 0xc7 and <= 0xcf:
            case >= 0xe1 and <= 0xef:
            case >= 0xf0 and <= 0xfd:
            case 0xfе:
            case 0xff:
                return (1, $"<undefined opcode 0x{*ptr:x2}>");
        }
    }
}
