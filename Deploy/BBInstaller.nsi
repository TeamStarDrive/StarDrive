;!include FileFunc.nsh

; This script is intended to be run with WorkingDir=C:\Projects\BlackBox
; Written by RedFox

;--------------------------------
; Project related helper defines
!define PRODUCT_PUBLISHER   "Mod by The BlackBox Team"
!define LAUNCHER            "StarDrive.exe"
!define REGPATH             "Software\StarDrive"
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "upload/${INSTALLER_NAME}_${PRODUCT_VERSION}.exe"

;Include Modern UI
!include "MUI2.nsh"
!include "Sections.nsh"
!include "LogicLib.nsh"
!addplugindir Installer

!define MUI_ABORTWARNING
!define MUI_ICON "blackbox.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP           "top.bmp" ; "Installer\upper_header.bmp" ; optional
!define MUI_WELCOMEFINISHPAGE_BITMAP     "left.bmp" ; "Installer\leftside_image.bmp"
!define MUI_COMPONENTSPAGE_SMALLDESC

;Pages
!define MUI_WELCOMEPAGE_TITLE        "BlackBox Installation Wizard"
!define MUI_WELCOMEPAGE_TEXT         "The wizard will guide you through the installation of $\r$\n${PRODUCT_NAME} ${PRODUCT_VERSION} onto your computer.$\r$\n$\r$\nClick Next to Continue"
!define MUI_DIRECTORYPAGE_TEXT_TOP   "Please verify that the Destination Folder is your Steam StarDrive installation folder: $\r$\nThe same folder where your ${LAUNCHER} is located.$\r$\n$\r$\nOtherwise the installation will not work"
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_FINISHPAGE_RUN              $INSTDIR\${LAUNCHER}
!define MUI_FINISHPAGE_RUN_TEXT         "Run BlackBox ${PRODUCT_VERSION}"
!define MUI_FINISHPAGE_RUN_PARAMETERS   ""
!define MUI_FINISHPAGE_RUN_NOTCHECKED
!define MUI_FINISHPAGE_LINK             "Visit our Discord for Announcements and Help"
!define MUI_FINISHPAGE_LINK_LOCATION    "https://discord.gg/dfvnfH4"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE         "LICENSE" ; Deploy/LICENSE text file
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

;Languages
!insertmacro MUI_LANGUAGE "English"

; Installer file INFO
VIProductVersion "${PRODUCT_VERSION}.0"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "StarDrive BlackBox"
VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "Codegremlins"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "Copyright ZeroSum Games and Codegremlins"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "StarDrive BlackBox Installer"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${PRODUCT_VERSION}"

Var STEAMDIR ; found steam dir
Var PREVDIR ; previous mod install dir
Function .onInit
        ; Get Game path from registry
        ReadRegStr $PREVDIR HKLM ${REGPATH} InstallPath
        IfFileExists "$PREVDIR\${LAUNCHER}" 0 CheckSteam
        StrCpy $INSTDIR $PREVDIR ;; use the previous path
        Goto Done
    CheckSteam:
        ReadRegStr $STEAMDIR HKLM "SOFTWARE\WOW6432Node\Valve\Steam" InstallPath
        StrCmp $STEAMDIR "" SetDefaultPath 0
        StrCpy $INSTDIR "$STEAMDIR\SteamApps\common\StarDrive"
        Goto Done
    SetDefaultPath:
        StrCpy $INSTDIR "C:\Program Files (x86)\steam\steamapps\common\StarDrive"
    Done:
FunctionEnd

SectionGroup /e "BlackBox"

    Section -Prerequisites
        ;Registry entries to figure out patch versions
        WriteRegStr HKLM ${REGPATH} "Author"       "${PRODUCT_PUBLISHER}"
        WriteRegStr HKLM ${REGPATH} "Version"      "${PRODUCT_VERSION}"
        WriteRegStr HKLM ${REGPATH} "InstallPath"  $INSTDIR
        DetailPrint "*** Compiled by RedFox ***"
        DetailPrint "${PRODUCT_NAME} ${PRODUCT_VERSION}"
        DetailPrint "Initializing Installation"
        DetailPrint "*************************"
        ;Check if the installation dir is correct.
        IfFileExists "$INSTDIR\${LAUNCHER}" FolderCorrect FolderIncorrect
    FolderIncorrect:
        MessageBox MB_OKCANCEL|MB_TOPMOST "${LAUNCHER} not found. This install will not work correctly unless installed to the main StarDrive folder$\n$\nClick OK to Continue anyway" IDOK ContinueInstallation IDCANCEL 0
        Abort
    FolderCorrect:
        DetailPrint "Found $INSTDIR\${LAUNCHER}"
    ContinueInstallation:
        DetailPrint "Installation directory: $INSTDIR "
    SectionEnd

    Section "-BlackBox" SecMain
        SectionIn RO
        DetailPrint "Unpacking ${PRODUCT_NAME} files"
        SetOutPath "$INSTDIR"
        !include "GeneratedFilesList.nsh"
    SectionEnd

    Section "-Finish Install" SECFinish
    SectionEnd

SectionGroupEnd

;--------------------------------
;Descriptions
LangString DESC_SecMain ${LANG_ENGLISH} "This installs the main contents of ${PRODUCT_NAME} ${PRODUCT_VERSION} on your computer."
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
!insertmacro MUI_DESCRIPTION_TEXT ${SecMain} $(DESC_SecMain)
!insertmacro MUI_FUNCTION_DESCRIPTION_END
