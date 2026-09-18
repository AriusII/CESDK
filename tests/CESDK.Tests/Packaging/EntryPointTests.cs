using CESDK.Tests.Infrastructure;

namespace CESDK.Tests.Packaging;

/// <summary>
///     The reason the package exists at all: a plugin project that only adds <c>PackageReference Include="CESDK"</c>
///     gets the host-mandated <c>CESDK.CESDK.CEPluginInitialize</c> entry point for free, and a
///     project that opts out with <c>CesdkGenerateEntryPoint=false</c> does not get it - proof that the
///     <c>CompilerVisibleProperty</c> declared in the packaged <c>build/CESDK.props</c> really reaches the generator,
///     not just its own internal default.
/// </summary>
[Collection(PackagedUmbrellaSuite.Name)]
public sealed class EntryPointTests(PackagedUmbrellaFixture fixture)
{
    [Fact]
    public void Default_consumer_gets_the_generated_entry_point_type()
    {
        Assert.True(fixture.DefaultEntryPointTypeExists,
            "CESDK.CESDK was not found in the default consumer's built assembly.");
    }

    [Fact]
    public void Default_consumer_entry_point_declares_CEPluginInitialize()
    {
        Assert.True(fixture.DefaultEntryPointMethodExists,
            "CESDK.CESDK.CEPluginInitialize(object, object) was not found in the default consumer's built assembly.");
    }

    [Fact]
    public void CesdkGenerateEntryPoint_false_switches_the_generator_off()
    {
        Assert.False(fixture.EntryPointOffTypeExists,
            "CESDK.CESDK was generated although the consumer set CesdkGenerateEntryPoint=false.");
    }
}
