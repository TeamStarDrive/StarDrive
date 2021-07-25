#!/usr/bin/python3
import os, sys, glob, subprocess
from DeployUtils import fatal_error, exit_with_message, is_appveyor_build, should_deploy, env, appveyor_branch

# need to make sure this script is executed in `BlackBox/` root dir
if not os.path.exists('Deploy/MakeInstaller.py'):
    fatal_error(f'MakeInstaller.py not found in {os.getcwd()}/Deploy/')

BB_UPLOAD_USER = env('BB_UPLOAD_USER', fatal=True)
BB_UPLOAD_PASS = env('BB_UPLOAD_PASS', fatal=True)
print(f"APPVEYOR_REPO_BRANCH={os.getenv('APPVEYOR_REPO_BRANCH')}")
print(f"APPVEYOR_REPO_COMMIT={os.getenv('APPVEYOR_REPO_COMMIT')}")
print(f"APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH={os.getenv('APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH')}")

if is_appveyor_build() and not should_deploy():
    exit_with_message(f'Auto-Upload is not enabled for this branch: {appveyor_branch()}')

upload_dir = os.path.abspath('Deploy/upload')
if not os.path.exists(upload_dir):
    fatal_error(f'Upload folder does not exist at: {upload_dir}')
    
upload_candidates = glob.glob(f'{upload_dir}/*.exe', recursive=False)
upload_candidates = sorted(upload_candidates, key=os.path.getmtime)
upload_candidates.reverse()

if not upload_candidates:
    exit_with_message(f'No files to upload in: Deploy/upload')

file = os.path.abspath(upload_candidates[0])
print(f'BitBucket Upload: {file}')
api_url = 'https://api.bitbucket.org/2.0/repositories/codegremlins/stardrive-blackbox/downloads'
result = os.system(f'curl -X POST -u "{BB_UPLOAD_USER}:{BB_UPLOAD_PASS}" -F files=@"{file}" "{api_url}"')
# dont care bout result
