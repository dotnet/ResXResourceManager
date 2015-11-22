namespace tomenglertde.ResXManager.VSIX
{
    using System.ComponentModel;

    using TomsToolbox.Desktop;

    public enum CodeGenerator
    {
        [Text("Icon", "")]
        None,

        [Text("Icon", "")]
        Unknown,

        [Description("Custom tool code generator, internal access.")]
        [Text("Icon", @"pack://application:,,,/ResXManager.VSIX;component/Assets/PrivateTool.png")]
        ResXFileCodeGenerator,

        [Description("Custom tool code generator, public access.")]
        [Text("Icon", @"pack://application:,,,/ResXManager.VSIX;component/Assets/PublicTool.png")]
        PublicResXFileCodeGenerator,

        [Description("TextTemplate code generator.")]
        [Text("Icon", @"pack://application:,,,/ResXManager.VSIX;component/Assets/TextTemplate.png")]
        TextTemplate
    }
}