Unicode True
SetCompress auto
SetCompressor /FINAL /SOLID bzip2
;SetCompressor /FINAL /SOLID lzma
;SetCompressorDictSize 25 ; LZMA dict size, default is 8MB
CRCCheck force

; This script is intended to be run with WorkingDir=C:\Projects\BlackBox
; Written by RedFox
!ifndef VERSION
  !error "Missing required Script argument VERSION. Pass it via /DVERSION=x.x.x to makensis.exe"
!endif
!ifndef SOURCE_DIR
  !error "Missing required Script argument SOURCE_DIR. Pass it via /DSOURCE_DIR=C:\Projects\BlackBox to makensis.exe"
!endif

!define PRODUCT_NAME     "StarDrive BlackBox Mars"
!define INSTALLER_NAME   "BlackBox_Mars"
!define PRODUCT_VERSION  ${VERSION}

;; Payload:
!include "BBInstaller.nsi"
