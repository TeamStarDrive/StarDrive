import os, sys

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
