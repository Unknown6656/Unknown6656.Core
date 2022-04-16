using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unknown6656.Runtime;


public class ASMDisassembler
{
    public static unsafe List<string> Disassemble(byte[]? bytes)
    {
        fixed (byte* ptr = bytes)
            return Disassemble(ptr, bytes.Length);
    }

    public static unsafe List<string> Disassemble(void* ptr, int count)
    {
        List<string> instructions = new();

        throw new NotImplementedException();

        return instructions;
    }
}
