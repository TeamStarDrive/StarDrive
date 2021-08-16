import os, sys
from DeployUtils import appveyor_branch, fatal_error, exit_with_message, should_deploy, env, is_appveyor_build

BUILD_VERSION = env('APPVEYOR_BUILD_VERSION', default='1.30.13000')

# chdir to given parameter if it exists
if len(sys.argv) > 1 and sys.argv[1]:
    print(f'WorkingDir: {sys.argv[1]}')
    os.chdir(sys.argv[1])

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

print('MakeNSIS Deploy/BlackBox-Mars.nsi')
result = os.system(f'"{makensis}" /V3 /DVERSION={BUILD_VERSION} /DSOURCE_DIR={source} Deploy/BlackBox-Mars.nsi')
if result != 0:
    fatal_error(f'MakeNSIS returned with error: {result}')
else:
    exit_with_message('MakeNSIS succeeded')
