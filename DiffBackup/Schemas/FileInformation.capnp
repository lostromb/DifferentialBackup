@0xa2d5072d4e71917b;

using CSharp = import "csharp.capnp";
$CSharp.namespace("DiffBackup.Schemas"); 
$CSharp.typeVisibility(public); 
$CSharp.nullableEnable(true); 
$CSharp.emitNullableDirective(true); 

struct FileInformation @0x9bcc69c15083a79a {
    path @0 :Text;
    nullableSize :union {
        noValue @1 :Void;
        value @2 :UInt64;
    }
    nullableHash :union {
        noValue @3 :Void;
        value @4 :UInt64;
    }
}