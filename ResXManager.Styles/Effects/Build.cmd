for %%a in (*.fx) do "%ProgramFiles(x86)%\Windows Kits\8.1\bin\x86\fxc.exe" /T ps_2_0 /E main /Fo %%~na.ps %%a
PAUSE