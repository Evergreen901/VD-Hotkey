@echo off
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe bin\HotkeyService.exe
sc start HotkeyService
