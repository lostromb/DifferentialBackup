@0xc4ae57d644fa2e39;

using CSharp = import "csharp.capnp";
using import "FileInformation.capnp".FileInformation;
$CSharp.namespace("DiffBackup.Schemas.Serialization"); 
$CSharp.typeVisibility(public); 
$CSharp.nullableEnable(true); 
$CSharp.emitNullableDirective(true); 

struct FileManifest @0xf7b9ff739b240a99 {
    files @0 :List(FileInformation);
}