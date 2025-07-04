using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Graphics
{
    public class TransformedShader : Shader
    {
        public readonly ShaderTransforms Transforms;
        public TransformedShader(string vsc, string psc, int maxInstancesCount, params ShaderMacro[] shaderMacros)
            : base(vsc, psc, shaderMacros)
        {
            Transforms = new ShaderTransforms(maxInstancesCount);
        }
    }
}
