; fractron9000.nsi
;
; Fractron9000 installation script

;--------------------------------

!define DOT_MAJOR "2"
!define DOT_MINOR "0"

!define VERSION_MAJOR "0"
!define VERSION_MINOR "4"
!define VERSION_REVISION "0"
!define VERSION_BUILD "0"

!define VERSION_SHORT "${VERSION_MAJOR}.${VERSION_MINOR}"
!define VERSION_FULL  "${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_REVISION}.${VERSION_BUILD}"

!include WordFunc.nsh
!include LogicLib.nsh

; The name of the installer
Name "Fractron 9000"

; The version
VIProductVersion "${VERSION_FULL}"

; The file to write
OutFile "installer\fractron9000setup-${VERSION_SHORT}.beta.exe"

; The default installation directory
InstallDir "$PROGRAMFILES32\Fractron 9000"

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\Fractron 9000" "Install_Dir"

; Request application privileges for Windows Vista
RequestExecutionLevel admin

;LicenseText "License"
LicenseData "license.txt"

VIAddVersionKey "ProductName" "Fractron 9000"
VIAddVersionKey "LegalCopyright" "© 2009 Michael J. Thiesen"
VIAddVersionKey "FileDescription" "A high performance fractal renderer."
VIAddVersionKey "FileVersion" "${VERSION_SHORT}"

;--------------------------------

;Function .onInit
; MessageBox MB_OK|MB_ICONSTOP ".NET runtime library v1.1 or newer is required. You have $0."
;FunctionEnd

;--------------------------------

; Pages
Page license
Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; The stuff to install
Section "Fractron 9000 Core (required)"

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File "fractron9000.exe"
  File "CudaDotNet.dll"
  File "OpenCLDotNet.dll"
  File "cpu_iterator.dll"
  File "MTUtil.dll"
  File "OpenTK.dll"
  File "OpenTK.Compatibility.dll"
  File "OpenTK.GLControl.dll"
  File "nunit.framework.dll"
  
  File "license.txt"
  File "changes.txt"
  
  ; Write the installation path into the registry
  WriteRegStr HKLM "SOFTWARE\Fractron 9000" "Install_Dir" "$INSTDIR"
  WriteRegStr HKLM "SOFTWARE\Fractron 9000" "Version" "${VERSION_SHORT}"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Fractron 9000" "DisplayName" "Fractron 9000"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Fractron 9000" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Fractron 9000" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Fractron 9000" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  
SectionEnd

; Optional section (can be disabled by the user)
Section "Start Menu Shortcuts"

  CreateDirectory "$SMPROGRAMS\Fractron 9000"
  CreateShortCut "$SMPROGRAMS\Fractron 9000\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
  CreateShortCut "$SMPROGRAMS\Fractron 9000\Fractron 9000.lnk" "$INSTDIR\fractron9000.exe" "" "$INSTDIR\fractron9000.exe" 0
  
SectionEnd

; Optional section (can be disabled by the user)
Section "Desktop Shortcut"

  CreateShortCut "$DESKTOP\Fractron 9000.lnk" "$INSTDIR\fractron9000.exe" "" "$INSTDIR\fractron9000.exe" 0
  
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Fractron 9000"
  DeleteRegKey HKLM "SOFTWARE\Fractron 9000"

  ; Remove files
  SetOutPath $INSTDIR
  
  ; Remove the palettes
  Delete "palettes\dark.png"
  Delete "palettes\default.png"
  Delete "palettes\frost.png"
  Delete "palettes\inferno2.png"
  
  Delete "fractron9000.exe"
  Delete "CudaDotNet.dll"
  Delete "OpenCLDotNet.dll"
  Delete "MTUtil.dll"
  Delete "OpenTK.dll"
  Delete "OpenTK.Compatibility.dll"
  Delete "OpenTK.GLControl.dll"
  Delete "nunit.framework.dll"

  Delete "license.txt"
  Delete "changes.txt"
  Delete "manual.html"
	
	; Files from old versions, delete these just in case they still exist
  Delete "OpenTK.Utilities.dll"
  Delete "cuda9k.dll" 
  Delete "default_fractals\*"
  Delete "default_palettes\*"
  RMDir "$INSTDIR\default_fractals"
  RMDir "$INSTDIR\default_palettes"
  
  ; Remove the uninstaller
  Delete uninstall.exe

  ; Remove shortcuts, if any
  Delete "$SMPROGRAMS\Fractron 9000\*.*"
  Delete "$DESKTOP\Fractron 9000.lnk"

  SetOutPath "$PROGRAMFILES32"
  ; Remove directories used
  RMDir "$SMPROGRAMS\Fractron 9000"
  RMDir "$INSTDIR\palettes"
  RMDir "$INSTDIR"

SectionEnd
