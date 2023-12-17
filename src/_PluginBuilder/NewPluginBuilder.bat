::::::::::
:: Init ::
::::::::::

@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

:::::::::::
:: Input ::
:::::::::::

:INPUT
echo Plugin name is the name that shows up in logs and such.
echo It will also be used in the name of several created files and folders.
echo Can be alphanumeric and can use spaces.
set /p "pname=Enter full plugin name: "

cscript //nologo .\Script\match.js "^[a-zA-Z0-9 ]+$" "%pname%"
if errorlevel 1 (
	echo: & echo.Plugin name is not alphanumeric with spaces.
	echo This may lead to the project files or folders bugging out.
	set /p "question=Do you want to restart? (Y/n) "
	set res=false
	if "!question!"=="y" set res=true
	if "!question!"=="Y" set res=true
	if "!res!"=="true" (
		cls
		GOTO INPUT
	)
)

echo: & echo.The main namespace in your plugin.
echo Leave empty to use the plugin name with spaces removed.
set /p "nsname=Enter main namespace name: "

cscript //nologo .\Script\match.js "^[a-zA-Z0-9.]*$" "%nsname%"
if errorlevel 1 (
	echo: & echo.Namespace must be purely alphanumeric.
	echo Restarting...
	pause
	cls
	GOTO INPUT
)

echo: & echo.The GUID is used by BepInEx to disambiguate the plugins between each other.
echo I recommend using something like "author.pluginname"
set /p "guid=Enter plugin GUID: "

cscript //nologo .\Script\match.js "^[a-zA-Z0-9\.]+$" "%guid%"
if errorlevel 1 (
	echo: & echo.GUID must be purely alphanumeric with periods.
	echo Restarting...
	pause
	cls
	GOTO INPUT
)

:::::::::::::::::::::::::::
:: Argument calculations ::
:::::::::::::::::::::::::::

set spname=%pname: =%
if [!nsname!] == [] set nsname=!spname!
for /f "tokens=*" %%a in ('powershell -command "[guid]::NewGuid().toString()"') do set uuid=%%a

:::::::::::::::
:: Execution ::
:::::::::::::::

echo: & echo.Plugin name: !pname!
echo Safe name:   !spname!
echo Namespace:   !nsname!
echo GUID:        !guid!

echo:
set /p "question=Generate new project (Y) or restart (n)? "
set res=true
if "!question!"=="y" set res=false
if "!question!"=="Y" set res=false
if "!res!"=="true" (
	cls
	GOTO INPUT
)

echo:
echo | set /p dummy=Generating folders... 

mkdir ..\!spname!.Core\
if errorlevel 1 GOTO DIRFAIL
mkdir ..\!spname!.Koikatu\
if errorlevel 1 GOTO DIRFAIL
mkdir ..\!spname!.KoikatsuSunshine\
if errorlevel 1 GOTO DIRFAIL

GOTO DIRSUCCESS

:DIRFAIL
echo: & echo.Something went wrong during folder creation. Exiting...
pause
GOTO eof

:DIRSUCCESS
echo Done^^!

echo Copying files... 

xcopy .\Templates\Template.Core\ ..\!spname!.Core\ /q
if errorlevel 1 GOTO COPYFAIL
xcopy .\Templates\Template.Koikatu\ ..\!spname!.Koikatu\ /q
if errorlevel 1 GOTO COPYFAIL
xcopy .\Templates\Template.KoikatsuSunshine\ ..\!spname!.KoikatsuSunshine\ /q
if errorlevel 1 GOTO COPYFAIL

GOTO COPYSUCCESS

:COPYFAIL
echo: & echo.Something went wrong during copying. Exiting...
pause
GOTO eof

:COPYSUCCESS
echo Done^^!

echo Populating files with inputted variables... 

echo $safeprojectname$,!spname!>__temp__Replacements.txt
echo $projectname$,!pname!>>__temp__Replacements.txt
echo $nsname$,!nsname!>>__temp__Replacements.txt
echo $guid$,!guid!>>__temp__Replacements.txt
echo $uuid$,!uuid!>>__temp__Replacements.txt

for %%x in (..\!spname!.Core\*) do (
	echo Populating %%x...
	for /F "tokens=1,2 delims=," %%1 in (__temp__Replacements.txt) do (
		call ".\Script\replace.bat" "%%x" "%%1" "%%2"
		if errorlevel 1 GOTO FILLFAIL
	)
)
for %%x in (..\!spname!.Koikatu\*) do (
	echo Populating %%x...
	for /F "tokens=1,2 delims=," %%1 in (__temp__Replacements.txt) do (
		call ".\Script\replace.bat" "%%x" "%%1" "%%2"
		if errorlevel 1 GOTO FILLFAIL
	)
)
for %%x in (..\!spname!.KoikatsuSunshine\*) do (
	echo Populating %%x...
	for /F "tokens=1,2 delims=," %%1 in (__temp__Replacements.txt) do (
		call ".\Script\replace.bat" "%%x" "%%1" "%%2"
		if errorlevel 1 GOTO FILLFAIL
	)
)

del __temp__Replacements.txt

GOTO FILLSUCCESS

:FILLFAIL
echo: & echo.Something went wrong during populating the files. Exiting...
pause
GOTO eof

:FILLSUCCESS
echo Done^^!

echo Renaming files to final form...

for %%x in (..\!spname!.Core\*) do (
	echo Renaming %%x...
	set newname=%%~nxx
	set newname=!newname:Template=%spname%!
	MOVE "%%~fx" "%%~px!newname!"
	if errorlevel 1 GOTO NAMEFAIL
)
for %%x in (..\!spname!.Koikatu\*) do (
	echo Renaming %%x...
	set newname=%%~nxx
	set newname=!newname:Template=%spname%!
	MOVE "%%~fx" "%%~px!newname!"
	if errorlevel 1 GOTO NAMEFAIL
)
for %%x in (..\!spname!.KoikatsuSunshine\*) do (
	echo Renaming %%x...
	set newname=%%~nxx
	set newname=!newname:Template=%spname%!
	MOVE "%%~fx" "%%~px!newname!"
	if errorlevel 1 GOTO NAMEFAIL
)

GOTO NAMESUCCESS

:NAMEFAIL
echo: & echo.Something went wrong during renaming the files. Exiting...
pause
GOTO eof

:NAMESUCCESS
echo Done^^!
echo: &echo.Plugin creation complete.
echo Now you can import the created projects into your IDE.

pause