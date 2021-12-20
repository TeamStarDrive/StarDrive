import os, argparse
from DeployUtils import appveyor_branch, fatal_error, exit_with_message, should_deploy, env, console, is_appveyor_build
from GenerateInstallerFileList import create_installer_files_list

parser = argparse.ArgumentParser()
parser.add_argument('--root_dir', type=str, help='BlackBox/ repository root directory', required=True)
parser.add_argument('--major', action='store_true', help='Is this a major release?')
parser.add_argument('--patch', action='store_true', help='Is this a cumulative patch?')
args = parser.parse_args()

BUILD_VERSION = env('APPVEYOR_BUILD_VERSION', default='1.40.14000')

os.chdir(args.root_dir)

installer = 'Deploy/BlackBox-Mars.nsi'
if args.major:
    create_installer_files_list(major=True)
elif args.patch:
    create_installer_files_list(patch=True)
    installer = 'Deploy/BlackBox-Mars-Patch.nsi'

# Require MakeInstaller.py to have working directory in `BlackBox/` root
makensis = os.path.abspath('Deploy/NSIS/makensis.exe')
if not os.path.exists(makensis):
    fatal_error('MakeInstaller.py must be executed with WorkingDir=BlackBox/')

# if this is a remote build on AppVeyor CI, then only build `develop` or `release` branch
if is_appveyor_build() and not should_deploy():
    exit_with_message(f'Not creating installer for this branch: {appveyor_branch()}')

# create the upload dir if it doesn't already exist
source = os.getcwd()
if not os.path.exists('Deploy/upload'):
    os.makedirs('Deploy/upload', exist_ok=True)

console(f'MakeNSIS {installer}')
result = os.system(f'"{makensis}" /V3 /DVERSION={BUILD_VERSION} /DSOURCE_DIR={source} {installer}')
if result != 0:
    fatal_error(f'MakeNSIS returned with error: {result}')
else:
    exit_with_message('MakeNSIS succeeded')
