@echo off
sc stop HotkeyService
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /u bin\HotkeyService.exe
