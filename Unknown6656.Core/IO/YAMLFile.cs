using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unknown6656.IO
{
    public abstract class YAMLObject
        : DynamicObject
        , ICloneable
    {
    }

    public sealed class YAMLValue
        : YAMLObject
    {
    }

    public class YAMLSection
        : YAMLObject
        , IDictionary<string, YAMLObject>
    {
    }

    public sealed class YAMLFile
        : YAMLSection
    {
    }
}
