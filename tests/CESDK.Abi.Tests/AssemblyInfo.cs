using System.Runtime.CompilerServices;

// Same switch as CESDK.Abi and as every real consumer (CESDK.Hosting): the function-pointer calls made by these
// tests then go through exactly the code path a plugin uses, with no marshalling stub that could hide a
// non-blittable signature.
[assembly: DisableRuntimeMarshalling]
