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
    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x9bcc69c15083a79aUL)]
    public class FileInformation : ICapnpSerializable
    {
        public const UInt64 typeId = 0x9bcc69c15083a79aUL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            Path = reader.Path;
            NullableModTime = CapnpSerializable.Create<DiffBackup.Schemas.FileInformation.nullableModTime>(reader.NullableModTime);
            NullableSize = CapnpSerializable.Create<DiffBackup.Schemas.FileInformation.nullableSize>(reader.NullableSize);
            NullableHash = CapnpSerializable.Create<DiffBackup.Schemas.FileInformation.nullableHash>(reader.NullableHash);
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.Path = Path;
            NullableModTime?.serialize(writer.NullableModTime);
            NullableSize?.serialize(writer.NullableSize);
            NullableHash?.serialize(writer.NullableHash);
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public string? Path
        {
            get;
            set;
        }

        public DiffBackup.Schemas.FileInformation.nullableModTime? NullableModTime
        {
            get;
            set;
        }

        public DiffBackup.Schemas.FileInformation.nullableSize? NullableSize
        {
            get;
            set;
        }

        public DiffBackup.Schemas.FileInformation.nullableHash? NullableHash
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
            public string? Path => ctx.ReadText(0, null);
            public nullableModTime.READER NullableModTime => new nullableModTime.READER(ctx);
            public nullableSize.READER NullableSize => new nullableSize.READER(ctx);
            public nullableHash.READER NullableHash => new nullableHash.READER(ctx);
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(4, 1);
            }

            public string? Path
            {
                get => this.ReadText(0, null);
                set => this.WriteText(0, value, null);
            }

            public nullableModTime.WRITER NullableModTime
            {
                get => Rewrap<nullableModTime.WRITER>();
            }

            public nullableSize.WRITER NullableSize
            {
                get => Rewrap<nullableSize.WRITER>();
            }

            public nullableHash.WRITER NullableHash
            {
                get => Rewrap<nullableHash.WRITER>();
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xe0bfd191237f9227UL)]
        public class nullableModTime : ICapnpSerializable
        {
            public const UInt64 typeId = 0xe0bfd191237f9227UL;
            public enum WHICH : ushort
            {
                NoValue = 0,
                Value = 1,
                undefined = 65535
            }

            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                switch (reader.which)
                {
                    case WHICH.NoValue:
                        which = reader.which;
                        break;
                    case WHICH.Value:
                        Value = reader.Value;
                        break;
                }

                applyDefaults();
            }

            private WHICH _which = WHICH.undefined;
            private object? _content;
            public WHICH which
            {
                get => _which;
                set
                {
                    if (value == _which)
                        return;
                    _which = value;
                    switch (value)
                    {
                        case WHICH.NoValue:
                            break;
                        case WHICH.Value:
                            _content = 0;
                            break;
                    }
                }
            }

            public void serialize(WRITER writer)
            {
                writer.which = which;
                switch (which)
                {
                    case WHICH.NoValue:
                        break;
                    case WHICH.Value:
                        writer.Value = Value!.Value;
                        break;
                }
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public ulong? Value
            {
                get => _which == WHICH.Value ? (ulong? )_content : null;
                set
                {
                    _which = WHICH.Value;
                    _content = value;
                }
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
                public WHICH which => (WHICH)ctx.ReadDataUShort(0U, (ushort)0);
                public ulong Value => which == WHICH.Value ? ctx.ReadDataULong(64UL, 0UL) : default;
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                }

                public WHICH which
                {
                    get => (WHICH)this.ReadDataUShort(0U, (ushort)0);
                    set => this.WriteData(0U, (ushort)value, (ushort)0);
                }

                public ulong Value
                {
                    get => which == WHICH.Value ? this.ReadDataULong(64UL, 0UL) : default;
                    set => this.WriteData(64UL, value, 0UL);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xf21baf18f865b0eeUL)]
        public class nullableSize : ICapnpSerializable
        {
            public const UInt64 typeId = 0xf21baf18f865b0eeUL;
            public enum WHICH : ushort
            {
                NoValue = 0,
                Value = 1,
                undefined = 65535
            }

            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                switch (reader.which)
                {
                    case WHICH.NoValue:
                        which = reader.which;
                        break;
                    case WHICH.Value:
                        Value = reader.Value;
                        break;
                }

                applyDefaults();
            }

            private WHICH _which = WHICH.undefined;
            private object? _content;
            public WHICH which
            {
                get => _which;
                set
                {
                    if (value == _which)
                        return;
                    _which = value;
                    switch (value)
                    {
                        case WHICH.NoValue:
                            break;
                        case WHICH.Value:
                            _content = 0;
                            break;
                    }
                }
            }

            public void serialize(WRITER writer)
            {
                writer.which = which;
                switch (which)
                {
                    case WHICH.NoValue:
                        break;
                    case WHICH.Value:
                        writer.Value = Value!.Value;
                        break;
                }
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public ulong? Value
            {
                get => _which == WHICH.Value ? (ulong? )_content : null;
                set
                {
                    _which = WHICH.Value;
                    _content = value;
                }
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
                public WHICH which => (WHICH)ctx.ReadDataUShort(16U, (ushort)0);
                public ulong Value => which == WHICH.Value ? ctx.ReadDataULong(128UL, 0UL) : default;
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                }

                public WHICH which
                {
                    get => (WHICH)this.ReadDataUShort(16U, (ushort)0);
                    set => this.WriteData(16U, (ushort)value, (ushort)0);
                }

                public ulong Value
                {
                    get => which == WHICH.Value ? this.ReadDataULong(128UL, 0UL) : default;
                    set => this.WriteData(128UL, value, 0UL);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xb97598c1b6bfb221UL)]
        public class nullableHash : ICapnpSerializable
        {
            public const UInt64 typeId = 0xb97598c1b6bfb221UL;
            public enum WHICH : ushort
            {
                NoValue = 0,
                Value = 1,
                undefined = 65535
            }

            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                switch (reader.which)
                {
                    case WHICH.NoValue:
                        which = reader.which;
                        break;
                    case WHICH.Value:
                        Value = reader.Value;
                        break;
                }

                applyDefaults();
            }

            private WHICH _which = WHICH.undefined;
            private object? _content;
            public WHICH which
            {
                get => _which;
                set
                {
                    if (value == _which)
                        return;
                    _which = value;
                    switch (value)
                    {
                        case WHICH.NoValue:
                            break;
                        case WHICH.Value:
                            _content = 0;
                            break;
                    }
                }
            }

            public void serialize(WRITER writer)
            {
                writer.which = which;
                switch (which)
                {
                    case WHICH.NoValue:
                        break;
                    case WHICH.Value:
                        writer.Value = Value!.Value;
                        break;
                }
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public ulong? Value
            {
                get => _which == WHICH.Value ? (ulong? )_content : null;
                set
                {
                    _which = WHICH.Value;
                    _content = value;
                }
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
                public WHICH which => (WHICH)ctx.ReadDataUShort(32U, (ushort)0);
                public ulong Value => which == WHICH.Value ? ctx.ReadDataULong(192UL, 0UL) : default;
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                }

                public WHICH which
                {
                    get => (WHICH)this.ReadDataUShort(32U, (ushort)0);
                    set => this.WriteData(32U, (ushort)value, (ushort)0);
                }

                public ulong Value
                {
                    get => which == WHICH.Value ? this.ReadDataULong(192UL, 0UL) : default;
                    set => this.WriteData(192UL, value, 0UL);
                }
            }
        }
    }
}
#nullable restore
