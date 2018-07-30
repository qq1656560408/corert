﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodFixupSignature : Signature
    {
        public enum SignatureKind
        {
            DefToken,
            RefToken,
            Signature,
        }

        private readonly ReadyToRunCodegenNodeFactory _factory;

        private readonly ReadyToRunFixupKind _fixupKind;

        private readonly MethodDesc _methodDesc;

        private readonly ModuleToken _methodToken;

        private readonly SignatureKind _signatureKind;

        public MethodFixupSignature(
            ReadyToRunCodegenNodeFactory factory, 
            ReadyToRunFixupKind fixupKind, 
            MethodDesc methodDesc, 
            ModuleToken methodToken, 
            SignatureKind signatureKind)
        {
            _factory = factory;
            _fixupKind = fixupKind;
            _methodDesc = methodDesc;
            _methodToken = methodToken;
            _signatureKind = signatureKind;
        }

        protected override int ClassCode => 150063499;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();
            dataBuilder.AddSymbol(this);

            dataBuilder.EmitByte((byte)_fixupKind);
            switch (_signatureKind)
            {
                case SignatureKind.DefToken:
                    dataBuilder.EmitMethodDefToken(_methodToken);
                    break;

                case SignatureKind.RefToken:
                    dataBuilder.EmitMethodRefToken(_methodToken);
                    break;

                case SignatureKind.Signature:
                    dataBuilder.EmitMethodSignature(_methodDesc, _methodToken, _methodToken.SignatureContext(_factory));
                    break;

                default:
                    throw new NotImplementedException();
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"MethodFixupSignature({_fixupKind.ToString()} {_methodToken}): {_methodDesc.ToString()}");
        }

        protected override int CompareToImpl(SortableDependencyNode other, CompilerComparer comparer)
        {
            return _methodToken.CompareTo(((MethodFixupSignature)other)._methodToken);
        }
    }
}