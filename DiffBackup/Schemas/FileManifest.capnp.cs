using Capnp;
using Capnp.Rpc;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace DiffBackup.Schemas
{
    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xf7b9ff739b240a99UL)]
    public class FileManifest : ICapnpSerializable
    {
        public const UInt64 typeId = 0xf7b9ff739b240a99UL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            Files = reader.Files?.ToReadOnlyList(_ => CapnpSerializable.Create<DiffBackup.Schemas.FileInformation>(_)!);
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.Files.Init(Files, (_s1, _v1) => _v1?.serialize(_s1));
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public IReadOnlyList<DiffBackup.Schemas.FileInformation>? Files
        {
            get;
            set;
        }

        public struct READER
        {
            readonly DeserializerState ctx;
            public READER(DeserializerState ctx)
            {
                this.ctx = ctx;
            }

            public static READER create(DeserializerState ctx) => new READER(ctx);
            public static implicit operator DeserializerState(READER reader) => reader.ctx;
            public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            public IReadOnlyList<DiffBackup.Schemas.FileInformation.READER> Files => ctx.ReadList(0).Cast(DiffBackup.Schemas.FileInformation.READER.create);
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(0, 1);
            }

            public ListOfStructsSerializer<DiffBackup.Schemas.FileInformation.WRITER> Files
            {
                get => BuildPointer<ListOfStructsSerializer<DiffBackup.Schemas.FileInformation.WRITER>>(0);
                set => Link(0, value);
            }
        }
    }
}
#nullable restore
