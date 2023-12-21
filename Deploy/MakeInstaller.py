import os, argparse
from DeployUtils import appveyor_branch, fatal_error, exit_with_message, should_deploy, env, console, is_appveyor_build
from GenerateInstallerFileList import create_installer_files_list

###
# Example usages:
# Generate a zip patch:
#   py -3 Deploy/MakeInstaller.py --root_dir=. --patch --type=zip
# Generate a NSIS installer:
#   py -3 Deploy/MakeInstaller.py --root_dir=. --patch --type=nsis

parser = argparse.ArgumentParser()
parser.add_argument('--root_dir', type=str, help='BlackBox/ repository root directory', required=True)
parser.add_argument('--major', action='store_true', help='Is this a major release?')
parser.add_argument('--patch', action='store_true', help='Is this a cumulative patch?')
parser.add_argument('--type', type=str, help='Type of installer: nsis, zip, msi', default='nsis')
args = parser.parse_args()

BUILD_VERSION = env('APPVEYOR_BUILD_VERSION', default='1.41.14824')

os.chdir(args.root_dir)

# if this is a remote build on AppVeyor CI, then only build `develop` or `release` branch
if is_appveyor_build() and not should_deploy():
    exit_with_message(f'Not creating installer for this branch: {appveyor_branch()}')

if   args.major: create_installer_files_list(major=True, type=args.type)
elif args.patch: create_installer_files_list(patch=True, type=args.type)

# create the upload dir if it doesn't already exist
source = os.getcwd()
if not os.path.exists('Deploy/upload'):
    os.makedirs('Deploy/upload', exist_ok=True)

if args.type == 'nsis':
    # Require MakeInstaller.py to have working directory in `BlackBox/` root
    makensis = os.path.abspath('Deploy/NSIS/makensis.exe')
    if not os.path.exists(makensis):
        fatal_error('makensis.exe was not found: MakeInstaller.py must be executed with WorkingDir=BlackBox/')

    installer = 'Deploy/BlackBox-Mars.nsi'
    if args.patch: installer = 'Deploy/BlackBox-Mars-Patch.nsi'

    console(f'\nMakeNSIS {installer}')
    result = os.system(f'"{makensis}" /V3 /DVERSION={BUILD_VERSION} /DSOURCE_DIR={source} {installer}')
    if result != 0: fatal_error(f'MakeNSIS returned with error: {result}')
    else: exit_with_message('MakeNSIS succeeded')
elif args.type == 'zip':
    zip7 = os.path.abspath('Deploy/7-Zip/7za.exe')
    if not os.path.exists(zip7):
        fatal_error('7za.exe was not found: MakeInstaller.py must be executed with WorkingDir=BlackBox/')

    installer = 'Deploy\\GeneratedFilesList.txt'
    archive = f'Deploy\\upload\\BlackBox_Mars_{BUILD_VERSION}.zip'
    console(f'\nMakeZIP {installer}')
    result = os.system(f'cd game && "{zip7}" a -tzip ..\\{archive} @..\\{installer}')
    if result != 0: fatal_error(f'7zip returned with error: {result}')
    else: exit_with_message('7zip succeeded')
