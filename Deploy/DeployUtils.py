import os, sys, uuid
from typing import List

def console(msg): print(msg, flush=True)
def fatal_error(msg): print(msg, file=sys.stderr, flush=True); sys.exit(-1)
def exit_with_message(msg): print(msg, flush=True); sys.exit(0) # not an error

# allowed CI auto-deploy branches:
def should_deploy():
    branch = appveyor_branch()
    return branch == "develop"

def env(env_var_name, default=None, fatal=False):
    var = os.getenv(env_var_name, default=default)
    if var is None and fatal: fatal_error(f'envvar {env_var_name} undefined')
    return var

def is_appveyor_build():
    return env('APPVEYOR_BUILD_FOLDER') is not None

def appveyor_branch():
    return env('APPVEYOR_REPO_BRANCH', fatal=True)

def new_guid():
    return str(uuid.uuid4()).upper()

def path_combine(a, b):
    return os.path.normpath(os.path.join(a, b))

def read_lines(listfile: str) -> List[str]:
    lines: List[str] = []
    if os.path.exists(listfile):
        with open(listfile, 'r') as f:
            for line in f.readlines():
                line = line.strip()
                if len(line) != 0 and not line.startswith('#'):
                    lines.append(line)
    return lines
