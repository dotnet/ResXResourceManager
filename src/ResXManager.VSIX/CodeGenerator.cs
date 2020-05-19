namespace ResXManager.VSIX
{
    using TomsToolbox.Essentials;

    public enum CodeGenerator
    {
        [Text(@"Icon", "")]
        None,

        [LocalizedDescription(StringResourceKey.CodeGenerator_Unkown)]
        [Text(@"Icon", @"pack://application:,,,/ResXManager.VSIX;component/Assets/Unknown.png")]
        Unknown,

        [LocalizedDescription(StringResourceKey.CodeGenerator_Internal)]
        [Text(@"Icon", @"pack://application:,,,/ResXManager.VSIX;component/Assets/PrivateTool.png")]
        ResXFileCodeGenerator,

        [LocalizedDescription(StringResourceKey.CodeGenerator_Public)]
        [Text(@"Icon", @"pack://application:,,,/ResXManager.VSIX;component/Assets/PublicTool.png")]
        PublicResXFileCodeGenerator,

        [LocalizedDescription(StringResourceKey.CodeGenerator_TextTemplate)]
        [Text(@"Icon", @"pack://application:,,,/ResXManager.VSIX;component/Assets/TextTemplate.png")]
        TextTemplate,

        [LocalizedDescription(StringResourceKey.CodeGenerator_WinForms)]
        [Text(@"Icon", @"pack://application:,,,/ResXManager.VSIX;component/Assets/WinForms.png")]
        WinForms,

    }
}