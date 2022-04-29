using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unknown6656.Runtime;


public unsafe class X86Disassembler
    : Disassembler
{
    public override unsafe List<string> DisassembleIntoLines(byte* ptr, int length, __empty config)
    {
        List<string> instructions = new();

        throw new NotImplementedException(); // TODO

        return instructions;
    }
}
