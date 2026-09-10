@echo off
set VSDIR=C:\Program Files\Microsoft Visual Studio\18\Community
set MSVC=%VSDIR%\VC\Tools\MSVC\14.44.35207
set SDK=C:\Program Files (x86)\Windows Kits\10

if not exist "%MSVC%\bin\Hostx64\x64\cl.exe" (
  set MSVC=%VSDIR%\VC\Tools\MSVC\14.51.36231
)

set PATH=%PATH%;%SDK%\bin\10.0.26100.0\x64

"%MSVC%\bin\Hostx64\x64\cl.exe" /nologo /EHsc /utf-8 /W3 native_probe.cpp ^
  /I"%MSVC%\include" ^
  /I"%SDK%\Include\10.0.26100.0\um" /I"%SDK%\Include\10.0.26100.0\shared" /I"%SDK%\Include\10.0.26100.0\ucrt" ^
  /link /SUBSYSTEM:CONSOLE /MANIFEST:EMBED /MANIFESTINPUT:native_probe.manifest ^
  /LIBPATH:"%SDK%\Lib\10.0.26100.0\um\x64" /LIBPATH:"%SDK%\Lib\10.0.26100.0\ucrt\x64" /LIBPATH:"%MSVC%\lib\x64" ^
  user32.lib comctl32.lib kernel32.lib
